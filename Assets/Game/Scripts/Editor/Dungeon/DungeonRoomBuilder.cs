using System.Collections.Generic;
using Game.Scripts.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Furnishes one module. Coordinates are module-local metres: X runs east, Z north, doorways sit in the middle of the walls.
    /// Ledges are either a single step (0.25) or taller than a NavMesh agent climbs (1+), everything between is reached by stairs.
    internal sealed class DungeonRoomBuilder
    {
        public const float Upper = 3.2f;

        private const float Half = DungeonMapBuilder.Module * 0.5f;
        private const float Inner = Half - DungeonPropBuilder.WallThickness * 0.5f;
        // Distance between the pilasters of the outer walls; wall-hung props go into the bays between them.
        private const float Bay = DungeonMapBuilder.Module / 11f;
        private const float Slab = 0.3f;
        private const float TableTop = 0.82f;
        private const float ChainLength = 2.35f;
        private const float Height = DungeonPropBuilder.WallHeight;
        private const WallSide North = WallSide.North;
        private const WallSide East = WallSide.East;
        private const WallSide South = WallSide.South;
        private const WallSide West = WallSide.West;

        private static readonly WallSide[] s_sides = { North, East, South, West };
        private static readonly float[] s_shelves = { 0.76f, 1.16f, 1.54f, 1.92f };
        private static readonly string[] s_tableware =
        {
            "Mug", "Table_Plate", "Table_Fork", "Table_Knife", "Table_Spoon", "Bottle_1", "Chalice", "CandleStick", "Pot_1", "Carrot", "SmallBottle", "Mug",
            "Table_Plate"
        };
        private static readonly string[] s_shelfItems = { "BookGroup_Small_1", "BookGroup_Small_2", "BookGroup_Small_3", "Book_Stack_1", "Book_Stack_2", "Potion_1", "Potion_4" };
        private static readonly Dictionary<string, GameObject> s_prefabs = new();

        private readonly Transform _module;
        private readonly Transform _markers;
        private readonly DungeonFloorResult _result;
        private readonly DungeonRoom _room;
        private readonly int _floor;
        private readonly bool[] _open;
        private readonly System.Random _random;
        private readonly float _ceiling;
        private readonly float _run = DungeonStructureBuilder.StairRun(Upper);

        public DungeonRoomBuilder(Transform module, Transform markers, DungeonFloorResult result, DungeonRoom room, int floor, bool[] open, System.Random random)
        {
            _module = module;
            _markers = markers;
            _result = result;
            _room = room;
            _floor = floor;
            _open = open;
            _random = random;
            _ceiling = DungeonMapBuilder.CeilingHeight(room);
        }

        public void Build()
        {
            WallLights();

            switch (_room)
            {
                case DungeonRoom.Spawn:
                    BuildSpawn();
                    break;
                case DungeonRoom.Hall:
                    BuildHall();
                    break;
                case DungeonRoom.GreatHall:
                    BuildGreatHall();
                    break;
                case DungeonRoom.Prison:
                    BuildPrison();
                    break;
                case DungeonRoom.Library:
                    BuildLibrary();
                    break;
                case DungeonRoom.Armory:
                    BuildArmory();
                    break;
                case DungeonRoom.Treasury:
                    BuildTreasury();
                    break;
                case DungeonRoom.Shrine:
                    BuildShrine();
                    break;
                case DungeonRoom.Arrival:
                    BuildArrival();
                    break;
                case DungeonRoom.BonePit:
                    BuildBonePit();
                    break;
                case DungeonRoom.Cellar:
                    BuildCellar();
                    break;
                case DungeonRoom.TrapCorridor:
                    BuildTrapCorridor();
                    break;
                case DungeonRoom.Labyrinth:
                    BuildLabyrinth();
                    break;
                case DungeonRoom.Crypt:
                    BuildCrypt();
                    break;
                case DungeonRoom.Throne:
                    BuildThrone();
                    break;
            }

            Dress();
        }

        /// Camp of the pilgrims: a fire ring, a dormitory under the north wall, a mess corner and supplies.
        private void BuildSpawn()
        {
            float s = IsOpen(West) ? 1f : -1f;
            WallSide closed = s > 0f ? East : West;

            for (int i = 0; i < 4; i++)
                _result.PlayerSpawns.Add(Marker("Player", -4.5f + i * 3f, 0f, -7f, 0f));

            Put("Brazier", 0f, 0f, 0f);
            Kit("Bench", 0f, 0f, 3f);
            Kit("Bench", 0f, 0f, -3f);
            Kit("Bench", 3f, 0f, 0f, 90f);
            Kit("Bench", -3f, 0f, 0f, 90f);

            foreach (int x in new[] { -1, 1 })
            {
                foreach (int z in new[] { -1, 1 })
                    Put("Pillar", x * 10f, 0f, z * 10f);
            }

            for (int i = 0; i < 6; i++)
            {
                float x = (i < 3 ? -15.5f : 6.5f) + i % 3 * 4.5f;
                Kit(i % 2 == 0 ? "Bed_Twin1" : "Bed_Twin2", AtWall(North, x, 1.6f), 180f);
                Kit("Nightstand_Shelf", AtWall(North, x + 1.7f, 0.55f), 180f);
                Kit(Pick("Candle_2", "Mug", "Book_5", "Bottle_1"), AtWall(North, x + 1.7f, 0.55f, 1.2f), Range(0f, 360f));
            }

            Put("StandPeasantMale", -2.2f, 0f, 18.6f, 180f);
            Put("StandPeasantFemale", 2.2f, 0f, 18.6f, 180f);
            Banner(North, 0, 4.2f, 1.4f, true);
            Mount("Peg_Rack", North, -1, 1.9f);
            Mount("Shield_Wooden", North, 1, 2f);

            Table(s * 11f, -9f, 90f, 4, Tableware(6));
            Table(s * 11f, -14f, 90f, 4, Tableware(5));
            Kit("Barrel_Holder", AtWall(closed, -3f, 0.75f), Facing(closed));
            Kit("Barrel_Holder", AtWall(closed, -4.6f, 0.75f), Facing(closed));
            Kit("Barrel_Apples", AtWall(closed, -6.2f, 0.8f));
            Kit("Bag", AtWall(closed, 3.4f, 0.8f), 40f);
            Kit("FarmCrate_Carrot", AtWall(closed, 4.6f, 0.8f), Facing(closed) + 15f);
            Kit("FarmCrate_Apple", AtWall(closed, 5.8f, 0.8f), Facing(closed) - 10f);
            Loot("SmallOakChest", AtWall(closed, 8.5f, 0.8f), Facing(closed));
            Loot("Barrel", AtWall(closed, -16.2f, 0.8f), 0f);
            Loot("Crate", AtWall(closed, -17.6f, 0.9f), 20f);
            Kit("Stall_Cart_Empty", s * 13f, 0f, 9f, s > 0f ? 200f : -20f);
            Kit("Bag", s * 11.2f, 0f, 10.4f, 80f);
            Kit("WeaponStand", AtWall(South, s * 12f, 1f), 0f);
            Mount("Sword_Bronze", South, s > 0f ? 4 : -4, 1.5f, 1f, 0.06f);
            Mount("Axe_Bronze", South, s > 0f ? 2 : -2, 1.7f, 1f, 0.06f);
            Crates(-s * 15f, -15f, 3);
            Kit("Rope_2", -s * 13f, 0f, -16.5f, 30f);
            Kit("Bucket_Wooden_1", 2.4f, 0f, 2.6f, 10f);
        }

        /// Entrance hall: a grand staircase to the gallery under the north wall, statues and pillars.
        private void BuildHall()
        {
            Material stone = DungeonPropBuilder.Cobble;
            const float edge = 16.5f;
            const float landing = 13f;
            Deck(-Inner, Inner, edge, Inner, Upper, stone);
            Deck(-5f, 5f, landing, edge, Upper, stone);
            Stairs(0f, 0f, landing - _run, 0f, 6f, Upper, stone);
            Rail(-Inner, edge, -5f, edge, Upper);
            Rail(-5f, edge, -5f, landing, Upper);
            Rail(-5f, landing, -3f, landing, Upper);
            Rail(3f, landing, 5f, landing, Upper);
            Rail(5f, landing, 5f, edge, Upper);
            Rail(5f, edge, Inner, edge, Upper);

            foreach (float x in new[] { -18f, -11.5f, 11.5f, 18f })
                Post(x, edge + 0.2f, Upper - Slab);

            Post(-4.8f, landing + 0.2f, Upper - Slab);
            Post(4.8f, landing + 0.2f, Upper - Slab);

            foreach (float z in new[] { -12f, -4f, 4f })
            {
                TallPillar(-8.5f, z);
                TallPillar(8.5f, z);
            }

            DungeonStructureBuilder.Carpet(_module, new Vector3(0f, 0f, -Inner), new Vector3(0f, 0f, landing - _run), 3f);
            Put("StatueGuardian", -4.6f, 0f, 6.4f, 180f);
            Put("StatueGuardian", 4.6f, 0f, 6.4f, 180f);
            Put("Brazier", -5f, 0f, -8f);
            Put("Brazier", 5f, 0f, -8f);
            Put("Rubble", -15f, 0f, -15f);
            Kit("Vase_Rubble_Medium", -13.4f, 0f, -14.2f, 50f);

            // Stores under the gallery.
            Kit("Barrel_Holder", AtWall(North, -14f, 0.75f), 180f);
            Kit("Barrel", AtWall(North, -11.8f, 0.8f), 20f);
            Crates(12f, 18.4f, 3);
            Kit("Bag", 15.2f, 0f, 18.3f, 120f);
            Loot("SmallOakChest", 0f, 0f, 18.8f, 180f);
            Loot("Barrel", 16.5f, 0f, -16.5f, 0f);
            Loot("Crate", 16.5f, 0f, -15.2f, 40f);

            // Gallery.
            Loot("LargeOakChest", -17.8f, Upper, 18.4f, 90f);
            Loot("SmallOakChest", 17.8f, Upper, 18.4f, -90f);
            Kit("Crate_Wooden", -14.6f, Upper, 18.8f, 12f);
            Kit("Vase_2", 13.6f, Upper, 18.8f);
            Kit("CandleStick_Stand", -7f, Upper, 18.8f);
            Kit("CandleStick_Stand", 7f, Upper, 18.8f);
            Banner(North, 0, 8.2f, 2f, true);

            foreach (int bay in new[] { -4, -2, 2, 4 })
                Banner(North, bay, 7.8f, 1.5f, bay % 4 == 0);

            Monster(-12f, Upper, 18f, 180f);
            Monster(12f, Upper, 18f, 180f);
            Monster(0f, 0f, 2f, 180f);
            Monster(3f, 0f, -6f, 0f);
        }

        /// Feast hall ringed by a wooden gallery: long tables below, archers' walk above.
        private void BuildGreatHall()
        {
            Material wood = DungeonPropBuilder.WoodPlanks;
            const float edge = 16.5f;
            Deck(-Inner, Inner, edge, Inner, Upper, wood);
            Deck(-Inner, Inner, -Inner, -edge, Upper, wood);
            Deck(-Inner, -edge, -edge, edge, Upper, wood);
            Deck(edge, Inner, -edge, edge, Upper, wood);
            Stairs(-edge + _run, 0f, 9f, -90f, 2.6f, Upper, wood);
            Stairs(edge - _run, 0f, -9f, 90f, 2.6f, Upper, wood);
            Rail(-edge, edge, edge, edge, Upper);
            Rail(-edge, -edge, edge, -edge, Upper);
            Rail(-edge, -edge, -edge, 7.7f, Upper);
            Rail(-edge, 10.3f, -edge, edge, Upper);
            Rail(edge, -edge, edge, -10.3f, Upper);
            Rail(edge, -7.7f, edge, edge, Upper);

            foreach (float a in new[] { -16.3f, -5.5f, 5.5f, 16.3f })
            {
                Post(a, 16.3f, Upper - Slab);
                Post(a, -16.3f, Upper - Slab);

                if (Mathf.Abs(a) > 10f)
                    continue;

                Post(-16.3f, a, Upper - Slab);
                Post(16.3f, a, Upper - Slab);
            }

            foreach (float x in new[] { -6f, 6f })
            {
                foreach (float z in new[] { -7.4f, -4.5f, 4.5f, 7.4f })
                {
                    Table(x, z, 90f, 0, Tableware(5));
                    Kit("Bench", x - 1.05f, 0f, z, 90f);
                    Kit("Bench", x + 1.05f, 0f, z, 90f);
                }
            }

            DungeonStructureBuilder.Carpet(_module, new Vector3(0f, 0f, -16f), new Vector3(0f, 0f, 16f), 2.4f);
            Put("StatueGuardian", -12.5f, 0f, -12.5f, 45f);
            Put("StatueMage", 12.5f, 0f, 12.5f, -135f);
            Put("Brazier", -12.5f, 0f, 13.5f);
            Put("Brazier", 12.5f, 0f, -13.5f);
            Kit("Barrel_Holder", 13f, 0f, -17.6f, 0f);
            Kit("Barrel_Apples", 10.8f, 0f, -17.8f);
            Kit("Cauldron", -13f, 0f, 17.6f);
            Loot("Barrel", -10.8f, 0f, 17.8f, 0f);

            Loot("SmallOakChest", 18.2f, Upper, 18.2f, -135f);
            Loot("LargeOakChest", -18.2f, Upper, -18.2f, 45f);
            Loot("SmallOakChest", -3.6f, Upper, 18.9f, 180f);
            Crates(18.3f, -17.6f, 2, Upper);
            Kit("Barrel", -18.3f, Upper, 17.8f);
            Kit("Bag", -17.3f, Upper, 18.6f, 70f);

            foreach (WallSide side in s_sides)
            {
                foreach (int bay in new[] { -4, -2, 2, 4 })
                    Banner(side, bay, 7.8f, 1.5f, (bay + (int)side) % 4 == 0);
            }

            Monster(-18.1f, Upper, -6f, 90f);
            Monster(18.1f, Upper, 6f, -90f);
            Monster(-2f, 0f, 12f, 180f);
            Monster(0f, 0f, -12f, 0f);
            Monster(11f, 0f, 2f, -90f);
        }

        /// Two storeys of cells behind iron bars, the warden's office and a torture chamber across the yard.
        private void BuildPrison()
        {
            Material stone = DungeonPropBuilder.Cobble;
            const float front = -13f;
            const float walk = -10.5f;
            const float cell = 6.5f;
            Deck(-Inner, walk, -Inner, Inner, Upper, stone);
            Stairs(walk + _run, 0f, 16f, -90f, 2.4f, Upper, stone);
            Stairs(walk + _run, 0f, -16f, -90f, 2.4f, Upper, stone);
            Rail(walk, -Inner, walk, -17.2f, Upper);
            Rail(walk, -14.8f, walk, 14.8f, Upper);
            Rail(walk, 17.2f, walk, Inner, Upper);

            for (int i = 0; i < 6; i++)
            {
                float z = -19.5f + cell * (i + 0.5f);

                if (i > 0)
                {
                    Wall(-16.35f, z - cell * 0.5f, 0f, 6.7f, Upper + 3f);
                    Post(walk - 0.2f, z - cell * 0.5f, Upper - Slab);
                }

                for (int level = 0; level < 2; level++)
                {
                    float y = level * Upper;
                    float height = level == 0 ? Upper - Slab : 3f;
                    Bars(front, y, z - 2.175f, 90f, 2.15f, height);
                    Bars(front, y, z + 2.175f, 90f, 2.15f, height);

                    // Some cells were broken out of long ago.
                    if ((i + level * 3) % 5 != 2)
                        Put("CellDoor", front, y, z, 90f, false);

                    FurnishCell(i, level, y, z);
                }
            }

            foreach (int bay in new[] { -4, -1, 1, 4 })
            {
                Put("WallTorch", AtWall(West, bay * Bay, 0.02f, 2.4f), Facing(West));
                Put("WallTorch", AtWall(West, bay * Bay, 0.02f, Upper + 2.4f), Facing(West));
            }

            // The warden's office and the torture chamber flank the passage to the east door.
            Doorway(5f, -12.85f, 90f, 13.7f, 0f, "Door");
            Wall(12.35f, -6f, 0f, 14.9f);
            Deck(4.75f, Inner, -Inner, -5.75f, Height + Slab, stone);
            Doorway(5f, 12.85f, 90f, 13.7f, 0f, "CellDoor");
            Wall(12.35f, 6f, 0f, 14.9f);
            Deck(4.75f, Inner, 5.75f, Inner, Height + Slab, stone);

            Table(12f, -13f, 0f, 3, "Key_Gold", "Scroll_1", "CandleStick_Triple", "Mug", "Book_7", "Scroll_2");
            Bookcase(AtWall(South, 8.5f, 0.55f), 0f);
            Loot("Bookshelf", AtWall(South, 10.2f, 0.55f), 0f);
            Kit("Cabinet", AtWall(South, 14.5f, 0.5f), 0f);
            Kit("Key_Metal", AtWall(South, 14.5f, 0.5f, 1.01f), 30f);
            Loot("LargeOakChest", AtWall(East, -16.5f, 0.8f), -90f);
            Kit("Bed_Twin1", AtWall(East, -9f, 1.6f), -90f);
            Mount("Shield_Wooden", East, -4, 2f);
            Kit("WeaponStand", 7f, 0f, -8f, 90f);
            Monster(10f, 0f, -10f, -90f);

            Put("Brazier", 12f, 0f, 13f);
            Kit("Cauldron", 14.4f, 0f, 13f);

            for (int i = 0; i < 3; i++)
                Kit("Cage_Small", 8f + i * 4.5f, 0f, 18f, Range(-20f, 20f), 1.7f);

            Table(15f, 8.5f, 0f, 0, "Table_Knife", "Bottle_1", "Rope_1", "Key_Metal", "Chalice");
            Put("Chain", 9f, Height, 10f);
            Put("Chain", 16.5f, Height, 15.5f, 40f);
            Put("FallenRanger", 10f, 0f, 9f, 130f);
            Put("SkullPile", 18.5f, 0f, 7.5f);
            Kit("Bucket_Metal", 7f, 0f, 16f, 20f);
            Kit("Chain_Coil", 17f, 0f, 11f, 70f);
            Loot("SmallOakChest", AtWall(East, 14.5f, 0.8f), -90f);
            Monster(12f, 0f, 10f, 180f);
            Monster(17f, 0f, 17f, -135f);

            // Yard between the cell block and the rooms.
            Table(-2f, -8f, 90f, 4, Tableware(5));
            Kit("WeaponStand", 1f, 0f, 10f, 90f);
            Loot("Barrel", 3.9f, 0f, 4.6f, 0f);
            Loot("Crate", 3.9f, 0f, -4.6f, 15f);
            Put("Brazier", -6f, 0f, 0f);

            foreach (float z in new[] { -4f, 8f })
            {
                Put("Chain", -3f, _ceiling, z);
                Kit("Cage_Small", -3f, _ceiling - ChainLength - 1.45f, z, 20f + z * 5f, 1.8f);
            }

            Monster(-4f, 0f, 4f, 90f);
            Monster(0f, 0f, -3f, 0f);
            Monster(walk - 1.2f, Upper, 0f, 90f);
        }

        private void FurnishCell(int index, int level, float y, float z)
        {
            Kit(index % 2 == 0 ? "Bed_Twin1" : "Bed_Twin2", -18.3f, y, z + 2.2f, 90f, 0.85f);
            Kit(Pick("Bucket_Wooden_1", "Bucket_Metal"), -19f, y, z - 2.6f, Range(0f, 360f));

            if ((index + level) % 2 == 0)
                Kit("Chain_Coil", -15.5f, y, z - 1.5f, Range(0f, 360f));
            else
                Kit("Stool", -16f, y, z - 2f, Range(0f, 360f));

            switch ((index * 2 + level) % 6)
            {
                case 0:
                    Put("FallenPeasant", -16.5f, y, z - 0.5f, 70f);
                    break;
                case 1:
                    Loot("Crate", -18.8f, y, z - 0.8f, 10f);
                    break;
                case 2:
                    Monster(-16.5f, y, z, 90f);
                    break;
                case 3:
                    Put("SkullPile", -18.6f, y, z - 0.6f);
                    Kit("Mug", -17f, y, z - 1f, 40f);
                    break;
                case 4:
                    Loot("Barrel", -18.9f, y, z - 0.8f, 0f);
                    break;
                default:
                    Monster(-16.5f, y, z, 90f);
                    Put("Rubble", -18.2f, y, z - 1f, 30f);
                    break;
            }

            if (level == 1 && index == 0)
                Loot("LargeOakChest", -18.9f, y, z - 0.6f, 90f);
            else if (level == 1 && index == 3)
                Loot("SmallOakChest", -18.9f, y, z + 0.2f, 90f);
        }

        /// Stacks forming narrow aisles, a reading hall and an L-shaped gallery of shelves.
        private void BuildLibrary()
        {
            Material wood = DungeonPropBuilder.WoodPlanks;
            const float edge = 16f;
            Deck(edge, Inner, -Inner, Inner, Upper, wood);
            Deck(-Inner, edge, -Inner, -edge, Upper, wood);
            Stairs(edge - _run, 0f, 12f, 90f, 2.4f, Upper, wood);
            Rail(edge, -edge, edge, 10.8f, Upper);
            Rail(edge, 13.2f, edge, Inner, Upper);
            Rail(-Inner, -edge, edge, -edge, Upper);

            foreach (float z in new[] { -10f, -4f, 2f, 8f, 17f })
                Post(edge + 0.2f, z, Upper - Slab);

            foreach (float x in new[] { -15f, -9f, -3f, 3f, 9f, 15.8f })
                Post(x, -edge - 0.2f, Upper - Slab);

            for (int row = 0; row < 4; row++)
            {
                float z = -13f + row * 4f;

                for (int i = 0; i < 4; i++)
                {
                    float x = 5.75f + i * 1.5f;
                    Shelf(new Vector3(x, 0f, z + 0.26f), 0f, (row + i) % 4 == 0);
                    Shelf(new Vector3(x, 0f, z - 0.26f), 180f, (row * 3 + i) % 7 == 3);
                }
            }

            foreach (float z in new[] { -11f, -7f, -3f })
                Kit("CandleStick_Stand", 12.4f, 0f, z);

            Table(-8f, 6f, 0f, 4, "Book_5", "Scroll_1", "CandleStick_Triple", "Book_Stack_1", "Potion_1", "Scroll_2");
            Table(-8f, -6f, 0f, 4, "Book_7", "Book_Stack_2", "Candle_2", "Scroll_1", "SmallBottles_1");
            Kit("BookStand", -3f, 0f, 11f, 160f);
            Kit("BookStand", -14f, 0f, -2.5f, 70f);
            Put("StatueMage", -14f, 0f, 14f, 135f);
            Kit("CandleStick_Stand", -12.5f, 0f, 12.5f);
            Scatter(-4f, 13f, 1.2f, "Scroll_1", "Book_5", "Scroll_2");

            // Alchemist's nook under the south gallery.
            Kit("Cauldron", -10f, 0f, -17.6f);
            Kit("Workbench", AtWall(South, -14f, 0.85f), 0f);
            Kit("Workbench_Drawers", AtWall(South, -14f, 0.85f), 0f);
            Kit("Potion_2", AtWall(South, -13.6f, 0.8f, 0.89f), 20f);
            Kit("Potion_4", AtWall(South, -14.6f, 0.7f, 0.89f), 80f);
            Kit("SmallBottles_1", AtWall(South, -14.2f, 1.1f, 0.89f), 10f);
            Mount("Shelf_Small_Bottles", South, -2, 1.5f);
            Mount("Shelf_Arch", South, -5, 1.5f);
            Kit("Vase_2", -6f, 0f, -18.6f);
            Kit("Vase_4", -5f, 0f, -18.9f);
            Loot("Barrel", -17.5f, 0f, -18.6f, 0f);

            foreach (float z in new[] { -12f, -8f, -4f, 0f, 4f, 8f })
                Shelf(AtWall(East, z, 0.55f, Upper), -90f, z == -4f || z == 8f);

            foreach (float x in new[] { -14f, -10f, -6f, -2f, 2f, 6f, 10f })
                Shelf(AtWall(South, x, 0.55f, Upper), 0f, x == -10f || x == 6f);

            Loot("SmallOakChest", 18.2f, Upper, 17.6f, -90f);
            Loot("LargeOakChest", -18.8f, Upper, -17.9f, 90f);
            Kit("BookStand", 17f, Upper, -17f, -45f);
            Kit("CandleStick_Stand", 17.6f, Upper, 15f);
            Monster(-4f, 0f, 3f, 90f);
            Monster(8f, 0f, -11f, 90f);
            Monster(18f, Upper, -4f, 180f);
            Monster(0f, Upper, -18f, 0f);
        }

        /// Barracks: a dormitory, a smithy and a training yard behind inner walls.
        private void BuildArmory()
        {
            Doorway(-8f, -6.85f, 90f, 25.7f, -4.85f, "Door");
            Wall(-13.85f, 6f, 0f, 12.2f);
            Doorway(5.85f, -8f, 0f, 27.7f, 2.15f, null);

            for (int i = 0; i < 6; i++)
            {
                float z = -17f + i * 4f;
                Kit(i % 2 == 0 ? "Bed_Twin2" : "Bed_Twin1", AtWall(West, z, 1.6f), 90f);

                if (i % 2 == 0)
                    Kit("Nightstand_Shelf", AtWall(West, z + 2f, 0.55f), 90f);
            }

            Mount("Peg_Rack", West, -2, 1.9f);
            Mount("Peg_Rack", West, 0, 1.9f);
            Table(-11.5f, -9f, 90f, 4, Tableware(5));
            Loot("Crate", -9.2f, 0f, -18.6f, 0f);
            Loot("Crate", -10.4f, 0f, -18.6f, 15f);
            Kit("Bag", -9.2f, 0f, 4.6f, 30f);
            Kit("Stool", -12f, 0f, 2f);
            Loot("SmallOakChest", -13f, 0f, 5f, 180f);
            Monster(-13f, 0f, -3f, 90f);

            Put("Brazier", 14f, 0f, -17.5f);
            Kit("Anvil_Log", 11f, 0f, -15f, 30f);
            Kit("Whetstone", 16.5f, 0f, -12.5f, -60f);
            Kit("Workbench", AtWall(South, 2f, 0.85f), 0f);
            Kit("Workbench_Drawers", AtWall(South, 2f, 0.85f), 0f);
            Kit("Bucket_Metal", 12.6f, 0f, -16.4f);
            Kit("Cauldron", 17.5f, 0f, -17.6f);
            Kit("WeaponStand", -3f, 0f, -17.5f, 0f);
            Kit("WeaponStand", -5.5f, 0f, -12f, 90f);
            Kit("Crate_Metal", 6.5f, 0f, -18.6f, 5f);
            Kit("Pickaxe_Bronze", AtWall(South, 4.2f, 0.4f, 0.5f), 0f);
            Mount("Sword_Bronze", South, 2, 1.8f, 1f, 0.06f);
            Mount("Axe_Bronze", South, 3, 1.9f, 1f, 0.06f);
            Mount("Shield_Wooden", South, 4, 2f);
            Mount("Shield_Wooden", East, -4, 2f);
            Loot("Barrel", 18.6f, 0f, -9.6f, 0f);
            Loot("Barrel", 17.4f, 0f, -9.4f, 0f);
            Monster(8f, 0f, -13f, 0f);

            foreach (float x in new[] { -14f, -10f, -6f, 6f, 10f })
                Kit("Dummy", x, 0f, 13f, 180f);

            Kit("WeaponStand", 14f, 0f, 13f, 90f);
            Loot("LargeOakChest", 17.5f, 0f, 18.4f, 180f);
            Put("StandRangerMale", -3.5f, 0f, 18.6f, 180f);
            Put("StandRangerFemale", 3.5f, 0f, 18.6f, 180f);
            Banner(North, -4, 4.2f, 1.3f, true);
            Banner(North, 4, 4.2f, 1.3f, true);
            Table(4f, 3f, 0f, 2, Tableware(4));
            _result.Levers.Add(Put("Lever", 18.9f, 0f, 8f, -90f, false).GetComponent<LeverComponent>());
            Monster(2f, 0f, 8f, 90f);
            Monster(-3f, 0f, 14f, 0f);
        }

        /// Strongroom behind bars and a locked door, wrapped by a ring corridor with the portal.
        private void BuildTreasury()
        {
            const float side = 10f;

            foreach (int sign in new[] { -1, 1 })
            {
                Wall(sign * 7.125f, side, 0f, 6.25f);
                Wall(side, sign * 7.125f, 90f, 6.25f);
                Wall(-side, sign * 7.125f, 90f, 6.25f);
            }

            Bars(0f, 0f, side, 0f, 8f, Height);
            Bars(side, 0f, 0f, 90f, 8f, Height);
            Bars(-side, 0f, 0f, 90f, 8f, Height);
            DoorComponent door = Doorway(0f, -side, 0f, 20.5f, 0f, "Door");
            SerializedObject so = new SerializedObject(door);
            so.FindProperty("_startsLocked").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            _result.LockedDoor = door;
            Put("WallTorch", -3.4f, 2.6f, -side - 0.27f, 180f);
            Put("WallTorch", 3.4f, 2.6f, -side - 0.27f, 180f);

            Solid(-3f, 3f, 4.5f, 8.5f, 0.25f, DungeonPropBuilder.Cobble);
            Loot("GoldenChest", 0f, 0.25f, 6.8f, 180f);
            Loot("SmallOakChest", -8.2f, 0f, 5f, 90f);
            Loot("SmallOakChest", 8.2f, 0f, 5f, -90f);
            Put("StatueGuardian", -4.6f, 0f, 8.6f, 180f);
            Put("StatueGuardian", 4.6f, 0f, 8.6f, 180f);
            Put("SpikeTrap", 0f, 0f, 2.6f, 0f, false);
            Kit("CandleStick_Stand", -2.4f, 0.25f, 5.2f);
            Kit("CandleStick_Stand", 2.4f, 0.25f, 5.2f);
            Kit("Chest_Wood", -7.6f, 0f, -6f, 60f);
            Kit("Coin_Pile_2", -7.6f, 0.32f, -6f, 0f, 3f);
            Kit("Crate_Metal", 8.2f, 0f, -7f, 10f);
            Kit("Crate_Metal", 8.3f, 0f, -5.6f, -8f);
            Kit("Vase_2", 6.6f, 0f, -8.6f);
            Kit("Vase_4", -5.6f, 0f, 8.8f);

            for (int i = 0; i < 12; i++)
                Kit(Pick("Coin_Pile", "Coin_Pile_2", "Chalice", "Coin"), Range(-7f, 7f), 0f, Range(-7f, 3f), Range(0f, 360f), Range(2f, 3.5f));

            Kit("Chalice", -1f, 0.25f, 5f, 0f, 1.5f);
            Kit("Key_Gold", 1.2f, 0.25f, 5f, 40f, 1.5f);
            Monster(0f, 0f, -4f, 180f);

            // Ring corridor.
            if (_floor == 1)
                _result.DescendPortal = Put("DescendPortal", -15f, 0f, 15f, 135f, false).GetComponent<PortalComponent>();
            else
                _result.EscapePortals.Add(Put("EscapePortal", -15f, 0f, 15f, 135f, false).GetComponent<PortalComponent>());

            Put("Pillar", 15f, 0f, 15f);
            Put("Pillar", 15f, 0f, -15f);
            Put("Pillar", -15f, 0f, -15f);
            Put("SpikeTrap", 0f, 0f, -13.5f, 0f, false);
            Put("Brazier", 13f, 0f, -17.5f);
            Put("Brazier", -13f, 0f, -17.5f);
            Put("StatueGuardian", -2.6f, 0f, -11.2f, 180f);
            Put("StatueGuardian", 2.6f, 0f, -11.2f, 180f);
            Loot("Barrel", 18.4f, 0f, 17f, 0f);
            Loot("Crate", 18.4f, 0f, 15.6f, 30f);
            Kit("Bag", 17.2f, 0f, 18.4f, 30f);
            Banner(South, -2, 3.9f, 1.3f, true);
            Banner(South, 2, 3.9f, 1.3f, true);
            Monster(15f, 0f, -8f, -90f);
            Monster(-15f, 0f, -8f, 90f);

            if (_floor == 2)
                Monster(8f, 0f, 15f, 180f);
        }

        /// Chapel: rows of benches down the nave and a raised sanctuary with the altar.
        private void BuildShrine()
        {
            Material stone = DungeonPropBuilder.Cobble;
            const float dais = 1f;
            const float front = 11f;
            Solid(front, Inner, -9f, 9f, dais, stone);
            Stairs(front - DungeonStructureBuilder.StairRun(dais), 0f, 0f, 90f, 8f, dais, stone);
            Put(ShrineName(0), 17.6f, dais, 0f, -90f, false);
            Kit("CandleStick_Stand", 16f, dais, 4f);
            Kit("CandleStick_Stand", 16f, dais, -4f);
            Put("StatuePilgrim", 17.6f, dais, 7f, -90f);
            Put("StatuePilgrim", 17.6f, dais, -7f, -90f);
            Banner(East, -1, 4.3f, 1.2f, true);
            Banner(East, 1, 4.3f, 1.2f, true);
            Kit("Chalice", 13f, dais, 6.5f, 0f, 1.6f);
            Kit("Vase_4", 12.6f, dais, -7.6f);
            Kit("Candle_2", 12f, dais, 4.6f);
            Kit("Candle_1", 12.3f, dais, 4.9f);
            Kit("Candle_2", 12f, dais, -4.6f);
            Loot("SmallOakChest", 18.4f, dais, -4.2f, -90f);

            foreach (float x in new[] { -12f, -9f, -6f, -3f, 0f, 3f })
            {
                foreach (float z in new[] { -6.4f, -3.2f, 3.2f, 6.4f })
                    Kit("Bench", x, 0f, z, 90f);
            }

            foreach (float x in new[] { -13f, -5f, 3f })
            {
                Put("Pillar", x, 0f, 9.5f);
                Put("Pillar", x, 0f, -9.5f);
            }

            DungeonStructureBuilder.Carpet(_module, new Vector3(-Inner, 0f, 0f), new Vector3(front - 1.8f, 0f, 0f), 2.6f);
            Kit("BookStand", 7.6f, 0f, 3.2f, -90f);
            Put(ShrineName(2), -8f, 0f, 18.6f, 180f, false);
            Kit("CandleStick_Stand", -10.4f, 0f, 18.6f);
            Kit("CandleStick_Stand", -5.6f, 0f, 18.6f);
            Loot("Coffin", 4f, 0f, 18.2f, 90f);
            Put("SkullPile", 7f, 0f, 18.6f);
            _result.EscapePortals.Add(Put("EscapePortal", -15f, 0f, -15f, 45f, false).GetComponent<PortalComponent>());
            Kit("Vase_2", 9f, 0f, -18.6f);
            Kit("Vase_Rubble_Medium", 7.6f, 0f, -18.4f, 40f);
            Loot("SmallOakChest", AtWall(East, -14.5f, 0.8f), -90f);
            Monster(-2f, 0f, -12f, 0f);
            Monster(6f, 0f, 0f, -90f);

            if (_floor == 2)
                Monster(14f, dais, 0f, -90f);
        }

        /// Bottom of the collapsed shaft: the descent ends on a ledge of rubble above a miners' camp.
        private void BuildArrival()
        {
            Material stone = DungeonPropBuilder.Cobble;
            const float ledge = 1.4f;
            const float edge = -9f;
            float run = DungeonStructureBuilder.StairRun(ledge);
            Solid(-Inner, edge, -Inner, edge, ledge, stone);
            Stairs(edge + run, 0f, -14f, -90f, 4f, ledge, stone);
            Stairs(-14f, 0f, edge + run, 180f, 4f, ledge, stone);
            _result.DescendDestination = Marker("Arrival", -14f, ledge, -14f, 45f);
            Loot("SmallOakChest", -18.6f, ledge, -11.5f, 90f);
            Put("Rubble", -17.5f, ledge, -17.5f);
            Kit("Rope_3", -12f, ledge, -18f, 20f);
            Kit("Pickaxe_Bronze", AtWall(South, -15f, 0.4f, ledge + 0.5f), 0f);

            Put("Brazier", -4f, 0f, -4f);
            Kit("Stall_Cart_Empty", -2f, 0f, -13f, 100f);
            Kit("Bag", -3.6f, 0f, -11.4f, 20f);
            Kit("Bucket_Metal", -1f, 0f, -10.6f);
            Kit("Chain_Coil", 4f, 0f, -15f, 60f);
            Crates(8f, -17.5f, 3);
            Kit("Pickaxe_Bronze", AtWall(South, 5.5f, 0.4f, 0.5f), 0f);
            Kit("Bed_Twin1", AtWall(East, -12f, 1.6f), -90f);
            Kit("Bed_Twin2", AtWall(East, -15.5f, 1.6f), -90f);
            Kit("Lantern_Wall", AtWall(South, 14.5f, 0.02f, 2.2f), 0f);
            Put("StatuePilgrim", 16.5f, 0f, 16.5f, -135f);
            Put("SkullPile", -16f, 0f, 16f, 30f);
            Put("FallenPeasant", -3f, 0f, 9f, 70f);
            Put("Rubble", -15f, 0f, 3f, 40f);
            Put("Rubble", 6f, 0f, 14f, 10f);
            Kit("Vase_Rubble_Medium", 8f, 0f, 12.6f);
            Put("Pillar", 9f, 0f, 9f);
            Put("Pillar", -9f, 0f, 9f);
            Put("Pillar", 9f, 0f, -9f);
            Loot("Barrel", 18.4f, 0f, -5f, 0f);
            Loot("SmallOakChest", AtWall(West, 7.3f, 0.8f), 90f);
        }

        /// A deep ossuary crossed by bridges without railings; stairs lead down to the dead.
        private void BuildBonePit()
        {
            Material stone = DungeonPropBuilder.Cobble;
            const float rim = 13f;
            float depth = DungeonMapBuilder.PitDepth;
            DungeonStructureBuilder.PitFloor(_module, "BonePit", DungeonMapBuilder.Module, new Rect(-rim, -rim, rim * 2f, rim * 2f), depth);
            Solid(-3f, 3f, -3f, 3f, 0f, stone, -depth);
            Deck(-1.5f, 1.5f, 3f, rim, 0f, stone);
            Deck(-1.5f, 1.5f, -rim, -3f, 0f, stone);
            Deck(-rim, -3f, -1.5f, 1.5f, 0f, stone);
            Deck(3f, rim, -1.5f, 1.5f, 0f, stone);

            foreach (float a in new[] { -8f, 8f })
            {
                Solid(-0.5f, 0.5f, a - 0.5f, a + 0.5f, -Slab, stone, -depth);
                Solid(a - 0.5f, a + 0.5f, -0.5f, 0.5f, -Slab, stone, -depth);
            }

            float run = DungeonStructureBuilder.StairRun(depth);
            Stairs(-rim + 1.2f, -depth, -rim + run, 180f, 2.4f, depth, stone);
            Stairs(rim - 1.2f, -depth, rim - run, 0f, 2.4f, depth, stone);

            Loot("LargeOakChest", 1.7f, 0f, 1.7f, -135f);
            Put("Brazier", -1.8f, 0f, -1.8f);

            for (int i = 0; i < 12; i++)
                Put("SkullPile", Range(-11f, 11f), -depth, Range(-11f, 11f) + (i % 2 == 0 ? 0.5f : -0.5f), Range(0f, 360f));

            Put("Rubble", -6f, -depth, 6f);
            Put("Rubble", 7f, -depth, -5f, 60f);
            Put("FallenPeasant", -5f, -depth, -6f, 120f);
            Put("FallenRanger", 6f, -depth, 8f, -60f);
            Put("Sarcophagus", 9f, -depth, -9.5f, 90f);
            Loot("Coffin", -9.5f, -depth, 9f, 0f);
            Loot("Coffin", 5f, -depth, -11.4f, 90f);
            Loot("SmallOakChest", -6f, -depth, 11.8f, 180f);
            Put("Brazier", -7f, -depth, -9f);
            Put("Brazier", 7f, -depth, 9f);
            Put("CandleCluster", -11.6f, -depth, 2.4f);
            Put("CandleCluster", 11.6f, -depth, -2.4f);
            Put("WallTorch", -rim + 0.02f, -1.6f, 6f, 90f);
            Put("WallTorch", rim - 0.02f, -1.6f, -6f, -90f);
            Put("WallTorch", 6f, -1.6f, -rim + 0.02f, 0f);
            Put("WallTorch", -6f, -1.6f, rim - 0.02f, 180f);
            Monster(-7f, -depth, 7f, 135f);
            Monster(7f, -depth, -7f, -45f);
            Monster(-6f, -depth, -3f, 90f);
            Monster(4f, -depth, 5f, -90f);

            foreach (int x in new[] { -1, 1 })
            {
                foreach (int z in new[] { -1, 1 })
                {
                    Put("Pillar", x * 16.5f, 0f, z * 16.5f);
                    Put("Chain", x * 6f, Height, z * 6f, x * 30f);
                }
            }

            Put("StatueGuardian", -4.5f, 0f, -17.8f, 0f);
            Put("StatueGuardian", 4.5f, 0f, -17.8f, 0f);
            Loot("SmallOakChest", 0f, 0f, -18.6f, 0f);
            Put("Brazier", -16.5f, 0f, -8f);
            Put("Brazier", 16.5f, 0f, 8f);
            Put("FallenRanger", 15.5f, 0f, -15f, 30f);
            Kit("Vase_2", -18.4f, 0f, 13f);
            Kit("Vase_Rubble_Medium", -17.4f, 0f, 14.4f, 20f);
            Monster(16f, 0f, -10f, -90f);
            Monster(-16f, 0f, 10f, 90f);
        }

        /// Wine cellar and underground market: aisles between barrel racks, stalls, a kitchen and a storage loft.
        private void BuildCellar()
        {
            Material wood = DungeonPropBuilder.WoodPlanks;
            const float edge = 15.5f;
            Deck(edge, Inner, -Inner, Inner, Upper, wood);
            Deck(-Inner, edge, -Inner, -edge, Upper, wood);
            Stairs(-8f, 0f, -edge + _run, 180f, 2.4f, Upper, wood);
            Rail(edge, -edge, edge, Inner, Upper);
            Rail(-Inner, -edge, -9.2f, -edge, Upper);
            Rail(-6.8f, -edge, edge, -edge, Upper);

            foreach (float z in new[] { -9f, -2f, 5f, 12f, 18f })
                Post(edge + 0.2f, z, Upper - Slab);

            foreach (float x in new[] { -17f, -11f, -4f, 3f, 10f, 15.3f })
                Post(x, -edge - 0.2f, Upper - Slab);

            foreach (float x in new[] { 3.5f, 9f })
            {
                for (int i = 0; i < 9; i++)
                {
                    float z = -11f + i * 2.1f;

                    // The west door lane runs through the racks.
                    if (Mathf.Abs(z) < 2f)
                        continue;

                    Kit("Barrel_Holder", x, 0f, z, 90f);
                }
            }

            Kit("Barrel", 6.2f, 0f, -12f);
            Kit("Barrel_Apples", 6.4f, 0f, 6f);
            Kit("Bucket_Wooden_1", 5f, 0f, 3.4f);
            Kit("Stall_Empty", -14f, 0f, 16f, 180f);
            Kit("FarmCrate_Apple", -14.4f, TableTop, 15.9f, 180f);
            Kit("FarmCrate_Carrot", -13.5f, TableTop, 15.9f, 170f);
            Kit("Stall_Empty", -9.5f, 0f, 16f, 180f);
            Kit("Bottle_1", -9.9f, TableTop, 15.8f);
            Kit("Pot_1_Lid", -9.2f, TableTop, 15.9f);
            Kit("Mug", -8.8f, TableTop, 15.7f, 50f);
            Kit("Stall_Cart_Empty", 9f, 0f, 16.5f, 180f);
            Kit("Bag", 10.8f, 0f, 15f, 30f);
            Kit("FarmCrate_Empty", 7.6f, 0f, 15.2f, 70f);
            Crates(-12f, 6f, 4);
            Crates(-15f, 9f, 3);
            Crates(-4f, 10f, 2);
            Loot("Crate", -13.4f, 0f, 7.6f, 20f);
            Loot("Barrel", -3f, 0f, 12f, 0f);
            Loot("SmallOakChest", AtWall(East, 0f, 0.8f), -90f);

            // Kitchen under the south loft.
            Put("Brazier", -13f, 0f, -17.6f);
            Kit("Cauldron", -15.4f, 0f, -17.6f);
            Table(-15f, -12f, 0f, 2, "Pot_1", "Carrot", "Table_Knife", "Bottle_1", "Table_Plate", "Carrot");
            Mount("Shelf_Small_Bottles", South, -3, 1.5f);
            Mount("Shelf_Simple", South, -5, 1.6f);
            Kit("Cabinet", AtWall(South, 4f, 0.5f), 0f);
            Kit("Pot_1_Lid", AtWall(South, 4f, 0.5f, 1.01f), 20f);
            Kit("Barrel_Apples", 0.5f, 0f, -18.6f);
            Kit("Bucket_Wooden_1", -17.6f, 0f, -14.6f);
            Kit("FarmCrate_Carrot", 7f, 0f, -18.2f, 10f);

            // Loft.
            Loot("LargeOakChest", 18.2f, Upper, -18.2f, -135f);
            Loot("Barrel", 18.6f, Upper, 4f, 0f);
            Loot("Crate", 18.6f, Upper, 12f, 10f);
            Crates(18.3f, 16.5f, 3, Upper);
            Crates(-17.5f, -18.4f, 3, Upper);
            Kit("Barrel", 18.4f, Upper, -6f);
            Kit("Barrel", 18.5f, Upper, -7.4f);
            Kit("Bag", 4f, Upper, -18.6f, 20f);
            Kit("Rope_2", 10f, Upper, -18.4f, 60f);
            Kit("Barrel_Holder", -2f, Upper, -18.9f, 0f);
            Monster(-5f, 0f, 5f, 135f);
            Monster(6.2f, 0f, -6f, 0f);
            Monster(17.6f, Upper, 8f, 180f);
            Monster(-12f, Upper, -17.6f, 90f);
        }

        /// Serpentine of blade and spike gates, then a bridge over a spiked pit to the lever and the treasure.
        private void BuildTrapCorridor()
        {
            Material stone = DungeonPropBuilder.Cobble;
            const float depth = 3f;
            const float bridge = 12.5f;
            DungeonStructureBuilder.PitFloor(_module, "TrapPit", DungeonMapBuilder.Module, new Rect(-9f, 5.3f, 20f, 14.45f), depth);
            Wall(-3.35f, -14f, 0f, 32.7f);
            Wall(3.35f, -9f, 0f, 32.7f);
            Wall(-3.35f, -4f, 0f, 32.7f);
            Doorway(0f, 5f, 0f, 39.4f, -15f, null);
            Gate(6f, -11.5f, true);
            Gate(-4f, -11.5f, false);
            Gate(-4f, -6.5f, true);
            Gate(6f, -6.5f, false);

            // Entry hall: a warning to the living.
            Put("FallenPeasant", 3f, 0f, -17f, 200f);
            Put("FallenRanger", 14f, 0f, -16.5f, 15f);
            Put("SkullPile", 17.5f, 0f, -18.5f);
            Put("Rubble", -12f, 0f, -17.5f);
            Loot("Crate", -18.4f, 0f, -18.4f, 10f);
            Kit("Vase_Rubble_Medium", -10f, 0f, -18.2f);
            Put("SkullPile", -18f, 0f, -11.5f, 40f);
            Put("FallenPeasant", 16f, 0f, -6.5f, -100f);

            // Quiet hall before the sanctum.
            Put("Brazier", -8f, 0f, 0.5f);
            Put("StatueGuardian", -17.4f, 0f, 3.6f, 180f);
            Put("StatueGuardian", -12.6f, 0f, 3.6f, 180f);
            Loot("Barrel", -18.6f, 0f, -2.6f, 0f);
            Monster(-10f, 0f, 0.5f, -90f);
            Monster(4f, 0f, 2f, -90f);

            // Sanctum.
            Deck(-9f, 11f, bridge - 1f, bridge + 1f, 0f, stone);
            _result.Traps.Add(Put("BladeTrap", -3f, 0f, bridge, 90f, false).GetComponent<TrapComponent>());
            _result.Traps.Add(Put("BladeTrap", 5f, 0f, bridge, 90f, false).GetComponent<TrapComponent>());
            Stairs(-9f + DungeonStructureBuilder.StairRun(depth), -depth, 7f, -90f, 2.4f, depth, stone);

            foreach (float x in new[] { -1f, 3f, 7f })
            {
                Put("SpikeTrap", x, -depth, 9.5f, 0f, false);
                Put("SpikeTrap", x, -depth, 15.5f, 0f, false);
            }

            Put("SpikeTrap", 8.5f, -depth, 12.5f, 0f, false);
            Put("FallenRanger", -5f, -depth, 15f, 40f);
            Put("SkullPile", 9.5f, -depth, 18f);
            Put("SkullPile", -7.5f, -depth, 17.5f, 70f);
            Loot("LargeOakChest", 4.5f, -depth, 18.9f, 180f);
            Loot("LargeOakChest", 17.5f, 0f, bridge, -90f);
            Loot("SmallOakChest", 17.8f, 0f, 8f, -90f);
            _result.Levers.Add(Put("Lever", 18.9f, 0f, 16.5f, -90f, false).GetComponent<LeverComponent>());
            Kit("CandleStick_Stand", 15.5f, 0f, 16f);
            Kit("CandleStick_Stand", 15.5f, 0f, 9f);
            Kit("Coin_Pile_2", 16.4f, 0f, 14f, 30f, 3f);
            Kit("Coin_Pile", 15.2f, 0f, 11.4f, 80f, 3f);
            Put("Brazier", -16f, 0f, 16f);
            Put("Brazier", -11f, 0f, bridge + 2.6f);
            Put("Brazier", 12.6f, 0f, bridge - 2.6f);
            Put("CandleCluster", 0f, -depth, 18.6f);
            Put("CandleCluster", 9.6f, -depth, 6.6f);
            Monster(-14f, 0f, bridge, 90f);
            Monster(14f, 0f, 16f, -90f);
        }

        /// Narrow gap between two wall stubs guarded by a swinging blade or floor spikes.
        private void Gate(float x, float z, bool isBlade)
        {
            Wall(x, z - 1.675f, 90f, 1.15f);
            Wall(x, z + 1.675f, 90f, 1.15f);
            Put("WallTorch", x + 0.27f, 2.5f, z + 1.675f, 90f);
            Put("WallTorch", x - 0.27f, 2.5f, z - 1.675f, -90f);

            if (isBlade)
                _result.Traps.Add(Put("BladeTrap", x, 0f, z, 90f, false).GetComponent<TrapComponent>());
            else
                Put("SpikeTrap", x, 0f, z, 0f, false);
        }

        /// Maze carved by a depth-first walk with a few extra openings and a guarded heart chamber.
        private void BuildLabyrinth()
        {
            const int count = 11;
            const int heartMin = 4;
            const int heartMax = 6;
            float cell = DungeonMapBuilder.Module / count;
            bool[,] wallsX = new bool[count - 1, count];
            bool[,] wallsZ = new bool[count, count - 1];
            (int x, int z)[] directions = { (0, 1), (1, 0), (0, -1), (-1, 0) };

            for (int x = 0; x < count; x++)
            {
                for (int z = 0; z < count; z++)
                {
                    bool isHeart = x >= heartMin && x <= heartMax && z >= heartMin && z <= heartMax;

                    if (x < count - 1)
                        wallsX[x, z] = !(isHeart && x < heartMax);

                    if (z < count - 1)
                        wallsZ[x, z] = !(isHeart && z < heartMax);
                }
            }

            bool IsWalled(int x, int z, (int x, int z) direction)
            {
                int nx = x + direction.x;
                int nz = z + direction.z;

                if (nx < 0 || nz < 0 || nx >= count || nz >= count)
                    return true;

                return direction.x != 0 ? wallsX[Mathf.Min(x, nx), z] : wallsZ[x, Mathf.Min(z, nz)];
            }

            void Open(int x, int z, (int x, int z) direction)
            {
                if (direction.x != 0)
                    wallsX[Mathf.Min(x, x + direction.x), z] = false;
                else
                    wallsZ[x, Mathf.Min(z, z + direction.z)] = false;
            }

            bool[,] visited = new bool[count, count];
            Stack<(int x, int z)> stack = new();
            stack.Push((count / 2, 0));
            visited[count / 2, 0] = true;

            while (stack.Count > 0)
            {
                (int x, int z) current = stack.Peek();
                List<(int x, int z)> options = new();

                foreach ((int x, int z) direction in directions)
                {
                    int nx = current.x + direction.x;
                    int nz = current.z + direction.z;

                    if (nx >= 0 && nz >= 0 && nx < count && nz < count && !visited[nx, nz])
                        options.Add(direction);
                }

                if (options.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                (int x, int z) step = options[_random.Next(options.Count)];
                Open(current.x, current.z, step);
                visited[current.x + step.x, current.z + step.z] = true;
                stack.Push((current.x + step.x, current.z + step.z));
            }

            // A few extra openings turn the tree into loops: chases and flanking instead of one true path.
            for (int x = 0; x < count; x++)
            {
                for (int z = 0; z < count; z++)
                {
                    if (x < count - 1 && wallsX[x, z] && _random.NextDouble() < 0.08)
                        wallsX[x, z] = false;

                    if (z < count - 1 && wallsZ[x, z] && _random.NextDouble() < 0.08)
                        wallsZ[x, z] = false;
                }
            }

            DungeonStructureBuilder.Maze(_module, "Maze" + _floor, wallsX, wallsZ, cell, Height);
            List<(Vector3 position, float yaw)> deadEnds = new();
            List<Vector3> passages = new();

            for (int x = 0; x < count; x++)
            {
                for (int z = 0; z < count; z++)
                {
                    Vector3 center = new Vector3(-Half + (x + 0.5f) * cell, 0f, -Half + (z + 0.5f) * cell);
                    bool isHeart = x >= heartMin && x <= heartMax && z >= heartMin && z <= heartMax;
                    bool isEntrance = (x == count / 2 && (z == 0 || z == count - 1)) || (z == count / 2 && (x == 0 || x == count - 1));

                    if (x < count - 1 && wallsX[x, z] && _random.NextDouble() < 0.07)
                        Put("WallTorch", center.x + cell * 0.5f - 0.27f, 2.4f, center.z, -90f);

                    if (z < count - 1 && wallsZ[x, z] && _random.NextDouble() < 0.07)
                        Put("WallTorch", center.x, 2.4f, center.z + cell * 0.5f - 0.27f, 180f);

                    if (isHeart || isEntrance)
                        continue;

                    (int x, int z) exit = default;
                    int exits = 0;

                    foreach ((int x, int z) direction in directions)
                    {
                        if (IsWalled(x, z, direction))
                            continue;

                        exits++;
                        exit = direction;
                    }

                    if (exits == 1)
                        deadEnds.Add((center - new Vector3(exit.x, 0f, exit.z) * (cell * 0.5f - 1.2f), Mathf.Atan2(exit.x, exit.z) * Mathf.Rad2Deg));
                    else
                        passages.Add(center);
                }
            }

            for (int i = 0; i < deadEnds.Count; i++)
            {
                (Vector3 position, float yaw) = deadEnds[i];
                Vector3 side = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

                switch (i % 7)
                {
                    case 0:
                        Loot("SmallOakChest", position, yaw);
                        break;
                    case 1:
                        Loot("Barrel", position + side * 0.6f, 0f);
                        Loot("Crate", position - side * 0.6f, yaw + 15f);
                        break;
                    case 2:
                        Put("SkullPile", position, yaw);
                        Put("FallenPeasant", position + side * 0.8f, yaw + 100f);
                        break;
                    case 3:
                        Loot("Coffin", position, yaw + 90f);
                        Put("CandleCluster", position + side * 1.2f, 0f);
                        break;
                    case 4:
                        Kit("Vase_2", position + side * 0.5f);
                        Kit("Vase_4", position - side * 0.5f);
                        Kit("Coin_Pile", position - side * 0.1f + Vector3.back * 0.2f, 40f, 2.5f);
                        break;
                    case 5:
                        Put("Rubble", position, yaw);
                        Kit("Vase_Rubble_Medium", position + side * 0.9f, 30f);
                        break;
                    default:
                        Kit("Crate_Wooden", position + side * 0.5f, yaw + 10f);
                        Kit("Bag", position - side * 0.5f, yaw + 60f);
                        break;
                }
            }

            for (int i = 0; i < 6 && passages.Count > 0; i++)
            {
                int index = _random.Next(passages.Count);
                Monster(passages[index].x, 0f, passages[index].z, Range(0f, 360f));
                passages.RemoveAt(index);
            }

            Put(ShrineName(1), 0f, 0f, 0f, 0f, false);
            Loot("LargeOakChest", 0f, 0f, 4.6f, 180f);

            foreach (int x in new[] { -1, 1 })
            {
                foreach (int z in new[] { -1, 1 })
                    Kit("CandleStick_Stand", x * 4.2f, 0f, z * 4.2f);
            }

            Monster(-3.5f, 0f, -3.5f, 45f);
            Monster(3.5f, 0f, 3f, -135f);
        }

        /// Catacombs: four tomb chambers leave a crossroads between them and a tight corridor around.
        private void BuildCrypt()
        {
            int index = 0;

            foreach (int x in new[] { -1, 1 })
            {
                foreach (int z in new[] { -1, 1 })
                    BuildTomb(x, z, index++);
            }

            Solid(-2f, 2f, -2.4f, 2.4f, 0.25f, DungeonPropBuilder.Cobble);
            Put("Sarcophagus", 0f, 0.25f, 0f, 0f);
            Kit("CandleStick_Stand", -1.5f, 0.25f, -1.9f);
            Kit("CandleStick_Stand", 1.5f, 0.25f, 1.9f);

            foreach (int x in new[] { -1, 1 })
            {
                Put("StatueGuardian", x * 4f, 0f, 9f, -x * 90f);
                Put("StatuePilgrim", x * 4f, 0f, -9f, -x * 90f);
            }

            Loot("LargeOakChest", 18.6f, 0f, 0f, -90f);
            Put("StatueMage", 18.4f, 0f, 3f, -90f);
            Put("StatueMage", 18.4f, 0f, -3f, -90f);
            Banner(East, 0, 4.2f, 1.4f, true);
            Put("SkullPile", 9f, 0f, 3.8f);
            Put("FallenRanger", 11f, 0f, -2f, 40f);
            Put("Brazier", 13f, 0f, 0f);
            Monster(0f, 0f, -8f, 0f);
            Monster(0f, 0f, 8f, 180f);
            Monster(-9f, 0f, 0f, 90f);
        }

        private void BuildTomb(int sx, int sz, int index)
        {
            const float center = 11f;
            const float half = 6f;
            const float length = 12.5f;
            float cx = sx * center;
            float cz = sz * center;
            float innerX = sx * (center - half);
            float outerX = sx * (center + half);
            float innerZ = sz * (center - half);
            float outerZ = sz * (center + half);
            bool isDoorOnX = sx * sz > 0;

            // A door towards the crossroads on one axis, an open arch to the outer corridor on the other: every tomb is a shortcut.
            if (isDoorOnX)
            {
                Doorway(innerX, cz, 90f, length, 0f, index % 2 == 0 ? "Door" : "CellDoor");
                Wall(cx, innerZ, 0f, length);
                Wall(outerX, cz, 90f, length);
                Doorway(cx, outerZ, 0f, length, 0f, null);
                Loot("Coffin", cx, 0f, innerZ + sz * 1f, 90f);
                Loot("Coffin", outerX - sx * 1f, 0f, cz, 0f);
            }
            else
            {
                Wall(innerX, cz, 90f, length);
                Doorway(cx, innerZ, 0f, length, 0f, index % 2 == 0 ? "Door" : "CellDoor");
                Doorway(outerX, cz, 90f, length, 0f, null);
                Wall(cx, outerZ, 0f, length);
                Loot("Coffin", innerX + sx * 1f, 0f, cz, 0f);
                Loot("Coffin", cx, 0f, outerZ - sz * 1f, 90f);
            }

            if (isDoorOnX)
                Put("WallTorch", outerX - sx * 0.27f, 2.6f, cz + 2.5f, sx > 0 ? -90f : 90f);
            else
                Put("WallTorch", cx + 2.5f, 2.6f, outerZ - sz * 0.27f, sz > 0 ? 180f : 0f);

            Put("Sarcophagus", cx, 0f, cz, isDoorOnX ? 0f : 90f);
            Put("CandleCluster", cx + 1.6f, 0f, cz + 1.8f);
            Put("CandleCluster", cx - 1.6f, 0f, cz - 1.8f);
            Put("SkullPile", cx - sx * 4.6f, 0f, cz - sz * 4.6f, index * 50f);
            Kit(Pick("Vase_2", "Vase_4"), cx + sx * 4.6f, 0f, cz + sz * 4.6f);
            Put("Cobweb", cx - sx * 5f, Height - 0.75f, cz + sz * 5f, sx * sz > 0 ? 45f : 135f);

            if (index == 0 || index == 3)
                Loot("SmallOakChest", cx + sx * 4.4f, 0f, cz - sz * 4.4f, sx > 0 ? -90f : 90f);

            if (index != 3)
                Monster(cx + 2.4f, 0f, cz - 2.4f, -45f);
        }

        /// Boss hall: the throne on a dais, a carpet down the colonnade and galleries along the side walls.
        private void BuildThrone()
        {
            Material stone = DungeonPropBuilder.Cobble;
            const float dais = 1.6f;
            const float front = 11f;
            const float edge = 16.5f;
            const float end = 8f;
            float stairs = front - DungeonStructureBuilder.StairRun(dais);
            Solid(-8f, 8f, front, Inner, dais, stone);
            Stairs(0f, 0f, stairs, 0f, 7f, dais, stone);
            DungeonStructureBuilder.Throne(_module, new Vector3(0f, dais, 17.4f), 180f);
            DungeonStructureBuilder.Carpet(_module, new Vector3(0f, 0f, -Inner), new Vector3(0f, 0f, stairs), 3f);
            DungeonStructureBuilder.Carpet(_module, new Vector3(0f, dais, front + 0.4f), new Vector3(0f, dais, 16f), 3f);
            Kit("CandleStick_Stand", -3f, dais, 16.6f);
            Kit("CandleStick_Stand", 3f, dais, 16.6f);
            Put("Brazier", -7f, dais, 12f);
            Put("Brazier", 7f, dais, 12f);
            Loot("LargeOakChest", -5.4f, dais, 18.4f, 180f);
            _result.EscapePortals.Add(Put("EscapePortal", 5.6f, dais, 17.6f, 180f, false).GetComponent<PortalComponent>());
            Banner(North, 0, 8.4f, 2.2f, true);

            foreach (int bay in new[] { -4, -2, 2, 4 })
                Banner(North, bay, 7.8f, 1.6f, bay % 4 != 0);

            _result.BossSpawn = Marker("Boss", 0f, dais, 13.5f, 180f);

            foreach (float z in new[] { -16f, -10f, -4f, 4f })
            {
                TallPillar(-6.5f, z);
                TallPillar(6.5f, z);
            }

            foreach (int sign in new[] { -1, 1 })
            {
                Deck(sign > 0 ? edge : -Inner, sign > 0 ? Inner : -edge, -Inner, end, Upper, stone);
                Stairs(sign * (edge - _run), 0f, -12f, sign * 90f, 2.4f, Upper, stone);
                Rail(sign * edge, -Inner, sign * edge, -13.2f, Upper);
                Rail(sign * edge, -10.8f, sign * edge, end, Upper);
                Rail(sign * edge, end, sign * Inner, end, Upper);

                foreach (float z in new[] { -17f, -6f, 0.5f, 7.6f })
                    Post(sign * (edge + 0.2f), z, Upper - Slab);

                Put("StatueGuardian", sign * 12f, 0f, 14.5f, 180f);
                Put("Brazier", sign * 11f, 0f, 5f);
                Kit("Barrel", sign * 18.4f, Upper, 6.6f);
                Monster(sign * 18f, Upper, -2f, -sign * 90f);
                Monster(sign * 4f, 0f, 2f, -sign * 135f);

                foreach (int bay in new[] { -3, -1, 1 })
                    Banner(sign > 0 ? East : West, bay, 7.8f, 1.5f, bay != -1);
            }

            Loot("SmallOakChest", 18.2f, Upper, -18.2f, -45f);
            Loot("LargeOakChest", -18.9f, Upper, -7.3f, 90f);
            Crates(-18.2f, -18f, 2, Upper);
        }

        /// Torches in the third bays of every wall; tall rooms get a lantern high above every doorway.
        private void WallLights()
        {
            foreach (WallSide side in s_sides)
            {
                Put("WallTorch", AtWall(side, -3f * Bay, 0.02f, 2.6f), Facing(side));
                Put("WallTorch", AtWall(side, 3f * Bay, 0.02f, 2.6f), Facing(side));

                // The cell block owns the west wall of the gaol and lights it itself.
                if (_ceiling > Height && !(_room == DungeonRoom.Prison && side == West))
                    Kit("Lantern_Wall", AtWall(side, 0f, 0.02f, 6f), Facing(side));
            }
        }

        /// Cobwebs in two corners, chandeliers, candle wax by the walls: the mood layer of every module.
        private void Dress()
        {
            float corner = Half - 0.55f;
            int first = _random.Next(4);
            int second = (first + 1 + _random.Next(3)) % 4;

            for (int i = 0; i < 4; i++)
            {
                if (i != first && i != second)
                    continue;

                float sx = i % 2 == 0 ? -1f : 1f;
                float sz = i < 2 ? -1f : 1f;
                float yaw = sx > 0f == sz > 0f ? 135f : 45f;
                Put("Cobweb", sx * corner, _ceiling - 0.75f, sz * corner, sx > 0f ? yaw + 180f : yaw);
            }

            foreach (Vector2 point in ChandelierPoints())
            {
                if (_ceiling > Height)
                {
                    // Light falls off fast: in tall rooms the wheel hangs low on a double chain.
                    Put("Chain", point.x, _ceiling, point.y);
                    Put("Chain", point.x, _ceiling - ChainLength, point.y, 90f);
                    Put("Chandelier", point.x, _ceiling - ChainLength * 2f, point.y);
                }
                else
                {
                    Put("Chandelier", point.x, _ceiling, point.y);
                }
            }

            int candles = _room is DungeonRoom.Crypt or DungeonRoom.Shrine or DungeonRoom.Throne ? 3 : 1;

            for (int i = 0; i < candles; i++)
            {
                WallSide side = s_sides[_random.Next(4)];
                float along = Range(4f, Inner - 2f) * (_random.Next(2) == 0 ? -1f : 1f);

                // Galleries, cells and pits own the foot of some walls.
                if (_room is DungeonRoom.Prison or DungeonRoom.TrapCorridor or DungeonRoom.BonePit or DungeonRoom.Arrival)
                    continue;

                Put("CandleCluster", AtWall(side, along, Range(0.6f, 1.1f)), Range(0f, 360f));
            }
        }

        private Vector2[] ChandelierPoints()
        {
            return _room switch
            {
                DungeonRoom.Treasury => new[] { new Vector2(0f, 0f), new Vector2(15f, -15f), new Vector2(-15f, -15f) },
                DungeonRoom.Labyrinth => new[] { new Vector2(0f, 0f), new Vector2(-14.5f, -14.5f), new Vector2(14.5f, 14.5f) },
                DungeonRoom.Crypt => new[] { new Vector2(0f, 6f), new Vector2(0f, -6f) },
                DungeonRoom.TrapCorridor => new[] { new Vector2(0f, -17f), new Vector2(4f, 0.5f), new Vector2(15.5f, 12.5f) },
                DungeonRoom.BonePit => new[] { new Vector2(0f, 0f), new Vector2(-16.5f, 0f), new Vector2(16.5f, 0f) },
                DungeonRoom.Prison => new[] { new Vector2(-3f, 2f), new Vector2(-3f, -12f), new Vector2(-3f, 14f) },
                DungeonRoom.Library => new[] { new Vector2(-8f, 8f), new Vector2(-8f, -6f), new Vector2(8f, 6f), new Vector2(8f, -3f), new Vector2(8f, -11f) },
                DungeonRoom.Hall => new[] { new Vector2(0f, -8f), new Vector2(0f, 2f), new Vector2(-13f, -8f), new Vector2(13f, -8f) },
                DungeonRoom.GreatHall => new[] { new Vector2(0f, -10f), new Vector2(0f, 10f), new Vector2(-10.5f, 0f), new Vector2(10.5f, 0f) },
                DungeonRoom.Throne => new[] { new Vector2(0f, -12f), new Vector2(0f, -2f), new Vector2(-11f, -4f), new Vector2(11f, -4f), new Vector2(0f, 5f) },
                DungeonRoom.Cellar => new[] { new Vector2(-8f, 6f), new Vector2(6.2f, -4f), new Vector2(6.2f, 10f) },
                DungeonRoom.Armory => new[] { new Vector2(-14f, -7f), new Vector2(6f, -14f), new Vector2(4f, 9f) },
                DungeonRoom.Spawn or DungeonRoom.Shrine => new[] { new Vector2(0f, 0f), new Vector2(-10f, -10f), new Vector2(10f, 10f) },
                _ => new[] { new Vector2(-10f, -10f), new Vector2(10f, 10f) }
            };
        }

        private string ShrineName(int offset)
        {
            string[] names = { "ShrineHealth", "ShrineProtection", "ShrinePower", "ShrineSpeed" };

            return names[(_floor + offset) % names.Length];
        }

        private bool IsOpen(WallSide side)
        {
            return _open[(int)side];
        }

        /// Point at the given distance from the inner face of a wall; along runs east on north/south walls and north on east/west ones.
        private static Vector3 AtWall(WallSide side, float along, float distance, float y = 0f)
        {
            float offset = Inner - distance;

            return side switch
            {
                North => new Vector3(along, y, offset),
                South => new Vector3(along, y, -offset),
                East => new Vector3(offset, y, along),
                _ => new Vector3(-offset, y, along)
            };
        }

        /// Yaw that turns local +Z away from the wall, into the room.
        private static float Facing(WallSide side)
        {
            return side switch
            {
                North => 180f,
                East => -90f,
                South => 0f,
                _ => 90f
            };
        }

        private void Mount(string kit, WallSide side, int bay, float y, float scale = 1f, float distance = 0.02f)
        {
            Kit(kit, AtWall(side, bay * Bay, distance, y), Facing(side), scale);
        }

        private void Banner(WallSide side, int bay, float y, float scale, bool isRed)
        {
            Mount(isRed ? "Banner_1_Cloth" : "Banner_2_Cloth", side, bay, y, scale, 0.06f);
        }

        /// Long table with things scattered over the top and chairs on both sides.
        private void Table(float x, float z, float yaw, int seats, params string[] items)
        {
            Kit("Table_Large", x, 0f, z, yaw);
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 origin = new Vector3(x, 0f, z);

            for (int i = 0; i < items.Length; i++)
            {
                Vector3 local = new Vector3(Mathf.Lerp(-1.15f, 1.15f, (i + 0.5f) / items.Length), TableTop, Range(-0.3f, 0.3f));
                Kit(items[i], origin + rotation * local, Range(0f, 360f));
            }

            int perSide = (seats + 1) / 2;

            for (int i = 0; i < seats; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                float along = perSide < 2 ? 0f : Mathf.Lerp(-0.9f, 0.9f, i / 2 / (perSide - 1f));
                Kit("Chair_1", origin + rotation * new Vector3(along, 0f, side * 0.95f), yaw + (side > 0f ? 180f : 0f) + Range(-15f, 15f));
            }
        }

        private string[] Tableware(int count)
        {
            string[] items = new string[count];

            for (int i = 0; i < count; i++)
                items[i] = i == count / 2 && _random.Next(3) == 0 ? "CandleStick_Triple" : s_tableware[_random.Next(s_tableware.Length)];

            return items;
        }

        /// Bookcase standing on the point: a lootable one, or a decorative one with books on the shelves.
        private void Shelf(Vector3 position, float yaw, bool isLoot)
        {
            if (isLoot)
                Loot("Bookshelf", position, yaw);
            else
                Bookcase(position, yaw);
        }

        private void Bookcase(Vector3 position, float yaw)
        {
            Kit("Bookcase_2", position, yaw);
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);

            foreach (float shelf in s_shelves)
            {
                if (_random.Next(5) == 0)
                    continue;

                Kit("BookGroup_Medium_" + _random.Next(1, 4), position + rotation * new Vector3(-0.12f, shelf, 0.03f), yaw);

                if (_random.Next(2) == 0)
                    Kit(s_shelfItems[_random.Next(s_shelfItems.Length)], position + rotation * new Vector3(0.5f, shelf, 0.03f), yaw);
            }
        }

        /// Pile of crates: rows of two on the floor, every third crate sits on top of its row.
        private void Crates(float x, float z, int count, float y = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                int place = i % 3;
                bool isTop = place == 2;
                Kit(isTop ? "Crate_Metal" : "Crate_Wooden", x + (isTop ? 0.5f : place) * 0.98f, y + (isTop ? 0.9f : 0f), z + i / 3 * 1.05f, Range(-10f, 10f));
            }
        }

        private void Scatter(float x, float z, float radius, params string[] items)
        {
            foreach (string item in items)
                Kit(item, x + Range(-radius, radius), 0f, z + Range(-radius, radius), Range(0f, 360f));
        }

        private void TallPillar(float x, float z)
        {
            Put("Pillar", x, 0f, z);
            Put("Pillar", x, Height, z);
        }

        private void Deck(float xMin, float xMax, float zMin, float zMax, float top, Material material)
        {
            Solid(xMin, xMax, zMin, zMax, top, material, top - Slab);
        }

        private void Solid(float xMin, float xMax, float zMin, float zMax, float top, Material material, float bottom = 0f)
        {
            DungeonStructureBuilder.Block(_module, "Deck", new Vector3((xMin + xMax) * 0.5f, (top + bottom) * 0.5f, (zMin + zMax) * 0.5f),
                new Vector3(xMax - xMin, top - bottom, zMax - zMin), material);
        }

        private void Stairs(float x, float y, float z, float yaw, float width, float height, Material material)
        {
            DungeonStructureBuilder.Stairs(_module, new Vector3(x, y, z), yaw, width, height, material);
        }

        private void Rail(float x1, float z1, float x2, float z2, float y)
        {
            DungeonStructureBuilder.Rail(_module, new Vector3(x1, y, z1), new Vector3(x2, y, z2));
        }

        private void Post(float x, float z, float height)
        {
            DungeonStructureBuilder.Block(_module, "Post", new Vector3(x, height * 0.5f, z), new Vector3(0.35f, height, 0.35f), DungeonPropBuilder.DarkWood);
        }

        private void Wall(float x, float z, float yaw, float length, float height = Height)
        {
            DungeonStructureBuilder.Wall(_module, new Vector3(x, 0f, z), yaw, length, height);
        }

        private void Bars(float x, float y, float z, float yaw, float length, float height)
        {
            DungeonStructureBuilder.Bars(_module, new Vector3(x, y, z), yaw, length, height);
        }

        /// Inner wall with a doorway; the door prefab (if any) is hung into it.
        private DoorComponent Doorway(float x, float z, float yaw, float length, float offset, string door)
        {
            Vector3 center = new Vector3(x, 0f, z);
            DungeonStructureBuilder.Doorway(_module, center, yaw, length, offset, Height);

            if (door == null)
                return null;

            return Put(door, center + Quaternion.Euler(0f, yaw, 0f) * new Vector3(offset, 0f, 0f), yaw, false).GetComponent<DoorComponent>();
        }

        private GameObject Put(string prefab, float x, float y, float z, float yaw = 0f, bool isStatic = true)
        {
            return Put(prefab, new Vector3(x, y, z), yaw, isStatic);
        }

        private GameObject Put(string prefab, Vector3 position, float yaw, bool isStatic = true)
        {
            return DungeonMapBuilder.Place(Prefab(prefab), _module, position, yaw, isStatic);
        }

        private GameObject Kit(string name, float x, float y, float z, float yaw = 0f, float scale = 1f)
        {
            return Kit(name, new Vector3(x, y, z), yaw, scale);
        }

        private GameObject Kit(string name, Vector3 position, float yaw = 0f, float scale = 1f)
        {
            GameObject instance = DungeonMapBuilder.Place(DungeonKitBuilder.Load(name), _module, position, yaw);
            instance.transform.localScale = Vector3.one * scale;

            return instance;
        }

        private void Loot(string prefab, float x, float y, float z, float yaw)
        {
            Loot(prefab, new Vector3(x, y, z), yaw);
        }

        private void Loot(string prefab, Vector3 position, float yaw)
        {
            _result.Containers.Add(Put(prefab, position, yaw, false).GetComponent<ContainerComponent>());
        }

        private void Monster(float x, float y, float z, float yaw)
        {
            _result.MonsterSpawns.Add(Marker("Monster", x, y, z, yaw));
        }

        private Transform Marker(string name, float x, float y, float z, float yaw)
        {
            Transform marker = new GameObject(name).transform;
            marker.SetParent(_markers, false);
            marker.position = _module.position + new Vector3(x, y, z);
            marker.rotation = Quaternion.Euler(0f, yaw, 0f);

            return marker;
        }

        private float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        private string Pick(params string[] options)
        {
            return options[_random.Next(options.Length)];
        }

        private static GameObject Prefab(string name)
        {
            if (s_prefabs.TryGetValue(name, out GameObject prefab) && prefab != null)
                return prefab;

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(name));

            if (prefab == null)
                throw new System.ArgumentException($"Dungeon prefab '{name}' not found");

            s_prefabs[name] = prefab;

            return prefab;
        }
    }
}
