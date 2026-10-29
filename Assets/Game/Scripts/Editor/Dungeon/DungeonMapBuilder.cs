using System.Collections.Generic;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Editor.Dungeon
{
    /// The Great Hall (CatacombRoomBuilder.BuildHall): the empire's shaft, two cells long and four storeys high, from its
    /// mockup. The escape portal opens at a random spot.
    /// The cursed village (VillageMapBuilder) is floor 1, cut from the game for now. Floor 2, sunk below it, is the Tangled
    /// Catacombs, where runs start: the hall is their key room in the middle of a 7x7 grid whose other rooms
    /// (CatacombRoomBuilder) CatacombGenerator lays out anew every run; two doorways on each side lead on.
    internal static class DungeonMapBuilder
    {
        /// Wall to wall in ten seconds at 300 move speed (run speed 3.36 m/s).
        public const float RoomSize = 33.6f;
        /// Side of the hall's own map.
        public const float WorldSize = RoomSize + 2f;
        public const float Height = 6f;
        /// Distance between neighbouring cells of the catacombs: a room and the wall two rooms share.
        public const float Pitch = RoomSize + DungeonPropBuilder.WallThickness;
        public const int CatacombGrid = 7;
        public const float CatacombSize = Pitch * CatacombGrid;
        /// Depth of the hall below the village, well under the deepest riverbed.
        public const float FloorDrop = -60f;
        public const float AgentRadius = 0.35f;
        public const string VillageNavMeshPath = "Assets/Game/Scenes/DungeonScene/VillageNavMesh.asset";

        public static readonly string[] FloorTitles = { "Cursed Village", "Tangled Catacombs" };
        public static readonly float[] FloorSizes = { VillageLayout.Size, CatacombSize };
        public static readonly int[] FloorGrids = { VillageLayout.Grid, CatacombGrid };
        /// How much of each floor the minimap window shows.
        public static readonly float[] WindowSizes = { 110f, 70f };
        public static readonly string[] ModuleNames = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Append(VillageLayout.ModuleNames, "Great Hall"));

        private const float WallThickness = DungeonPropBuilder.WallThickness;
        private const float Half = RoomSize * 0.5f;
        private const float DoorOffset = 8.4f;

        public static Transform Build(DungeonDirector director, out Texture2D[] floorMaps)
        {
            BuildPieces();
            CatacombRoomBuilder.Result rooms = CatacombRoomBuilder.Build();
            Transform root = new GameObject("[Dungeon]").transform;
            Transform village = BattleEditorUtility.CreateChild("Floor1", root).transform;
            Transform floor = BattleEditorUtility.CreateChild("Floor2", root, Vector3.up * FloorDrop).transform;
            Transform spawns = new GameObject("[Spawns]").transform;
            // The village is cut while runs start in the catacombs: an empty Floor1 keeps the floor indices, its hundreds of network
            // objects, colliders and lights cost no peer anything.
            bool isVillageCut = MatchComponent.EntryFloor > 1;
            VillageMapBuilder.Result upper = isVillageCut
                ? new VillageMapBuilder.Result { Map = AssetDatabase.LoadAssetAtPath<Texture2D>($"{DungeonMinimapBuilder.Folder}/Floor1.png") }
                : VillageMapBuilder.Build(village, spawns);
            CatacombRoomComponent hall = CatacombRoomBuilder.BuildHall(floor, rooms.Sides);

            Transform[] arrivals =
            {
                BattleEditorUtility.CreateChild("Arrival Graveyard", spawns, floor.TransformPoint(new Vector3(-DoorOffset, 0f, Half - 1.6f))).transform,
                BattleEditorUtility.CreateChild("Arrival Swamp", spawns, floor.TransformPoint(new Vector3(DoorOffset, 0f, 1.6f - Half))).transform
            };
            arrivals[0].rotation = Quaternion.Euler(0f, 180f, 0f);

            // Parked out of the way and invisible until they open one by one; the director then moves each to a random free spot.
            PortalComponent[] portals = new PortalComponent[4];

            for (int i = 0; i < portals.Length; i++)
                portals[i] = Place(Load("EscapePortal"), floor, new Vector3(i * 2.5f - 3.75f, 0f, -Half * 0.5f), 0f, false).GetComponent<PortalComponent>();

            // The catacombs get their NavMesh at runtime, once the rooms of the run are laid out.
            if (!isVillageCut)
                BakeNavMesh(village.gameObject, VillageNavMeshPath, 0.2f, upper.Sources);
            CatacombGenerator catacombs = BuildCatacombs(floor, rooms, hall, out Texture2D catacombMap);

            SerializedObject so = new SerializedObject(director);
            so.FindProperty("_floors").arraySize = 2;
            const string first = "_floors.Array.data[0].";
            BattleEditorUtility.Set(so, first + "Title", FloorTitles[0]);
            BattleEditorUtility.Set(so, first + "PlayerSpawns", upper.PlayerSpawns);
            BattleEditorUtility.Set(so, first + "SpawnHouses", upper.SpawnHouses);
            BattleEditorUtility.Set(so, first + "MonsterSpawns", upper.MonsterSpawns);
            SetMonsters(so, first, upper.Monsters.ToArray());
            BattleEditorUtility.Set(so, first + "Containers", upper.Containers);
            BattleEditorUtility.Set(so, first + "EscapePortals", upper.EscapePortals);
            BattleEditorUtility.Set(so, first + "EscapeArea", VillageLayout.Playable - 12f);
            BattleEditorUtility.Set(so, first + "DescendPortals", upper.DescendPortals);
            BattleEditorUtility.Set(so, first + "Arrivals", new Transform[0]);
            BattleEditorUtility.Set(so, first + "Center", Vector3.up * 2f);
            // The circle starts wide enough to cover the corners of the square map.
            BattleEditorUtility.Set(so, first + "Radius", VillageLayout.Half * 1.45f);

            const string layout = "_floors.Array.data[1].";
            BattleEditorUtility.Set(so, layout + "Title", FloorTitles[1]);
            BattleEditorUtility.Set(so, layout + "PlayerSpawns", new Transform[0]);
            BattleEditorUtility.Set(so, layout + "SpawnHouses", new Transform[0]);
            BattleEditorUtility.Set(so, layout + "MonsterSpawns", new Transform[0]);
            SetMonsters(so, layout, new DungeonDirector.MonsterPlacement[0]);
            BattleEditorUtility.Set(so, layout + "Containers", new ContainerComponent[0]);
            BattleEditorUtility.Set(so, layout + "EscapePortals", portals);
            BattleEditorUtility.Set(so, layout + "EscapeArea", CatacombSize * 0.5f - 3f);
            BattleEditorUtility.Set(so, layout + "DescendPortals", new PortalComponent[0]);
            BattleEditorUtility.Set(so, layout + "Arrivals", arrivals);
            BattleEditorUtility.Set(so, layout + "Center", Vector3.up * FloorDrop);
            BattleEditorUtility.Set(so, layout + "Radius", CatacombSize * 0.75f);
            BattleEditorUtility.Set(so, "_catacombs", catacombs);
            so.ApplyModifiedPropertiesWithoutUndo();
            floorMaps = new[] { upper.Map, catacombMap };

            return root;
        }

        /// The generator of the catacombs on the hall's floor, with the rooms, the key room and, for the menus, the map of one
        /// sample layout.
        private static CatacombGenerator BuildCatacombs(Transform floor, CatacombRoomBuilder.Result rooms, CatacombRoomComponent hall, out Texture2D sampleMap)
        {
            CatacombGenerator generator = floor.gameObject.AddComponent<CatacombGenerator>();
            SerializedObject so = new SerializedObject(generator);
            BattleEditorUtility.Set(so, "_floor", MatchComponent.EntryFloor);
            BattleEditorUtility.Set(so, "_grid", CatacombGrid);
            BattleEditorUtility.Set(so, "_pitch", Pitch);
            BattleEditorUtility.Set(so, "_keyRoom", hall);
            BattleEditorUtility.Set(so, "_isKeyRoomLong", true);
            BattleEditorUtility.Set(so, "_sides", rooms.Sides);
            BattleEditorUtility.Set(so, "_post", rooms.Post);
            BattleEditorUtility.Set(so, "_door", DungeonSceneBuilder.LoadNetworkObject("CatacombDoor"));
            BattleEditorUtility.Set(so, "_doorWidth", CatacombRoomBuilder.DoorWidth);
            BattleEditorUtility.Set(so, "_doorOffset", CatacombRoomBuilder.DoorOffset);
            BattleEditorUtility.Set(so, "_cellPixels", CatacombRoomBuilder.MapPixels);
            BattleEditorUtility.Set(so, "_agentRadius", AgentRadius);
            SerializedProperty kinds = so.FindProperty("_kinds");
            kinds.arraySize = rooms.Kinds.Length;

            for (int i = 0; i < rooms.Kinds.Length; i++)
            {
                SerializedProperty kind = kinds.GetArrayElementAtIndex(i);
                kind.FindPropertyRelative("Prefab").objectReferenceValue = rooms.Kinds[i].Prefab;
                kind.FindPropertyRelative("Weight").floatValue = rooms.Kinds[i].Weight;
                kind.FindPropertyRelative("Limit").intValue = rooms.Kinds[i].Limit;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Texture2D sample = generator.Paint(generator.Lay(1));
            sampleMap = DungeonMinimapBuilder.Save(sample, "Floor2");
            Object.DestroyImmediate(sample);

            return generator;
        }

        private static void SetMonsters(SerializedObject so, string layout, DungeonDirector.MonsterPlacement[] monsters)
        {
            SerializedProperty placements = so.FindProperty(layout + "Monsters");
            placements.arraySize = monsters.Length;

            for (int i = 0; i < monsters.Length; i++)
            {
                placements.GetArrayElementAtIndex(i).FindPropertyRelative("Point").objectReferenceValue = monsters[i].Point;
                placements.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab").objectReferenceValue = monsters[i].Prefab;
            }
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

        internal static void BakeNavMesh(GameObject root, string path, float voxelSize = 0.12f, List<NavMeshBuildSource> extra = null)
        {
            NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1;
            surface.overrideVoxelSize = true;
            surface.voxelSize = voxelSize;

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

            if (extra != null)
                sources.AddRange(extra);
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
            DungeonPropBuilder.SavePrefab(Sconce("HallTorch", "Torch"), "HallTorch");
            DungeonPropBuilder.SavePrefab(Sconce("SanctumTorch", "Ornated Torch"), "SanctumTorch");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Table(), "Table");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.SkullPile(), "SkullPile");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.CandleCluster(), "CandleCluster");
        }

        /// A pack torch leaning out of the wall in an iron bracket: plate on the wall, an arm and a collar the shaft passes through.
        private static GameObject Sconce(string name, string torch)
        {
            Vector3 collar = new Vector3(0f, -0.08f, 0.25f);
            Mesh bracket = new DungeonMeshBuilder(1f)
                .Box(new Vector3(0f, 0f, 0.03f), new Vector3(0.16f, 0.32f, 0.06f))
                .Box(new Vector3(0f, collar.y, 0.14f), new Vector3(0.05f, 0.05f, 0.2f))
                .Box(collar, new Vector3(0.14f, 0.04f, 0.14f))
                .Save("SconceBracket");
            GameObject root = new GameObject(name);
            DungeonPropBuilder.MeshObject("Bracket", root.transform, bracket, DungeonPropBuilder.RustyMetal, default, default, false, false);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(DungeonMedievalBuilder.Load(torch), root.transform);
            instance.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            instance.transform.localPosition = collar - instance.transform.localRotation * Vector3.up * 0.3f;

            return root;
        }
    }
}
