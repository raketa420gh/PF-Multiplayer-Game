using System.Collections.Generic;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Editor.Dungeon
{
    /// Stand-in for the dungeon that is yet to be built: one room with the player spawns, a chest and an escape portal.
    /// Then the NavMesh and the map.
    internal static class DungeonMapBuilder
    {
        public const float RoomSize = 14f;
        /// Side of the floor; its map is drawn to this scale.
        public const float WorldSize = RoomSize + 2f;
        public const float FloorRadius = WorldSize * 0.75f;
        /// Height step between floors, should there be more than one.
        public const float FloorDrop = -26f;
        public const float AgentRadius = 0.35f;
        public const string NavMeshPath = "Assets/Game/Scenes/DungeonScene/NavMesh.asset";

        public static readonly float[] FloorSizes = { WorldSize };
        public static readonly int[] FloorGrids = { 1 };
        public static readonly string[] ModuleNames = { "Antechamber" };

        private const int PlayerSpawnCount = 6;
        private const float WallThickness = DungeonPropBuilder.WallThickness;

        public static Transform Build(DungeonDirector director, out Texture2D[] floorMaps)
        {
            BuildPieces();
            Transform root = new GameObject("[Dungeon]").transform;
            Transform floor = BattleEditorUtility.CreateChild("Floor1", root).transform;
            Transform spawns = new GameObject("[Spawns]").transform;
            Material stone = DungeonPropBuilder.Cobble;
            float half = RoomSize * 0.5f;
            float height = DungeonPropBuilder.WallHeight;
            Vector3 slab = new Vector3(RoomSize + WallThickness * 2f, WallThickness, RoomSize + WallThickness * 2f);

            DungeonStructureBuilder.Block(floor, "Floor", Vector3.down * WallThickness * 0.5f, slab, stone);
            DungeonStructureBuilder.Block(floor, "Ceiling", Vector3.up * (height + WallThickness * 0.5f), slab, stone);

            for (int i = 0; i < 4; i++)
            {
                Quaternion side = Quaternion.Euler(0f, i * 90f, 0f);
                DungeonStructureBuilder.Block(floor, "Wall", side * new Vector3(0f, height * 0.5f, half + WallThickness * 0.5f), new Vector3(slab.x, height, WallThickness), stone, i * 90f);
                Place(Load("WallTorch"), floor, side * new Vector3(0f, 2.6f, half - 0.07f), i * 90f + 180f);
            }

            List<Transform> players = new();

            for (int i = 0; i < PlayerSpawnCount; i++)
                players.Add(BattleEditorUtility.CreateChild("Player" + i, spawns, new Vector3((i - (PlayerSpawnCount - 1) * 0.5f) * 1.5f, 0f, 2f - half)).transform);

            ContainerComponent chest = Place(Load("LargeOakChest"), floor, new Vector3(0f, 0f, half - 1f), 180f, false).GetComponent<ContainerComponent>();
            PortalComponent portal = Place(Load("EscapePortal"), floor, new Vector3(half - 2f, 0f, 0f), -90f, false).GetComponent<PortalComponent>();

            BakeNavMesh(root.gameObject, NavMeshPath);

            SerializedObject so = new SerializedObject(director);
            so.FindProperty("_floors").arraySize = 1;
            const string layout = "_floors.Array.data[0].";
            BattleEditorUtility.Set(so, layout + "PlayerSpawns", players);
            BattleEditorUtility.Set(so, layout + "MonsterSpawns", new Transform[0]);
            BattleEditorUtility.Set(so, layout + "Containers", new[] { chest });
            BattleEditorUtility.Set(so, layout + "EscapePortals", new[] { portal });
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
