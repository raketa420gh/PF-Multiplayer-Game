using System.Collections.Generic;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Editor.Dungeon
{
    /// The Great Hall after the user's reference: a square hall of rough stone with four doors, banners, torches and chandeliers,
    /// and a walled sanctum in the middle (openings east and west) holding the treasure around a brazier on a stepped plinth.
    /// The hall around it is dressed with the Medieval Assets Pack, one purpose per stretch: an execution corner (north-west),
    /// a prison cell (north-east), a library (west), an armoury (east), the guards' mess (south-west) and a store (south-east).
    /// Skeletons and a flying head guard it; the escape portal opens at a random spot. Then the NavMesh and the map.
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
        private const float Half = RoomSize * 0.5f;
        private const float SanctumSize = 13.6f;
        private const float SanctumGap = 3f;
        private const float DoorOffset = 8.4f;
        private const float TorchHeight = 2.6f;
        private const float CellHeight = 3.2f;
        private const float CellThickness = 0.3f;
        private const float TableTop = 0.9f;

        public static Transform Build(DungeonDirector director, out Texture2D[] floorMaps)
        {
            BuildPieces();
            Transform root = new GameObject("[Dungeon]").transform;
            Transform floor = BattleEditorUtility.CreateChild("Floor1", root).transform;
            Transform spawns = new GameObject("[Spawns]").transform;
            Vector3 slab = new Vector3(RoomSize + WallThickness * 2f, WallThickness, RoomSize + WallThickness * 2f);
            List<ContainerComponent> containers = new();

            DungeonStructureBuilder.Block(floor, "Floor", Vector3.down * WallThickness * 0.5f, slab, DungeonPropBuilder.Flagstone);
            DungeonStructureBuilder.Block(floor, "Ceiling", Vector3.up * (Height + WallThickness * 0.5f), slab, DungeonPropBuilder.Ashlar);

            for (int i = 1; i < 8; i++)
                DungeonStructureBuilder.Block(floor, "Ceiling Beam", new Vector3(0f, Height - 0.2f, i * RoomSize / 8f - Half), new Vector3(RoomSize, 0.4f, 0.35f), DungeonPropBuilder.DarkWood, 0f, false);

            for (int i = 0; i < 4; i++)
            {
                Quaternion side = Quaternion.Euler(0f, i * 90f, 0f);
                float yaw = i * 90f;
                Wall(floor, side * new Vector3(0f, 0f, Half + WallThickness * 0.5f), slab.x, yaw);
                Wall(floor, side * new Vector3(Half - 0.45f, 0f, Half - 0.45f), 0.9f, yaw, 0.9f, Height, "Wall Pilaster");

                if (i % 2 == 0)
                {
                    Wall(floor, side * new Vector3(0f, 0f, Half - 0.25f), 1.2f, yaw, 0.5f, Height, "Wall Pilaster");

                    foreach (float x in new[] { -DoorOffset, DoorOffset })
                    {
                        Door(floor, side * new Vector3(x, 0f, Half), yaw + 180f);
                        Torch(floor, "HallTorch", side * new Vector3(x - 1.9f, TorchHeight, Half), yaw + 180f);
                        Torch(floor, "HallTorch", side * new Vector3(x + 1.9f, TorchHeight, Half), yaw + 180f);
                    }

                    foreach (float x in new[] { -3.4f, 3.4f })
                        Banner(floor, "Large Banner", side * new Vector3(x, 0f, Half), yaw + 180f);
                }
                else
                {
                    Torch(floor, "HallTorch", side * new Vector3(0f, TorchHeight, Half), yaw + 180f);

                    foreach (float x in new[] { -4.2f, 4.2f })
                        Banner(floor, "Small Banner", side * new Vector3(x, 0f, Half), yaw + 180f);
                }
            }

            foreach (Vector3 point in new[] { new Vector3(0f, 0f, 12.6f), new Vector3(0f, 0f, -12.6f), new Vector3(12.6f, 0f, 0f), new Vector3(-12.6f, 0f, 0f) })
                Prop(floor, "Chandelier", point + Vector3.up * (Height - 0.4f));

            BuildSanctum(floor, containers);
            BuildExecution(floor);
            BuildPrison(floor, containers);
            BuildLibrary(floor, containers);
            BuildArmoury(floor, containers);
            BuildMess(floor, containers);
            BuildStore(floor, containers);

            List<Transform> players = new();

            for (int i = 0; i < PlayerSpawnCount; i++)
                players.Add(BattleEditorUtility.CreateChild("Player" + i, spawns, new Vector3((i - (PlayerSpawnCount - 1) * 0.5f) * 2f, 0f, 3f - Half)).transform);

            DungeonDirector.MonsterPlacement[] monsters =
            {
                Monster(spawns, "SkeletonArcher", new Vector3(0f, 0f, Half - 4f), 180f),
                Monster(spawns, "SkeletonSwordsman", new Vector3(-9.5f, 0f, 0f), -90f),
                Monster(spawns, "FlyingHead", new Vector3(Half - 4.5f, 0f, -3f), -90f)
            };

            // Parked out of the way and invisible until it opens; the director then moves it to a random free spot.
            PortalComponent portal = Place(Load("EscapePortal"), floor, new Vector3(0f, 0f, -Half * 0.5f), 0f, false).GetComponent<PortalComponent>();

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
            BattleEditorUtility.Set(so, layout + "EscapeArea", Half);
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

        /// Walled room in the middle of the hall: openings east and west, the golden chest north and the large one south,
        /// heaps of gold in two corners, candelabra by the chests and offerings on the plinth steps.
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
                    Torch(floor, "SanctumTorch", side * new Vector3(-3.2f, TorchHeight, half), yaw + 180f);
                    Torch(floor, "SanctumTorch", side * new Vector3(3.2f, TorchHeight, half), yaw + 180f);

                    foreach (float x in new[] { -3.4f, 3.4f })
                        Banner(floor, "Large Banner", side * new Vector3(x, 0f, half + WallThickness), yaw);
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

            containers.Add(Container("GoldenChest", floor, new Vector3(0f, 0f, half - 1.2f), 180f));
            containers.Add(Container("LargeOakChest", floor, new Vector3(0f, 0f, 1.2f - half), 0f));
            containers.Add(Container("SmallOakChest", floor, new Vector3(-5.3f, 0f, 5.3f), 135f));

            foreach (float x in new[] { -1.6f, 1.6f })
            {
                Prop(floor, "Ornate Candle Stick", new Vector3(x, 0f, half - 0.9f));
                Prop(floor, "Ornate Candle Stick", new Vector3(x, 0f, 0.9f - half));
            }

            Prop(floor, "Gold Pile", new Vector3(4.6f, 0f, 4.6f), 20f, 0.55f);
            Prop(floor, "Gold Pile", new Vector3(-4.8f, 0f, -4.8f), 200f, 0.4f);
            Prop(floor, "Diamond", new Vector3(3.5f, 0f, 5.7f), 15f);
            Prop(floor, "Ruby", new Vector3(5.7f, 0f, 3.4f), 70f);
            Prop(floor, "Saphire", new Vector3(-3.8f, 0f, -5.7f));
            Prop(floor, "Necklace", new Vector3(1.4f, 0f, 4.4f), 30f);
            Prop(floor, "Goblet", new Vector3(0.92f, 0.4f, 0f));
            Prop(floor, "Silver Plate", new Vector3(0f, 0.2f, -1.3f));
            Prop(floor, "Goblet with Gem", new Vector3(0f, 0.24f, -1.3f), 40f);

            Prop(floor, "Wall Lever 01", new Vector3(edge + WallThickness * 0.5f, 1.3f, SanctumGap + 0.6f), 90f);
            Prop(floor, "Wall Lever 02", new Vector3(-edge - WallThickness * 0.5f, 1.3f, -SanctumGap - 0.6f), -90f);
        }

        /// North-west: guillotine and rack in the middle, a cross of stocks and a shackle plank on the walls, a spiked pit.
        private static void BuildExecution(Transform floor)
        {
            Prop(floor, "Guillotine", new Vector3(-13.6f, 0f, 12.6f), 90f);
            Prop(floor, "Rack Stretcher", new Vector3(-12.4f, 0f, 9f), 90f);
            Prop(floor, "X Stocks", new Vector3(-Half + 0.4f, 0f, 14.9f), 90f);
            Prop(floor, "Shackle Plank", new Vector3(-12f, 2f, Half), 180f);
            Banner(floor, "Large Banner", new Vector3(-14.6f, 0f, Half), 180f);
            Prop(floor, "Floor Trap Tile", new Vector3(-Half + 0.9f, 0f, 9.45f));
            Prop(floor, "Floor Trap Tile", new Vector3(-Half + 0.9f, 0f, 8.55f));
            Prop(floor, "Bucket", new Vector3(-10.3f, 0f, 11.4f), 30f);
            Prop(floor, "Big Candle", new Vector3(-Half + 0.9f, 0f, 11.6f));
            Prop(floor, "Medium Candle", new Vector3(-11.4f, 0f, 14.6f));
        }

        /// North-east: a cell with a barred door and a bunk on chains; stocks and a gibbet cage outside.
        private static void BuildPrison(Transform floor, List<ContainerComponent> containers)
        {
            const float front = 12.2f;
            const float side = 11.2f;
            const float door = 13.4f;
            const float doorHalf = 1.05f;
            float back = Half - front;
            float corner = side - CellThickness * 0.5f;

            CellWall(floor, new Vector3(side, 0f, front + back * 0.5f), back, 90f);
            CellWall(floor, new Vector3((corner + door - doorHalf) * 0.5f, 0f, front), door - doorHalf - corner, 0f);
            CellWall(floor, new Vector3((door + doorHalf + Half) * 0.5f, 0f, front), Half - door - doorHalf, 0f);
            DungeonStructureBuilder.Block(floor, "Wall Lintel", new Vector3(door, 2.75f, front), new Vector3(doorHalf * 2f, 0.9f, CellThickness), DungeonPropBuilder.Ashlar);
            Place(Load("CellDoor"), floor, new Vector3(door, 0f, front), 0f, false);

            Prop(floor, "Bed", new Vector3(14.6f, 1.2f, Half), 180f);
            Prop(floor, "Pillow", new Vector3(15.4f, 0.52f, Half - 0.45f));
            Prop(floor, "Shackle Plank", new Vector3(Half, 2f, 13.6f), -90f);
            Prop(floor, "Bowl", new Vector3(12.2f, 0f, 15.9f));
            Prop(floor, "Spoon", new Vector3(12.55f, 0f, 15.7f), 40f);
            Prop(floor, "Bucket", new Vector3(Half - 0.5f, 0f, front + 0.6f));
            Prop(floor, "Small Candle", new Vector3(11.7f, 0f, 16.4f));
            containers.Add(Container("SmallOakChest", floor, new Vector3(Half - 0.7f, 0f, 14.6f), -90f));

            Prop(floor, "Stocks", new Vector3(14f, 0f, 9.4f), 180f);
            Prop(floor, "Cage", new Vector3(10.8f, 0f, 9.6f));
            Prop(floor, "Wall Lever 03", new Vector3(5.7f, 1.3f, Half), 180f);
        }

        /// West: bookcases along the wall, a reading table with scrolls and a candle.
        private static void BuildLibrary(Transform floor, List<ContainerComponent> containers)
        {
            const float x = -13.4f;
            const float wall = -Half + 0.26f;

            Prop(floor, "Large Bookcase", new Vector3(wall, 0f, 2.2f), 90f);
            Prop(floor, "Large Bookcase", new Vector3(wall, 0f, -6.3f), 90f);
            Prop(floor, "Drawer Bookcase", new Vector3(wall, 0f, 6.3f), 90f);
            containers.Add(Container("Bookshelf", floor, new Vector3(wall, 0f, -2.2f), 90f));
            Prop(floor, "Medium Candle", new Vector3(wall, 1.76f, 6f));
            Prop(floor, "Small Candle", new Vector3(wall, 1.76f, 6.6f));

            Prop(floor, "Table", new Vector3(x, 0f, 0f), 90f);
            Prop(floor, "Chair", new Vector3(x + 1f, 0f, 0.1f), -90f);
            Prop(floor, "Stool", new Vector3(x - 1.2f, 0f, -0.5f));
            Prop(floor, "Open Scroll", new Vector3(x + 0.1f, TableTop, 0.15f), 90f);
            Prop(floor, "Large Scroll", new Vector3(x + 0.3f, TableTop + 0.045f, -0.9f), 0f, 1f, 90f);
            Prop(floor, "Small Scroll", new Vector3(x - 0.25f, TableTop + 0.045f, -0.8f), 30f, 1f, 90f);
            Prop(floor, "Folded Scroll", new Vector3(x - 0.2f, TableTop, 0.75f), 15f);
            Prop(floor, "Big Candle", new Vector3(x + 0.3f, TableTop, 0.8f));
        }

        /// East: weapon racks either side of a shield, swords on a crate and leaning on the wall, a helmet on a crate.
        private static void BuildArmoury(Transform floor, List<ContainerComponent> containers)
        {
            float wall = Half - 0.3f;

            Prop(floor, "Weapon Rack", new Vector3(wall, 0f, 2.2f), -90f);
            Prop(floor, "Weapon Rack", new Vector3(wall, 0f, -2.2f), -90f);
            Prop(floor, "Shield", new Vector3(Half - 0.25f, 0.48f, 0f), -90f);
            Prop(floor, "Big Crate", new Vector3(15.2f, 0f, 5.4f), 90f);
            Prop(floor, "Sword 01", new Vector3(15.05f, 0.75f, 4.8f), 0f, 1f, 90f);
            Prop(floor, "Sword 02", new Vector3(15.35f, 0.75f, 4.85f), 0f, 1f, 90f);
            Prop(floor, "Sword 03", new Vector3(Half - 0.29f, 0f, -4.7f), -90f, 1f, -14f);
            Prop(floor, "Sword 04", new Vector3(Half - 0.29f, 0f, -5.3f), -90f, 1f, -12f);
            containers.Add(Container("Crate", floor, new Vector3(15.4f, 0f, -6.6f), 10f));
            Prop(floor, "Helmet", new Vector3(15.4f, 0.87f, -6.6f), -60f);
            containers.Add(Container("Barrel", floor, new Vector3(15.9f, 0f, -8f), 40f));
        }

        /// South-west: a long table with benches, mugs, a jug and a supper; barrels and a crate in the corner.
        private static void BuildMess(Transform floor, List<ContainerComponent> containers)
        {
            const float x = -13f;
            const float z = -12.6f;

            Prop(floor, "Table", new Vector3(x, 0f, z));
            Prop(floor, "Seat", new Vector3(x, 0f, z + 1f), 180f);
            Prop(floor, "Seat", new Vector3(x, 0f, z - 1f));
            Prop(floor, "Chair", new Vector3(x - 1.6f, 0f, z), 90f);
            Prop(floor, "Stool", new Vector3(x + 1.7f, 0f, z + 0.2f));
            Prop(floor, "Plate", new Vector3(x - 0.75f, TableTop, z - 0.2f));
            Prop(floor, "Spoon", new Vector3(x - 0.7f, TableTop + 0.02f, z - 0.2f), 30f);
            Prop(floor, "Big Ale Mug", new Vector3(x - 0.3f, TableTop, z + 0.25f), 200f);
            Prop(floor, "Jug", new Vector3(x + 0.05f, TableTop, z - 0.1f));
            Prop(floor, "Small Ale Mug", new Vector3(x + 0.45f, TableTop, z + 0.2f), 120f);
            Prop(floor, "Bowl", new Vector3(x + 0.5f, TableTop, z - 0.3f));
            Prop(floor, "Candle Stick", new Vector3(x + 0.85f, TableTop, z));
            Banner(floor, "Small Banner", new Vector3(-14f, 0f, -Half), 0f);

            containers.Add(Container("Barrel", floor, new Vector3(-Half + 0.9f, 0f, -Half + 0.9f), 0f));
            containers.Add(Container("Barrel", floor, new Vector3(-Half + 1.95f, 0f, -Half + 0.75f), 50f));
            containers.Add(Container("Crate", floor, new Vector3(-Half + 0.8f, 0f, -Half + 2.05f), 8f));
            Prop(floor, "Big Crate", new Vector3(-11.6f, 0f, -Half + 0.5f));
        }

        /// South-east: barrels, crates stacked on a big one and a small chest.
        private static void BuildStore(Transform floor, List<ContainerComponent> containers)
        {
            containers.Add(Container("Barrel", floor, new Vector3(Half - 0.9f, 0f, -Half + 0.9f), 0f));
            containers.Add(Container("Barrel", floor, new Vector3(Half - 1.95f, 0f, -Half + 0.75f), 70f));
            containers.Add(Container("Barrel", floor, new Vector3(Half - 0.85f, 0f, -Half + 2f), 20f));
            containers.Add(Container("Crate", floor, new Vector3(13.4f, 0f, -Half + 0.6f), 8f));
            containers.Add(Container("SmallOakChest", floor, new Vector3(11.8f, 0f, -Half + 0.5f), 0f));
            Prop(floor, "Big Crate", new Vector3(Half - 0.4f, 0f, -12.9f), 90f);
            Prop(floor, "Crate", new Vector3(Half - 0.45f, 0.68f, -12.9f), 15f);
            Banner(floor, "Small Banner", new Vector3(14f, 0f, -Half), 0f);
            Prop(floor, "Ground Lever 01", new Vector3(-6f, 0f, -Half + 0.3f));
            Prop(floor, "Ground Lever 02", new Vector3(6f, 0f, -Half + 0.3f));
        }

        /// Wall block standing on the floor; length runs along local X, thickness faces the room.
        private static void Wall(Transform parent, Vector3 position, float length, float yaw, float thickness = WallThickness, float height = Height,
            string name = "Wall")
        {
            DungeonStructureBuilder.Block(parent, name, position + Vector3.up * height * 0.5f, new Vector3(length, height, thickness), DungeonPropBuilder.Ashlar, yaw);
        }

        private static void CellWall(Transform parent, Vector3 position, float length, float yaw)
        {
            Wall(parent, position, length, yaw, CellThickness, CellHeight);
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

        /// Torch in an iron bracket on the wall face; local +Z faces the room.
        private static void Torch(Transform parent, string name, Vector3 position, float yaw)
        {
            Place(Load(name), parent, position, yaw);
        }

        /// The pack banners hang from their bar; the big ones reach down to the knees, the small ones to the chest.
        private static void Banner(Transform parent, string name, Vector3 position, float yaw)
        {
            Place(DungeonMedievalBuilder.Load(name), parent, position + Vector3.up * (name == "Large Banner" ? 4.6f : 4.4f), yaw);
        }

        /// A pack prop; pitch tips it over along its local Z (scrolls and swords lying down, blades leaning on a wall).
        private static void Prop(Transform parent, string name, Vector3 position, float yaw = 0f, float scale = 1f, float pitch = 0f)
        {
            GameObject prop = Place(DungeonMedievalBuilder.Load(name), parent, position, yaw);
            prop.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            prop.transform.localScale = Vector3.one * scale;
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
