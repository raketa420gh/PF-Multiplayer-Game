using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Turns every model of the Medieval Assets Pack into a prefab: one URP material per texture set (metallic and roughness packed
    /// into a mask, alpha into the albedo), box colliders on what stands on the floor, light on what burns.
    internal static class DungeonMedievalBuilder
    {
        public const string PrefabsFolder = DungeonPropBuilder.PrefabsFolder + "/Medieval";

        private const string PackFolder = "Assets/SpecialFolder/3D Models/Medieval Assets Pack";
        private const string ModelsFolder = PackFolder + "/Models";
        private const string TexturesFolder = PackFolder + "/Textures";
        private const string MaterialsFolder = DungeonPropBuilder.MaterialsFolder + "/Medieval";
        private const string MapsFolder = "Assets/Game/Textures/Dungeon/Medieval";
        private const float CellDoorWidth = 2.1f;

        private sealed class Fire
        {
            // Candles carry flames of their own; only torches get the particle fire.
            public float Flame;
            public float Range;
            public float Intensity;
            public Vector3[] Points;
        }

        private static readonly HashSet<string> s_solid = new()
        {
            "Barrel", "Bed", "Big Chest", "Big Crate", "Cage", "Chair", "Crate", "Drawer Bookcase", "Floor Trap Tile", "Gold Pile", "Guillotine",
            "Large Bookcase", "Ornate Candle Stick", "Rack Stretcher", "Seat", "Small Chest", "Stocks", "Stool", "Table", "Treasure Chest", "Weapon Rack",
            "X Stocks"
        };

        /// Models whose pivot sits at an end or a corner: they are moved to stand on their middle.
        private static readonly HashSet<string> s_centered = new() { "Cage", "Gold Pile", "Seat", "Stocks", "Weapon Rack", "X Stocks" };

        private static readonly Dictionary<string, Fire> s_fires = new()
        {
            ["Small Candle"] = new Fire { Range = 3.5f, Intensity = 1f, Points = new[] { new Vector3(0f, 0.48f, 0f) } },
            ["Medium Candle"] = new Fire { Range = 4f, Intensity = 1.2f, Points = new[] { new Vector3(0f, 0.4f, 0f) } },
            ["Big Candle"] = new Fire { Range = 5f, Intensity = 1.5f, Points = new[] { new Vector3(0f, 0.56f, 0f) } },
            ["Candle Stick"] = new Fire { Range = 5f, Intensity = 1.5f, Points = new[] { new Vector3(0f, 0.64f, 0f) } },
            ["Ornate Candle Stick"] = new Fire { Range = 8f, Intensity = 2.5f, Points = new[] { new Vector3(0f, 1.09f, 0f) } },
            ["Chandelier"] = new Fire
            {
                Range = 15f, Intensity = 4.5f,
                Points = new[]
                {
                    new Vector3(0.12f, -1.37f, -0.58f), new Vector3(0.49f, -1.39f, -0.32f), new Vector3(0.33f, -1.41f, 0.5f),
                    new Vector3(-0.11f, -1.43f, 0.59f), new Vector3(-0.49f, -1.43f, 0.33f), new Vector3(-0.32f, -1.44f, -0.49f)
                }
            },
            ["Ornated Torch"] = new Fire { Flame = 0.8f, Range = 16f, Intensity = 8f, Points = new[] { new Vector3(0f, 0.78f, 0f) } },
            ["Torch"] = new Fire { Flame = 0.8f, Range = 16f, Intensity = 8f, Points = new[] { new Vector3(0f, 0.8f, 0f) } }
        };

        /// Pack material names that do not match their texture folder.
        private static readonly Dictionary<string, string> s_folders = new()
        {
            ["00_Candle"] = "Candle Stick",
            ["Goblet Gem"] = "Goblet with Gem Gems",
            ["Helmet.001"] = "Helmet"
        };

        private static readonly Dictionary<string, Material> s_materials = new();

        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(PrefabsFolder);
            BattleEditorUtility.EnsureFolder(MaterialsFolder);
            BattleEditorUtility.EnsureFolder(MapsFolder);
            s_materials.Clear();

            foreach (string file in Directory.GetFiles(ModelsFolder, "*.fbx"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                GameObject root = new GameObject(name);
                GameObject model = Model(name, root.transform);

                if (s_centered.Contains(name))
                {
                    Vector3 center = DungeonKitBuilder.Bounds(model).center;
                    model.transform.localPosition = new Vector3(-center.x, 0f, -center.z);
                }

                if (s_solid.Contains(name))
                    AddCollider(root, model);

                if (s_fires.TryGetValue(name, out Fire fire))
                    AddFire(root, fire);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(name));
                Object.DestroyImmediate(root);
            }

            Debug.Log($"[{nameof(DungeonMedievalBuilder)}] Medieval prefabs built in {PrefabsFolder}");
        }

        public static string PrefabPath(string name) => $"{PrefabsFolder}/{name}.prefab";

        public static GameObject Load(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));

            if (prefab == null)
                throw new System.ArgumentException($"Medieval prefab '{name}' not found");

            return prefab;
        }

        /// Bare pack model with the project materials, for prefabs that add their own logic (containers, doors).
        public static GameObject Model(string name, Transform parent)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/{name}.fbx");
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            model.name = "Model";
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                // The barrel carries a second, untextured copy of itself.
                if (renderer.sharedMaterials.Any(material => material.name.StartsWith("00_") && material.name != "00_Candle"))
                {
                    Object.DestroyImmediate(renderer.gameObject);
                    continue;
                }

                renderer.sharedMaterials = renderer.sharedMaterials.Select(material => Material(material.name)).ToArray();
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }

            return model;
        }

        /// Pack model with a box collider, ready to receive container logic.
        public static GameObject Solid(string name, string model, float scale)
        {
            GameObject root = new GameObject(name);
            Model(model, root.transform).transform.localScale *= scale;
            AddCollider(root, root);

            return root;
        }

        /// Pack chest cut in two at the seam: the lid swings open around the back edge of the box. No seam = a chest without a lid.
        public static GameObject Chest(string name, string model, float seam, out Transform lid)
        {
            GameObject root = Solid(name, model, 1f);
            lid = null;

            if (seam <= 0f)
                return root;

            MeshFilter filter = root.GetComponentInChildren<MeshFilter>();
            Material[] materials = filter.GetComponent<Renderer>().sharedMaterials;
            Matrix4x4 matrix = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            BoxCollider body = root.GetComponent<BoxCollider>();
            Vector3 hinge = new Vector3(0f, seam, body.center.z - body.size.z * 0.5f);
            Mesh source = filter.sharedMesh;
            Object.DestroyImmediate(root.transform.Find("Model").gameObject);
            lid = BattleEditorUtility.CreateChild("Lid", root.transform, hinge).transform;
            Part(Cut(source, matrix, seam, false, Vector3.zero, $"{Sanitize(model)}Box"), materials, root.transform);
            Part(Cut(source, matrix, seam, true, hinge, $"{Sanitize(model)}Lid"), materials, lid);

            return root;
        }

        /// Barred cell door leaf made of the pack's blood-stained grate: hinge post at the origin, bars along +X.
        public static GameObject CellDoorLeaf()
        {
            GameObject leaf = new GameObject("Leaf");
            Transform model = Model("Trap", leaf.transform).transform;
            Bounds bounds = DungeonKitBuilder.Bounds(model.gameObject);
            float scale = CellDoorWidth / bounds.size.x;
            model.localScale *= scale;
            model.localPosition = new Vector3(-bounds.min.x, -bounds.min.y, -bounds.center.z) * scale;
            AddCollider(leaf, model.gameObject);

            return leaf;
        }

        private static void AddCollider(GameObject root, GameObject model)
        {
            Bounds bounds = DungeonKitBuilder.Bounds(model);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(bounds.center);
            collider.size = bounds.size;
        }

        private static void AddFire(GameObject root, Fire fire)
        {
            Vector3 center = Vector3.zero;

            foreach (Vector3 point in fire.Points)
            {
                center += point / fire.Points.Length;

                if (fire.Flame > 0f)
                    DungeonPropBuilder.Flame(root.transform, point, fire.Flame);
            }

            DungeonPropBuilder.PointLight(root.transform, center + Vector3.up * 0.25f, new Color(1f, 0.76f, 0.5f), fire.Range, fire.Intensity, true);
        }

        private static void Part(Mesh mesh, Material[] materials, Transform parent)
        {
            GameObject part = DungeonPropBuilder.MeshObject(mesh.name, parent, mesh, materials[0], default, default, false, false);
            part.GetComponent<MeshRenderer>().sharedMaterials = materials;
        }

        /// The triangles above (lid) or below the seam, baked into the prefab's space around the pivot.
        private static Mesh Cut(Mesh source, Matrix4x4 matrix, float seam, bool isLid, Vector3 pivot, string name)
        {
            Vector3[] vertices = source.vertices.Select(vertex => matrix.MultiplyPoint3x4(vertex) - pivot).ToArray();
            string path = $"{DungeonMeshBuilder.Folder}/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh == null)
            {
                BattleEditorUtility.EnsureFolder(DungeonMeshBuilder.Folder);
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.name = name;
            mesh.vertices = vertices;
            mesh.normals = source.normals.Select(normal => matrix.MultiplyVector(normal).normalized).ToArray();
            mesh.tangents = source.tangents.Select(tangent =>
            {
                Vector3 direction = matrix.MultiplyVector(tangent).normalized;

                return new Vector4(direction.x, direction.y, direction.z, tangent.w);
            }).ToArray();
            mesh.uv = source.uv;
            mesh.subMeshCount = source.subMeshCount;

            for (int i = 0; i < source.subMeshCount; i++)
            {
                int[] triangles = source.GetTriangles(i);
                List<int> kept = new();

                for (int t = 0; t < triangles.Length; t += 3)
                {
                    float y = (vertices[triangles[t]].y + vertices[triangles[t + 1]].y + vertices[triangles[t + 2]].y) / 3f + pivot.y;

                    if (y > seam == isLid)
                        kept.AddRange(new[] { triangles[t], triangles[t + 1], triangles[t + 2] });
                }

                mesh.SetTriangles(kept, i);
            }

            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            return mesh;
        }

        private static string Sanitize(string name)
        {
            return name.Replace(" ", string.Empty);
        }

        private static Material Material(string source)
        {
            if (s_materials.TryGetValue(source, out Material material) && material != null)
                return material;

            material = source == "Water" ? Water() : Textured(s_folders.TryGetValue(source, out string folder) ? folder : source.TrimEnd('_'));
            s_materials[source] = material;

            return material;
        }

        private static Material Textured(string set)
        {
            string folder = $"{TexturesFolder}/{set}";

            if (!AssetDatabase.IsValidFolder(folder))
                throw new System.ArgumentException($"No textures for medieval material '{set}'");

            string Map(string map) => $"{folder}/T_{map} - 512px.png";
            bool hasAlpha = File.Exists(Map("Alpha"));
            Material material = LoadMaterial(set);
            material.SetTexture("_BaseMap", hasAlpha ? Albedo(set, Map("Base Color"), Map("Alpha")) : AssetDatabase.LoadAssetAtPath<Texture2D>(Map("Base Color")));
            material.SetTexture("_BumpMap", DungeonKitBuilder.Normal(Map("Normal")));
            material.SetTexture("_MetallicGlossMap", Mask(set, Map("Metallic"), Map("Roughness")));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_AlphaClip", hasAlpha ? 1f : 0f);
            material.SetFloat("_Cutoff", 0.5f);
            material.SetFloat("_Cull", hasAlpha ? 0f : 2f);
            material.renderQueue = hasAlpha ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;

            if (hasAlpha)
                material.EnableKeyword("_ALPHATEST_ON");
            else
                material.DisableKeyword("_ALPHATEST_ON");

            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(Map("Emission Color"));

            if (emission != null)
            {
                material.SetTexture("_EmissionMap", emission);
                material.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.45f) * 3f);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material Water()
        {
            Material material = LoadMaterial("Water");
            material.SetColor("_BaseColor", new Color(0.08f, 0.09f, 0.08f));
            material.SetFloat("_Smoothness", 0.92f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material LoadMaterial(string name)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);

            return material;
        }

        /// URP reads metallic from red and smoothness from alpha; the pack ships metallic and roughness apart.
        private static Texture2D Mask(string set, string metallicPath, string roughnessPath)
        {
            return Combine($"{MapsFolder}/{set}_Mask.png", metallicPath, roughnessPath, false, (metal, rough) => new Color(metal.r, 1f, 0f, 1f - rough.r));
        }

        private static Texture2D Albedo(string set, string colorPath, string alphaPath)
        {
            return Combine($"{MapsFolder}/{set}_Albedo.png", colorPath, alphaPath, true, (color, alpha) => new Color(color.r, color.g, color.b, alpha.r));
        }

        private static Texture2D Combine(string path, string firstPath, string secondPath, bool isColor, System.Func<Color, Color, Color> combine)
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (existing != null)
                return existing;

            Color[] first = Readable(firstPath).GetPixels();
            Texture2D second = Readable(secondPath);
            Color[] other = second.GetPixels();
            Texture2D result = new Texture2D(second.width, second.height, TextureFormat.RGBA32, false, !isColor);

            for (int i = 0; i < first.Length; i++)
                first[i] = combine(first[i], other[i]);

            result.SetPixels(first);
            File.WriteAllBytes(path, result.EncodeToPNG());
            Object.DestroyImmediate(result);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = isColor;
            importer.alphaIsTransparency = isColor;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D Readable(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
