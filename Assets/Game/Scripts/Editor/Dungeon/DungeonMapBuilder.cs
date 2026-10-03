using System.Collections.Generic;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Editor.Dungeon
{
    /// Two 3x3 crypt floors built from modules: walls, doorways, props, lights, containers, portals, spawns and the NavMesh.
    internal static class DungeonMapBuilder
    {
        public const float Module = 40f;
        public const int Grid = 3;
        public const float FloorDrop = -26f;
        public const float FloorRadius = Module * Grid * 0.75f;
        // Room layouts are authored for this module size and spread to the real one by Spread().
        private const float LayoutModule = 14f;
        private const float WallHugDistance = 1.6f;
        private const float DoorLaneHalfWidth = 3.2f;
        private const float DoorLaneDepth = 4f;

        private enum Room
        {
            Spawn,
            Hall,
            Crypt,
            Library,
            Armory,
            Treasury,
            Shrine,
            TrapCorridor,
            Throne,
            BonePit,
            Arrival
        }

        private sealed class FloorResult
        {
            public readonly List<Transform> PlayerSpawns = new();
            public readonly List<Transform> MonsterSpawns = new();
            public readonly List<ContainerComponent> Containers = new();
            public readonly List<PortalComponent> EscapePortals = new();
            public readonly List<LeverComponent> Levers = new();
            public readonly List<TrapComponent> Traps = new();
            public PortalComponent DescendPortal;
            public Transform DescendDestination;
            public DoorComponent LockedDoor;
            public Transform BossSpawn;
            public Vector3 Center;
        }

        private sealed class Kit
        {
            public GameObject Wall;
            public GameObject WallHalf;
            public GameObject DoorFrame;
            public GameObject Pillar;
            public GameObject Floor;
            public GameObject Ceiling;
            public GameObject Brazier;
            public GameObject Torch;
            public GameObject Table;
            public GameObject Banner;
            public GameObject Skulls;
            public GameObject Rubble;
            public GameObject Sarcophagus;
            public GameObject Cobweb;
            public GameObject Chandelier;
            public GameObject Candles;
            public GameObject Chain;
            public GameObject Door;
            public GameObject Chest;
            public GameObject LargeChest;
            public GameObject GoldenChest;
            public GameObject Coffin;
            public GameObject Barrel;
            public GameObject Crate;
            public GameObject Bookshelf;
            public GameObject EscapePortal;
            public GameObject DescendPortal;
            public GameObject[] Shrines;
            public GameObject Lever;
            public GameObject SpikeTrap;
            public GameObject BladeTrap;
        }

        private static readonly Room[,] s_floor1 =
        {
            { Room.Armory, Room.Treasury, Room.Shrine },
            { Room.Crypt, Room.Throne, Room.Library },
            { Room.Spawn, Room.Hall, Room.Spawn }
        };

        private static readonly Room[,] s_floor2 =
        {
            { Room.Treasury, Room.Crypt, Room.Shrine },
            { Room.TrapCorridor, Room.Throne, Room.Library },
            { Room.Arrival, Room.BonePit, Room.Crypt }
        };

        private static System.Random s_random;

        public static Texture2D[] FloorMaps { get; private set; }
        public static string[] ModuleNames { get; private set; }

        public static Transform Build(DungeonDirector director)
        {
            s_random = new System.Random(2024);
            Kit kit = BuildKit();
            Transform root = new GameObject("[Dungeon]").transform;
            Transform spawns = new GameObject("[Spawns]").transform;

            FloorResult first = BuildFloor(kit, root, spawns, "Floor1", s_floor1, 0f, 1);
            FloorResult second = BuildFloor(kit, root, spawns, "Floor2", s_floor2, FloorDrop, 2);

            BakeNavMesh(root.gameObject);
            WriteLayouts(director, first, second);
            FloorMaps = new[] { DungeonMinimapBuilder.Render(root.Find("Floor1"), 0f, "Floor1"), DungeonMinimapBuilder.Render(root.Find("Floor2"), FloorDrop, "Floor2") };
            ModuleNames = BuildModuleNames();

            return root;
        }

        private static string[] BuildModuleNames()
        {
            string[] names = new string[Grid * Grid * 2];

            for (int floor = 0; floor < 2; floor++)
            {
                Room[,] rooms = floor == 0 ? s_floor1 : s_floor2;

                for (int z = 0; z < Grid; z++)
                {
                    for (int x = 0; x < Grid; x++)
                        names[floor * 9 + z * 3 + x] = RoomName(rooms[z, x], floor);
                }
            }

            return names;
        }

        private static string RoomName(Room room, int floor)
        {
            return room switch
            {
                Room.Spawn => "Pilgrim's Rest",
                Room.Hall => "Entrance Hall",
                Room.Crypt => floor == 0 ? "Old Tomb" : "Howling Crypt",
                Room.Library => floor == 0 ? "Dark Magic Library" : "Forbidden Archive",
                Room.Armory => "Barracks",
                Room.Treasury => floor == 0 ? "Vault" : "Treasure Hoard",
                Room.Shrine => "High Priest's Chapel",
                Room.TrapCorridor => "Death Hall",
                Room.Throne => floor == 0 ? "Great Hall" : "Ritual Room",
                Room.BonePit => "Bone Pit",
                Room.Arrival => "Descent",
                _ => room.ToString()
            };
        }

        private static Kit BuildKit()
        {
            Kit kit = new Kit
            {
                Wall = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Wall(Module, "Wall" + Module), "Wall"),
                WallHalf = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Wall(Module * 0.5f, "WallHalf"), "WallHalf"),
                DoorFrame = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.DoorFrame(Module), "DoorFrame"),
                Pillar = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Pillar(), "Pillar"),
                Floor = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Floor(Module), "FloorTile"),
                Ceiling = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Ceiling(Module), "CeilingTile"),
                Brazier = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Brazier(), "Brazier"),
                Torch = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.WallTorch(), "WallTorch"),
                Table = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Table(), "Table"),
                Banner = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Banner(), "Banner"),
                Skulls = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.SkullPile(), "SkullPile"),
                Rubble = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Rubble(), "Rubble"),
                Sarcophagus = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Sarcophagus(), "Sarcophagus"),
                Cobweb = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Cobweb(), "Cobweb"),
                Chandelier = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Chandelier(), "Chandelier"),
                Candles = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.CandleCluster(), "CandleCluster"),
                Chain = DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Chain(), "Chain"),
                Door = Load("Door"),
                Chest = Load("SmallOakChest"),
                LargeChest = Load("LargeOakChest"),
                GoldenChest = Load("GoldenChest"),
                Coffin = Load("Coffin"),
                Barrel = Load("Barrel"),
                Crate = Load("Crate"),
                Bookshelf = Load("Bookshelf"),
                EscapePortal = Load("EscapePortal"),
                DescendPortal = Load("DescendPortal"),
                Shrines = new[] { Load("ShrineHealth"), Load("ShrineProtection"), Load("ShrinePower"), Load("ShrineSpeed") },
                Lever = Load("Lever"),
                SpikeTrap = Load("SpikeTrap"),
                BladeTrap = Load("BladeTrap")
            };

            return kit;
        }

        private static GameObject Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(name));
        }

        private static FloorResult BuildFloor(Kit kit, Transform root, Transform spawns, string name, Room[,] rooms, float y, int floorIndex)
        {
            FloorResult result = new FloorResult { Center = new Vector3(0f, y, 0f) };
            Transform floor = new GameObject(name).transform;
            floor.SetParent(root, false);
            floor.localPosition = new Vector3(0f, y, 0f);
            Transform markers = new GameObject(name).transform;
            markers.SetParent(spawns, false);

            for (int x = 0; x < Grid; x++)
            {
                for (int z = 0; z < Grid; z++)
                {
                    Vector3 center = ModuleCenter(x, z);
                    Transform module = new GameObject($"Module_{x}_{z}_{rooms[z, x]}").transform;
                    module.SetParent(floor, false);
                    module.localPosition = center;
                    Place(kit.Floor, module, Vector3.zero, 0f);
                    Place(kit.Ceiling, module, new Vector3(0f, DungeonPropBuilder.WallHeight, 0f), 0f);
                    Decorate(kit, module, markers, rooms[z, x], x, z, result, floorIndex);
                }
            }

            BuildEdges(kit, floor, rooms, result);
            LinkLevers(result);

            return result;
        }

        private static void LinkLevers(FloorResult result)
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

        private static void BuildEdges(Kit kit, Transform floor, Room[,] rooms, FloorResult result)
        {
            Transform edges = new GameObject("Edges").transform;
            edges.SetParent(floor, false);
            float half = Module * 0.5f;

            for (int x = 0; x < Grid; x++)
            {
                for (int z = 0; z < Grid; z++)
                {
                    Vector3 center = ModuleCenter(x, z);

                    if (z == 0)
                        Place(kit.Wall, edges, center + new Vector3(0f, 0f, -half), 0f);

                    if (x == 0)
                        Place(kit.Wall, edges, center + new Vector3(-half, 0f, 0f), 90f);

                    if (z == Grid - 1)
                        Place(kit.Wall, edges, center + new Vector3(0f, 0f, half), 0f);
                    else
                        Opening(kit, edges, center + new Vector3(0f, 0f, half), 0f, rooms[z, x], rooms[z + 1, x], result);

                    if (x == Grid - 1)
                        Place(kit.Wall, edges, center + new Vector3(half, 0f, 0f), 90f);
                    else
                        Opening(kit, edges, center + new Vector3(half, 0f, 0f), 90f, rooms[z, x], rooms[z, x + 1], result);
                }
            }
        }

        private static void Opening(Kit kit, Transform parent, Vector3 position, float yaw, Room a, Room b, FloorResult result)
        {
            bool isBlocked = (a == Room.Armory && b == Room.Crypt) || (a == Room.Shrine && b == Room.Library) || (a == Room.TrapCorridor && b == Room.Treasury);

            if (isBlocked)
            {
                Place(kit.Wall, parent, position, yaw);

                return;
            }

            Place(kit.DoorFrame, parent, position, yaw);
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Place(kit.Torch, parent, position + rotation * new Vector3(-2.1f, 2.6f, -0.35f), yaw + 180f);
            Place(kit.Torch, parent, position + rotation * new Vector3(2.1f, 2.6f, 0.35f), yaw);

            bool hasDoor = a is Room.Crypt or Room.Library or Room.Treasury or Room.Armory || b is Room.Crypt or Room.Library or Room.Treasury or Room.Armory;

            if (!hasDoor)
                return;

            DoorComponent door = Place(kit.Door, parent, position, yaw, false).GetComponent<DoorComponent>();
            bool isTreasury = a == Room.Treasury || b == Room.Treasury;

            if (!isTreasury || result.LockedDoor != null)
                return;

            BattleEditorUtility.Set(door, "_startsLocked", true);
            result.LockedDoor = door;
        }

        private static void Decorate(Kit kit, Transform module, Transform markers, Room room, int x, int z, FloorResult result, int floorIndex)
        {
            float half = LayoutModule * 0.5f;
            WallTorches(kit, module);
            Dress(kit, module, room);

            switch (room)
            {
                case Room.Spawn:
                    for (int i = 0; i < 4; i++)
                        result.PlayerSpawns.Add(Marker(markers, "Player", module.position + Spread(-3f + i * 2f, 0f, -3f), 0f));

                    Place(kit.Banner, module, Spread(-4f, 3.6f, half - 0.4f), 180f);
                    Place(kit.Banner, module, Spread(4f, 3.6f, half - 0.4f), 180f);
                    Place(kit.Table, module, Spread(0f, 0f, 3.5f), 0f);
                    Container(kit.Chest, module, result, Spread(5f, 0f, 4.5f), -90f);
                    Container(kit.Barrel, module, result, Spread(-5.5f, 0f, 5f), 0f);
                    Container(kit.Crate, module, result, Spread(-5.5f, 0f, 3.8f), 20f);
                    Pillars(kit, module, 4.5f);
                    break;

                case Room.Arrival:
                    result.DescendDestination = Marker(markers, "Arrival", module.position + Spread(0f, 0f, -2f), 0f);
                    Place(kit.Brazier, module, Spread(0f, 0f, 3f), 0f);
                    Place(kit.Skulls, module, Spread(-5f, 0f, -5f), 30f);
                    Container(kit.Chest, module, result, Spread(5.5f, 0f, -3f), -90f);
                    Pillars(kit, module, 4.5f);
                    break;

                case Room.Hall:
                    Place(kit.Brazier, module, Spread(-4f, 0f, 0f), 0f);
                    Place(kit.Brazier, module, Spread(4f, 0f, 0f), 0f);
                    Place(kit.Rubble, module, Spread(-5f, 0f, 5f), 0f);
                    Container(kit.Barrel, module, result, Spread(5.5f, 0f, -5.5f), 0f);
                    Container(kit.Crate, module, result, Spread(5.5f, 0f, -4.4f), 40f);
                    Monster(markers, result, module.position + Spread(0f, 0f, 3f), 180f);
                    Monster(markers, result, module.position + Spread(2f, 0f, -3f), 0f);
                    Pillars(kit, module, 5f);
                    break;

                case Room.Crypt:
                    for (int i = 0; i < 3; i++)
                    {
                        Container(kit.Coffin, module, result, Spread(-4.5f, 0f, -4f + i * 4f), 90f);
                        Place(kit.Sarcophagus, module, Spread(4.5f, 0f, -4f + i * 4f), 90f);
                    }

                    Place(kit.Skulls, module, Spread(0f, 0f, 5.5f), 0f);
                    Place(kit.Brazier, module, Spread(0f, 0f, 0f), 0f);
                    Monster(markers, result, module.position + Spread(-1.5f, 0f, -2f), 0f);
                    Monster(markers, result, module.position + Spread(2f, 0f, 3f), 180f);
                    break;

                case Room.Library:
                    for (int i = 0; i < 3; i++)
                    {
                        Container(kit.Bookshelf, module, result, Spread(-6.4f, 0f, -4f + i * 4f), 90f);
                        Container(kit.Bookshelf, module, result, Spread(6.4f, 0f, -4f + i * 4f), -90f);
                    }

                    Place(kit.Table, module, Spread(0f, 0f, 0f), 90f);
                    Place(kit.Table, module, Spread(0f, 0f, -4f), 90f);
                    Container(kit.Chest, module, result, Spread(0f, 0f, 5.5f), 180f);
                    Monster(markers, result, module.position + Spread(3f, 0f, 2f), 180f);
                    break;

                case Room.Armory:
                    Container(kit.LargeChest, module, result, Spread(0f, 0f, 5.5f), 180f);
                    Container(kit.Crate, module, result, Spread(-5.5f, 0f, -5.5f), 0f);
                    Container(kit.Crate, module, result, Spread(-4.5f, 0f, -5.5f), 15f);
                    Container(kit.Barrel, module, result, Spread(5.5f, 0f, -5.5f), 0f);
                    Container(kit.Barrel, module, result, Spread(5.5f, 0f, -4.4f), 0f);
                    Place(kit.Table, module, Spread(-4f, 0f, 1f), 90f);
                    Place(kit.Banner, module, Spread(0f, 3.6f, -half + 0.4f), 0f);
                    result.Levers.Add(Place(kit.Lever, module, Spread(6.3f, 0f, 3f), -90f, false).GetComponent<LeverComponent>());
                    Monster(markers, result, module.position + Spread(2f, 0f, 1f), 90f);
                    Monster(markers, result, module.position + Spread(-2f, 0f, -2f), 0f);
                    break;

                case Room.Treasury:
                    Container(kit.GoldenChest, module, result, Spread(0f, 0f, 5.2f), 180f);
                    Container(kit.Chest, module, result, Spread(-5.5f, 0f, 5.2f), 90f);
                    Container(kit.Chest, module, result, Spread(5.5f, 0f, 5.2f), -90f);
                    Place(kit.Brazier, module, Spread(-3f, 0f, 3f), 0f);
                    Place(kit.Brazier, module, Spread(3f, 0f, 3f), 0f);
                    Place(kit.SpikeTrap, module, Spread(0f, 0f, 2f), 0f, false);
                    Place(kit.Banner, module, Spread(-3f, 3.6f, half - 0.4f), 180f);
                    Place(kit.Banner, module, Spread(3f, 3.6f, half - 0.4f), 180f);

                    if (floorIndex == 1)
                        result.DescendPortal = Place(kit.DescendPortal, module, Spread(0f, 0f, -3f), 180f, false).GetComponent<PortalComponent>();
                    else
                        result.EscapePortals.Add(Place(kit.EscapePortal, module, Spread(0f, 0f, -3f), 180f, false).GetComponent<PortalComponent>());

                    Monster(markers, result, module.position + Spread(-3f, 0f, -1f), 90f);
                    Monster(markers, result, module.position + Spread(3f, 0f, -1f), -90f);
                    Monster(markers, result, module.position + Spread(0f, 0f, 4f), 180f);
                    break;

                case Room.Shrine:
                    Place(kit.Shrines[(x + z + floorIndex) % kit.Shrines.Length], module, Spread(0f, 0f, 5.5f), 180f, false);
                    Place(kit.Shrines[(x + z + floorIndex + 2) % kit.Shrines.Length], module, Spread(-5.8f, 0f, 0f), 90f, false);
                    Place(kit.Skulls, module, Spread(5f, 0f, 5f), 0f);
                    Container(kit.Chest, module, result, Spread(5.5f, 0f, -2f), -90f);
                    result.EscapePortals.Add(Place(kit.EscapePortal, module, Spread(2f, 0f, -4.5f), 0f, false).GetComponent<PortalComponent>());
                    Monster(markers, result, module.position + Spread(-2f, 0f, -3f), 0f);
                    Pillars(kit, module, 4.5f);
                    break;

                case Room.TrapCorridor:
                    result.Traps.Add(Place(kit.BladeTrap, module, Spread(0f, 0f, -3.5f), 90f, false).GetComponent<TrapComponent>());
                    result.Traps.Add(Place(kit.BladeTrap, module, Spread(0f, 0f, 3.5f), 90f, false).GetComponent<TrapComponent>());
                    Place(kit.SpikeTrap, module, Spread(0f, 0f, 0f), 0f, false);
                    Place(kit.Rubble, module, Spread(-4.5f, 0f, -4.5f), 0f);
                    Place(kit.Rubble, module, Spread(4.5f, 0f, 4.5f), 70f);
                    Container(kit.LargeChest, module, result, Spread(5.5f, 0f, -5.5f), -90f);
                    result.Levers.Add(Place(kit.Lever, module, Spread(-6.3f, 0f, 4f), 90f, false).GetComponent<LeverComponent>());
                    Monster(markers, result, module.position + Spread(-3f, 0f, 2f), 90f);
                    break;

                case Room.Throne:
                    Place(kit.Brazier, module, Spread(-4f, 0f, 4f), 0f);
                    Place(kit.Brazier, module, Spread(4f, 0f, 4f), 0f);
                    Place(kit.Brazier, module, Spread(-4f, 0f, -4f), 0f);
                    Place(kit.Brazier, module, Spread(4f, 0f, -4f), 0f);
                    Place(kit.Table, module, Spread(0f, 0f, 0f), 0f);
                    Place(kit.Banner, module, Spread(-3f, 3.6f, half - 0.4f), 180f);
                    Place(kit.Banner, module, Spread(3f, 3.6f, half - 0.4f), 180f);
                    Container(kit.LargeChest, module, result, Spread(0f, 0f, 5.5f), 180f);

                    if (floorIndex == 2)
                    {
                        result.BossSpawn = Marker(markers, "Boss", module.position + Spread(0f, 0f, 3f), 180f);
                        Monster(markers, result, module.position + Spread(-4f, 0f, 2f), 135f);
                        Monster(markers, result, module.position + Spread(4f, 0f, 2f), -135f);
                        result.EscapePortals.Add(Place(kit.EscapePortal, module, Spread(-5f, 0f, 0f), 90f, false).GetComponent<PortalComponent>());
                    }
                    else
                    {
                        Monster(markers, result, module.position + Spread(-2f, 0f, 2f), 135f);
                        Monster(markers, result, module.position + Spread(2f, 0f, -2f), -45f);
                        Monster(markers, result, module.position + Spread(0f, 0f, -5f), 0f);
                    }
                    break;

                case Room.BonePit:
                    for (int i = 0; i < 5; i++)
                        Place(kit.Skulls, module, Spread(-4f + i * 2f, 0f, (i % 2 == 0 ? -3f : 3f)), i * 40f);

                    Place(kit.Rubble, module, Spread(0f, 0f, 0f), 0f);
                    Container(kit.Coffin, module, result, Spread(5.5f, 0f, 0f), 0f);
                    Container(kit.Chest, module, result, Spread(-5.5f, 0f, -5f), 90f);
                    Monster(markers, result, module.position + Spread(-2f, 0f, 0f), 90f);
                    Monster(markers, result, module.position + Spread(2f, 0f, 1f), -90f);
                    Monster(markers, result, module.position + Spread(0f, 0f, -4f), 0f);
                    break;
            }
        }

        /// Cobwebs in two corners, hanging chains or a chandelier, candle wax on the floor: the mood layer of every module.
        private static void Dress(Kit kit, Transform module, Room room)
        {
            float half = Module * 0.5f;
            float corner = half - 0.55f;
            int first = s_random.Next(4);
            int second = (first + 1 + s_random.Next(3)) % 4;

            for (int i = 0; i < 4; i++)
            {
                if (i != first && i != second)
                    continue;

                float sx = i % 2 == 0 ? -1f : 1f;
                float sz = i < 2 ? -1f : 1f;
                float yaw = sx > 0f == sz > 0f ? 135f : 45f;
                Place(kit.Cobweb, module, new Vector3(sx * corner, DungeonPropBuilder.WallHeight - 0.75f, sz * corner), sx > 0f ? yaw + 180f : yaw);
            }

            if (room is Room.Hall or Room.Throne or Room.Library or Room.Spawn or Room.Shrine)
                Place(kit.Chandelier, module, new Vector3(0f, DungeonPropBuilder.WallHeight - 2f, 0f), 0f);

            float quarter = Module * 0.25f;
            Place(kit.Chandelier, module, new Vector3(-quarter, DungeonPropBuilder.WallHeight - 2f, -quarter), 0f);
            Place(kit.Chandelier, module, new Vector3(quarter, DungeonPropBuilder.WallHeight - 2f, quarter), 0f);

            if (room is Room.Crypt or Room.BonePit or Room.Armory or Room.TrapCorridor or Room.Arrival)
            {
                Place(kit.Chain, module, new Vector3(-2.5f, DungeonPropBuilder.WallHeight, 2f), 0f);
                Place(kit.Chain, module, new Vector3(3f, DungeonPropBuilder.WallHeight, -1.5f), 30f);
            }

            int candles = room is Room.Crypt or Room.Shrine or Room.BonePit or Room.Throne ? 3 : 1;

            for (int i = 0; i < candles; i++)
            {
                float x = ((float)s_random.NextDouble() - 0.5f) * (Module - 2.5f);
                float z = (s_random.Next(2) == 0 ? -1f : 1f) * (half - 0.9f - (float)s_random.NextDouble() * 0.6f);
                Place(kit.Candles, module, new Vector3(x, 0f, z), s_random.Next(360));
            }
        }

        /// Two torches on every wall, between the corners and the doorway, so the large rooms keep readable light.
        private static void WallTorches(Kit kit, Transform module)
        {
            float wall = Module * 0.5f - 0.35f;
            float offset = Module * 0.28f;

            for (int side = -1; side <= 1; side += 2)
            {
                Place(kit.Torch, module, new Vector3(-offset * side, 2.6f, wall * side), side > 0 ? 180f : 0f);
                Place(kit.Torch, module, new Vector3(offset * side, 2.6f, wall * side), side > 0 ? 180f : 0f);
                Place(kit.Torch, module, new Vector3(wall * side, 2.6f, -offset * side), side > 0 ? -90f : 90f);
                Place(kit.Torch, module, new Vector3(wall * side, 2.6f, offset * side), side > 0 ? -90f : 90f);
            }
        }

        private static void Pillars(Kit kit, Transform module, float offset)
        {
            Place(kit.Pillar, module, Spread(-offset, 0f, -offset), 0f);
            Place(kit.Pillar, module, Spread(offset, 0f, -offset), 0f);
            Place(kit.Pillar, module, Spread(-offset, 0f, offset), 0f);
            Place(kit.Pillar, module, Spread(offset, 0f, offset), 0f);
        }

        /// Maps a layout point to the real module: wall-mounted props keep their distance to the wall, the rest scale with
        /// the room; anything left in front of a doorway is moved aside along the wall.
        private static Vector3 Spread(float x, float y, float z)
        {
            float half = Module * 0.5f;
            Vector3 point = new Vector3(SpreadAxis(x), y, SpreadAxis(z));

            if (Mathf.Abs(point.x) < DoorLaneHalfWidth && Mathf.Abs(point.z) > half - DoorLaneDepth)
                point.x = point.x < 0f ? -DoorLaneHalfWidth : DoorLaneHalfWidth;

            if (Mathf.Abs(point.z) < DoorLaneHalfWidth && Mathf.Abs(point.x) > half - DoorLaneDepth)
                point.z = point.z < 0f ? -DoorLaneHalfWidth : DoorLaneHalfWidth;

            return point;
        }

        private static float SpreadAxis(float value)
        {
            float toWall = LayoutModule * 0.5f - Mathf.Abs(value);

            return toWall < WallHugDistance ? Mathf.Sign(value) * (Module * 0.5f - toWall) : value * Module / LayoutModule;
        }

        private static void Container(GameObject prefab, Transform module, FloorResult result, Vector3 position, float yaw)
        {
            GameObject instance = Place(prefab, module, position, yaw, false);
            result.Containers.Add(instance.GetComponent<ContainerComponent>());
        }

        private static void Monster(Transform markers, FloorResult result, Vector3 position, float yaw)
        {
            result.MonsterSpawns.Add(Marker(markers, "Monster", position, yaw));
        }

        private static Transform Marker(Transform parent, string name, Vector3 position, float yaw)
        {
            Transform marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.position = position;
            marker.rotation = Quaternion.Euler(0f, yaw, 0f);

            return marker;
        }

        private static GameObject Place(GameObject prefab, Transform parent, Vector3 localPosition, float yaw, bool isStatic = true)
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

        private static void BakeNavMesh(GameObject root)
        {
            NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1;
            NavMeshBuildSettings settings = surface.GetBuildSettings();
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.12f;
            surface.BuildNavMesh();

            BattleEditorUtility.EnsureFolder("Assets/Game/Scenes/DungeonScene");
            string path = "Assets/Game/Scenes/DungeonScene/NavMesh.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        }

        private static void WriteLayouts(DungeonDirector director, FloorResult first, FloorResult second)
        {
            SerializedObject so = new SerializedObject(director);
            SerializedProperty floors = so.FindProperty("_floors");
            floors.arraySize = 2;
            WriteFloor(floors.GetArrayElementAtIndex(0), first, 1);
            WriteFloor(floors.GetArrayElementAtIndex(1), second, 2);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteFloor(SerializedProperty property, FloorResult result, int index)
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
