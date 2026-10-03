using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Turns every model of the Fantasy Props MegaKit into a ready prefab: pack textures on URP materials,
    /// colliders on furniture, fire and light on everything that burns.
    internal static class DungeonKitBuilder
    {
        public const string PrefabsFolder = DungeonPropBuilder.PrefabsFolder + "/Kit";

        private const string PackFolder = "Assets/SpecialFolder/3D Models/Fantasy Props MegaKit[Standard]";
        private const string ModelsFolder = PackFolder + "/Exports/FBX";
        private const string MaterialsFolder = DungeonPropBuilder.MaterialsFolder + "/Kit";
        private const float CandleSpacing = 0.12f;

        private sealed class Fire
        {
            public float Flame;
            public float Range;
            public float Intensity;
            // Burning points for models without candles; candles are found on the mesh.
            public Vector3[] Points;
        }

        private static readonly HashSet<string> s_solid = new()
        {
            "Anvil", "Anvil_Log", "Bag", "Barrel", "Barrel_Apples", "Barrel_Holder", "Bed_Twin1", "Bed_Twin2", "Bench", "Bookcase_2", "BookStand",
            "Cabinet", "Cage_Small", "CandleStick_Stand", "Cauldron", "Chair_1", "Chest_Wood", "Crate_Metal", "Crate_Wooden", "Dummy",
            "Nightstand_Shelf", "Stall_Cart_Empty", "Stall_Empty", "Stool", "Table_Large", "Vase_2", "WeaponStand", "Whetstone", "Workbench"
        };

        private static readonly Dictionary<string, Fire> s_fires = new()
        {
            ["Candle_1"] = new Fire { Flame = 0.3f, Range = 4f, Intensity = 1.3f },
            ["Candle_2"] = new Fire { Flame = 0.3f, Range = 4f, Intensity = 1.3f },
            ["CandleStick_Triple"] = new Fire { Flame = 0.3f, Range = 6f, Intensity = 2f },
            ["CandleStick_Stand"] = new Fire { Flame = 0.3f, Range = 12f, Intensity = 4.5f },
            ["Chandelier"] = new Fire { Flame = 0.35f, Range = 22f, Intensity = 9f },
            ["Torch_Metal"] = new Fire { Flame = 0.8f, Range = 18f, Intensity = 10f, Points = new[] { new Vector3(0f, 0.36f, 0.27f) } },
            ["Lantern_Wall"] = new Fire { Flame = 0.4f, Range = 15f, Intensity = 7f, Points = new[] { new Vector3(0f, 0.38f, 1.05f) } }
        };

        private static readonly Dictionary<string, Material> s_materials = new();

        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(PrefabsFolder);
            BattleEditorUtility.EnsureFolder(MaterialsFolder);
            s_materials.Clear();

            foreach (string file in Directory.GetFiles(ModelsFolder, "*.fbx"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                GameObject root = new GameObject(name);
                GameObject model = Model(name, root.transform);

                if (s_solid.Contains(name))
                    AddCollider(root, model);

                if (s_fires.TryGetValue(name, out Fire fire))
                    AddFire(root, model, fire);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(name));
                Object.DestroyImmediate(root);
            }

            Debug.Log($"[{nameof(DungeonKitBuilder)}] Kit prefabs built in {PrefabsFolder}");
        }

        public static string PrefabPath(string name) => $"{PrefabsFolder}/{name}.prefab";

        public static GameObject Load(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));

            if (prefab == null)
                throw new System.ArgumentException($"Kit prefab '{name}' not found");

            return prefab;
        }

        /// Bare pack model with the project materials, for prefabs that add their own logic (containers).
        public static GameObject Model(string name, Transform parent)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/{name}.fbx");
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            model.name = "Model";

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material => Material(material.name)).ToArray();
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }

            return model;
        }

        public static Bounds Bounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers)
                bounds.Encapsulate(renderer.bounds);

            return bounds;
        }

        private static void AddCollider(GameObject root, GameObject model)
        {
            Bounds bounds = Bounds(model);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
        }

        private static void AddFire(GameObject root, GameObject model, Fire fire)
        {
            List<Vector3> points = fire.Points != null ? fire.Points.ToList() : CandleTops(model);
            // A ring of candles burns every second one: the picture is the same, the particle count is halved.
            int step = points.Count > 4 ? 2 : 1;
            Vector3 center = Vector3.zero;

            for (int i = 0; i < points.Count; i++)
            {
                center += points[i] / points.Count;

                if (i % step == 0)
                    DungeonPropBuilder.Flame(root.transform, points[i] + Vector3.up * 0.02f, fire.Flame);
            }

            DungeonPropBuilder.PointLight(root.transform, center + Vector3.up * 0.25f, new Color(1f, 0.68f, 0.35f), fire.Range, fire.Intensity, true);
        }

        /// Wick points: the highest vertex of every separate candle of the wax submeshes.
        private static List<Vector3> CandleTops(GameObject model)
        {
            MeshFilter filter = model.GetComponentInChildren<MeshFilter>();
            Mesh mesh = filter.sharedMesh;
            Material[] materials = filter.GetComponent<Renderer>().sharedMaterials;
            Vector3[] vertices = mesh.vertices;
            List<Vector3> wax = new();

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if (!materials[i].name.StartsWith("KitProps"))
                    continue;

                foreach (int index in new HashSet<int>(mesh.GetIndices(i)))
                    wax.Add(filter.transform.TransformPoint(vertices[index]));
            }

            List<Vector3> tops = new();

            foreach (Vector3 point in wax.OrderByDescending(point => point.y))
            {
                if (!tops.Any(top => new Vector2(point.x - top.x, point.z - top.z).magnitude < CandleSpacing))
                    tops.Add(point);
            }

            return tops;
        }

        private static Material Material(string source)
        {
            if (s_materials.TryGetValue(source, out Material material) && material != null)
                return material;

            material = source switch
            {
                "MI_Trim_Metal" => Trim("KitMetal", "Metal", false),
                "MI_Trim_Metal_Vertex" => Trim("KitMetalTint", "Metal", true),
                "MI_Trim_Furniture" => Trim("KitFurniture", "Furniture", false),
                "MI_Trim_Props" => Trim("KitProps", "Props", false),
                "MI_Trim_Props_Vertex" => Trim("KitPropsTint", "Props", true),
                "MI_Trim_Cloth" => Trim("KitCloth", "Cloth", false),
                "MI_Banner" => Trim("KitBanner", "Cloth", true),
                "MI_Page_Empty" => Page(),
                _ => throw new System.ArgumentException($"Unknown kit material '{source}'")
            };
            s_materials[source] = material;

            return material;
        }

        /// The pack tints shared trim sheets with vertex colours; of the stock URP shaders only the particle one reads them.
        private static Material Trim(string name, string sheet, bool isTinted)
        {
            string prefix = $"{PackFolder}/Textures/T_Trim_{sheet}_";
            Material material = LoadMaterial(name, isTinted ? "Universal Render Pipeline/Particles/Lit" : "Universal Render Pipeline/Lit");
            Texture2D mask = BattleCharacterBuilder.CreateMask(prefix + "ORM.png", true);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "BaseColor.png"));
            material.SetTexture("_BumpMap", Normal(prefix + "Normal.png"));
            material.SetTexture("_MetallicGlossMap", mask);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");

            if (!isTinted)
            {
                material.SetTexture("_OcclusionMap", mask);
                material.EnableKeyword("_OCCLUSIONMAP");
            }

            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material Page()
        {
            Material material = LoadMaterial("KitPage", "Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{PackFolder}/Textures/T_Page_Noise.png"));
            material.SetColor("_BaseColor", new Color(0.85f, 0.78f, 0.6f));
            material.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material LoadMaterial(string name, string shader)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
                return material;

            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);

            return material;
        }

        private static Texture2D Normal(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            if (importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
