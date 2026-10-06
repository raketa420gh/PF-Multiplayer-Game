using System.Collections.Generic;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Editor.Dungeon
{
    /// The Great Hall after the user's reference: a square hall of rough stone with four doors, banners and torches,
    /// barrels and crates in the corners, and a walled sanctum in the middle (openings east and west) holding two chests
    /// and a brazier on a stepped plinth. One archer and one flying head guard it; the escape portal opens at a random spot.
    /// Then the NavMesh and the map.
    internal static class DungeonMapBuilder
    {
        /// Wall to wall in ten seconds at 300 move speed (run speed 3.36 m/s).
        public const float RoomSize = 33.6f;
        /// Side of the floor; its map is drawn to this scale.
        public const float WorldSize = RoomSize + 2f;
        public const float FloorRadius = WorldSize * 0.75f;
        /// Height step between floors, should there be more than one.
        public const float FloorDrop = -26f;
        public const float AgentRadius = 0.35f;
        public const string NavMeshPath = "Assets/Game/Scenes/DungeonScene/NavMesh.asset";

        public static readonly float[] FloorSizes = { WorldSize };
        public static readonly int[] FloorGrids = { 1 };
        public static readonly string[] ModuleNames = { "Great Hall" };

        private const int PlayerSpawnCount = 6;
        private const float WallThickness = DungeonPropBuilder.WallThickness;
        private const float Height = 6f;
        private const float SanctumSize = 13.6f;
        private const float SanctumGap = 3f;
        private const float DoorOffset = 8.4f;
        private const float TorchHeight = 2.6f;

        public static Transform Build(DungeonDirector director, out Texture2D[] floorMaps)
        {
            BuildPieces();
            Transform root = new GameObject("[Dungeon]").transform;
            Transform floor = BattleEditorUtility.CreateChild("Floor1", root).transform;
            Transform spawns = new GameObject("[Spawns]").transform;
            float half = RoomSize * 0.5f;
            Vector3 slab = new Vector3(RoomSize + WallThickness * 2f, WallThickness, RoomSize + WallThickness * 2f);
            List<ContainerComponent> containers = new();

            DungeonStructureBuilder.Block(floor, "Floor", Vector3.down * WallThickness * 0.5f, slab, DungeonPropBuilder.Flagstone);
            DungeonStructureBuilder.Block(floor, "Ceiling", Vector3.up * (Height + WallThickness * 0.5f), slab, DungeonPropBuilder.Ashlar);

            for (int i = 1; i < 8; i++)
                DungeonStructureBuilder.Block(floor, "Ceiling Beam", new Vector3(0f, Height - 0.2f, i * RoomSize / 8f - half), new Vector3(RoomSize, 0.4f, 0.35f), DungeonPropBuilder.DarkWood, 0f, false);

            for (int i = 0; i < 4; i++)
            {
                Quaternion side = Quaternion.Euler(0f, i * 90f, 0f);
                float yaw = i * 90f;
                Wall(floor, side * new Vector3(0f, 0f, half + WallThickness * 0.5f), slab.x, yaw);
                Wall(floor, side * new Vector3(half - 0.45f, 0f, half - 0.45f), 0.9f, yaw, 0.9f, Height, "Wall Pilaster");

                if (i % 2 == 0)
                {
                    Wall(floor, side * new Vector3(0f, 0f, half - 0.25f), 1.2f, yaw, 0.5f, Height, "Wall Pilaster");

                    foreach (float x in new[] { -DoorOffset, DoorOffset })
                    {
                        Door(floor, side * new Vector3(x, 0f, half), yaw + 180f);
                        Torch(floor, side * new Vector3(x - 1.9f, TorchHeight, half - 0.07f), yaw + 180f);
                        Torch(floor, side * new Vector3(x + 1.9f, TorchHeight, half - 0.07f), yaw + 180f);
                    }

                    foreach (float x in new[] { -14f, 14f })
                        Banner(floor, "Banner_2_Cloth", side * new Vector3(x, 0f, half - 0.06f), yaw + 180f);
                }
                else
                {
                    Torch(floor, side * new Vector3(0f, TorchHeight, half - 0.07f), yaw + 180f);

                    foreach (float x in new[] { -4.2f, 4.2f, -10.5f, 10.5f })
                        Banner(floor, Mathf.Abs(x) < 5f ? "Banner_2_Cloth" : "Banner_1_Cloth", side * new Vector3(x, 0f, half - 0.06f), yaw + 180f);
                }

                Corner(floor, side, half, i, containers);
            }

            BuildSanctum(floor, containers);

            List<Transform> players = new();

            for (int i = 0; i < PlayerSpawnCount; i++)
                players.Add(BattleEditorUtility.CreateChild("Player" + i, spawns, new Vector3((i - (PlayerSpawnCount - 1) * 0.5f) * 2f, 0f, 3f - half)).transform);

            DungeonDirector.MonsterPlacement[] monsters =
            {
                Monster(spawns, "SkeletonArcher", new Vector3(0f, 0f, half - 4f), 180f),
                Monster(spawns, "FlyingHead", new Vector3(half - 4.5f, 0f, -3f), -90f)
            };

            // Parked out of the way and invisible until it opens; the director then moves it to a random free spot.
            PortalComponent portal = Place(Load("EscapePortal"), floor, new Vector3(0f, 0f, -half * 0.5f), 0f, false).GetComponent<PortalComponent>();

            BakeNavMesh(root.gameObject, NavMeshPath);

            SerializedObject so = new SerializedObject(director);
            so.FindProperty("_floors").arraySize = 1;
            const string layout = "_floors.Array.data[0].";
            BattleEditorUtility.Set(so, layout + "PlayerSpawns", players);
            BattleEditorUtility.Set(so, layout + "MonsterSpawns", new Transform[0]);
            SerializedProperty placements = so.FindProperty(layout + "Monsters");
            placements.arraySize = monsters.Length;

            for (int i = 0; i < monsters.Length; i++)
            {
                placements.GetArrayElementAtIndex(i).FindPropertyRelative("Point").objectReferenceValue = monsters[i].Point;
                placements.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab").objectReferenceValue = monsters[i].Prefab;
            }

            BattleEditorUtility.Set(so, layout + "Containers", containers);
            BattleEditorUtility.Set(so, layout + "EscapePortals", new[] { portal });
            BattleEditorUtility.Set(so, layout + "EscapeArea", half);
            BattleEditorUtility.Set(so, layout + "Center", Vector3.zero);
            BattleEditorUtility.Set(so, layout + "Radius", FloorRadius);
            so.ApplyModifiedPropertiesWithoutUndo();
            floorMaps = new[] { DungeonMinimapBuilder.Render(floor, 0f, "Floor1") };

            return root;
        }

        internal static GameObject Place(GameObject prefab, Transform parent, Vector3 localPosition, float yaw, bool isStatic = true)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            if (isStatic)
            {
                foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                    child.gameObject.isStatic = true;
            }

            return instance;
        }

        internal static void BakeNavMesh(GameObject root, string path)
        {
            NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.12f;

            // Paths are walked by character controllers 0.3 wide: the stock agent would not fit through narrow doorways.
            NavMeshBuildSettings settings = surface.GetBuildSettings();
            settings.agentRadius = AgentRadius;
            List<NavMeshBuildMarkup> markups = new();
            List<NavMeshBuildSource> sources = new();
            Bounds bounds = new Bounds(root.transform.position, Vector3.zero);

            foreach (NavMeshModifier modifier in root.GetComponentsInChildren<NavMeshModifier>())
                markups.Add(new NavMeshBuildMarkup { root = modifier.transform, overrideArea = modifier.overrideArea, area = modifier.area, ignoreFromBuild = modifier.ignoreFromBuild });

            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
                bounds.Encapsulate(collider.bounds);

            UnityEngine.AI.NavMeshBuilder.CollectSources(null, surface.layerMask, surface.useGeometry, surface.defaultArea, markups, sources);
            bounds.center -= root.transform.position;
            NavMeshData data = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, root.transform.position, Quaternion.identity);

            BattleEditorUtility.EnsureFolder(path.Substring(0, path.LastIndexOf('/')));
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(data, path);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        }

        internal static GameObject Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(name));
        }

        /// Walled room in the middle of the hall: openings east and west, chests north and south, barrels and a brazier inside.
        private static void BuildSanctum(Transform floor, List<ContainerComponent> containers)
        {
            float half = SanctumSize * 0.5f;
            float edge = half + WallThickness * 0.5f;
            float segment = (SanctumSize - SanctumGap) * 0.5f;
            float segmentCenter = (SanctumGap + segment) * 0.5f;

            for (int i = 0; i < 4; i++)
            {
                Quaternion side = Quaternion.Euler(0f, i * 90f, 0f);
                float yaw = i * 90f;

                if (i % 2 == 0)
                {
                    Wall(floor, side * new Vector3(0f, 0f, edge), SanctumSize + WallThickness * 2f, yaw);
                    Wall(floor, side * new Vector3(0f, 0f, edge + 0.45f), 1.2f, yaw, 0.3f, Height, "Wall Pilaster");
                    Torch(floor, side * new Vector3(-3.2f, TorchHeight, half - 0.07f), yaw + 180f);
                    Torch(floor, side * new Vector3(3.2f, TorchHeight, half - 0.07f), yaw + 180f);
                }
                else
                {
                    foreach (float x in new[] { -segmentCenter, segmentCenter })
                        Wall(floor, side * new Vector3(x, 0f, edge), segment, yaw);

                    foreach (float x in new[] { -SanctumGap * 0.5f - 0.3f, SanctumGap * 0.5f + 0.3f })
                        Wall(floor, side * new Vector3(x, 0f, edge), 0.6f, yaw, 1f, Height, "Wall Post");
                }

                Wall(floor, side * new Vector3(edge, 0f, edge), 1f, yaw, 1f, Height, "Wall Post");
            }

            Mesh plinth = new DungeonMeshBuilder(0.5f)
                .Cylinder(new Vector3(0f, 0.1f, 0f), 1.5f, 0.2f, 8)
                .Cylinder(new Vector3(0f, 0.3f, 0f), 1.1f, 0.2f, 8)
                .Cylinder(new Vector3(0f, 0.5f, 0f), 0.75f, 0.2f, 8)
                .Save("SanctumPlinth");
            DungeonPropBuilder.MeshObject("Plinth", floor, plinth, DungeonPropBuilder.Ashlar, Vector3.zero, new Vector3(0f, 22.5f, 0f));
            Place(Load("Brazier"), floor, Vector3.up * 0.6f, 0f);

            containers.Add(Container("LargeOakChest", floor, new Vector3(0f, 0f, half - 1.2f), 180f));
            containers.Add(Container("LargeOakChest", floor, new Vector3(0f, 0f, 1.2f - half), 0f));
            containers.Add(Container("Barrel", floor, new Vector3(-3.6f, 0f, 0f), 0f));
            containers.Add(Container("Barrel", floor, new Vector3(3.6f, 0f, 0f), 0f));
        }

        /// Wall block standing on the floor; length runs along local X, thickness faces the room.
        private static void Wall(Transform parent, Vector3 position, float length, float yaw, float thickness = WallThickness, float height = Height,
            string name = "Wall")
        {
            DungeonStructureBuilder.Block(parent, name, position + Vector3.up * height * 0.5f, new Vector3(length, height, thickness), DungeonPropBuilder.Ashlar, yaw);
        }

        /// Shut door in a stone frame, set against the wall; local +Z faces the room.
        private static void Door(Transform parent, Vector3 position, float yaw)
        {
            GameObject door = BattleEditorUtility.CreateChild("Door", parent, position);
            door.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            door.isStatic = true;
            Mesh frame = new DungeonMeshBuilder(0.5f)
                .Box(new Vector3(-1.3f, 1.6f, 0.15f), new Vector3(0.45f, 3.2f, 0.3f))
                .Box(new Vector3(1.3f, 1.6f, 0.15f), new Vector3(0.45f, 3.2f, 0.3f))
                .Box(new Vector3(0f, 3.4f, 0.17f), new Vector3(3.05f, 0.5f, 0.34f))
                .Box(new Vector3(0f, 3.75f, 0.1f), new Vector3(1.2f, 0.3f, 0.2f))
                .Save("DoorFrame");
            DungeonPropBuilder.MeshObject("Wall Door Frame", door.transform, frame, DungeonPropBuilder.Ashlar);
            GameObject leaf = DungeonPropBuilder.DoorLeaf();
            leaf.transform.SetParent(door.transform, false);
            leaf.transform.localPosition = new Vector3(-1.05f, 0.02f, 0.07f);
            leaf.isStatic = true;
        }

        private static void Torch(Transform parent, Vector3 position, float yaw)
        {
            Place(Load("WallTorch"), parent, position, yaw);
        }

        private static void Banner(Transform parent, string name, Vector3 position, float yaw)
        {
            // The cloth hangs down from its pivot.
            GameObject banner = Place(DungeonKitBuilder.Load(name), parent, position + Vector3.up * 4.3f, yaw);
            banner.transform.localScale = Vector3.one * 1.25f;
            Material red = RedBanner();

            foreach (Renderer renderer in banner.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = System.Array.ConvertAll(renderer.sharedMaterials, material => material.name == "KitBanner" ? red : material);
        }

        /// The kit cloth is dyed by vertex colours (navy); the reference hangs blood-red banners, so plain Lit ignores them.
        private static Material RedBanner()
        {
            string path = $"{DungeonPropBuilder.MaterialsFolder}/BannerRed.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>($"{DungeonPropBuilder.MaterialsFolder}/Kit/KitBanner.mat"));
                material.shader = Shader.Find("Universal Render Pipeline/Lit");
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", new Color(0.5f, 0.07f, 0.05f));
            material.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(material);

            return material;
        }

        /// Barrels, crates and a little junk heaped in a corner; every second corner also hides a small chest.
        private static void Corner(Transform parent, Quaternion side, float half, int index, List<ContainerComponent> containers)
        {
            Vector3 Point(float x, float z) => side * new Vector3(half - x, 0f, half - z);

            containers.Add(Container("Barrel", parent, Point(1.5f, 0.85f), index * 37f));
            containers.Add(Container("Barrel", parent, Point(2.5f, 0.9f), index * 53f));
            containers.Add(Container("Crate", parent, Point(0.95f, 2.1f), index * 90f + 8f));
            Place(DungeonKitBuilder.Load(index % 2 == 0 ? "Barrel_Holder" : "Crate_Wooden"), parent, Point(1.1f, 3.4f), index * 90f + 90f);
            Place(DungeonKitBuilder.Load(index % 2 == 0 ? "Bucket_Wooden_1" : "Vase_Rubble_Medium"), parent, Point(3.4f, 0.7f), index * 70f);

            if (index % 2 == 1)
                containers.Add(Container("SmallOakChest", parent, Point(4.8f, 0.8f), index * 90f + 180f));
        }

        private static ContainerComponent Container(string name, Transform parent, Vector3 position, float yaw)
        {
            return Place(Load(name), parent, position, yaw, false).GetComponent<ContainerComponent>();
        }

        private static DungeonDirector.MonsterPlacement Monster(Transform parent, string name, Vector3 position, float yaw)
        {
            Transform point = BattleEditorUtility.CreateChild(name, parent, position).transform;
            point.rotation = Quaternion.Euler(0f, yaw, 0f);

            return new DungeonDirector.MonsterPlacement { Point = point, Prefab = DungeonSceneBuilder.LoadNetworkObject(name) };
        }

        /// Props saved as prefabs that scenes place by name.
        private static void BuildPieces()
        {
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Pillar(), "Pillar");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Brazier(), "Brazier");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.WallTorch(), "WallTorch");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Table(), "Table");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.SkullPile(), "SkullPile");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.CandleCluster(), "CandleCluster");
        }
    }
}
