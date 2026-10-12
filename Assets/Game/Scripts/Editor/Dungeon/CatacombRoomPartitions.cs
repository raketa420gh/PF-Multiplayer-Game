using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Rooms close in like the Labyrinth: big walls grow into the widest open floor first, each a straight run, an L, a T, a cross,
    /// a U or a solid mass, until the mean distance from a walkable spot to the nearest obstacle drops to the room's openness.
    /// A wall keeps a passage clear of everything else, stays off doorways, markers and lights, and is taken back if it cuts
    /// off a doorway or any floor that was reachable before.
    internal static partial class CatacombRoomBuilder
    {
        private const float Cell = 0.5f;
        private const float Passage = 1.6f;
        private const float DefaultOpenness = 2.1f;
        private const int Attempts = 800;
        private const byte Free = 0;
        private const byte Solid = 1;
        private const byte Keep = 2;
        /// Stops a body but not the eye: furniture, bars, pits, platforms and stairs.
        private const byte Low = 3;
        private const float EyeLevel = 1.5f;
        /// Platforms no higher than a step are floor to walk on, though no wall stands on them.
        private const float StepHeight = 0.3f;

        /// Rooms that stay a little roomier: their point is the open floor.
        private static readonly Dictionary<string, float> s_openness = new() { ["PillaredHall"] = 2.8f, ["LordsTomb"] = 3f, ["Chapel"] = 2.6f };

        private static void Partition(Layout layout)
        {
            int n = Mathf.FloorToInt(2f * Half / Cell);
            byte[,] grid = Rasterize(layout, n);
            bool[,] start = Reach(grid, n, DoorCells(n));
            // Doorways behind a platform or a pit lead nowhere already.
            List<Vector2Int> doors = DoorCells(n).Where(door => start[door.x, door.y]).ToList();
            int lost = Unreachable(grid, n, doors, out _);
            float target = s_openness.TryGetValue(layout.Name, out float openness) ? openness : DefaultOpenness;
            float before = Openness(grid, n, doors);
            float now = before;
            int added = 0;
            System.Random random = new System.Random(layout.Name.Aggregate(17, (hash, c) => hash * 31 + c));

            for (int attempt = 0; attempt < Attempts && now > target; attempt++)
            {
                float[,] distance = Distances(grid, n, false);
                float[,] clearance = Distances(grid, n, true);
                List<Vector2Int> open = Cells(n).Where(c => clearance[c.x, c.y] >= Passage + 0.5f && distance[c.x, c.y] >= 2.4f).OrderByDescending(c => distance[c.x, c.y]).ToList();

                if (open.Count == 0)
                    break;

                Vector2Int hub = open[random.Next(Mathf.Max(1, open.Count / 3))];
                List<(Rect rect, bool isSnapped)> shape = null;

                // Smaller and smaller pieces until one fits the spot.
                for (float scale = 1f; scale > 0.3f && shape == null; scale *= 0.7f)
                {
                    List<(Rect rect, bool isSnapped)> candidate = Partition(random, Middle(hub.x), Middle(hub.y), scale);

                    if (candidate.All(part => Fits(grid, n, part.rect, part.isSnapped)))
                        shape = candidate;
                }

                if (shape == null)
                    continue;

                byte[,] trial = (byte[,])grid.Clone();

                foreach ((Rect rect, bool _) in shape)
                    Mark(trial, n, rect.center.x, rect.center.y, rect.width, rect.height, 0f, 0f, Solid);

                if (Unreachable(trial, n, doors, out bool isDoorLost) > lost || isDoorLost)
                    continue;

                grid = trial;
                now = Openness(grid, n, doors);
                added += shape.Count;
                layout.Items.AddRange(shape.Select(part => Block(part.rect.center.x, part.rect.center.y, part.rect.width, part.rect.height)));
            }

            Debug.Log($"[{nameof(CatacombRoomBuilder)}] {layout.Name}: openness {before:0.00} -> {now:0.00} m, {added} wall blocks");
        }

        /// Wall pieces around a hub, laid side by side and never overlapping, so no two faces share a plane. An arm that ends near
        /// the room's edge runs on into the side wall.
        private static List<(Rect, bool)> Partition(System.Random random, float x, float z, float scale)
        {
            float t = random.NextDouble() < 0.6 ? 0.6f : 1f;
            float Range(float min, float max) => Mathf.Max(1.2f, (min + (float)random.NextDouble() * (max - min)) * scale);
            List<(Rect, bool)> parts = new();
            int kind = random.Next(6);

            if (kind == 0)
            {
                bool isAlongX = random.Next(2) == 0;
                float half = Range(2.5f, 5.5f);
                (float from, bool a) = Snap((isAlongX ? x : z) - half, -1f);
                (float to, bool b) = Snap((isAlongX ? x : z) + half, 1f);
                parts.Add((isAlongX ? Rect.MinMaxRect(from, z - t * 0.5f, to, z + t * 0.5f) : Rect.MinMaxRect(x - t * 0.5f, from, x + t * 0.5f, to), a || b));

                return parts;
            }

            if (kind == 5)
            {
                float size = Range(2f, 3.6f);
                parts.Add((new Rect(x - size * 0.5f, z - size * 0.5f, size, Range(2f, 3.6f)), false));

                return parts;
            }

            if (kind == 4)
            {
                // A U: a base and two arms off its ends on one side, an alcove between them.
                int axis = random.Next(2);
                float half = Range(2.2f, 3.6f);
                float reach = Range(2f, 3.6f) * (random.Next(2) == 0 ? 1f : -1f);
                parts.Add((Arm(x, z, t, axis == 0 ? Vector2.right : Vector2.up, half), false));

                foreach (float end in new[] { -half + t * 0.5f, half - t * 0.5f })
                {
                    Vector2 foot = axis == 0 ? new Vector2(x + end, z) : new Vector2(x, z + end);
                    Vector2 direction = axis == 0 ? Vector2.up * Mathf.Sign(reach) : Vector2.right * Mathf.Sign(reach);
                    parts.Add(Branch(foot.x, foot.y, t, direction, Mathf.Abs(reach)));
                }

                return parts;
            }

            // L, T or cross: a hub with two, three or four arms.
            Vector2[] directions = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
            int first = random.Next(4);
            int[] arms = kind == 1 ? new[] { first, first + 1 } : kind == 2 ? new[] { first, first + 1, first + 3 } : new[] { 0, 1, 2, 3 };
            parts.Add((new Rect(x - t * 0.5f, z - t * 0.5f, t, t), false));
            parts.AddRange(arms.Select(arm => Branch(x, z, t, directions[arm % 4], Range(2.5f, 6.5f))));

            return parts;
        }

        /// An arm of thickness t from the face of a t × t hub at x, z along direction, snapped into the side wall when it ends near it.
        private static (Rect, bool) Branch(float x, float z, float t, Vector2 direction, float length)
        {
            float start = t * 0.5f;
            bool isAlongX = direction.x != 0f;
            float sign = isAlongX ? direction.x : direction.y;
            float origin = isAlongX ? x : z;
            (float end, bool isSnapped) = Snap(origin + sign * (start + length), sign);
            float a = origin + sign * start;
            float from = Mathf.Min(a, end);
            float to = Mathf.Max(a, end);

            return (isAlongX ? Rect.MinMaxRect(from, z - t * 0.5f, to, z + t * 0.5f) : Rect.MinMaxRect(x - t * 0.5f, from, x + t * 0.5f, to), isSnapped);
        }

        private static Rect Arm(float x, float z, float t, Vector2 axis, float half)
        {
            return axis.x != 0f ? Rect.MinMaxRect(x - half, z - t * 0.5f, x + half, z + t * 0.5f) : Rect.MinMaxRect(x - t * 0.5f, z - half, x + t * 0.5f, z + half);
        }

        private static (float, bool) Snap(float end, float sign)
        {
            return Half - Mathf.Abs(end) < 2.4f && Mathf.Sign(end) == sign ? (sign * (Half + 0.1f), true) : (end, false);
        }

        /// The piece stands on free floor inside the room and keeps a passage to everything else; only a snapped piece may reach the edge.
        private static bool Fits(byte[,] grid, int n, Rect rect, bool isSnapped)
        {
            if (rect.xMin < -Half - 0.2f || rect.xMax > Half + 0.2f || rect.yMin < -Half - 0.2f || rect.yMax > Half + 0.2f)
                return false;

            for (float x = rect.xMin - Passage; x <= rect.xMax + Passage; x += Cell)
            {
                for (float z = rect.yMin - Passage; z <= rect.yMax + Passage; z += Cell)
                {
                    int i = Index(x);
                    int j = Index(z);

                    if (i < 0 || j < 0 || i >= n || j >= n)
                    {
                        if (!isSnapped)
                            return false;

                        continue;
                    }

                    if (grid[i, j] != Free)
                        return false;
                }
            }

            return true;
        }

        /// Solid floor of the layout and the places walls keep off: doorways, markers, lights, stairs' ends. Pits, platforms and
        /// stairs count as solid, the floor at their foot is the one walls stand on.
        private static byte[,] Rasterize(Layout layout, int n)
        {
            byte[,] grid = new byte[n, n];

            foreach (Item item in layout.Items)
            {
                switch (item.Shape)
                {
                    case Shape.Block:
                    case Shape.Bars:
                    case Shape.Prop:
                        Mark(grid, n, item.X, item.Z, item.W, item.D, item.R, 0f, item.Shape != Shape.Bars && item.H >= EyeLevel ? Solid : Low);
                        Mark(grid, n, item.X, item.Z, item.W, item.D, item.R, 0.6f, Keep);
                        break;
                    case Shape.Pit:
                        Mark(grid, n, item.X, item.Z, item.W, item.D, 0f, 0f, Low);
                        break;
                    case Shape.Platform:
                        Mark(grid, n, item.X, item.Z, item.W, item.D, 0f, 0f, item.H <= StepHeight ? Keep : item.H >= EyeLevel ? Solid : Low);
                        break;
                    case Shape.Stairs:
                        Mark(grid, n, item.X, item.Z, item.W, item.D, item.R, 0f, Low);
                        Mark(grid, n, item.X, item.Z, item.W, item.D, item.R, 2f, Keep);
                        break;
                    case Shape.Container:
                        Mark(grid, n, item.X, item.Z, 1.6f, 1.6f, item.R, 0f, Low);
                        Mark(grid, n, item.X, item.Z, 1.6f, 1.6f, item.R, 1.2f, Keep);
                        break;
                    case Shape.Monster:
                        Mark(grid, n, item.X, item.Z, 0f, 0f, 0f, 1.4f, Keep);
                        break;
                    case Shape.Trap:
                        Mark(grid, n, item.X, item.Z, 0f, 0f, 0f, 2f, Keep);
                        break;
                    case Shape.Start:
                        Mark(grid, n, item.X, item.Z, 3f, 0f, item.R, 2.5f, Keep);
                        break;
                    case Shape.Rune:
                        Mark(grid, n, item.X, item.Z, 0f, 0f, 0f, item.Radius + 1f, Keep);
                        break;
                    case Shape.Light:
                        Glim glim = (Glim)item.Kind;
                        float reach = glim is Glim.Shaft or Glim.Glow ? item.Radius * 0.6f : glim == Glim.Candle ? 1.2f : 2.5f;
                        Mark(grid, n, item.X, item.Z, 0f, 0f, 0f, reach, Keep);
                        break;
                }
            }

            foreach (Vector2 door in Doorways())
            {
                Vector2 inward = -new Vector2(Mathf.Round(door.x / Half), Mathf.Round(door.y / Half));
                Vector2 middle = door + inward * 2.5f;
                bool isAcross = inward.x != 0f;
                Mark(grid, n, middle.x, middle.y, isAcross ? 5f : DoorWidth + 2f, isAcross ? DoorWidth + 2f : 5f, 0f, 0f, Keep);
            }

            return grid;
        }

        /// Raises the cells whose middles fall in the turned w × d rectangle grown by pad to value.
        private static void Mark(byte[,] grid, int n, float x, float z, float w, float d, float yaw, float pad, byte value)
        {
            float radians = yaw * Mathf.Deg2Rad;
            float reach = Mathf.Sqrt(w * w + d * d) * 0.5f + pad;

            for (int i = Mathf.Max(0, Index(x - reach)); i <= Mathf.Min(n - 1, Index(x + reach)); i++)
            {
                for (int j = Mathf.Max(0, Index(z - reach)); j <= Mathf.Min(n - 1, Index(z + reach)); j++)
                {
                    float dx = Middle(i) - x;
                    float dz = Middle(j) - z;
                    float u = Mathf.Abs(dx * Mathf.Cos(radians) - dz * Mathf.Sin(radians)) - w * 0.5f;
                    float v = Mathf.Abs(dx * Mathf.Sin(radians) + dz * Mathf.Cos(radians)) - d * 0.5f;

                    if (new Vector2(Mathf.Max(u, 0f), Mathf.Max(v, 0f)).magnitude <= pad && Rank(value) > Rank(grid[i, j]))
                        grid[i, j] = value;
                }
            }
        }

        /// Free cells nobody walking in from the doorways reaches; a body needs a cell of room around it.
        private static int Unreachable(byte[,] grid, int n, List<Vector2Int> doors, out bool isDoorLost)
        {
            bool[,] reached = Reach(grid, n, doors);
            isDoorLost = doors.Any(door => !reached[door.x, door.y]);

            return Cells(n).Count(c => IsWalkable(grid, n, c.x, c.y) && !reached[c.x, c.y]);
        }

        private static bool[,] Reach(byte[,] grid, int n, List<Vector2Int> doors)
        {
            bool[,] reached = new bool[n, n];
            Queue<Vector2Int> queue = new();

            foreach (Vector2Int door in doors.Where(door => IsWalkable(grid, n, door.x, door.y)))
            {
                reached[door.x, door.y] = true;
                queue.Enqueue(door);
            }

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();

                foreach (Vector2Int step in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    Vector2Int next = cell + step;

                    if (next.x < 0 || next.y < 0 || next.x >= n || next.y >= n || reached[next.x, next.y] || !IsWalkable(grid, n, next.x, next.y))
                        continue;

                    reached[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return reached;
        }

        private static bool IsWalkable(byte[,] grid, int n, int i, int j)
        {
            for (int a = Mathf.Max(0, i - 1); a <= Mathf.Min(n - 1, i + 1); a++)
            {
                for (int b = Mathf.Max(0, j - 1); b <= Mathf.Min(n - 1, j + 1); b++)
                {
                    if (grid[a, b] is Solid or Low)
                        return false;
                }
            }

            return true;
        }

        /// Mean distance from every reachable cell to the nearest solid or the room's edge.
        private static float Openness(byte[,] grid, int n, List<Vector2Int> doors)
        {
            bool[,] reached = Reach(grid, n, doors);
            float[,] distance = Distances(grid, n, false);
            List<float> values = Cells(n).Where(c => reached[c.x, c.y]).Select(c => distance[c.x, c.y]).ToList();

            return values.Count > 0 ? values.Average() : 0f;
        }

        /// Chamfer distance in metres to the nearest solid cell or the room's edge, or, for clearance, to anything but free floor.
        private static float[,] Distances(byte[,] grid, int n, bool isClearance)
        {
            const float diagonal = 1.4142f;
            float[,] d = new float[n, n];

            foreach (Vector2Int c in Cells(n))
            {
                bool isSource = isClearance ? grid[c.x, c.y] != Free : grid[c.x, c.y] == Solid;
                d[c.x, c.y] = isSource ? 0f : isClearance ? n : Mathf.Min(Mathf.Min(c.x, n - 1 - c.x), Mathf.Min(c.y, n - 1 - c.y)) + 0.5f;
            }

            void Relax(int i, int j, int di, int dj, float cost)
            {
                int a = i + di;
                int b = j + dj;

                if (a >= 0 && b >= 0 && a < n && b < n)
                    d[i, j] = Mathf.Min(d[i, j], d[a, b] + cost);
            }

            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    Relax(i, j, -1, 0, 1f);
                    Relax(i, j, 0, -1, 1f);
                    Relax(i, j, -1, -1, diagonal);
                    Relax(i, j, 1, -1, diagonal);
                }
            }

            for (int j = n - 1; j >= 0; j--)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    Relax(i, j, 1, 0, 1f);
                    Relax(i, j, 0, 1, 1f);
                    Relax(i, j, 1, 1, diagonal);
                    Relax(i, j, -1, 1, diagonal);
                }
            }

            foreach (Vector2Int c in Cells(n))
                d[c.x, c.y] *= Cell;

            return d;
        }

        /// Middles of the eight doorways on the room's edge.
        private static IEnumerable<Vector2> Doorways()
        {
            return Enumerable.Range(0, 4).SelectMany(side => new[] { -DoorOffset, DoorOffset }.Select(offset =>
            {
                Item door = new Item { X = offset, Z = Half }.Turn(side);

                return new Vector2(door.X, door.Z);
            }));
        }

        private static List<Vector2Int> DoorCells(int n)
        {
            return Doorways().Select(door => door * (1f - 1.2f / Half)).Select(p => new Vector2Int(Mathf.Clamp(Index(p.x), 0, n - 1), Mathf.Clamp(Index(p.y), 0, n - 1))).ToList();
        }

        private static IEnumerable<Vector2Int> Cells(int n)
        {
            return Enumerable.Range(0, n).SelectMany(i => Enumerable.Range(0, n).Select(j => new Vector2Int(i, j)));
        }

        /// Solid outranks low, low outranks keep, keep outranks free.
        private static int Rank(byte value)
        {
            return value switch { Solid => 3, Low => 2, Keep => 1, _ => 0 };
        }

        private static int Index(float coordinate)
        {
            return Mathf.FloorToInt((coordinate + Half) / Cell);
        }

        private static float Middle(int index)
        {
            return -Half + (index + 0.5f) * Cell;
        }
    }
}
