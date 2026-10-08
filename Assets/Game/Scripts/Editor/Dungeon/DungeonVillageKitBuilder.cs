using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Turns every model of the Medieval Village MegaKit into a prefab for the cursed village. The pack is bright and cheerful;
    /// its texture sets are re-baked darker, greyer and grimier (red roof tiles become weathered slate-brown) so the houses
    /// sit in Dark and Darker's palette. Walls, corners, fences and crates get colliders, door walls leave their opening free.
    internal static class DungeonVillageKitBuilder
    {
        public const string PrefabsFolder = DungeonPropBuilder.PrefabsFolder + "/Village";
        public const string PackFolder = "Assets/SpecialFolder/Models/Environment/MedievalVillageMegaKit";
        /// Every wall module is 2 m wide; a storey is 3 m and the walls overlap the next one by 0.12 m of trim.
        public const float Module = 2f;
        public const float Storey = 3f;
        /// Half width and height of the opening in the door walls.
        public const float DoorHalf = 0.65f;
        public const float DoorHeight = 2.25f;
        /// Walls run along X with the outside towards +Z; their body reaches this far inside.
        public const float WallDepth = 0.31f;

        private const string ModelsFolder = PackFolder + "/Models";
        private const string TexturesFolder = PackFolder + "/Textures";
        private const string MaterialsFolder = DungeonPropBuilder.MaterialsFolder + "/Village";
        private const string MapsFolder = "Assets/Game/Textures/Dungeon/Village";
        private const int MapSize = 1024;

        /// How a pack texture set is weathered: saturation kept, brightness, tint multiplied in, grime amount.
        private sealed class Look
        {
            public string Albedo;
            public string Normal;
            public string Rough;
            public bool IsOrm;
            public float Saturation;
            public float Value;
            public Color Tint;
            public float Grime;
            public float Gloss = 0.8f;
        }

        private static readonly Dictionary<string, Look> s_looks = new()
        {
            ["MI_Plaster"] = new Look { Albedo = "Plaster", Normal = "Plaster", Rough = "Plaster", IsOrm = true, Saturation = 0.35f, Value = 0.62f, Tint = new Color(0.96f, 0.93f, 0.86f), Grime = 0.45f },
            ["MI_WoodTrim"] = new Look { Albedo = "WoodTrim", Normal = "WoodTrim", Rough = "WoodTrim", IsOrm = true, Saturation = 0.55f, Value = 0.55f, Tint = new Color(0.9f, 0.82f, 0.74f), Grime = 0.3f },
            ["MI_WoodTrim_Wear"] = new Look { Albedo = "WoodTrim", Normal = "WoodTrim", Rough = "WoodTrim", IsOrm = true, Saturation = 0.45f, Value = 0.45f, Tint = new Color(0.86f, 0.8f, 0.74f), Grime = 0.45f },
            ["MI_Brick"] = new Look { Albedo = "Brick", Normal = "Brick", Rough = "Brick", Saturation = 0.3f, Value = 0.6f, Tint = new Color(0.92f, 0.9f, 0.86f), Grime = 0.4f },
            ["MI_RedBrick"] = new Look { Albedo = "RedBrick", Normal = "Brick", Rough = "Brick", Saturation = 0.45f, Value = 0.55f, Tint = new Color(0.9f, 0.84f, 0.8f), Grime = 0.4f },
            ["MI_UnevenBrick"] = new Look { Albedo = "UnevenBrick", Normal = "UnevenBrick", Rough = "UnevenBrick", Saturation = 0.25f, Value = 0.6f, Tint = new Color(0.9f, 0.9f, 0.86f), Grime = 0.5f },
            ["MI_RockTrim"] = new Look { Albedo = "RockTrim", Normal = "RockTrim", Rough = "RockTrim", IsOrm = true, Saturation = 0.25f, Value = 0.6f, Tint = new Color(0.9f, 0.9f, 0.86f), Grime = 0.45f },
            ["MI_RoundTiles"] = new Look { Albedo = "RoundTiles", Normal = "RoundTiles", Rough = "RoundTiles", Saturation = 0.12f, Value = 0.5f, Tint = new Color(0.82f, 0.76f, 0.7f), Grime = 0.6f, Gloss = 0.6f }
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
                Model(name, root.transform);
                AddColliders(name, root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(name));
                Object.DestroyImmediate(root);
            }

            Debug.Log($"[{nameof(DungeonVillageKitBuilder)}] Village prefabs built in {PrefabsFolder}");
        }

        public static string PrefabPath(string name) => $"{PrefabsFolder}/{name}.prefab";

        public static GameObject Load(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));

            if (prefab == null)
                throw new System.ArgumentException($"Village prefab '{name}' not found");

            return prefab;
        }

        public static GameObject Model(string name, Transform parent)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/{name}.fbx");
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            model.name = "Model";
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material => Material(material.name)).ToArray();
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }

            return model;
        }

        /// Warm glass of a window someone still keeps a candle behind.
        public static Material LitWindow => Glass("WindowLit", new Color(0.25f, 0.16f, 0.08f), new Color(1f, 0.62f, 0.3f) * 1.6f);

        /// Tileable surfaces of the pack for procedural buildings (the trim sheets are not tileable).
        public static Material Surface(string name) => Material(name);

        private static void AddColliders(string name, GameObject root)
        {
            if (name.StartsWith("Wall_") && name.Contains("_Door_"))
            {
                const float side = (Module * 0.5f - DoorHalf) * 0.5f;
                Box(root, new Vector3(-Module * 0.5f + side, Storey * 0.5f, -0.11f), new Vector3(side * 2f, Storey, 0.4f));
                Box(root, new Vector3(Module * 0.5f - side, Storey * 0.5f, -0.11f), new Vector3(side * 2f, Storey, 0.4f));
                Box(root, new Vector3(0f, (Storey + DoorHeight) * 0.5f, -0.11f), new Vector3(DoorHalf * 2f, Storey - DoorHeight, 0.4f));
            }
            else if (name.StartsWith("Wall_") && name != "Wall_Arch" && name != "Wall_BottomCover")
            {
                Box(root, new Vector3(0f, Storey * 0.5f, -0.11f), new Vector3(Module, Storey, 0.4f));
            }
            else if (name.StartsWith("Corner_Exterior") || name.StartsWith("Prop_WoodenFence") || name.StartsWith("Prop_MetalFence") || name is "Prop_Crate" or "Prop_Wagon" or "Prop_Chimney" or "Prop_Chimney2")
            {
                Bounds bounds = DungeonKitBuilder.Bounds(root);
                Box(root, bounds.center, Vector3.Max(bounds.size, new Vector3(0.12f, 0f, 0.12f)));
            }
        }

        private static void Box(GameObject root, Vector3 center, Vector3 size)
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private static Material Material(string source)
        {
            if (s_materials.TryGetValue(source, out Material material) && material != null)
                return material;

            material = source switch
            {
                "MI_WindowGlass" => Glass("WindowDark", new Color(0.05f, 0.06f, 0.06f), Color.black),
                "MI_MetalOrnaments" => Plain("Iron", new Color(0.16f, 0.15f, 0.14f), 0.8f, 0.45f),
                "MI_Vine" => Vine(),
                _ when s_looks.ContainsKey(source) => Weathered(source.Substring(3), s_looks[source]),
                _ => throw new System.ArgumentException($"Unknown village material '{source}'")
            };
            s_materials[source] = material;

            return material;
        }

        private static Material Weathered(string name, Look look)
        {
            Material material = LoadMaterial(name);
            material.SetTexture("_BaseMap", Albedo(name, look));
            material.SetTexture("_BumpMap", DungeonKitBuilder.Normal($"{TexturesFolder}/T_{look.Normal}_Normal.png"));
            material.SetTexture("_MetallicGlossMap", Mask(look));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material Glass(string name, Color color, Color emission)
        {
            Material material = LoadMaterial(name);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.9f);
            material.SetFloat("_Metallic", 0f);
            material.SetColor("_EmissionColor", emission);

            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material Plain(string name, Color color, float metallic, float smoothness)
        {
            Material material = LoadMaterial(name);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material Vine()
        {
            Material material = LoadMaterial("Vine");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_VineLeaf.png"));
            material.SetColor("_BaseColor", new Color(0.2f, 0.24f, 0.12f));
            material.SetFloat("_Smoothness", 0.2f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
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

        /// The pack colour, desaturated, darkened and tinted, with soot settling into its cavities (ambient occlusion of ORM sets)
        /// and blotches of large-scale grime.
        private static Texture2D Albedo(string name, Look look)
        {
            Color[] color = ReadScaled($"{TexturesFolder}/T_{look.Albedo}_BaseColor.png");
            Color[] orm = look.IsOrm ? ReadScaled($"{TexturesFolder}/T_{look.Rough}_ORM.png") : null;

            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    int i = y * MapSize + x;
                    Color c = color[i];
                    float grey = c.grayscale;
                    c = Color.Lerp(new Color(grey, grey, grey), c, look.Saturation) * look.Value * look.Tint;
                    float blotch = DungeonTextureBuilder.Noise(x / (float)MapSize, y / (float)MapSize, 3f, 4);
                    c *= Mathf.Lerp(1f, 0.55f + blotch * 0.5f, look.Grime);

                    if (orm != null)
                        c *= Mathf.Lerp(0.45f, 1f, orm[i].r);

                    c.a = 1f;
                    color[i] = c;
                }
            }

            return Write($"{MapsFolder}/{name}_Albedo.png", color, true);
        }

        /// URP reads metallic from red and smoothness from alpha.
        private static Texture2D Mask(Look look)
        {
            string path = $"{MapsFolder}/{look.Rough}_Mask.png";
            Color[] source = ReadScaled($"{TexturesFolder}/T_{look.Rough}_{(look.IsOrm ? "ORM" : "Roughness")}.png");

            for (int i = 0; i < source.Length; i++)
            {
                float rough = look.IsOrm ? source[i].g : source[i].r;
                float metal = look.IsOrm ? source[i].b : 0f;
                source[i] = new Color(metal, 1f, 0f, (1f - rough) * look.Gloss);
            }

            return Write(path, source, false);
        }

        private static Color[] ReadScaled(string path)
        {
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            source.LoadImage(File.ReadAllBytes(path));
            Color[] result = new Color[MapSize * MapSize];

            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                    result[y * MapSize + x] = source.GetPixelBilinear((x + 0.5f) / MapSize, (y + 0.5f) / MapSize);
            }

            Object.DestroyImmediate(source);

            return result;
        }

        private static Texture2D Write(string path, Color[] pixels, bool isColor)
        {
            Texture2D texture = new Texture2D(MapSize, MapSize, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = isColor;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
