using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Rooms of the Tangled Catacombs and the pieces the generator joins them with. A room is one grid cell (floor, ceiling
    /// and what stands inside) without side walls: the generator puts a side with two doorways, each open or shut, on every
    /// side and a post on every corner. The rooms are the user's layouts from Tools/RoomLayouts/index.html, item for item:
    /// the helpers here mirror the page's (B → Block, C → Column, L → Prop, M → Monster, K → Container, T → Trap, TO → Torch,
    /// S → Starts and so on) and take the same arguments in the same coordinates (x east, z north, yaw clockwise, metres from
    /// the middle of the room), so a room is copied over from the page as it stands. The layouts are in CatacombRoomLayouts.
    /// Monsters, containers and traps are markers the host spawns at.
    internal static partial class CatacombRoomBuilder
    {
        public const string Folder = DungeonPropBuilder.PrefabsFolder + "/Catacombs";
        public const int MapPixels = 144;
        public const float DoorWidth = 3f;
        /// The two doorways of a side sit this far either side of its middle: in the middles of its halves.
        public const float DoorOffset = DungeonMapBuilder.RoomSize * 0.25f;

        private const float Pitch = DungeonMapBuilder.Pitch;
        private const float Height = DungeonMapBuilder.Height;
        private const float Thickness = DungeonPropBuilder.WallThickness;
        private const float Half = DungeonMapBuilder.RoomSize * 0.5f;
        private const float DoorHeight = 3.4f;
        private const float TorchHeight = 2.6f;
        private const float PitWall = 0.3f;
        private const float WaterLevel = 0.12f;
        /// Water slows whoever wades in it but not those on the walkways a quarter metre above it.
        private const float WaterDepth = 0.24f;
        private const int WaterLayer = 2;
        private const float BarSpacing = 0.25f;
        private const float BarHeight = 3f;
        /// Rooms are built and photographed for their map far below everything else of the scene.
        private static readonly Vector3 s_workshop = new(0f, -400f, 0f);

        /// Medieval pack models standing in for the page's furniture; their fronts face local +Z.
        private static readonly Dictionary<string, string> s_models = new()
        {
            ["Стол"] = "Table", ["Лавка"] = "Seat", ["Табурет"] = "Stool", ["Койка"] = "Bed", ["Стойка с оружием"] = "Weapon Rack",
            ["Стеллаж с банками"] = "Drawer Bookcase", ["Буфет с посудой"] = "Drawer Bookcase", ["Полки с припасами"] = "Large Bookcase"
        };

        private static readonly HashSet<string> s_againstWall = new() { "Weapon Rack", "Large Bookcase", "Drawer Bookcase" };
        private static readonly HashSet<string> s_stone = new() { "Алтарь", "Жертвенник", "Парапет", "Укрытие", "Обломки", "Колодец", "Очаг" };

        public sealed class Kind
        {
            public CatacombRoomComponent Prefab;
            public float Weight;
            public int Limit;
        }

        public sealed class Result
        {
            public Kind[] Kinds;
            /// Sides by their open doorways: bit 0 the one at local -X, bit 1 the one at +X.
            public GameObject[] Sides;
            public GameObject Post;
        }

        private enum Shape
        {
            Block,
            Bars,
            Prop,
            Monster,
            Container,
            Trap,
            Light,
            Start,
            Pit,
            Water,
            Platform,
            Stairs,
            Rune
        }

        private enum Mob
        {
            Sword,
            Archer,
            Crossbow,
            Warrior,
            Mage,
            Skull
        }

        private enum Loot
        {
            Rich,
            Large,
            Small,
            Coffin,
            Shelf,
            Barrel,
            Crate,
            Urn
        }

        private enum Snare
        {
            Spike,
            Blade
        }

        private enum Glim
        {
            Torch,
            Candle,
            Brazier,
            Fire,
            Shaft,
            Glow
        }

        /// One thing of a layout, as the page has it.
        private sealed class Item
        {
            public Shape Shape;
            public int Kind;
            public string Name;
            public float X, Z, W, D, H, R, Y, Y0, Radius;
            public bool IsColumn, IsRound, IsOnWall;
            public Color Color;

            /// The item turned by quarter turns clockwise around the middle of the room.
            public Item Turn(int quarters)
            {
                Item item = (Item)MemberwiseClone();

                for (int i = 0; i < (quarters % 4 + 4) % 4; i++)
                    (item.X, item.Z) = (item.Z, -item.X);

                item.R += quarters * 90f;

                if (quarters % 2 != 0 && Shape is Shape.Pit or Shape.Water or Shape.Platform)
                    (item.W, item.D) = (D, W);

                return item;
            }
        }

        private sealed class Layout
        {
            public string Name;
            public string Title;
            public float Weight;
            public int Limit;
            public bool IsCorner;
            public List<Item> Items;
        }

        private sealed class Site
        {
            public Layout Layout;
            public Transform Root;
            public readonly List<DungeonDirector.MonsterPlacement> Monsters = new();
            public readonly List<DungeonDirector.MonsterPlacement> Containers = new();
            public readonly List<DungeonDirector.MonsterPlacement> Traps = new();
            public Transform Start;
        }

        public static Result Build()
        {
            BattleEditorUtility.EnsureFolder(Folder);
            Save(DungeonPropBuilder.Campfire(), "CatacombFire");

            return new Result
            {
                Kinds = Layouts().Select(Realize).ToArray(),
                Sides = Enumerable.Range(0, 4).Select(doorways => Save(Side(doorways), $"CatacombSide{doorways}")).ToArray(),
                Post = Save(Post(), "CatacombPost")
            };
        }

        // ---------- the page's helpers ----------

        private static Layout Room(string name, string title, float weight, int limit, bool isCorner, params object[] parts)
        {
            List<Item> items = new();

            foreach (object part in parts)
            {
                if (part is Item item)
                    items.Add(item);
                else
                    items.AddRange(((IEnumerable)part).Cast<Item>());
            }

            return new Layout { Name = name, Title = title, Weight = weight, Limit = limit, IsCorner = isCorner, Items = items };
        }

        /// The items and their turns by one, two and three quarters.
        private static IEnumerable<Item> Symmetric(params object[] parts)
        {
            List<Item> items = Room(null, null, 0f, 0, false, parts).Items;

            return Enumerable.Range(0, 4).SelectMany(quarters => items.Select(item => item.Turn(quarters))).ToList();
        }

        private static Item Block(double x, double z, double w, double d, double h = Height, double r = 0)
        {
            return new Item { Shape = Shape.Block, X = (float)x, Z = (float)z, W = (float)w, D = (float)d, H = (float)h, R = (float)r };
        }

        private static Item Column(double x, double z, double s = 1.2, double h = Height)
        {
            Item column = Block(x, z, s, s, h);
            column.IsColumn = true;

            return column;
        }

        /// A wall from one point to another along X or Z.
        private static Item Segment(double x1, double z1, double x2, double z2, double th = Thickness, double h = Height)
        {
            return x1 == x2 ? Block(x1, (z1 + z2) / 2, th, System.Math.Abs(z2 - z1), h) : Block((x1 + x2) / 2, z1, System.Math.Abs(x2 - x1), th, h);
        }

        private static Item Bars(double x1, double z1, double x2, double z2)
        {
            Item bars = Segment(x1, z1, x2, z2, 0.12, BarHeight);
            bars.Shape = Shape.Bars;

            return bars;
        }

        private static Item Prop(double x, double z, double w, double d, double h, string name, double r = 0, bool isRound = false)
        {
            return new Item { Shape = Shape.Prop, Name = name, X = (float)x, Z = (float)z, W = (float)w, D = (float)d, H = (float)h, R = (float)r, IsRound = isRound };
        }

        private static Item Skulls(double x, double z, double s = 1)
        {
            return Prop(x, z, 1.1 * s, 1.1 * s, 0.5 * s, "Черепа", 0, true);
        }

        private static Item Crates(double x, double z, double w, double d, double h = 2.5, double r = 0)
        {
            return Prop(x, z, w, d, h, "Штабель ящиков", r);
        }

        private static Item Monster(Mob kind, double x, double z, double r = 0)
        {
            return new Item { Shape = Shape.Monster, Kind = (int)kind, X = (float)x, Z = (float)z, R = (float)r };
        }

        private static Item Container(Loot kind, double x, double z, double r = 0)
        {
            return new Item { Shape = Shape.Container, Kind = (int)kind, X = (float)x, Z = (float)z, R = (float)r };
        }

        private static Item Trap(Snare kind, double x, double z, double r = 0)
        {
            return new Item { Shape = Shape.Trap, Kind = (int)kind, X = (float)x, Z = (float)z, R = (float)r };
        }

        /// Torch on the wall of a side (0 north, 1 east, 2 south, 3 west), offset along it.
        private static Item Torch(int side, double offset)
        {
            Item torch = new Item { X = (float)offset, Z = Half }.Turn(side);
            torch.Shape = Shape.Light;
            torch.Kind = (int)Glim.Torch;
            torch.R = side * 90f + 180f;
            torch.IsOnWall = true;

            return torch;
        }

        /// Torch on the face of a wall inside the room, facing yaw.
        private static Item TorchAt(double x, double z, double r)
        {
            return Lamp(Glim.Torch, x, z, r: r);
        }

        private static Item Candle(double x, double z, double y = 0)
        {
            return Lamp(Glim.Candle, x, z, y);
        }

        private static Item Brazier(double x, double z)
        {
            return Lamp(Glim.Brazier, x, z);
        }

        private static Item Fire(double x, double z)
        {
            return Lamp(Glim.Fire, x, z);
        }

        /// Cold light falling through a breach in the ceiling.
        private static Item Shaft(double x, double z, double radius = 9)
        {
            Item shaft = Lamp(Glim.Shaft, x, z);
            shaft.Radius = (float)radius;

            return shaft;
        }

        private static Item Glow(double x, double z, Color color, double radius = 7)
        {
            Item glow = Lamp(Glim.Glow, x, z);
            glow.Color = color;
            glow.Radius = (float)radius;

            return glow;
        }

        private static Item Lamp(Glim kind, double x, double z, double y = 0, double r = 0)
        {
            return new Item { Shape = Shape.Light, Kind = (int)kind, X = (float)x, Z = (float)z, Y = (float)y, R = (float)r };
        }

        private static Item Starts(double x, double z, double r = 0)
        {
            return new Item { Shape = Shape.Start, X = (float)x, Z = (float)z, R = (float)r };
        }

        private static Item Pit(double x, double z, double w, double d, double depth)
        {
            return new Item { Shape = Shape.Pit, X = (float)x, Z = (float)z, W = (float)w, D = (float)d, H = (float)depth };
        }

        private static Item Water(double x, double z, double w, double d)
        {
            return new Item { Shape = Shape.Water, X = (float)x, Z = (float)z, W = (float)w, D = (float)d };
        }

        private static Item Platform(double x, double z, double w, double d, double h)
        {
            return new Item { Shape = Shape.Platform, X = (float)x, Z = (float)z, W = (float)w, D = (float)d, H = (float)h };
        }

        /// Flight of w × d climbing h along its local +Z from y0.
        private static Item Stairs(double x, double z, double w, double d, double h, double r = 0, double y0 = 0)
        {
            return new Item { Shape = Shape.Stairs, X = (float)x, Z = (float)z, W = (float)w, D = (float)d, H = (float)h, R = (float)r, Y0 = (float)y0 };
        }

        private static Item Rune(double x, double z, double radius)
        {
            return new Item { Shape = Shape.Rune, X = (float)x, Z = (float)z, Radius = (float)radius };
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);

            return color;
        }

        // ---------- building a room ----------

        private static Kind Realize(Layout layout)
        {
            Site room = new Site { Layout = layout, Root = new GameObject(layout.Name).transform };
            room.Root.position = s_workshop;
            Floor(room);
            DungeonStructureBuilder.Block(room.Root, "Ceiling", Vector3.up * (Height + Thickness * 0.5f), new Vector3(Pitch, Thickness, Pitch), DungeonPropBuilder.Ashlar);

            foreach (Item item in layout.Items)
                Place(room, item, FloorAt(layout, item.X, item.Z));

            Texture2D map = DungeonMinimapBuilder.Render(room.Root, s_workshop, Pitch * 0.5f, MapPixels, $"Catacomb{layout.Name}", true);
            SerializedObject so = new SerializedObject(room.Root.gameObject.AddComponent<CatacombRoomComponent>());
            BattleEditorUtility.Set(so, "_title", layout.Title);
            BattleEditorUtility.Set(so, "_isCorner", layout.IsCorner);
            BattleEditorUtility.Set(so, "_map", map);
            BattleEditorUtility.Set(so, "_startSpots", room.Start);
            SetPlacements(so.FindProperty("_monsters"), room.Monsters);
            SetPlacements(so.FindProperty("_containers"), room.Containers);
            SetPlacements(so.FindProperty("_traps"), room.Traps);
            so.ApplyModifiedPropertiesWithoutUndo();
            room.Root.position = Vector3.zero;

            return new Kind { Prefab = Save(room.Root.gameObject, layout.Name).GetComponent<CatacombRoomComponent>(), Weight = layout.Weight, Limit = layout.Limit };
        }

        private static void Place(Site room, Item item, float y)
        {
            Transform root = room.Root;

            switch (item.Shape)
            {
                case Shape.Block:
                    DungeonStructureBuilder.Block(root, item.IsColumn ? "Wall Column" : "Wall", new Vector3(item.X, item.H * 0.5f, item.Z), new Vector3(item.W, item.H, item.D),
                        item.IsColumn ? DungeonPropBuilder.StoneWall : DungeonPropBuilder.Ashlar, item.R);
                    break;
                case Shape.Bars:
                    BuildBars(root, item);
                    break;
                case Shape.Prop:
                    BuildProp(root, item, y);
                    break;
                case Shape.Monster:
                    room.Monsters.Add(MonsterPlacement(root, (Mob)item.Kind, new Vector3(item.X, y, item.Z), item.R));
                    break;
                case Shape.Container:
                    room.Containers.Add(LootPlacement(root, (Loot)item.Kind, new Vector3(item.X, y, item.Z), item.R));
                    break;
                case Shape.Trap:
                    string trap = (Snare)item.Kind == Snare.Spike ? "SpikeTrap" : "BladeTrap";
                    room.Traps.Add(new DungeonDirector.MonsterPlacement { Point = Marker(root, $"Trap {trap}", new Vector3(item.X, y, item.Z), item.R), Prefab = DungeonSceneBuilder.LoadNetworkObject(trap) });
                    break;
                case Shape.Light:
                    BuildLight(root, item, y);
                    break;
                case Shape.Start:
                    room.Start = Marker(root, "Start", new Vector3(item.X, y, item.Z), item.R);

                    for (int i = -1; i <= 1; i++)
                        BattleEditorUtility.CreateChild("Spot", room.Start, new Vector3(i * 1.2f, 0f, 0f)).transform.localRotation = Quaternion.identity;
                    break;
                case Shape.Water:
                    BuildWater(root, item);
                    break;
                case Shape.Platform:
                    BuildPlatform(root, item);
                    break;
                case Shape.Stairs:
                    BuildStairs(root, item);
                    break;
                case Shape.Rune:
                    BuildRune(root, item);
                    break;
            }
        }

        /// Height of the floor under a point, as the page reckons it: pits sink it, platforms raise it, stairs slope it.
        private static float FloorAt(Layout layout, float x, float z)
        {
            float y = 0f;

            foreach (Item item in layout.Items)
            {
                if (item.Shape == Shape.Pit && Inside(item, x, z))
                    y = -item.H;
            }

            foreach (Item item in layout.Items)
            {
                if (item.Shape == Shape.Platform && Inside(item, x, z))
                    y = Mathf.Max(y, item.H);
            }

            foreach (Item item in layout.Items)
            {
                if (item.Shape != Shape.Stairs || !Inside(item, x, z))
                    continue;

                float along = (x - item.X) * Mathf.Sin(item.R * Mathf.Deg2Rad) + (z - item.Z) * Mathf.Cos(item.R * Mathf.Deg2Rad);
                y = item.Y0 + item.H * Mathf.Clamp01((along + item.D * 0.5f) / item.D);
            }

            return y;
        }

        private static bool Inside(Item item, float x, float z)
        {
            float yaw = item.Shape == Shape.Stairs ? item.R * Mathf.Deg2Rad : 0f;
            float dx = x - item.X;
            float dz = z - item.Z;

            return Mathf.Abs(dx * Mathf.Cos(yaw) - dz * Mathf.Sin(yaw)) <= item.W * 0.5f && Mathf.Abs(dx * Mathf.Sin(yaw) + dz * Mathf.Cos(yaw)) <= item.D * 0.5f;
        }

        /// The floor cut by the edges of the pits into rectangles, each at its own depth; a deeper one is walled where it meets
        /// a shallower one. The floor reaches under the walls, so doorways have floor too.
        private static void Floor(Site room)
        {
            List<Item> pits = room.Layout.Items.Where(item => item.Shape == Shape.Pit).ToList();
            float edge = Pitch * 0.5f;
            float[] xs = Cuts(pits.SelectMany(pit => new[] { pit.X - pit.W * 0.5f, pit.X + pit.W * 0.5f }), edge);
            float[] zs = Cuts(pits.SelectMany(pit => new[] { pit.Z - pit.D * 0.5f, pit.Z + pit.D * 0.5f }), edge);

            float Depth(int i, int j)
            {
                if (i < 0 || j < 0 || i >= xs.Length - 1 || j >= zs.Length - 1)
                    return 0f;

                float x = (xs[i] + xs[i + 1]) * 0.5f;
                float z = (zs[j] + zs[j + 1]) * 0.5f;
                Item pit = pits.FirstOrDefault(p => Inside(p, x, z));

                return pit != null ? pit.H : 0f;
            }

            for (int i = 0; i < xs.Length - 1; i++)
            {
                for (int j = 0; j < zs.Length - 1; j++)
                {
                    float depth = Depth(i, j);
                    Vector2 min = new Vector2(xs[i], zs[j]);
                    Vector2 max = new Vector2(xs[i + 1], zs[j + 1]);
                    Vector2 middle = (min + max) * 0.5f;
                    Vector2 size = max - min;
                    DungeonStructureBuilder.Block(room.Root, "Floor", new Vector3(middle.x, -depth - Thickness * 0.5f, middle.y), new Vector3(size.x, Thickness, size.y), DungeonPropBuilder.Flagstone);

                    if (depth <= 0f)
                        continue;

                    for (int side = 0; side < 4; side++)
                    {
                        int di = side == 0 ? -1 : side == 1 ? 1 : 0;
                        int dj = side == 2 ? -1 : side == 3 ? 1 : 0;
                        float rise = depth - Depth(i + di, j + dj);

                        if (rise <= 0f)
                            continue;

                        Vector3 center = new Vector3(di == 0 ? middle.x : di < 0 ? min.x + PitWall * 0.5f : max.x - PitWall * 0.5f, -depth + rise * 0.5f,
                            dj == 0 ? middle.y : dj < 0 ? min.y + PitWall * 0.5f : max.y - PitWall * 0.5f);
                        Vector3 extent = new Vector3(di == 0 ? size.x : PitWall, rise, dj == 0 ? size.y : PitWall);
                        DungeonStructureBuilder.Block(room.Root, "Pit Wall", center, extent, DungeonPropBuilder.Ashlar);
                    }
                }
            }
        }

        private static float[] Cuts(IEnumerable<float> values, float edge)
        {
            return values.Select(value => Mathf.Round(Mathf.Clamp(value, -edge, edge) * 1000f) / 1000f).Append(-edge).Append(edge).Distinct().OrderBy(value => value).ToArray();
        }

        private static DungeonDirector.MonsterPlacement MonsterPlacement(Transform root, Mob kind, Vector3 point, float yaw)
        {
            // Crossbowmen and mages are not in the game yet (DD-016): their points take the nearest monster there is and keep
            // their name, to be pointed at the real one when it comes.
            (string prefab, bool isStandIn) = kind switch
            {
                Mob.Archer => ("SkeletonArcher", false),
                Mob.Crossbow => ("SkeletonArcher", true),
                Mob.Warrior => ("SkeletonWarrior", false),
                Mob.Mage => ("SkeletonArcher", true),
                Mob.Skull => ("FlyingHead", false),
                _ => ("SkeletonSwordsman", false)
            };
            string name = isStandIn ? $"Monster {kind} (stand-in {prefab}, DD-016)" : $"Monster {kind}";

            return new DungeonDirector.MonsterPlacement { Point = Marker(root, name, point, yaw), Prefab = DungeonSceneBuilder.LoadNetworkObject(prefab) };
        }

        private static DungeonDirector.MonsterPlacement LootPlacement(Transform root, Loot kind, Vector3 point, float yaw)
        {
            // There is no urn yet: a crate stands in for it.
            string prefab = kind switch
            {
                Loot.Rich => "GoldenChest",
                Loot.Large => "LargeOakChest",
                Loot.Small => "SmallOakChest",
                Loot.Coffin => "Coffin",
                Loot.Shelf => "Bookshelf",
                Loot.Barrel => "Barrel",
                _ => "Crate"
            };
            string name = kind == Loot.Urn ? "Container Urn (stand-in Crate)" : $"Container {prefab}";

            return new DungeonDirector.MonsterPlacement { Point = Marker(root, name, point, yaw), Prefab = DungeonSceneBuilder.LoadNetworkObject(prefab) };
        }

        /// Furniture and rubble: a pack model where the page's piece has one of its size, otherwise a block of stone, metal or wood.
        /// Anything taller than a step stops bodies with a box of the page's size. Named as a wall, so nothing stands on its top.
        private static void BuildProp(Transform root, Item item, float y)
        {
            GameObject prop = BattleEditorUtility.CreateChild($"Wall Prop {item.Name}", root, new Vector3(item.X, y, item.Z));
            prop.transform.localRotation = Quaternion.Euler(0f, item.R, 0f);
            prop.isStatic = true;

            if (item.H > 0.3f)
            {
                BoxCollider collider = prop.AddComponent<BoxCollider>();
                collider.center = Vector3.up * item.H * 0.5f;
                collider.size = new Vector3(item.W, item.H, item.D);
            }

            switch (item.Name)
            {
                case "Черепа":
                    DungeonMapBuilder.Place(DungeonMapBuilder.Load("SkullPile"), prop.transform, Vector3.zero, 0f).transform.localScale = Vector3.one * item.W / 1.1f;

                    return;
                case "Гроб":
                    GameObject coffin = DungeonPropBuilder.Coffin(out _);
                    coffin.transform.SetParent(prop.transform, false);

                    return;
                case "Штабель ящиков":
                    // The plain crate: the size of the big one at a quarter of its triangles, stacks run to hundreds per room.
                    Fill(prop.transform, "Crate", item.W, item.D, item.H);

                    return;
                case "Бочки":
                    Fill(prop.transform, "Barrel", item.W, item.D, 0f);

                    return;
            }

            if (s_models.TryGetValue(item.Name, out string model) && Mathf.Max(item.W, item.D) <= 2.5f)
            {
                float yaw = s_againstWall.Contains(model) ? WallFacing(item) : item.R + (model == "Bed" && item.W > item.D ? 90f : 0f);
                DungeonMapBuilder.Place(DungeonMedievalBuilder.Load(model), prop.transform, Vector3.zero, yaw - item.R);

                return;
            }

            Material material = s_stone.Contains(item.Name) ? DungeonPropBuilder.StoneWall
                : item.Name == "Котёл" ? DungeonPropBuilder.RustyMetal
                : item.Name == "Кости" ? DungeonPropBuilder.Bone
                : item.Name is "Спальник" or "Мешки" ? DungeonPropBuilder.WoodPlanks
                : DungeonPropBuilder.DarkWood;
            string key = $"CatacombProp_{(item.IsRound ? "Round" : "Box")}_{item.W:0.##}x{item.H:0.##}x{item.D:0.##}".Replace(',', '.');
            DungeonMeshBuilder builder = new DungeonMeshBuilder(0.5f);
            Mesh mesh = (item.IsRound ? builder.Cylinder(Vector3.up * item.H * 0.5f, item.W * 0.5f, item.H, 16) : builder.Box(Vector3.up * item.H * 0.5f, new Vector3(item.W, item.H, item.D))).Save(key);
            DungeonPropBuilder.MeshObject("Body", prop.transform, mesh, material, default, default, false);
        }

        /// Wall-standing pieces turn their front to the room, away from the nearest wall.
        private static float WallFacing(Item item)
        {
            if (Mathf.Abs(item.X) > Mathf.Abs(item.Z))
                return item.X > 0f ? -90f : 90f;

            return item.Z > 0f ? 180f : 0f;
        }

        /// Pack models of one kind side by side over w × d, stacked up to h.
        private static void Fill(Transform parent, string model, float width, float depth, float height)
        {
            GameObject prefab = DungeonMedievalBuilder.Load(model);
            GameObject probe = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Bounds bounds = new Bounds(probe.transform.position, Vector3.zero);

            foreach (Renderer renderer in probe.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(renderer.bounds);

            Object.DestroyImmediate(probe);
            int across = Mathf.Max(1, Mathf.FloorToInt(width / bounds.size.x));
            int along = Mathf.Max(1, Mathf.FloorToInt(depth / bounds.size.z));
            int up = Mathf.Max(1, Mathf.FloorToInt(height / bounds.size.y));

            for (int i = 0; i < across; i++)
            {
                for (int j = 0; j < along; j++)
                {
                    for (int k = 0; k < up; k++)
                    {
                        Vector3 point = new Vector3((i - (across - 1) * 0.5f) * bounds.size.x, k * bounds.size.y, (j - (along - 1) * 0.5f) * bounds.size.z);
                        DungeonMapBuilder.Place(prefab, parent, point, (i + j + k) % 2 * 90f);
                    }
                }
            }
        }

        private static void BuildLight(Transform root, Item item, float y)
        {
            switch ((Glim)item.Kind)
            {
                case Glim.Torch:
                    DungeonMapBuilder.Place(DungeonMapBuilder.Load("HallTorch"), root, new Vector3(item.X, y + TorchHeight, item.Z), item.R);
                    break;
                case Glim.Candle:
                    DungeonMapBuilder.Place(DungeonMapBuilder.Load("CandleCluster"), root, new Vector3(item.X, y + item.Y, item.Z), 0f);
                    break;
                case Glim.Brazier:
                    DungeonMapBuilder.Place(DungeonMapBuilder.Load("Brazier"), root, new Vector3(item.X, y, item.Z), 0f);
                    break;
                case Glim.Fire:
                    DungeonMapBuilder.Place(AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/CatacombFire.prefab"), root, new Vector3(item.X, y, item.Z), 0f);
                    break;
                case Glim.Shaft:
                    Light shaft = BattleEditorUtility.CreateChild("Shaft Light", root, new Vector3(item.X, Height - 0.1f, item.Z)).AddComponent<Light>();
                    shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    shaft.type = LightType.Spot;
                    shaft.color = new Color(0.8f, 0.88f, 1f);
                    shaft.range = Height + 2f;
                    shaft.spotAngle = 2f * Mathf.Atan(item.Radius * 0.6f / Height) * Mathf.Rad2Deg;
                    shaft.innerSpotAngle = shaft.spotAngle * 0.5f;
                    shaft.intensity = 12f;
                    shaft.shadows = LightShadows.Soft;
                    break;
                case Glim.Glow:
                    Material glow = DungeonPropBuilder.Emissive("CatacombGlow", item.Color, 3f);
                    Mesh surface = new DungeonMeshBuilder(1f).Cylinder(Vector3.zero, 0.65f, 0.02f, 16).Save("CatacombGlowSurface");
                    DungeonPropBuilder.MeshObject("Glow", root, surface, glow, new Vector3(item.X, y + 1.01f, item.Z), default, false);
                    DungeonPropBuilder.PointLight(root, new Vector3(item.X, y + 1.3f, item.Z), item.Color, item.Radius * 1.5f, 2.5f, true);
                    break;
            }
        }

        /// Iron bars every quarter metre under a rail: bodies cannot squeeze through, light and shots pass between them.
        private static void BuildBars(Transform root, Item item)
        {
            bool isAlongX = item.W >= item.D;
            float length = Mathf.Max(item.W, item.D);
            int count = Mathf.FloorToInt(length / BarSpacing) + 1;
            DungeonMeshBuilder builder = new DungeonMeshBuilder(1f).Box(new Vector3(0f, BarHeight - 0.1f, 0f), new Vector3(length, 0.08f, 0.08f));
            GameObject bars = BattleEditorUtility.CreateChild("Bars", root, new Vector3(item.X, 0f, item.Z));
            bars.transform.localRotation = Quaternion.Euler(0f, item.R + (isAlongX ? 0f : 90f), 0f);
            bars.isStatic = true;

            for (int i = 0; i < count; i++)
            {
                float x = -length * 0.5f + i * BarSpacing;
                builder.Cylinder(new Vector3(x, BarHeight * 0.5f, 0f), 0.025f, BarHeight, 6);
                BoxCollider collider = bars.AddComponent<BoxCollider>();
                collider.center = new Vector3(x, BarHeight * 0.5f, 0f);
                collider.size = new Vector3(0.06f, BarHeight, 0.06f);
            }

            DungeonPropBuilder.MeshObject("Iron", bars.transform, builder.Save($"CatacombBars_{length:0.##}".Replace(',', '.')), DungeonPropBuilder.RustyMetal, default, default, false);
        }

        /// A surface to see and, on the Ignore Raycast layer, a shallow trigger the movement code slows bodies in.
        private static void BuildWater(Transform root, Item item)
        {
            Material material = DungeonPropBuilder.TransparentUnlit("CatacombWater", new Color(0.06f, 0.16f, 0.22f, 0.75f));
            Mesh mesh = new DungeonMeshBuilder(0.5f).Quad(Vector3.zero, Vector3.up, Vector3.right * item.W * 0.5f, Vector3.forward * item.D * 0.5f).Save($"CatacombWater_{item.W:0.##}x{item.D:0.##}".Replace(',', '.'));
            GameObject water = DungeonPropBuilder.MeshObject("Water", root, mesh, material, new Vector3(item.X, WaterLevel, item.Z), default, false);
            GameObject volume = BattleEditorUtility.CreateChild("Water Volume", root, new Vector3(item.X, 0f, item.Z));
            volume.layer = WaterLayer;
            volume.isStatic = true;
            BoxCollider trigger = volume.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.up * WaterDepth * 0.5f;
            trigger.size = new Vector3(item.W, WaterDepth, item.D);
            water.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// The NavMesh sees only the faces of colliders: a block taller than a body would leave walkable floor inside it, so
        /// a platform is stacked of layers lower than that, the lowest sunk into the floor slab.
        private static void BuildPlatform(Transform root, Item item)
        {
            int layers = Mathf.CeilToInt((item.H + Thickness) / 1.5f);
            float layer = (item.H + Thickness) / layers;

            for (int i = 0; i < layers; i++)
                DungeonStructureBuilder.Block(root, "Platform", new Vector3(item.X, -Thickness + (i + 0.5f) * layer, item.Z), new Vector3(item.W, layer, item.D), DungeonPropBuilder.Ashlar);
        }

        /// Steps to see and a hidden convex ramp to walk, as the NavMesh and the character controllers need; hidden blocks under
        /// the ramp, never above it, fill the space below, so the NavMesh finds no floor there.
        private static void BuildStairs(Transform root, Item item)
        {
            int steps = Mathf.Max(2, Mathf.RoundToInt(item.H / 0.25f));
            float run = item.D / steps;
            float top = item.Y0 + item.H;
            string key = $"CatacombStairs_{item.W:0.##}x{item.D:0.##}x{item.H:0.##}x{item.Y0:0.##}".Replace(',', '.');
            DungeonMeshBuilder visual = new DungeonMeshBuilder(0.5f);

            for (int i = 0; i < steps; i++)
            {
                float height = item.H * (i + 1) / steps;
                visual.Box(new Vector3(0f, item.Y0 + height * 0.5f, -item.D * 0.5f + (i + 0.5f) * run), new Vector3(item.W, height, run));
            }

            Mesh ramp = new DungeonMeshBuilder(0.5f).Prism(item.W, new Vector2(-item.D * 0.5f, item.Y0), new Vector2(-item.D * 0.5f, item.Y0 + item.H / steps * 0.5f),
                new Vector2(item.D * 0.5f - run * 0.5f, top), new Vector2(item.D * 0.5f, top), new Vector2(item.D * 0.5f, item.Y0)).Save(key + "_Ramp");
            GameObject stairs = DungeonPropBuilder.MeshObject("Stairs", root, visual.Save(key), DungeonPropBuilder.Ashlar, new Vector3(item.X, 0f, item.Z), new Vector3(0f, item.R, 0f), false);
            MeshCollider collider = stairs.AddComponent<MeshCollider>();
            collider.sharedMesh = ramp;
            collider.convex = true;
            float rise = item.H / steps;

            for (int i = 0; i < steps; i++)
            {
                BoxCollider filler = stairs.AddComponent<BoxCollider>();
                filler.center = new Vector3(0f, item.Y0 + (i + 0.5f) * rise * 0.5f, -item.D * 0.5f + (i + 0.5f) * run);
                filler.size = new Vector3(item.W, (i + 0.5f) * rise, run);
            }
        }

        /// A glowing circle with a pentagram drawn on the floor.
        private static void BuildRune(Transform root, Item item)
        {
            const int segments = 64;
            const float line = 0.06f;
            DungeonMeshBuilder builder = new DungeonMeshBuilder(1f);
            Vector3 Point(float angle, float radius) => new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0f, Mathf.Cos(angle * Mathf.Deg2Rad)) * radius;

            void Stroke(Vector3 a, Vector3 b)
            {
                Vector3 along = (b - a) * 0.5f;
                builder.Quad((a + b) * 0.5f, Vector3.up, along, Vector3.Cross(Vector3.up, along).normalized * line);
            }

            for (int i = 0; i < segments; i++)
                Stroke(Point(i * 360f / segments, item.Radius), Point((i + 1) * 360f / segments, item.Radius));

            for (int i = 0; i < 5; i++)
                Stroke(Point(i * 144f, item.Radius * 0.95f), Point((i + 1) * 144f, item.Radius * 0.95f));

            Material material = DungeonPropBuilder.Emissive("CatacombRune", new Color(0.7f, 0.08f, 0.06f), 2f);
            DungeonPropBuilder.MeshObject("Rune", root, builder.Save($"CatacombRune_{item.Radius:0.##}".Replace(',', '.')), material, new Vector3(item.X, 0.02f, item.Z), default, false);
        }

        private static void SetPlacements(SerializedProperty property, List<DungeonDirector.MonsterPlacement> placements)
        {
            property.arraySize = placements.Count;

            for (int i = 0; i < placements.Count; i++)
            {
                property.GetArrayElementAtIndex(i).FindPropertyRelative("Point").objectReferenceValue = placements[i].Point;
                property.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab").objectReferenceValue = placements[i].Prefab;
            }
        }

        private static Transform Marker(Transform root, string name, Vector3 point, float yaw)
        {
            Transform marker = BattleEditorUtility.CreateChild(name, root, point).transform;
            marker.localRotation = Quaternion.Euler(0f, yaw, 0f);

            return marker;
        }

        private static GameObject Save(GameObject root, string name)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{name}.prefab");
            Object.DestroyImmediate(root);

            return prefab;
        }

        // ---------- the pieces between rooms ----------

        /// The side between two cells, length along local X, with a framed doorway in the middle of each half that is open.
        private static GameObject Side(int doorways)
        {
            GameObject root = new GameObject($"CatacombSide{doorways}");
            float end = (Pitch - Thickness) * 0.5f;
            float from = -end;

            for (int i = 0; i < 2; i++)
            {
                if ((doorways & 1 << i) == 0)
                    continue;

                float middle = (i * 2 - 1) * DoorOffset;
                Wall(root.transform, from, middle - DoorWidth * 0.5f);
                Doorway(root.transform, middle);
                from = middle + DoorWidth * 0.5f;
            }

            Wall(root.transform, from, end);

            return root;
        }

        private static void Wall(Transform root, float from, float to)
        {
            DungeonStructureBuilder.Block(root, "Wall", new Vector3((from + to) * 0.5f, Height * 0.5f, 0f), new Vector3(to - from, Height, Thickness), DungeonPropBuilder.Ashlar);
        }

        private static void Doorway(Transform root, float middle)
        {
            foreach (float sign in new[] { -1f, 1f })
                DungeonStructureBuilder.Block(root, "Wall Jamb", new Vector3(middle + sign * (DoorWidth * 0.5f + 0.2f), DoorHeight * 0.5f, 0f), new Vector3(0.4f, DoorHeight, Thickness + 0.3f), DungeonPropBuilder.StoneWall);

            DungeonStructureBuilder.Block(root, "Lintel", new Vector3(middle, (DoorHeight + Height) * 0.5f, 0f), new Vector3(DoorWidth, Height - DoorHeight, Thickness), DungeonPropBuilder.Ashlar);
            DungeonStructureBuilder.Block(root, "Arch", new Vector3(middle, DoorHeight + 0.2f, 0f), new Vector3(DoorWidth + 0.8f, 0.4f, Thickness + 0.3f), DungeonPropBuilder.StoneWall);
        }

        private static GameObject Post()
        {
            GameObject root = new GameObject("CatacombPost");
            DungeonStructureBuilder.Block(root.transform, "Wall Post", Vector3.up * Height * 0.5f, new Vector3(Thickness + 0.2f, Height, Thickness + 0.2f), DungeonPropBuilder.Ashlar);

            return root;
        }
    }
}
