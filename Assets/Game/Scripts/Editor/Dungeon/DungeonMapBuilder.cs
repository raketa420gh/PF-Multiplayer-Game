using System.Collections.Generic;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Editor.Dungeon
{
    /// Two 3x3 floors built from modules: outer walls and doorways, rooms furnished by DungeonRoomBuilder, the NavMesh and the minimaps.
    internal static class DungeonMapBuilder
    {
        public const float Module = 40f;
        public const int Grid = 3;
        public const float FloorDrop = -26f;
        public const float FloorRadius = Module * Grid * 0.75f;
        public const float TallHeight = DungeonPropBuilder.WallHeight * 2f;
        public const float PitDepth = 4f;
        public const string NavMeshPath = "Assets/Game/Scenes/DungeonScene/NavMesh.asset";

        // Rows run south to north, columns west to east.
        private static readonly DungeonRoom[,] s_floor1 =
        {
            { DungeonRoom.Armory, DungeonRoom.Treasury, DungeonRoom.Shrine },
            { DungeonRoom.Prison, DungeonRoom.GreatHall, DungeonRoom.Library },
            { DungeonRoom.Spawn, DungeonRoom.Hall, DungeonRoom.Spawn }
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

            DungeonFloorResult first = BuildFloor(root, spawns, "Floor1", s_floor1, 0f, 1, random);
            DungeonFloorResult second = BuildFloor(root, spawns, "Floor2", s_floor2, FloorDrop, 2, random);

            BakeNavMesh(root.gameObject, NavMeshPath);
            WriteLayouts(director, first, second);
            FloorMaps = new[] { DungeonMinimapBuilder.Render(root.Find("Floor1"), 0f, "Floor1"), DungeonMinimapBuilder.Render(root.Find("Floor2"), FloorDrop, "Floor2") };
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
            surface.BuildNavMesh();

            BattleEditorUtility.EnsureFolder(path.Substring(0, path.LastIndexOf('/')));
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        }

        private static bool IsTall(DungeonRoom room)
        {
            return room is DungeonRoom.Hall or DungeonRoom.GreatHall or DungeonRoom.Prison or DungeonRoom.Library or DungeonRoom.Cellar or DungeonRoom.Throne;
        }

        private static bool HasPit(DungeonRoom room)
        {
            return room is DungeonRoom.BonePit or DungeonRoom.TrapCorridor;
        }

        private static bool HasDoor(DungeonRoom room)
        {
            return room is DungeonRoom.Library or DungeonRoom.Armory or DungeonRoom.Prison or DungeonRoom.Treasury or DungeonRoom.Crypt or DungeonRoom.Cellar;
        }

        /// Walled-up doorways: the chapel is reached through the vault, the hoard through the throne room.
        private static bool IsBlocked(DungeonRoom a, DungeonRoom b)
        {
            return (a == DungeonRoom.Shrine && b == DungeonRoom.Library) || (a == DungeonRoom.TrapCorridor && b == DungeonRoom.Treasury);
        }

        private static string[] BuildModuleNames()
        {
            string[] names = new string[Grid * Grid * 2];

            for (int floor = 0; floor < 2; floor++)
            {
                DungeonRoom[,] rooms = floor == 0 ? s_floor1 : s_floor2;

                for (int z = 0; z < Grid; z++)
                {
                    for (int x = 0; x < Grid; x++)
                        names[floor * 9 + z * 3 + x] = RoomName(rooms[z, x], floor);
                }
            }

            return names;
        }

        private static string RoomName(DungeonRoom room, int floor)
        {
            return room switch
            {
                DungeonRoom.Spawn => "Pilgrim's Rest",
                DungeonRoom.Hall => "Entrance Hall",
                DungeonRoom.GreatHall => "Feast Hall",
                DungeonRoom.Prison => "Gaol",
                DungeonRoom.Library => "Dark Magic Library",
                DungeonRoom.Armory => "Barracks",
                DungeonRoom.Treasury => floor == 0 ? "Vault" : "Treasure Hoard",
                DungeonRoom.Shrine => floor == 0 ? "Pilgrims' Chapel" : "High Priest's Chapel",
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

        private static GameObject Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(name));
        }

        private static DungeonFloorResult BuildFloor(Transform root, Transform spawns, string name, DungeonRoom[,] rooms, float y, int floorIndex, System.Random random)
        {
            DungeonFloorResult result = new DungeonFloorResult { Center = new Vector3(0f, y, 0f) };
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
                    bool[] open = { IsOpen(rooms, x, z, 0, 1), IsOpen(rooms, x, z, 1, 0), IsOpen(rooms, x, z, 0, -1), IsOpen(rooms, x, z, -1, 0) };
                    new DungeonRoomBuilder(module, markers, result, room, floorIndex, open, random).Build();
                }
            }

            BuildEdges(floor, rooms);
            LinkLevers(result);

            return result;
        }

        /// Whether the wall towards the neighbour has a doorway; the room builder gets these in WallSide order.
        private static bool IsOpen(DungeonRoom[,] rooms, int x, int z, int dx, int dz)
        {
            int nx = x + dx;
            int nz = z + dz;

            if (nx < 0 || nz < 0 || nx >= Grid || nz >= Grid)
                return false;

            return dx + dz > 0 ? !IsBlocked(rooms[z, x], rooms[nz, nx]) : !IsBlocked(rooms[nz, nx], rooms[z, x]);
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
            property.FindPropertyRelative("Radius").floatValue = FloorRadius;
        }

        private static void SetArray<T>(SerializedProperty property, IList<T> values) where T : Object
        {
            property.arraySize = values.Count;

            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
