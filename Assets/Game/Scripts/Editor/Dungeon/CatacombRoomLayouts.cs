using System.Collections.Generic;
using System.Linq;

namespace Game.Scripts.Editor.Dungeon
{
    /// The rooms the user picked on Tools/RoomLayouts/index.html (catacomb-room-choice.json), copied from the page's ROOMS item
    /// for item. Dark zones of the page are no objects: a corner is dark because no light reaches it.
    internal static partial class CatacombRoomBuilder
    {
        /// Half the room and the offset of a doorway from the middle of its side, as the page calls them.
        private const double H = Half;
        private const double O = DoorOffset;

        private static readonly double[] s_corners = { -12.5, 12.5 };

        private static IEnumerable<Layout> Layouts()
        {
            return new[]
            {
                Crypt(), Ossuary(), PillaredHall(), BladeCorridors(), SpikeGallery(), GuardRoom(), Chapel(), SunkenPit(), FourChambers(), PrisonBlock(),
                FloodedCistern(), Mausoleum(), RitualCircle(), CollapsedHall(), ArcherGalleries(), Labyrinth(), BurialNiches(), Storeroom(), LordsTomb(),
                WellRest(), Chasm(), Refectory(), AlchemyLab(),
                CornerWatch(), CornerShrine(), BonePit(), CornerBarracks(), CornerVault()
            };
        }

        private static IEnumerable<Item> Grid(IEnumerable<double> xs, IEnumerable<double> zs, System.Func<double, double, Item> item)
        {
            return xs.SelectMany(x => zs.Select(z => item(x, z))).ToList();
        }

        private static Layout Crypt()
        {
            return Room("Crypt", "Crypt", 1.2f, 0, false,
                Column(-5.5, 5.5), Column(5.5, -5.5), Column(5.5, 5.5, 1.2, 1.6), Column(-5.5, -5.5, 1.2, 2.4),
                Block(7.8, 3.6, 1.1, 5, 1.1, 131), Prop(9.9, 1.6, 1.4, 1.2, 0.6, "Обломки", 20), Prop(6.6, 6.7, 1.2, 1, 0.5, "Обломки", 60),
                Block(-3.1, -6.5, 1.1, 5, 1.1, 112), Prop(-0.4, -7.6, 1.3, 1.3, 0.6, "Обломки", 35), Prop(-7, -4.2, 1, 1, 0.5, "Обломки", 10),
                Container(Loot.Coffin, 0, 0, 0), Candle(-1.3, 1.8), Candle(1.3, -1.8),
                Symmetric(Prop(-3.6, 15.6, 0.8, 2.1, 0.7, "Гроб"), Prop(-1.2, 15.6, 0.8, 2.1, 0.7, "Гроб"), Prop(3.6, 15.6, 0.8, 2.1, 0.7, "Гроб"),
                    Container(Loot.Coffin, 1.2, 15.6, 180), Skulls(14.6, 14.6)),
                Torch(0, 0), Torch(2, 0), Monster(Mob.Sword, O, 0, 270), Monster(Mob.Sword, -O, 0, 90), Monster(Mob.Skull, 0, -9, 0), Starts(-12, -4, 90));
        }

        private static Layout Ossuary()
        {
            return Room("Ossuary", "Ossuary", 1f, 0, false,
                Symmetric(new[] { -13.5, -4, 4, 13.5 }.Select(x => Block(x, H - 1.6, 3.2, 0.5, 3, 90)), Skulls(15.4, 15.4, 1.2),
                    Block(6.5, 0, 0.5, 4.5, 2.2), Column(10.5, 4.5, 1), Skulls(9.5, 9.5, 1.3), Prop(10, -7, 2.2, 0.6, 1.1, "Кости", 30)),
                Container(Loot.Small, 0, 15.6, 180), Container(Loot.Coffin, 0, -15.4, 0), Skulls(15.6, 0, 1.4), Skulls(-15.6, 0, 1.4),
                Skulls(0, 0, 2.6), Candle(1.6, 1.2), Monster(Mob.Skull, 5, 5, 225), Monster(Mob.Skull, -5, -5, 45), Monster(Mob.Sword, 5, -5, 315), Starts(-5, 5, 135));
        }

        private static Layout PillaredHall()
        {
            double[] rows = { -12.6, -4.2, 4.2, 12.6 };

            return Room("PillaredHall", "Pillared Hall", 1f, 0, false,
                Grid(rows, rows, (x, z) => Column(x, z)),
                Brazier(0, 0), Container(Loot.Large, -15, 15, 135), Container(Loot.Barrel, 15.2, -15.2), Container(Loot.Crate, 13.8, -15.4, 20), Skulls(15, 15),
                Monster(Mob.Archer, O, O, 225), Monster(Mob.Sword, -O, -O, 45), Monster(Mob.Warrior, -O, O, 135), Starts(0, -12, 0));
        }

        private static Layout BladeCorridors()
        {
            return Room("BladeCorridors", "Blade Corridors", 0.8f, 3, false,
                Symmetric(Block(13.35, 13.35, 6.9, 6.9), Block(0, 13.35, 13.8, 6.9), Block(-3.95, 5.95, 5.9, 1.9), Block(3.95, 5.95, 5.9, 1.9), Trap(Snare.Blade, 0, O, 90)),
                Trap(Snare.Spike, 0, 0), Container(Loot.Large, 2.5, 2.5, 225), Candle(-2.6, 2.6), Monster(Mob.Sword, -2.4, -2.4, 45),
                Monster(Mob.Skull, -O, 13, 180), Monster(Mob.Skull, O, -13, 0));
        }

        private static Layout SpikeGallery()
        {
            const double o = 13.4;
            const double n = 4.5;
            const double ps = 1;
            const double ring = (o + H) / 2;

            return Room("SpikeGallery", "Spike Gallery", 0.7f, 5, false,
                Block(0, (o + n) / 2, o * 2, o - n), Block(-(o + ps) / 2, -(o + n) / 2, o - ps, o - n), Block((o + ps) / 2, -(o + n) / 2, o - ps, o - n),
                Block(-(o + n) / 2, 0, o - n, n * 2), Block((o + n) / 2, 0, o - n, n * 2),
                new[] { (ring, ring), (-ring, ring), (ring, -ring), (-ring, -ring), (ring, 0), (-ring, 0), (0, ring), (0, -ring), (0, -9.0) }.Select(p => Trap(Snare.Spike, p.Item1, p.Item2)),
                Container(Loot.Large, 0, 3, 180), Candle(2.6, 3), Skulls(-3, 3), Monster(Mob.Sword, ring, -4, 0), Monster(Mob.Sword, -ring, 4, 180));
        }

        private static Layout GuardRoom()
        {
            return Room("GuardRoom", "Guard Room", 1f, 0, false,
                new[] { (-4.0, 4.0), (4.0, -4.0) }.SelectMany(t => new[]
                {
                    Prop(t.Item1, t.Item2, 1.8, 0.9, 0.8, "Стол"), Prop(t.Item1, t.Item2 + 1, 1.6, 0.4, 0.45, "Лавка"), Prop(t.Item1, t.Item2 - 1, 1.6, 0.4, 0.45, "Лавка"),
                    Candle(t.Item1 + 0.6, t.Item2, 0.8)
                }),
                Segment(-12.8, -6, -12.8, -1.5, 0.4), Segment(-12.8, 1.5, -12.8, 6, 0.4), Segment(-H, 6, -12.6, 6, 0.4), Segment(-H, -6, -12.6, -6, 0.4),
                new[] { -4.0, 0, 4 }.Select(z => Prop(-15.7, z, 2.1, 1, 0.6, "Койка")),
                Segment(10.6, 10.4, 10.6, H, 0.4), Segment(10.4, 10.6, 15, 10.6, 0.4), Container(Loot.Barrel, 15, 15), Container(Loot.Crate, 13.2, 15.4, 15), Monster(Mob.Sword, 12.6, 12.8, 225),
                Segment(-10.6, -H, -10.6, -12.6, 0.4), Segment(-H, -10.6, -10.4, -10.6, 0.4), Container(Loot.Barrel, -15, -15, 40), Container(Loot.Small, -14.8, -12.6, 90),
                Prop(-12.4, -15.6, 1.2, 1.6, 0.6, "Койка"),
                Segment(10.6, -H, 10.6, -13, 0.4), Segment(12.4, -10.6, H, -10.6, 0.4), Prop(14, -14, 1.8, 0.9, 0.8, "Стол"), Candle(14.4, -14, 0.8), Container(Loot.Small, 15.6, -12.2, 270),
                Prop(16.5, -4, 0.4, 1.8, 1.8, "Стойка с оружием"), Prop(16.5, 4, 0.4, 1.8, 1.8, "Стойка с оружием"),
                Container(Loot.Small, 0, 15.6, 180),
                Torch(0, 4), Torch(2, 4), Torch(1, 0), Monster(Mob.Sword, -5, -5, 45), Monster(Mob.Sword, 5, 5, 225), Monster(Mob.Warrior, 12, 0, 270), Starts(-12.5, 12.5, 135));
        }

        private static Layout Chapel()
        {
            return Room("Chapel", "Chapel", 0.8f, 3, false,
                Platform(0, 13, 12, 7.6, 0.25), Prop(0, 13.5, 3, 1.2, 1.1, "Алтарь"), Candle(-1.2, 13.5, 1.1), Candle(1.2, 13.5, 1.1), Candle(-4.5, 12), Candle(4.5, 12),
                Grid(new[] { -3.5, 3.5 }, new[] { 4, 1.5, -1, -3.5, -6 }, (x, z) => Prop(x, z, 4.5, 0.6, 0.9, "Скамья")),
                Grid(new[] { -11.5, 11.5 }, new[] { -12.0, -4, 4, 12 }, (x, z) => Column(x, z)),
                Container(Loot.Large, 0, 15.6, 180), Container(Loot.Small, -15, -15, 45), Container(Loot.Coffin, 15.4, 0, 0),
                Monster(Mob.Mage, 0, 11.3, 180), Monster(Mob.Sword, 13.5, -2, 270), Monster(Mob.Sword, -13.5, 2, 90), Monster(Mob.Archer, -13.5, 13.5, 135),
                Torch(0, -3), Torch(0, 3), Starts(0, -12, 0));
        }

        private static Layout SunkenPit()
        {
            return Room("SunkenPit", "Sunken Pit", 0.8f, 3, false,
                Pit(0, 0, 12, 12, 3), Stairs(0, 3.5, 2.6, 5, 3, 0, -3), Stairs(0, -3.5, 2.6, 5, 3, 180, -3),
                Symmetric(Prop(-3.75, 6.2, 4.5, 0.4, 1, "Парапет"), Prop(3.75, 6.2, 4.5, 0.4, 1, "Парапет"), Prop(0, 10.5, 3, 0.4, 1, "Парапет")),
                Grid(s_corners, s_corners, (x, z) => Column(x, z)),
                Brazier(-3.5, 3.5), Container(Loot.Large, -3.8, -1, 90), Container(Loot.Coffin, 3.8, -1, 0), Skulls(-3, -4), Skulls(3.5, 3.5), Monster(Mob.Skull, 0, 0, 0),
                Monster(Mob.Archer, 12.5, 10.5, 225), Monster(Mob.Archer, -12.5, -10.5, 45), Monster(Mob.Sword, 12, -3, 270), Monster(Mob.Crossbow, -12, 3, 90),
                Torch(0, 0), Torch(1, 0), Torch(2, 0), Torch(3, 0), Starts(0, -12.5, 0));
        }

        private static Layout FourChambers()
        {
            return Room("FourChambers", "Four Chambers", 1f, 0, false,
                Symmetric(Segment(5, 5, 5, 10), Segment(5, 12, 5, H), Segment(5, 5, H, 5)),
                Brazier(0, 0), Torch(0, 0), Torch(1, 0), Torch(2, 0), Torch(3, 0),
                Container(Loot.Large, 14.5, 14.5, 225), Monster(Mob.Warrior, 11, 9, 225),
                Monster(Mob.Sword, 12, -12, 315), Monster(Mob.Sword, 7, -7, 315), Container(Loot.Small, 15.2, -15.2, 315),
                Trap(Snare.Spike, -7, -11), Container(Loot.Large, -14.5, -14.5, 45), Candle(-14, -11),
                Container(Loot.Barrel, -15.3, 12.5), Container(Loot.Crate, -12.5, 15.4, 10), Monster(Mob.Skull, -11, 11, 135),
                Monster(Mob.Sword, 2.5, -2.5, 0), Starts(0, -13.5, 0));
        }

        private static Layout PrisonBlock()
        {
            return Room("PrisonBlock", "Prison Block", 0.8f, 3, false,
                Symmetric(Segment(6.7, 10.1, 6.7, H, 0.5), Segment(-6.7, 10.1, -6.7, H, 0.5), Segment(0, 10.1, 0, H, 0.4),
                    Bars(-6.45, 10.1, -4, 10.1), Bars(-2.8, 10.1, 2.8, 10.1), Bars(4, 10.1, 6.45, 10.1),
                    Segment(10.1, 10.1, 10.1, H, 0.5), Bars(10.35, 10.1, 12.9, 10.1), Bars(14.1, 10.1, H, 10.1), TorchAt(6.95, 13.4, 90)),
                Container(Loot.Small, -3.4, 15.6, 180), Monster(Mob.Sword, 3.4, 13, 180), Container(Loot.Large, 14.8, 14.8, 225),
                Trap(Snare.Spike, 13.5, 3.4), Monster(Mob.Skull, 13.5, -3.4, 270), Container(Loot.Crate, 14.8, -14.8),
                Container(Loot.Coffin, 3.4, -14.8, 0), Monster(Mob.Sword, -3.4, -13, 0), Monster(Mob.Sword, -13.5, -13.5, 45),
                Container(Loot.Small, -15.6, -3.4, 90), Container(Loot.Barrel, -14.8, 14.8),
                Prop(0, 0, 1.8, 0.9, 0.8, "Стол"), Candle(0.5, 0, 0.8), Brazier(0, 3.5), Monster(Mob.Warrior, 0, -3, 0), Monster(Mob.Crossbow, -3, 2, 90), Starts(3.5, -4, 90));
        }

        private static Layout FloodedCistern()
        {
            return Room("FloodedCistern", "Flooded Cistern", 0.6f, 2, false,
                Water(0, 0, 2 * H, 2 * H), Platform(O, 0, 3, 2 * H, 0.25), Platform(-O, 0, 3, 2 * H, 0.25), Platform(0, O, 2 * H, 3, 0.25), Platform(0, -O, 2 * H, 3, 0.25),
                Platform(0, 0, 6, 6, 0.25),
                new[] { (0, 13), (0, -13), (13, 0), (-13, 0), (13, 13), (-13, 13), (13, -13), (-13, -13), (4.4, 4.4), (-4.4, 4.4), (4.4, -4.4), (-4.4, -4.4) }.Select(p => Column(p.Item1, p.Item2)),
                Shaft(0, 0, 10), Torch(0, 0), Torch(2, 0),
                Container(Loot.Large, 0, 0, 180), Container(Loot.Small, -15, 15, 135), Container(Loot.Crate, 15, -15),
                Monster(Mob.Skull, -4, 12, 0), Monster(Mob.Skull, 4, -12, 0), Monster(Mob.Archer, O, 0, 270), Monster(Mob.Sword, -O, -3, 0), Monster(Mob.Warrior, 0, O, 180),
                Starts(O, -12, 0));
        }

        private static Layout Mausoleum()
        {
            return Room("Mausoleum", "Mausoleum", 0.6f, 2, false,
                Segment(-5, 5, 5, 5, 0.8, 4.5), Segment(-5, -5, -5, 5, 0.8, 4.5), Segment(5, -5, 5, 5, 0.8, 4.5), Segment(-5, -5, -1.2, -5, 0.8, 4.5), Segment(1.2, -5, 5, -5, 0.8, 4.5),
                Container(Loot.Rich, 0, 1.5, 180), Candle(-2.5, 2.5), Candle(2.5, 2.5), Trap(Snare.Spike, 0, -3.5),
                Grid(s_corners, s_corners, (x, z) => Column(x, z, 1.2, 3)),
                Torch(1, 0), Torch(2, 0), Torch(3, 0), Container(Loot.Coffin, 15.4, 0, 0), Container(Loot.Coffin, -15.4, 0, 0),
                Monster(Mob.Warrior, 0, -7, 180), Monster(Mob.Sword, -11.5, -3, 90), Monster(Mob.Sword, 11.5, 3, 270), Monster(Mob.Archer, -14.5, 14.5, 135),
                Monster(Mob.Skull, 3, 11, 180), Starts(14, -14, 315));
        }

        private static Layout RitualCircle()
        {
            return Room("RitualCircle", "Ritual Circle", 0.6f, 2, false,
                Rune(0, 0, 6.5), Prop(0, 0, 2, 2, 0.8, "Жертвенник"),
                Enumerable.Range(0, 8).Select(i => Candle(5.5 * System.Math.Sin(i * 45 * System.Math.PI / 180), 5.5 * System.Math.Cos(i * 45 * System.Math.PI / 180))),
                Grid(s_corners, s_corners, (x, z) => Column(x, z, 1.6)),
                Container(Loot.Large, 0, 2.2, 180), Monster(Mob.Mage, 0, -2.4, 0), Monster(Mob.Skull, 4, 4, 225), Monster(Mob.Skull, -4, -4, 45),
                Monster(Mob.Sword, -13.5, 4, 90), Monster(Mob.Sword, 13.5, -4, 270), Starts(-14.5, -14.5, 45));
        }

        private static Layout CollapsedHall()
        {
            return Room("CollapsedHall", "Collapsed Hall", 1f, 0, false,
                Block(-3, 11, 6, 4, 2.6, 25), Prop(-12, 3, 3, 3, 1.2, "Обломки", 10), Prop(3, 12.5, 3, 2, 1, "Обломки", 40), Block(3, -10, 5, 3, 3, -20),
                Prop(12.5, -2, 2.5, 2.5, 1, "Обломки", 15), Block(1.5, 3.5, 1.4, 8, 1.4, 65), Block(-12.5, -12.8, 4, 4.5, 4, 15), Prop(12.5, 12.5, 3, 3, 1.2, "Обломки", 30),
                Block(-2, -3, 4, 2.5, 2.2, -8),
                Shaft(4, -1, 10), Torch(3, 0),
                Container(Loot.Crate, -15.6, -10.5, 30), Container(Loot.Small, 15.2, 15.2, 225), Container(Loot.Barrel, 14.5, -14.8),
                Monster(Mob.Skull, 5, -1, 0), Monster(Mob.Sword, -13.5, -3, 90), Monster(Mob.Sword, 13.5, 3, 270), Monster(Mob.Warrior, -5, 5, 135), Starts(0, -14, 0));
        }

        private static Layout ArcherGalleries()
        {
            return Room("ArcherGalleries", "Archer Galleries", 0.8f, 3, false,
                Symmetric(Platform(0, 14.3, 13.4, 5, 2.5), Stairs(0, 8.8, 2.4, 6, 2.5, 0), Prop(-3.95, 11.95, 5.5, 0.4, 1, "Парапет"), Prop(3.95, 11.95, 5.5, 0.4, 1, "Парапет"), Torch(0, -4)),
                Prop(12.6, 12.6, 3, 0.8, 1.2, "Укрытие", 45), Prop(-12.6, 12.6, 3, 0.8, 1.2, "Укрытие", 135), Prop(-12.6, -12.6, 3, 0.8, 1.2, "Укрытие", 45),
                Container(Loot.Large, -15.5, -4, 90), Container(Loot.Small, 4, 15.8, 180), Container(Loot.Barrel, 15.5, 15.5),
                Monster(Mob.Archer, -4, 14, 180), Monster(Mob.Archer, 4, -14, 0), Monster(Mob.Crossbow, 14, 4, 270), Monster(Mob.Warrior, 0, 0, 0), Monster(Mob.Sword, -12.5, -11, 45),
                Starts(12.6, -12.6, 315));
        }

        /// A 6×6 maze of 5.6 m cells grown by the page's own random numbers, so it is the very maze of the page: the doorways
        /// fall in the middle of the second and fifth cell of every side; loot in the dead ends farthest from the middle.
        private static Layout Labyrinth()
        {
            const int n = 6;
            double c = 2 * H / n;
            System.Func<double> random = PageRandom(11);
            bool[,] east = new bool[n, n];
            bool[,] north = new bool[n, n];
            HashSet<(int, int)> seen = new() { (2, 2) };
            Stack<(int, int)> stack = new();
            stack.Push((2, 2));

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    east[i, j] = north[i, j] = true;
            }

            while (stack.Count > 0)
            {
                (int i, int j) = stack.Peek();
                List<(int, int)> next = new[] { (i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1) }
                    .Where(p => p.Item1 >= 0 && p.Item2 >= 0 && p.Item1 < n && p.Item2 < n && !seen.Contains(p)).ToList();

                if (next.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                (int a, int b) = next[(int)System.Math.Floor(random() * next.Count)];

                if (a != i)
                    east[System.Math.Min(a, i), j] = false;
                else
                    north[i, System.Math.Min(b, j)] = false;

                seen.Add((a, b));
                stack.Push((a, b));
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i < n - 1 && random() < 0.12)
                        east[i, j] = false;

                    if (j < n - 1 && random() < 0.12)
                        north[i, j] = false;
                }
            }

            List<Item> items = new();
            double Middle(int i) => -H + (i + 0.5) * c;
            bool IsDoor(int k) => k == 1 || k == 4;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i < n - 1 && east[i, j])
                        items.Add(Block(-H + (i + 1) * c, Middle(j), 0.5, c + 0.5, 3.2));

                    if (j < n - 1 && north[i, j])
                        items.Add(Block(Middle(i), -H + (j + 1) * c, c + 0.5, 0.5, 3.2));
                }
            }

            int Walls(int i, int j) => ((i == 0 ? !IsDoor(j) : east[i - 1, j]) ? 1 : 0) + ((i == n - 1 ? !IsDoor(j) : east[i, j]) ? 1 : 0)
                + ((j == 0 ? !IsDoor(i) : north[i, j - 1]) ? 1 : 0) + ((j == n - 1 ? !IsDoor(i) : north[i, j]) ? 1 : 0);

            List<(int, int)> dead = Enumerable.Range(0, n).SelectMany(i => Enumerable.Range(0, n).Select(j => (i, j))).Where(p => Walls(p.Item1, p.Item2) >= 3)
                .OrderByDescending(p => System.Math.Sqrt((p.Item1 - 2.5) * (p.Item1 - 2.5) + (p.Item2 - 2.5) * (p.Item2 - 2.5))).ToList();

            void At(int k, System.Func<double, double, Item> item)
            {
                if (k < dead.Count)
                    items.Add(item(Middle(dead[k].Item1), Middle(dead[k].Item2)));
            }

            At(0, (x, z) => Container(Loot.Large, x, z));
            At(0, (x, z) => Candle(x + 1.5, z + 1.5));
            At(1, (x, z) => Container(Loot.Small, x, z));
            At(2, (x, z) => Container(Loot.Coffin, x, z));
            At(2, (x, z) => Candle(x - 1.5, z + 1.5));
            At(3, (x, z) => Trap(Snare.Spike, x, z));
            At(4, (x, z) => Trap(Snare.Spike, x, z));
            At(5, (x, z) => Container(Loot.Urn, x, z));

            return Room("Labyrinth", "Labyrinth", 0.6f, 2, false, items,
                Brazier(Middle(3), Middle(3)), Torch(0, 0), Torch(2, 0), Monster(Mob.Skull, Middle(0), Middle(5)), Monster(Mob.Skull, Middle(5), Middle(0)),
                Monster(Mob.Sword, Middle(5), Middle(5), 225), Monster(Mob.Sword, Middle(0), Middle(0), 45), Starts(Middle(2), Middle(0), 0));
        }

        /// The page's mulberry32: the same seed gives the same numbers as there.
        private static System.Func<double> PageRandom(int seed)
        {
            int s = seed;

            return () =>
            {
                unchecked
                {
                    s += 0x6D2B79F5;
                    int t = (s ^ (int)((uint)s >> 15)) * (1 | s);
                    t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;

                    return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
                }
            };
        }

        private static Layout BurialNiches()
        {
            return Room("BurialNiches", "Burial Niches", 1f, 0, false,
                new[] { -12.0, -5, 5, 12 }.SelectMany(x => new[] { Block(x, 0, 1.6, 13.2), Block(x, 12.35, 1.6, 4.3), Block(x, -12.35, 1.6, 4.3) }),
                Container(Loot.Coffin, 6.6, 0, 0), Container(Loot.Coffin, -10.4, 2, 0), Container(Loot.Coffin, 15.9, -3, 0), Container(Loot.Coffin, -15.9, 3, 0),
                Container(Loot.Urn, -3.4, 12.5), Container(Loot.Urn, 3.4, -12.5), Container(Loot.Urn, 14.5, 12.5),
                Candle(0, 4), Candle(0, -4), Torch(0, 0), Torch(2, 0),
                Monster(Mob.Sword, -8.5, 4, 180), Monster(Mob.Sword, 8.5, -4, 0), Monster(Mob.Skull, 14.8, 0, 270), Monster(Mob.Archer, 0, -11, 0), Monster(Mob.Crossbow, -14.8, -12, 0),
                Starts(-14.8, 12, 180));
        }

        private static Layout Storeroom()
        {
            return Room("Storeroom", "Storeroom", 1f, 0, false,
                Crates(-3, 3, 4, 3), Crates(3.5, -2.5, 3, 4), Crates(-3.5, 13.5, 4, 3), Prop(3.5, 14, 3, 2.5, 2.2, "Бочки"), Crates(13.5, -3, 3, 5), Crates(-13.5, 3, 3, 5),
                Crates(2, -13.5, 5, 3), Crates(13.5, 13.5, 3, 3, 1.6), Crates(-12, -12, 2.4, 2.4),
                Container(Loot.Barrel, 11.2, 15.6), Container(Loot.Crate, -15.5, 15.5), Container(Loot.Barrel, 15.5, -15.5), Container(Loot.Crate, -3, -12.5),
                Container(Loot.Small, -15, -15, 45), Container(Loot.Crate, 15.6, 3), Container(Loot.Barrel, -15.6, -2),
                Torch(1, 0), Torch(3, 0), Monster(Mob.Sword, 0, O, 180), Monster(Mob.Sword, -O, 0, 90), Monster(Mob.Crossbow, 12.5, -12.5, 315), Starts(5, 5, 225));
        }

        private static Layout LordsTomb()
        {
            return Room("LordsTomb", "Lord's Tomb", 0.4f, 1, false,
                Platform(0, 1, 12, 10, 1), Stairs(0, -5, 4, 2, 1, 0), Container(Loot.Rich, 0, 3, 180), Brazier(-4.5, 5), Brazier(4.5, 5),
                Column(-11.5, 2.5), Column(11.5, 2.5), Column(-11.5, -2.5), Column(11.5, -2.5),
                Container(Loot.Coffin, 15.4, 0, 0), Container(Loot.Coffin, -15.4, 0, 0), Torch(1, 0), Torch(3, 0), Torch(0, 0),
                Monster(Mob.Warrior, -3, -7, 180), Monster(Mob.Warrior, 3, -7, 180), Monster(Mob.Mage, 0, 5, 180), Monster(Mob.Archer, -13.5, 13.5, 135), Monster(Mob.Archer, 13.5, 13.5, 225));
        }

        private static Layout WellRest()
        {
            return Room("WellRest", "Well Rest", 0.6f, 2, false,
                Platform(0, 0, 6, 6, 0.25),
                Platform(13.6, 13.6, 6.4, 6.4, 1), Stairs(12.6, 9.4, 2.4, 2, 1, 0), Prop(10.6, 13.6, 0.4, 6.4, 1, "Парапет"), Prop(15.3, 10.6, 3, 0.4, 1, "Парапет"),
                Container(Loot.Small, 15.2, 15.2, 225), Candle(12, 15.6),
                Platform(-13.6, -13.6, 6.4, 6.4, 2.5), Stairs(-7.4, -13.1, 2.4, 6, 2.5, 270), Prop(-13.6, -10.6, 6.4, 0.4, 1, "Парапет"), Prop(-10.6, -15.55, 0.4, 2.5, 1, "Парапет"),
                Container(Loot.Crate, -15.4, -15.4, 20),
                Prop(0, 0, 3, 3, 1, "Колодец", 0, true), Fire(4.5, -4.5),
                Prop(4.5, -2.1, 2.4, 0.5, 0.45, "Бревно"), Prop(6.9, -4.5, 0.5, 2.4, 0.45, "Бревно"), Prop(2.1, -4.9, 0.5, 2.2, 0.45, "Бревно"),
                Prop(-3, -13.5, 1, 2, 0.15, "Спальник"), Prop(-1, -13.5, 1, 2, 0.15, "Спальник"),
                Container(Loot.Barrel, -15, 15), Container(Loot.Crate, -12.5, 15.5, 8), Torch(0, 0), Starts(-5, 5, 135));
        }

        /// Flying skulls walk like everyone else here, so the two the page hangs over the abyss wait on its ledges.
        private static Layout Chasm()
        {
            return Room("Chasm", "Chasm", 0.5f, 2, false,
                Symmetric(Pit(12.6, 12.6, 5.4, 5.4, 12), Pit(0, 12.6, 13.8, 5.4, 12), Pit(4.95, 3.95, 3.9, 5.9, 12), Pit(2, 4.95, 2, 3.9, 12)),
                Container(Loot.Rich, 1.5, 1.5, 225), Brazier(-1.5, -1.5), Monster(Mob.Warrior, -1.5, 1.5, 135),
                Monster(Mob.Skull, 12.6, 16, 180), Monster(Mob.Skull, -12.6, -16, 0), Monster(Mob.Archer, 16, 0, 270), Monster(Mob.Crossbow, -16, 0, 90),
                Torch(0, 0), Torch(1, 0), Torch(2, 0), Torch(3, 0));
        }

        private static Layout Refectory()
        {
            return Room("Refectory", "Refectory", 0.8f, 3, false,
                new[] { -3.0, 3 }.SelectMany(x => new[] { Prop(x, 0, 1.2, 11, 0.8, "Стол"), Prop(x - 1.1, 0, 0.4, 11, 0.45, "Лавка"), Prop(x + 1.1, 0, 0.4, 11, 0.45, "Лавка") }),
                Prop(0, 15.9, 4.5, 1.6, 2.8, "Очаг"), Fire(0, 14.6),
                Prop(13.4, -15.8, 5, 1.2, 0.9, "Разделочный стол"), Container(Loot.Barrel, 15.8, -12.5), Container(Loot.Barrel, 15.8, -11.6), Container(Loot.Crate, 12, -12.5),
                Prop(9.6, -13.4, 1.2, 1.2, 0.9, "Котёл", 0, true), Prop(15.9, -3, 0.6, 3, 2.2, "Полки с припасами"), Prop(15.6, 3.2, 1, 2.6, 0.8, "Мешки"),
                Prop(-15.9, 3.5, 0.6, 3, 2.2, "Буфет с посудой"), Prop(-15.9, -3.5, 0.6, 3, 2.2, "Буфет с посудой"),
                Prop(4.5, 15.9, 2, 0.9, 1.2, "Поленница"), Prop(-4.5, 15.9, 2, 0.9, 1.2, "Поленница"),
                Prop(0, -10, 3, 1, 0.8, "Стол"), Prop(-1.9, -10, 0.5, 0.5, 0.45, "Табурет"), Prop(1.9, -10, 0.5, 0.5, 0.45, "Табурет"), Candle(0.6, -10, 0.8),
                Crates(-13, -15.4, 2.4, 1.4, 1.6), Prop(-15.6, -12.4, 1, 2, 0.8, "Мешки"), Prop(15.4, 12.8, 1.4, 1.4, 1.2, "Бочки"),
                Container(Loot.Small, -15, 15, 135), Container(Loot.Crate, -12.5, 15.6), Candle(-3, 2, 0.8), Candle(3, -2, 0.8), Torch(1, 0), Torch(3, 0),
                Monster(Mob.Sword, -1.9, 2, 270), Monster(Mob.Sword, 4.1, -3, 270), Monster(Mob.Warrior, -12.5, -12.5, 45), Monster(Mob.Archer, 12.5, 12.5, 225), Starts(-13, 0, 90));
        }

        private static Layout AlchemyLab()
        {
            return Room("AlchemyLab", "Alchemy Lab", 0.6f, 2, false,
                Platform(0, 0, 6, 6, 0.25), Prop(0, 0, 1.6, 1.6, 1, "Котёл", 0, true), Glow(0, 0, Hex("#5cff7a"), 8),
                Platform(13.6, 13.6, 6.4, 6.4, 2.5), Stairs(7.4, 12.6, 2.4, 6, 2.5, 90), Prop(13.6, 10.6, 6.4, 0.4, 1, "Парапет"), Prop(10.6, 15.3, 0.4, 3, 1, "Парапет"),
                Platform(-13.6, -13.6, 6.4, 6.4, 1), Stairs(-12.6, -9.4, 2.4, 2, 1, 180), Prop(-10.6, -13.6, 0.4, 6.4, 1, "Парапет"),
                Prop(12.5, 12.5, 1.2, 4, 0.9, "Стол алхимика", 45), Prop(-12.5, 12.5, 1.2, 4, 0.9, "Стол алхимика", -45), Prop(-12.5, -12.5, 1.2, 4, 0.9, "Стол алхимика", 45),
                Candle(12.5, 12.5, 0.9), Candle(-12.5, -12.5, 0.9),
                Container(Loot.Shelf, -16.5, 0, 90), Container(Loot.Shelf, 0, -16.5, 0), Prop(16.5, 0, 0.5, 2.4, 2.6, "Стеллаж с банками"), Prop(0, 16.5, 2.4, 0.5, 2.6, "Стеллаж с банками"),
                Container(Loot.Small, 15.5, -15.5, 315), Torch(0, 4),
                Monster(Mob.Mage, 14.6, 14.6, 225), Monster(Mob.Skull, 3, 3, 225), Monster(Mob.Skull, -5, -5, 45), Monster(Mob.Skull, 5, -3, 270), Monster(Mob.Sword, -12, 4, 90),
                Starts(-4, -12, 0));
        }

        private static Layout CornerWatch()
        {
            return Room("CornerWatch", "Corner Watch", 1f, 1, true,
                Platform(-11, -11, 11.6, 11.6, 2.5), Stairs(-11, -2.2, 2.4, 6, 2.5, 180), Stairs(-2.2, -11, 2.4, 6, 2.5, 270),
                Prop(-14.5, -5.4, 4.6, 0.4, 1, "Парапет"), Prop(-7.5, -5.4, 4.6, 0.4, 1, "Парапет"), Prop(-5.4, -14.5, 0.4, 4.6, 1, "Парапет"), Prop(-5.4, -7.5, 0.4, 4.6, 1, "Парапет"),
                Container(Loot.Large, -15.5, -15.5, 45), Brazier(-12, -12), Prop(4, 4, 3, 0.8, 1.2, "Укрытие", 45),
                Monster(Mob.Archer, -8, -8, 45), Monster(Mob.Crossbow, -14, -8, 45), Monster(Mob.Warrior, 4, -2, 225), Monster(Mob.Sword, 2, 8, 180),
                Container(Loot.Barrel, 15.5, -15.5), Container(Loot.Crate, -15.5, 15.5), Torch(2, 4), Torch(3, -4), Torch(0, 0), Starts(12, 12, 225));
        }

        private static Layout CornerShrine()
        {
            return Room("CornerShrine", "Corner Shrine", 1f, 1, true,
                Platform(-12.5, -12.5, 8.6, 8.6, 0.25), Prop(-13, -13, 2.4, 1.2, 1.1, "Алтарь", 45), Candle(-12, -14, 1.1), Candle(-14, -12, 1.1), Container(Loot.Rich, -15.4, -15.4, 45),
                Column(-6, -14, 1.2, 3), Column(-14, -6, 1.2, 3),
                new[] { -5.5, -3, -0.5 }.Select(d => Prop(d, d, 6, 0.6, 0.9, "Скамья", 45)),
                Monster(Mob.Mage, -10.5, -10.5, 45), Monster(Mob.Sword, -9, 3, 135), Monster(Mob.Sword, 3, -9, 315), Monster(Mob.Archer, 10, 10, 225),
                Torch(2, -4), Torch(3, 4), Container(Loot.Coffin, 15.4, 0, 0), Container(Loot.Small, 0, 15.6, 180), Starts(12, -3, 270));
        }

        private static Layout BonePit()
        {
            return Room("BonePit", "Bone Pit", 1f, 1, true,
                Pit(-9, -9, 12, 12, 4), Stairs(-9, -7, 2.6, 8, 4, 0, -4),
                Skulls(-13, -6, 1.5), Skulls(-5, -13, 1.5), Skulls(-13, -13, 1.2), Container(Loot.Large, -12, -13.5, 45), Container(Loot.Coffin, -5.5, -9, 0), Candle(-11, -11),
                Monster(Mob.Skull, -12, -9, 90), Monster(Mob.Skull, -9, -12, 0), Monster(Mob.Skull, -6, -5, 225),
                Monster(Mob.Sword, 6, 6, 225), Monster(Mob.Archer, 12, -12, 315), Container(Loot.Crate, 15.4, 15.4), Torch(0, 0), Torch(1, 0), Starts(0, 12, 180));
        }

        private static Layout CornerBarracks()
        {
            return Room("CornerBarracks", "Corner Barracks", 1f, 1, true,
                new[] { -7.0, -4, -1, 2, 5, 8 }.Select(x => Prop(x, -15.7, 1, 2.1, 0.6, "Койка")),
                new[] { -7.0, -4, -1, 2, 5, 8, 11 }.Select(z => Prop(-15.7, z, 2.1, 1, 0.6, "Койка")),
                Prop(2, 2, 4, 1.2, 0.8, "Стол"), Prop(2, 3.1, 4, 0.4, 0.45, "Лавка"), Prop(2, 0.9, 4, 0.4, 0.45, "Лавка"), Candle(2, 2, 0.8),
                Segment(-10, -H, -10, -12, 0.4), Segment(-H, -10, -9.8, -10, 0.4), Container(Loot.Large, -15.3, -15.3, 45), Prop(-13, -13, 1.8, 0.9, 0.8, "Стол"), Candle(-12.4, -13, 0.8),
                Monster(Mob.Warrior, -12.5, -11.5, 135),
                Segment(-5, 11.5, -5, H, 0.4), Segment(5, 11.5, 5, H, 0.4), Segment(-5.2, 11.5, -1, 11.5, 0.4), Segment(1, 11.5, 5.2, 11.5, 0.4),
                Prop(0, 16.5, 2.4, 0.4, 1.8, "Стойка с оружием"), Prop(-3.5, 16.5, 2, 0.4, 1.8, "Стойка с оружием"), Container(Loot.Small, 3.5, 15.8, 180),
                Segment(10.5, -H, 10.5, -12.5, 0.4), Segment(12.5, -10.5, H, -10.5, 0.4), Container(Loot.Barrel, 15.3, -15.3), Container(Loot.Crate, 13, -15.4, 10),
                Prop(15.6, -12.5, 1, 1, 0.9, "Бочки"),
                Container(Loot.Crate, 15.4, 15.4),
                Monster(Mob.Sword, -6, -13, 0), Monster(Mob.Sword, -13, 4, 90), Monster(Mob.Warrior, 2, -1.5, 0), Monster(Mob.Crossbow, 10, 10, 225),
                Torch(2, 4), Torch(3, -4), Torch(0, 4), Starts(12, 0, 270));
        }

        private static Layout CornerVault()
        {
            return Room("CornerVault", "Corner Vault", 1f, 1, true,
                Segment(-7, -H, -7, -7, 0.8), Segment(-H, -7, -12, -7, 0.8), Segment(-10, -7, -7, -7, 0.8),
                Container(Loot.Rich, -14, -14, 45), Container(Loot.Large, -10, -14.5, 0), Trap(Snare.Spike, -11, -8.6), Candle(-14.5, -11),
                Column(-3, 3), Column(3, -3), TorchAt(-6.6, -12, 90), Torch(0, 0), Torch(1, 0),
                Monster(Mob.Warrior, -11, -5, 0), Monster(Mob.Sword, -4, -11, 90), Monster(Mob.Mage, 4, 4, 225), Monster(Mob.Archer, 12, 12, 225),
                Container(Loot.Barrel, 15.3, -15.3), Container(Loot.Crate, -15.3, 15.3), Starts(12, 0, 270));
        }
    }
}
