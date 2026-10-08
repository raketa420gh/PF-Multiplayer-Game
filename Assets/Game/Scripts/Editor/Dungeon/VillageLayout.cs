using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// The first floor after the user's map "Этаж 1 — Проклятая деревня": 540 m square (the map's scale bar), 5 x 5 modules of 108 m.
    /// North-west the farm with its fields (where adventurers start), north-east the walled graveyard with a ruined chapel, the village round
    /// its square in the middle, the swamp along the south, the abandoned windmill on the north road. Red portals down to floor 2
    /// stand behind the chapel and on an island of the swamp: from the farm one goes either way, past the dangerous village or round it. A river comes down between farm and graveyard and curls round the
    /// village to the swamp; a stream leaves it at the bend and closes the ring in the west. World X is east, Z is north, the origin is the map centre.
    /// Level design: the floor is a set of zones (settlements, points of interest in the woods, islands of the swamp) joined by
    /// winding paths, every zone with two or more ways out. Everything between them is closed: wooded rocky ridges, the river
    /// in its ravine, deep bog water. Nothing is straight or round: paths, zone rims and the land carry noise.
    internal static class VillageLayout
    {
        public const float Size = 540f;
        public const float Half = Size * 0.5f;
        public const int Grid = 5;
        public const float WaterLevel = 0.5f;
        public const float RiverWidth = 13f;
        public const float StreamWidth = 8f;
        /// Wooden decks stand this high above the water.
        public const float DeckHeight = WaterLevel + 0.5f;
        /// The playable square; the hills beyond it are walled off.
        public const float Playable = Half - 22f;
        /// Open ground ends this far beyond a zone rim or a path verge; the invisible wall stands there.
        public const float WallLine = 1.5f;

        public static readonly Vector2 FarmCenter = new(-138f, 168f);
        public static readonly Vector2 VillageCenter = new(22f, 4f);
        public static readonly Vector2 Plaza = new(18f, 2f);
        public const float PlazaRadius = 14f;
        public static readonly Rect Graveyard = Rect.MinMaxRect(84f, 96f, 236f, 238f);
        public static readonly Vector2 Chapel = new(176f, 210f);
        /// The swamp begins south of this line (shifted by noise).
        public const float SwampEdge = -86f;

        public enum ZoneKind
        {
            Settlement,
            Wild,
            Swamp
        }

        /// Open ground of a place: a rounded box (or a circle without half size) with a ragged rim.
        public sealed class Zone
        {
            public string Name;
            public ZoneKind Kind;
            public Vector2 Center;
            public Vector2 HalfSize;
            public float Radius;
            /// How far the rim wanders in and out, metres.
            public float Ragged = 5f;
            /// Share of the size by which the whole shape is pushed out of round (low-frequency domain warp).
            public float Warp = 0.22f;

            public float Distance(float x, float z)
            {
                float reach = (HalfSize.magnitude + Radius) * Warp;
                float wx = (DungeonTextureBuilder.Noise(x / Size + Center.y * 0.003f, z / Size + 7f, 6f, 2) - 0.5f) * 4f * reach;
                float wz = (DungeonTextureBuilder.Noise(x / Size + 11f, z / Size + Center.x * 0.003f, 6f, 2) - 0.5f) * 4f * reach;
                x += wx;
                z += wz;
                Vector2 q = new Vector2(Mathf.Abs(x - Center.x) - HalfSize.x, Mathf.Abs(z - Center.y) - HalfSize.y);
                float box = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f);
                float noise = DungeonTextureBuilder.Noise(x / Size + Center.x * 0.01f, z / Size + Center.y * 0.01f, 16f, 2) - 0.5f;

                return box - Radius + noise * 2f * Ragged;
            }
        }

        public static readonly Zone Village = new() { Name = "Village", Kind = ZoneKind.Settlement, Center = VillageCenter, Radius = 88f, Ragged = 10f, Warp = 0.3f };
        public static readonly Zone Farm = new() { Name = "Farmstead", Kind = ZoneKind.Settlement, Center = FarmCenter, Radius = 84f, Ragged = 8f, Warp = 0.2f };

        public static readonly Zone Yard = new() { Name = "Graveyard", Kind = ZoneKind.Settlement, Center = Graveyard.center, HalfSize = new Vector2(60f, 55f), Radius = 16f, Ragged = 4f, Warp = 0.15f };

        public static readonly Zone[] Zones =
        {
            Village,
            Farm,
            Yard,
            new() { Name = "Woodcutter's Camp", Kind = ZoneKind.Wild, Center = new Vector2(-222f, 224f), Radius = 21f },
            new() { Name = "Standing Stones", Kind = ZoneKind.Wild, Center = new Vector2(-214f, 52f), Radius = 19f },
            new() { Name = "Hunter's Lodge", Kind = ZoneKind.Wild, Center = new Vector2(-214f, -36f), Radius = 21f },
            new() { Name = "Gallows Hill", Kind = ZoneKind.Wild, Center = new Vector2(-116f, -12f), Radius = 18f },
            new() { Name = "Abandoned Windmill", Kind = ZoneKind.Wild, Center = new Vector2(44f, 230f), Radius = 24f },
            new() { Name = "Watchtower", Kind = ZoneKind.Wild, Center = new Vector2(172f, -14f), Radius = 21f },
            new() { Name = "Willow Ford", Kind = ZoneKind.Swamp, Center = new Vector2(-212f, -116f), Radius = 22f },
            new() { Name = "Swamp Edge", Kind = ZoneKind.Swamp, Center = new Vector2(-112f, -104f), Radius = 20f },
            new() { Name = "Stilt Huts", Kind = ZoneKind.Swamp, Center = new Vector2(-80f, -162f), Radius = 20f },
            new() { Name = "Western Mire", Kind = ZoneKind.Swamp, Center = new Vector2(-146f, -208f), Radius = 22f },
            new() { Name = "Crimson Isle", Kind = ZoneKind.Swamp, Center = new Vector2(-2f, -214f), Radius = 22f },
            new() { Name = "South Causeway", Kind = ZoneKind.Swamp, Center = new Vector2(22f, -120f), Radius = 15f },
            new() { Name = "Reed Banks", Kind = ZoneKind.Swamp, Center = new Vector2(106f, -116f), Radius = 18f },
            new() { Name = "Black Bog", Kind = ZoneKind.Swamp, Center = new Vector2(90f, -192f), Radius = 20f },
            new() { Name = "Hermit's Hut", Kind = ZoneKind.Swamp, Center = new Vector2(160f, -214f), Radius = 20f },
            new() { Name = "Fisher's Jetty", Kind = ZoneKind.Swamp, Center = new Vector2(206f, -142f), Radius = 20f }
        };

        /// Red portals down to floor 2: behind the chapel and on an island in the middle of the swamp. One walks in along the yaw.
        public static readonly (Vector2 position, float yaw)[] RedPortals = { (new Vector2(208f, 214f), -90f), (new Vector2(-2f, -214f), 0f) };

        public static readonly Vector2[] River =
        {
            new(-22f, 290f), new(-14f, 218f), new(-6f, 181f), new(9f, 143f), new(36f, 115f), new(78f, 99f), new(120f, 84f), new(162f, 73f),
            new(199f, 54f), new(224f, 26f), new(230f, -12f), new(216f, -49f), new(194f, -77f), new(172f, -102f), new(150f, -125f)
        };

        public static readonly Vector2[] Stream =
        {
            new(4f, 142f), new(-26f, 112f), new(-70f, 85f), new(-110f, 62f), new(-150f, 36f), new(-170f, 0f), new(-182f, -32f), new(-190f, -70f), new(-200f, -108f)
        };

        public sealed class Road
        {
            public Vector2[] Points;
            public float Width;
            /// Open ground on each side of the trodden width.
            public float Verge = 3.5f;
            /// How far the line wanders off the authored points.
            public float Wobble = 5f;
            /// Forest trails: no houses or street lamps along them.
            public bool IsTrail;
        }

        public static readonly Road[] Roads =
        {
            // Farm to the village over the bridge at the head of the stream.
            new() { Width = 5f, Points = new Vector2[] { new(-104f, 150f), new(-66f, 140f), new(-40f, 126f), new(-22f, 104f), new(-6f, 82f), new(12f, 50f), new(18f, 16f) } },
            // Village to the graveyard's south gate.
            new() { Width = 4.5f, Points = new Vector2[] { new(30f, 12f), new(58f, 40f), new(92f, 62f), new(112f, 80f), new(124f, 100f), new(132f, 126f), new(150f, 150f), new(168f, 178f), new(176f, 196f) } },
            // East road past the watchtower, over the east bridge to the graveyard's east gate.
            new() { Width = 4.5f, Points = new Vector2[] { new(32f, 0f), new(76f, 6f), new(128f, 14f), new(166f, 22f), new(200f, 60f), new(222f, 80f), new(226f, 112f), new(214f, 140f) } },
            // South street down to the swamp causeway.
            new() { Width = 5f, Wobble = 3f, Points = new Vector2[] { new(18f, -12f), new(18f, -40f), new(20f, -66f), new(22f, -84f), new(22f, -110f) } },
            // West road past the gallows, over the west bridge to the hunter's lodge.
            new() { Width = 4.5f, Points = new Vector2[] { new(4f, 0f), new(-40f, -6f), new(-96f, -16f), new(-150f, -36f), new(-178f, -46f), new(-208f, -40f) } },
            // Ring street of the village.
            new() { Width = 4f, Wobble = 3f, Points = new Vector2[] { new(-40f, -6f), new(-52f, -40f), new(-20f, -66f), new(20f, -66f), new(70f, -60f), new(110f, -36f), new(128f, 14f) } },
            new() { Width = 4f, Wobble = 3f, Points = new Vector2[] { new(-40f, -6f), new(-36f, 34f), new(-6f, 62f), new(34f, 72f), new(72f, 58f), new(92f, 62f) } },
            // Farm past the abandoned windmill to the graveyard's west gate over the north bridge.
            new() { Width = 4f, Points = new Vector2[] { new(-104f, 214f), new(-60f, 222f), new(-30f, 222f), new(-12f, 214f), new(16f, 212f), new(44f, 222f), new(70f, 210f), new(110f, 196f) } },
            // Farm yard loop, a ragged ring round the well.
            new() { Width = 4f, Wobble = 4f, Points = Arc(FarmCenter, 36f, -40f, 250f, 11, 9f) },
            // Trails through the woods.
            Trail(new(-176f, 196f), new(-196f, 214f), new(-222f, 224f)),
            Trail(new(-160f, 232f), new(-186f, 246f), new(-214f, 238f)),
            Trail(new(-196f, 122f), new(-214f, 96f), new(-212f, 70f), new(-214f, 52f), new(-208f, 20f), new(-214f, -36f)),
            Trail(new(-214f, -36f), new(-222f, -70f), new(-212f, -116f)),
            Trail(new(-212f, -116f), new(-190f, -160f), new(-146f, -208f)),
            Trail(new(-116f, -12f), new(-104f, -44f), new(-118f, -74f), new(-112f, -104f)),
            Trail(new(-58f, -52f), new(-84f, -76f), new(-112f, -104f)),
            Trail(new(166f, 22f), new(172f, -14f), new(160f, -52f), new(130f, -84f), new(106f, -116f)),
            Trail(new(84f, -50f), new(96f, -80f), new(106f, -116f)),
            Trail(new(106f, -116f), new(140f, -142f), new(178f, -152f), new(206f, -142f)),
            Trail(new(206f, -142f), new(190f, -186f), new(160f, -214f))
        };

        /// Bridges are given by a point near the water; the builder snaps them onto the river and lays them across it.
        public static readonly Vector2[] RiverBridges = { new(117f, 86f), new(204f, 64f), new(-13f, 214f) };
        public static readonly Vector2[] StreamBridges = { new(-30f, 112f), new(-184f, -46f) };

        public static readonly (Vector2 center, Vector2 size, float yaw)[] Fields =
        {
            (new Vector2(-192f, 158f), new Vector2(32f, 22f), -6f),
            (new Vector2(-112f, 128f), new Vector2(30f, 24f), 11f),
            (new Vector2(-170f, 210f), new Vector2(24f, 18f), 4f)
        };

        public static readonly Vector2 Pond = new(-150f, 120f);
        public const float PondRadius = 7f;

        /// Boardwalks over the swamp, all from the end of the south causeway.
        public static readonly Vector2[][] Boardwalks =
        {
            new Vector2[] { new(22f, -104f), new(20f, -116f), new(22f, -128f) },
            new Vector2[] { new(22f, -128f), new(10f, -160f), new(0f, -196f) },
            new Vector2[] { new(22f, -128f), new(-24f, -142f), new(-74f, -158f) },
            new Vector2[] { new(22f, -128f), new(58f, -156f), new(88f, -176f) },
            new Vector2[] { new(0f, -228f), new(-40f, -226f), new(-90f, -222f), new(-136f, -206f) },
            new Vector2[] { new(88f, -176f), new(130f, -204f), new(158f, -214f) },
            new Vector2[] { new(-80f, -162f), new(-96f, -134f), new(-112f, -104f) },
            new Vector2[] { new(26f, -126f), new(66f, -118f), new(106f, -116f) }
        };

        /// Huts on stilts: position and the way their door faces.
        public static readonly (Vector2 position, float yaw)[] StiltHuts =
        {
            (new Vector2(-116f, -102f), 30f), (new Vector2(-80f, -164f), 70f), (new Vector2(92f, -182f), -60f), (new Vector2(204f, -150f), -120f),
            (new Vector2(162f, -220f), -90f), (new Vector2(-142f, -210f), 80f)
        };

        public static readonly string[] ModuleNames =
        {
            "Western Mire", "Stilt Huts", "Crimson Isle", "Black Bog", "Hermit's Hut",
            "Willow Ford", "Swamp Edge", "South Causeway", "Reed Banks", "Fisher's Jetty",
            "Hunter's Lodge", "Gallows Hill", "Village Square", "East Quarter", "Watchtower",
            "Wheat Fields", "Farmstead", "North Bridge", "Gravekeeper's Path", "Graveyard",
            "Woodcutter's Camp", "North Fields", "Abandoned Windmill", "Old Graves", "Ruined Chapel"
        };

        public static Zone FindZone(string name) => System.Array.Find(Zones, zone => zone.Name == name);

        /// Signed distance to the nearest zone rim (negative inside).
        public static float ZoneDistance(float x, float z)
        {
            float best = float.MaxValue;

            foreach (Zone zone in Zones)
                best = Mathf.Min(best, zone.Distance(x, z));

            return best;
        }

        public static float SwampShare(float x, float z)
        {
            float edge = SwampEdge + (Mathf.PerlinNoise(x * 0.02f + 40f, 3.3f) - 0.5f) * 24f;

            return DungeonTextureBuilder.Step(edge + 10f, edge - 18f, z);
        }

        public static float EdgeDistance(float x, float z)
        {
            return Half - Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
        }

        /// The road as it lies on the ground: its curve wandering sideways by noise, held still near the bridges it crosses.
        public static List<Vector2> RoadLine(Road road, float step)
        {
            List<Vector2> line = Smooth(road.Points, step);
            List<Vector2> result = new(line.Count);
            float travelled = 0f;
            float seed = road.Points[0].x * 0.13f + road.Points[0].y * 0.07f;

            for (int i = 0; i < line.Count; i++)
            {
                Vector2 tangent = (line[Mathf.Min(i + 1, line.Count - 1)] - line[Mathf.Max(i - 1, 0)]).normalized;

                if (i > 0)
                    travelled += Vector2.Distance(line[i - 1], line[i]);

                float hold = Mathf.Min(DungeonTextureBuilder.Step(0f, 14f, BridgeDistance(line[i])), DungeonTextureBuilder.Step(0f, 10f, Mathf.Min(travelled, RemainingLength(line, i))));
                float offset = (DungeonTextureBuilder.Noise(travelled / 90f + seed, seed, 1f, 2) - 0.5f) * 2f * road.Wobble * hold;
                result.Add(line[i] + new Vector2(-tangent.y, tangent.x) * offset);
            }

            return result;
        }

        /// Catmull-Rom curve through the points, sampled about every step metres.
        public static List<Vector2> Smooth(IReadOnlyList<Vector2> points, float step)
        {
            List<Vector2> result = new();

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p0 = points[Mathf.Max(i - 1, 0)];
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];
                Vector2 p3 = points[Mathf.Min(i + 2, points.Count - 1)];
                int count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p1, p2) / step));

                for (int k = 0; k < count; k++)
                {
                    float t = k / (float)count;
                    float t2 = t * t;
                    float t3 = t2 * t;
                    result.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3));
                }
            }

            result.Add(points[^1]);

            return result;
        }

        /// Nearest point of a polyline and the direction it runs there.
        public static Vector2 Nearest(IReadOnlyList<Vector2> line, Vector2 point, out Vector2 direction)
        {
            float best = float.MaxValue;
            Vector2 nearest = line[0];
            direction = Vector2.up;

            for (int i = 0; i < line.Count - 1; i++)
            {
                Vector2 segment = line[i + 1] - line[i];
                float t = Mathf.Clamp01(Vector2.Dot(point - line[i], segment) / Mathf.Max(segment.sqrMagnitude, 1e-4f));
                Vector2 candidate = line[i] + segment * t;
                float distance = (point - candidate).sqrMagnitude;

                if (distance < best)
                {
                    best = distance;
                    nearest = candidate;
                    direction = segment.normalized;
                }
            }

            return nearest;
        }

        private static Road Trail(params Vector2[] points)
        {
            return new Road { Width = 2.6f, Verge = 2.8f, Wobble = 6f, IsTrail = true, Points = points };
        }

        private static float BridgeDistance(Vector2 point)
        {
            float best = float.MaxValue;

            foreach (Vector2 bridge in RiverBridges)
                best = Mathf.Min(best, (bridge - point).magnitude);

            foreach (Vector2 bridge in StreamBridges)
                best = Mathf.Min(best, (bridge - point).magnitude);

            return best;
        }

        private static float RemainingLength(List<Vector2> line, int index)
        {
            float length = 0f;

            for (int i = index; i < line.Count - 1 && length < 10f; i++)
                length += Vector2.Distance(line[i], line[i + 1]);

            return length;
        }

        /// A horseshoe round the centre from one angle to the other (degrees) whose radius breathes with noise.
        private static Vector2[] Arc(Vector2 center, float radius, float from, float to, int count, float ragged)
        {
            Vector2[] points = new Vector2[count + 1];

            for (int i = 0; i <= count; i++)
            {
                float angle = Mathf.Lerp(from, to, i / (float)count) * Mathf.Deg2Rad;
                float r = radius + (Mathf.PerlinNoise(Mathf.Cos(angle) * 1.3f + 5f, Mathf.Sin(angle) * 1.3f + 9f) - 0.5f) * 2f * ragged;
                points[i] = center + new Vector2(Mathf.Cos(angle + 0.12f * Mathf.Sin(angle * 3f)), Mathf.Sin(angle + 0.12f * Mathf.Sin(angle * 3f))) * r;
            }

            return points;
        }
    }
}
