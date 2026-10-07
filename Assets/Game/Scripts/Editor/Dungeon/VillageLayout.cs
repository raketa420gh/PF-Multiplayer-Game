using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// The first floor after the user's map "Этаж 1 — Проклятая деревня": 540 m square (the map's scale bar), 5 x 5 modules of 108 m.
    /// North-west the farm with its windmill and fields, north-east the walled graveyard with a ruined chapel, the village round
    /// its square in the middle, the swamp along the south. A river comes down between farm and graveyard and curls round the
    /// village to the swamp; a stream leaves it at the bend and closes the ring in the west. World X is east, Z is north, the origin is the map centre.
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

        public static readonly Vector2 FarmCenter = new(-138f, 168f);
        public const float FarmRadius = 78f;
        public static readonly Vector2 VillageCenter = new(22f, 4f);
        public const float VillageRadius = 98f;
        public static readonly Vector2 Plaza = new(18f, 2f);
        public const float PlazaRadius = 14f;
        public static readonly Rect Graveyard = Rect.MinMaxRect(84f, 96f, 236f, 238f);
        public static readonly Vector2 Chapel = new(176f, 210f);
        /// The swamp begins south of this line (shifted by noise).
        public const float SwampEdge = -86f;

        /// Ways down to the stone cellars: behind the chapel and on an island in the middle of the swamp. Stairs run along the yaw.
        public static readonly (Vector2 position, float yaw)[] Cellars = { (new Vector2(208f, 214f), 90f), (new Vector2(-2f, -214f), 180f) };

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
        }

        public static readonly Road[] Roads =
        {
            // Farm to the village over the bridge at the head of the stream.
            new() { Width = 5f, Points = new Vector2[] { new(-104f, 150f), new(-66f, 140f), new(-40f, 126f), new(-22f, 104f), new(-6f, 82f), new(12f, 50f), new(18f, 16f) } },
            // Village to the graveyard's south gate.
            new() { Width = 4.5f, Points = new Vector2[] { new(30f, 12f), new(58f, 40f), new(92f, 62f), new(112f, 80f), new(124f, 100f), new(132f, 126f), new(150f, 150f), new(168f, 178f), new(176f, 196f) } },
            // East road over the east bridge to the graveyard's east gate.
            new() { Width = 4.5f, Points = new Vector2[] { new(32f, 0f), new(76f, 6f), new(128f, 18f), new(170f, 36f), new(200f, 60f), new(222f, 80f), new(226f, 112f), new(214f, 140f) } },
            // South street down to the swamp causeway.
            new() { Width = 5f, Points = new Vector2[] { new(18f, -12f), new(18f, -40f), new(20f, -66f), new(22f, -84f) } },
            // West road over the west bridge into the woods.
            new() { Width = 4.5f, Points = new Vector2[] { new(4f, 0f), new(-40f, -6f), new(-96f, -16f), new(-150f, -36f), new(-178f, -46f), new(-208f, -52f) } },
            // Ring street of the village.
            new() { Width = 4f, Points = new Vector2[] { new(-40f, -6f), new(-52f, -40f), new(-20f, -66f), new(20f, -66f), new(70f, -60f), new(110f, -36f), new(128f, 18f) } },
            new() { Width = 4f, Points = new Vector2[] { new(-40f, -6f), new(-36f, 34f), new(-6f, 62f), new(34f, 72f), new(72f, 58f), new(92f, 62f) } },
            // Farm to the graveyard's west gate over the north ford bridge.
            new() { Width = 4f, Points = new Vector2[] { new(-104f, 214f), new(-60f, 222f), new(-30f, 222f), new(-12f, 214f), new(16f, 210f), new(52f, 206f), new(84f, 200f), new(110f, 196f) } },
            // Farm yard loop.
            new() { Width = 4f, Points = Circle(FarmCenter, 36f, 18) }
        };

        /// Bridges are given by a point near the water; the builder snaps them onto the river and lays them across it.
        public static readonly Vector2[] RiverBridges = { new(117f, 86f), new(204f, 64f), new(-13f, 214f) };
        public static readonly Vector2[] StreamBridges = { new(-30f, 112f), new(-184f, -46f) };

        public static readonly (Vector2 center, Vector2 size, float yaw)[] Fields =
        {
            (new Vector2(-198f, 150f), new Vector2(44f, 34f), 0f),
            (new Vector2(-96f, 106f), new Vector2(38f, 30f), 8f),
            (new Vector2(-186f, 212f), new Vector2(28f, 24f), 0f)
        };

        public static readonly Vector2 Pond = new(-136f, 118f);
        public const float PondRadius = 7f;

        /// Boardwalks over the swamp, all from the end of the south causeway.
        public static readonly Vector2[][] Boardwalks =
        {
            new Vector2[] { new(22f, -82f), new(22f, -104f), new(22f, -128f) },
            new Vector2[] { new(22f, -128f), new(10f, -160f), new(0f, -196f) },
            new Vector2[] { new(22f, -128f), new(-24f, -142f), new(-74f, -158f) },
            new Vector2[] { new(22f, -128f), new(58f, -156f), new(88f, -176f) },
            new Vector2[] { new(0f, -228f), new(-40f, -226f), new(-136f, -206f) },
            new Vector2[] { new(88f, -176f), new(130f, -204f), new(158f, -214f) }
        };

        /// Huts on stilts: position and the way their door faces.
        public static readonly (Vector2 position, float yaw)[] StiltHuts =
        {
            (new Vector2(-116f, -102f), 30f), (new Vector2(-80f, -164f), 70f), (new Vector2(92f, -182f), -60f), (new Vector2(204f, -150f), -120f),
            (new Vector2(162f, -220f), -90f), (new Vector2(-142f, -210f), 80f)
        };

        public static readonly string[] ModuleNames =
        {
            "Western Mire", "Stilt Huts", "Sunken Cellar", "Black Bog", "Hermit's Hut",
            "Willow Ford", "Swamp Edge", "South Causeway", "Reed Banks", "River Mouth",
            "West Stream", "West Quarter", "Village Square", "East Quarter", "East Bridge",
            "Wheat Fields", "Farmstead", "North Bridge", "Gravekeeper's Path", "Graveyard",
            "Western Woods", "Windmill", "River Bend", "Old Graves", "Ruined Chapel"
        };

        public static float SwampShare(float x, float z)
        {
            float edge = SwampEdge + (Mathf.PerlinNoise(x * 0.02f + 40f, 3.3f) - 0.5f) * 24f;

            return DungeonTextureBuilder.Step(edge + 10f, edge - 18f, z);
        }

        public static float EdgeDistance(float x, float z)
        {
            return Half - Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
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

        private static Vector2[] Circle(Vector2 center, float radius, int count)
        {
            Vector2[] points = new Vector2[count + 1];

            for (int i = 0; i <= count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            return points;
        }
    }
}
