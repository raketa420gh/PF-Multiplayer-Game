using System.Collections.Generic;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Editor.Dungeon
{
    /// Two floors. The first is the crypt of Dark and Darker, carved after its map by DungeonLayoutBuilder. The second is a
    /// 3x3 grid of modules: outer walls and doorways, rooms furnished by DungeonRoomBuilder. Then the NavMesh and the maps.
    internal static class DungeonMapBuilder
    {
        public const float Module = 40f;
        public const int Grid = 3;
        public const string Crypt = "Crypt";
        public const float CryptModule = 30f;
        public const int CryptGrid = 5;
        /// Side of the widest floor; the maps of all floors are drawn to this scale.
        public const float WorldSize = CryptModule * CryptGrid;
        public const float FloorDrop = -26f;
        public const float FloorRadius = Module * Grid * 0.75f;
        public const float TallHeight = DungeonPropBuilder.WallHeight * 2f;
        public const float PitDepth = 4f;
        public const float AgentRadius = 0.35f;
        public const string NavMeshPath = "Assets/Game/Scenes/DungeonScene/NavMesh.asset";

        public static readonly float[] FloorSizes = { WorldSize, Module * Grid };
        public static readonly int[] FloorGrids = { CryptGrid, Grid };

        // Rows run south to north, columns west to east.
        private static readonly string[] s_cryptNames =
        {
            "Sunken Tunnels", "Pilgrims' Chapel", "Guard Post", "Circle of Pillars", "Labyrinth",
            "Hermit's Cell", "Stepped Pyramid", "Dark Stairway", "Summoning Hall", "Winding Passage",
            "Altar Chambers", "Chapel of Thrones", "Sunken Halls", "Pillared Halls", "Crossroads Shrine",
            "Sealed Vault", "Cave Passage", "Sacrificial Arena", "Sacrificial Arena", "Ossuary",
            "Twin Halls", "Sunken Cave", "Sacrificial Arena", "Sacrificial Arena", "Gatehouse"
        };

        private static readonly DungeonRoom[,] s_floor2 =
        {
            { DungeonRoom.Arrival, DungeonRoom.BonePit, DungeonRoom.Cellar },
            { DungeonRoom.TrapCorridor, DungeonRoom.Labyrinth, DungeonRoom.Crypt },
            { DungeonRoom.Treasury, DungeonRoom.Throne, DungeonRoom.Shrine }
        };

        public static Texture2D[] FloorMaps { get; private set; }
        public static string[] ModuleNames { get; private set; }

        public static Transform Build(DungeonDirector director)
        {
            System.Random random = new System.Random(2024);
            BuildPieces();
            Transform root = new GameObject("[Dungeon]").transform;
            Transform spawns = new GameObject("[Spawns]").transform;

            DungeonLayoutBuilder crypt = new DungeonLayoutBuilder(Crypt, WorldSize, DungeonPropBuilder.WallHeight);
            DungeonFloorResult first = BuildCrypt(root, spawns, crypt);
            DungeonFloorResult second = BuildFloor(root, spawns, "Floor2", s_floor2, FloorDrop, 2, random);

            BakeNavMesh(root.gameObject, NavMeshPath);
            WriteLayouts(director, first, second);
            FloorMaps = new[] { DungeonMinimapBuilder.Paint(crypt, "Floor1"), DungeonMinimapBuilder.Render(root.Find("Floor2"), FloorDrop, "Floor2") };
            ModuleNames = BuildModuleNames();

            return root;
        }

        public static float CeilingHeight(DungeonRoom room)
        {
            return IsTall(room) ? TallHeight : DungeonPropBuilder.WallHeight;
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

            // Paths are walked by character controllers 0.3 wide: the stock agent would not fit through the doorways of the crypt.
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

        private static bool IsTall(DungeonRoom room)
        {
            return room is DungeonRoom.Cellar or DungeonRoom.Throne;
        }

        private static bool HasPit(DungeonRoom room)
        {
            return room is DungeonRoom.BonePit or DungeonRoom.TrapCorridor;
        }

        private static bool HasDoor(DungeonRoom room)
        {
            return room is DungeonRoom.Treasury or DungeonRoom.Crypt or DungeonRoom.Cellar;
        }

        /// Walled-up doorway: the hoard is reached through the throne room.
        private static bool IsBlocked(DungeonRoom a, DungeonRoom b)
        {
            return a == DungeonRoom.TrapCorridor && b == DungeonRoom.Treasury;
        }

        private static string[] BuildModuleNames()
        {
            List<string> names = new List<string>(s_cryptNames);

            foreach (DungeonRoom room in s_floor2)
                names.Add(RoomName(room));

            return names.ToArray();
        }

        private static string RoomName(DungeonRoom room)
        {
            return room switch
            {
                DungeonRoom.Treasury => "Treasure Hoard",
                DungeonRoom.Shrine => "High Priest's Chapel",
                DungeonRoom.Arrival => "Descent",
                DungeonRoom.BonePit => "Bone Pit",
                DungeonRoom.Cellar => "Wine Cellar",
                DungeonRoom.TrapCorridor => "Death Hall",
                DungeonRoom.Labyrinth => "Labyrinth",
                DungeonRoom.Crypt => "Catacombs",
                DungeonRoom.Throne => "Throne Room",
                _ => room.ToString()
            };
        }

        /// Shared pieces saved as prefabs: architecture and the props that rooms and other scenes place by name.
        private static void BuildPieces()
        {
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Wall(Module, "Wall" + Module), "Wall");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.DoorFrame(Module), "DoorFrame");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Pillar(), "Pillar");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Floor(Module), "FloorTile");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Ceiling(Module), "CeilingTile");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Brazier(), "Brazier");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.WallTorch(), "WallTorch");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Table(), "Table");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.SkullPile(), "SkullPile");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Rubble(), "Rubble");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Sarcophagus(), "Sarcophagus");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Cobweb(), "Cobweb");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Chandelier(), "Chandelier");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.CandleCluster(), "CandleCluster");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Chain(), "Chain");
        }

        internal static GameObject Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(name));
        }

        private static DungeonFloorResult BuildCrypt(Transform root, Transform spawns, DungeonLayoutBuilder crypt)
        {
            DungeonFloorResult result = new DungeonFloorResult { Center = Vector3.zero, Radius = WorldSize * 0.75f };
            Transform floor = new GameObject("Floor1").transform;
            floor.SetParent(root, false);
            Transform markers = new GameObject("Floor1").transform;
            markers.SetParent(spawns, false);
            crypt.Build(floor, markers, result, Mathf.RoundToInt(crypt.Count / (float)CryptGrid));

            return result;
        }

        private static DungeonFloorResult BuildFloor(Transform root, Transform spawns, string name, DungeonRoom[,] rooms, float y, int floorIndex, System.Random random)
        {
            DungeonFloorResult result = new DungeonFloorResult { Center = new Vector3(0f, y, 0f), Radius = FloorRadius };
            Transform floor = new GameObject(name).transform;
            floor.SetParent(root, false);
            floor.localPosition = new Vector3(0f, y, 0f);
            Transform markers = new GameObject(name).transform;
            markers.SetParent(spawns, false);

            for (int x = 0; x < Grid; x++)
            {
                for (int z = 0; z < Grid; z++)
                {
                    DungeonRoom room = rooms[z, x];
                    Transform module = new GameObject($"Module_{x}_{z}_{room}").transform;
                    module.SetParent(floor, false);
                    module.localPosition = ModuleCenter(x, z);

                    if (!HasPit(room))
                        Place(Load("FloorTile"), module, Vector3.zero, 0f);

                    Place(Load("CeilingTile"), module, new Vector3(0f, CeilingHeight(room), 0f), 0f);
                    new DungeonRoomBuilder(module, markers, result, room, floorIndex, random).Build();
                }
            }

            BuildEdges(floor, rooms);
            LinkLevers(result);

            return result;
        }

        private static void LinkLevers(DungeonFloorResult result)
        {
            if (result.Levers.Count == 0)
                return;

            TrapComponent blade = result.Traps.Find(trap => trap.Kind == TrapKind.SwingingBlade);
            result.Levers[0].Setup(result.LockedDoor, blade);
            EditorUtility.SetDirty(result.Levers[0]);
        }

        private static Vector3 ModuleCenter(int x, int z)
        {
            return new Vector3((x - 1) * Module, 0f, (z - 1) * Module);
        }

        private static void BuildEdges(Transform floor, DungeonRoom[,] rooms)
        {
            Transform edges = new GameObject("Edges").transform;
            edges.SetParent(floor, false);
            float half = Module * 0.5f;

            for (int x = 0; x < Grid; x++)
            {
                for (int z = 0; z < Grid; z++)
                {
                    Vector3 center = ModuleCenter(x, z);
                    DungeonRoom room = rooms[z, x];

                    if (z == 0)
                        Edge(edges, center + new Vector3(0f, 0f, -half), 0f, room, null);

                    if (x == 0)
                        Edge(edges, center + new Vector3(-half, 0f, 0f), 90f, room, null);

                    Edge(edges, center + new Vector3(0f, 0f, half), 0f, room, z == Grid - 1 ? null : rooms[z + 1, x]);
                    Edge(edges, center + new Vector3(half, 0f, 0f), 90f, room, x == Grid - 1 ? null : rooms[z, x + 1]);
                }
            }
        }

        /// Wall between two modules (or the outer wall): solid or with a doorway, doubled in height next to a tall room.
        private static void Edge(Transform parent, Vector3 position, float yaw, DungeonRoom a, DungeonRoom? b)
        {
            if (b == null || IsBlocked(a, b.Value))
                Place(Load("Wall"), parent, position, yaw);
            else
                Opening(parent, position, yaw, a, b.Value);

            if (IsTall(a) || (b != null && IsTall(b.Value)))
                Place(Load("Wall"), parent, position + Vector3.up * DungeonPropBuilder.WallHeight, yaw);
        }

        private static void Opening(Transform parent, Vector3 position, float yaw, DungeonRoom a, DungeonRoom b)
        {
            Place(Load("DoorFrame"), parent, position, yaw);
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Place(Load("WallTorch"), parent, position + rotation * new Vector3(-2.1f, 2.6f, -0.32f), yaw + 180f);
            Place(Load("WallTorch"), parent, position + rotation * new Vector3(2.1f, 2.6f, 0.32f), yaw);

            if (HasDoor(a) || HasDoor(b))
                Place(Load("Door"), parent, position, yaw, false);
        }

        private static void WriteLayouts(DungeonDirector director, DungeonFloorResult first, DungeonFloorResult second)
        {
            SerializedObject so = new SerializedObject(director);
            SerializedProperty floors = so.FindProperty("_floors");
            floors.arraySize = 2;
            WriteFloor(floors.GetArrayElementAtIndex(0), first, 1);
            WriteFloor(floors.GetArrayElementAtIndex(1), second, 2);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteFloor(SerializedProperty property, DungeonFloorResult result, int index)
        {
            SetArray(property.FindPropertyRelative("PlayerSpawns"), result.PlayerSpawns.Count > 0 ? result.PlayerSpawns : new List<Transform> { result.DescendDestination });
            SetArray(property.FindPropertyRelative("MonsterSpawns"), result.MonsterSpawns);
            SetArray(property.FindPropertyRelative("Containers"), result.Containers);
            SetArray(property.FindPropertyRelative("EscapePortals"), result.EscapePortals);
            property.FindPropertyRelative("DescendPortal").objectReferenceValue = result.DescendPortal;
            property.FindPropertyRelative("DescendDestination").objectReferenceValue = result.DescendDestination;
            property.FindPropertyRelative("BossSpawn").objectReferenceValue = result.BossSpawn;
            property.FindPropertyRelative("Center").vector3Value = result.Center;
            property.FindPropertyRelative("Radius").floatValue = result.Radius;
        }

        private static void SetArray<T>(SerializedProperty property, IList<T> values) where T : Object
        {
            property.arraySize = values.Count;

            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
