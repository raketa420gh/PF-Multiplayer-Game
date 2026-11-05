using System.Linq;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Scripts.Editor.Dungeon
{
    /// Tripo studio models (Docs/06_Art/Tripo_Batch_*.md) as prefabs: the importer scales a model to its real size, the prefab turns
    /// it upright, stands its pivot where it touches the world (floor: bottom middle; wall: back face, front along +Z; ceiling:
    /// top) and gives the solid ones a box. Models live in Assets/Game/Models/Environment/<Category>/<Name>, taken in by hand.
    internal static class TripoPropBuilder
    {
        public const string PrefabsFolder = DungeonPropBuilder.PrefabsFolder + "/Tripo";

        private const string ModelsFolder = "Assets/Game/Models/Environment";
        /// Smaller pieces cast no shadows.
        private const float ShadowSize = 0.6f;

        public enum Mount
        {
            Floor,
            Wall,
            Ceiling
        }

        private sealed class Spec
        {
            public string Name;
            public string Category;
            /// Largest dimension in metres.
            public float Size;
            public Vector3 Rotation;
            public Mount Mount;
            public bool IsSolid;
            /// A model borrowed from elsewhere (a monster's): its importer is shared, so the prefab scales it, not the importer.
            public string Source;
            public System.Func<Material> Material;
        }

        private static readonly Spec[] s_specs =
        {
            Floor("CatacombDoor", "Catacomb", 2.35f, true),
            Floor("StonePillar", "Catacomb", 6f, true),
            Floor("StoneWell", "Catacomb", 2.6f, true),
            Floor("WellDaisStep", "Catacomb", 2f, false),
            Floor("CandleClusterA", "Lights", 0.3f, false),
            Floor("CandleClusterB", "Lights", 0.35f, false),
            Wall("WallTorch", "Lights", 0.7f),
            Floor("Campfire", "Lights", 1.2f, false),
            Floor("IronCandlestick", "Lights", 1.4f, true),
            Hanging("HangingLantern", "Lights", 0.8f),
            Floor("SmallOakChest", "Containers", 0.9f, true),
            Floor("SupplyCrate", "Containers", 1f, true),
            Floor("SupplyCrateSmall", "Containers", 0.6f, false),
            Floor("WoodBarrel", "Containers", 1f, true),
            Floor("BrokenBarrel", "Containers", 1f, true),
            Floor("GrainSack", "Containers", 0.7f, true),
            Floor("SackPile", "Containers", 1f, true),
            Floor("ColumnDrum", "Debris", 0.8f, true),
            Floor("FallenBlock", "Debris", 0.8f, true),
            Floor("RockA", "Debris", 0.3f, false),
            Floor("RockB", "Debris", 0.4f, false, new Vector3(90f, 0f, 0f)),
            Floor("RubblePileSmall", "Debris", 0.9f, false),
            Floor("SkullPile", "Debris", 1f, true),
            new Spec { Name = "SkullSingle", Size = 0.24f, Source = DungeonSkeletonBuilder.SkullPath, Material = () => DungeonSkeletonBuilder.SkullMaterial },
            Floor("Ribcage", "Debris", 0.6f, false, new Vector3(-90f, 0f, 0f)),
            Floor("BoneScatter", "Debris", 0.8f, false, new Vector3(90f, 0f, 0f)),
            Floor("SkeletonLying", "Debris", 1.8f, false, new Vector3(-90f, 0f, 0f)),
            Floor("PotteryShards", "Debris", 0.5f, false),
            Floor("PlankDebris", "Debris", 1.2f, true),
            Wall("WallShackles", "Debris", 0.8f),
            Hanging("HangingChain", "Debris", 2f),
            Floor("OssariteBone", "Debris", 0.5f, false, new Vector3(0f, 0f, 90f)),
            Floor("OssariteCluster", "Debris", 0.5f, false)
        };

        [MenuItem("Tools/Game/Art/Build Tripo Props")]
        public static void BuildAll()
        {
            BattleEditorUtility.EnsureFolder(PrefabsFolder);

            foreach (Spec spec in s_specs.Where(spec => AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(spec)) != null))
                Build(spec);

            AssetDatabase.SaveAssets();
            Debug.Log("Tripo props built");
        }

        public static bool Has(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name)) != null;
        }

        public static GameObject Load(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));

            if (prefab == null)
                throw new System.ArgumentException($"Tripo prop '{name}' not found");

            return prefab;
        }

        /// Bare unpacked copy without colliders, named "Model", for prefabs that add their own logic (containers, fires).
        public static GameObject Model(string name, Transform parent)
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(Load(name), parent);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model";

            foreach (Collider collider in model.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(collider);

            return model;
        }

        public static Mount MountOf(string name)
        {
            return s_specs.First(spec => spec.Name == name).Mount;
        }

        private static string PrefabPath(string name) => $"{PrefabsFolder}/{name}.prefab";

        private static string ModelPath(Spec spec) => spec.Source ?? $"{ModelsFolder}/{spec.Category}/{spec.Name}/{spec.Name}.fbx";

        private static void Build(Spec spec)
        {
            string path = ModelPath(spec);

            if (spec.Source == null)
                Scale(spec, path);

            Material material = spec.Material != null ? spec.Material() : AssetDatabase.LoadAssetAtPath<Material>($"{ModelsFolder}/{spec.Category}/{spec.Name}/{spec.Name}.mat");

            GameObject root = new GameObject(spec.Name);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
            model.name = "Mesh";
            model.transform.localRotation = Quaternion.Euler(spec.Rotation) * model.transform.localRotation;
            Bounds bounds = DungeonKitBuilder.Bounds(model);

            if (spec.Source != null)
            {
                model.transform.localScale *= spec.Size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                bounds = DungeonKitBuilder.Bounds(model);
            }

            bool isSmall = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) < ShadowSize;

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.shadowCastingMode = isSmall ? ShadowCastingMode.Off : ShadowCastingMode.On;
            }

            Vector3 anchor = spec.Mount switch
            {
                Mount.Wall => new Vector3(bounds.center.x, bounds.min.y, bounds.min.z),
                Mount.Ceiling => new Vector3(bounds.center.x, bounds.max.y, bounds.center.z),
                _ => new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)
            };
            model.transform.localPosition = -anchor;

            if (spec.IsSolid)
            {
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = bounds.center - anchor;
                collider.size = bounds.size;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(spec.Name));
            Object.DestroyImmediate(root);
        }

        /// The importer takes the scale that brings the model's largest side to the spec's; materials come from the prefab.
        private static void Scale(Spec spec, string path)
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            GameObject probe = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            Bounds bounds = DungeonKitBuilder.Bounds(probe);
            Object.DestroyImmediate(probe);
            float raw = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) / importer.globalScale;
            float scale = spec.Size / raw;

            if (Mathf.Abs(importer.globalScale / scale - 1f) < 0.002f && !importer.importCameras && importer.materialImportMode == ModelImporterMaterialImportMode.None)
                return;

            importer.globalScale = scale;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
        }

        private static Spec Floor(string name, string category, float size, bool isSolid, Vector3 rotation = default)
        {
            return new Spec { Name = name, Category = category, Size = size, Rotation = rotation, Mount = Mount.Floor, IsSolid = isSolid };
        }

        /// Wall pieces come out of Tripo with their back to +Z: turned round, their front looks along +Z.
        private static Spec Wall(string name, string category, float size)
        {
            return new Spec { Name = name, Category = category, Size = size, Rotation = new Vector3(0f, 180f, 0f), Mount = Mount.Wall };
        }

        private static Spec Hanging(string name, string category, float size)
        {
            return new Spec { Name = name, Category = category, Size = size, Mount = Mount.Ceiling };
        }
    }
}
