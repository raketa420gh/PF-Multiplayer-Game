using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Height field of the village floor and the distance fields the builders share (river, stream, roads). Heights are made in
    /// two steps: the natural ground first, so the planners can read it, then the pads levelled under buildings, yards and bridges.
    internal sealed class VillageGround
    {
        public const int Resolution = 1025;
        public const float Bottom = -4f;
        public const float Depth = 48f;

        /// Levelled ground: a rectangle at a height, blending into the land over the margin.
        public sealed class Pad
        {
            public Vector2 Center;
            public Vector2 HalfSize;
            public float Yaw;
            public float Margin = 3f;
            public float Height = float.NaN;
            public bool IsRound;
        }

        public float Spacing => VillageLayout.Size / (Resolution - 1);
        public IReadOnlyList<Vector2> River => _river;
        public IReadOnlyList<Vector2> Stream => _stream;
        public IReadOnlyList<Pad> Pads => _pads;

        private readonly float[,] _heights = new float[Resolution, Resolution];
        private readonly float[,] _riverDistance;
        private readonly float[,] _streamDistance;
        private readonly float[,] _roadDistance;
        private readonly float[,] _padMask = new float[Resolution, Resolution];
        private readonly List<Vector2> _river;
        private readonly List<Vector2> _stream;
        private readonly List<Pad> _pads = new();

        public VillageGround()
        {
            _river = VillageLayout.Smooth(VillageLayout.River, 2f);
            _stream = VillageLayout.Smooth(VillageLayout.Stream, 2f);
            _riverDistance = Field(60f);
            _streamDistance = Field(60f);
            _roadDistance = Field(30f);
            Stamp(_river, 60f, _riverDistance, 0f);
            Stamp(_stream, 60f, _streamDistance, 0f);

            foreach (VillageLayout.Road road in VillageLayout.Roads)
                Stamp(VillageLayout.Smooth(road.Points, 2f), 30f, _roadDistance, road.Width * 0.5f);

            for (int z = 0; z < Resolution; z++)
            {
                for (int x = 0; x < Resolution; x++)
                    _heights[z, x] = Natural(z, x);
            }
        }

        /// Ground height at a world point (bilinear).
        public float Height(float x, float z)
        {
            return Bilinear(_heights, x, z);
        }

        public float Height(Vector2 point) => Height(point.x, point.y);

        public float RiverDistance(float x, float z) => Bilinear(_riverDistance, x, z);
        public float StreamDistance(float x, float z) => Bilinear(_streamDistance, x, z);
        /// Distance to the edge of the nearest road (negative on it).
        public float RoadDistance(float x, float z) => Bilinear(_roadDistance, x, z);
        public float PadMask(float x, float z) => Bilinear(_padMask, x, z);

        public float WaterDistance(float x, float z)
        {
            return Mathf.Min(RiverDistance(x, z) - VillageLayout.RiverWidth * 0.5f, StreamDistance(x, z) - VillageLayout.StreamWidth * 0.5f);
        }

        public void AddPad(Pad pad)
        {
            _pads.Add(pad);
        }

        /// Levels every pad in the order they were added; a pad without a height takes the ground under its centre.
        public void ApplyPads()
        {
            foreach (Pad pad in _pads)
            {
                if (float.IsNaN(pad.Height))
                    pad.Height = Mathf.Max(Height(pad.Center), VillageLayout.WaterLevel + 0.4f);

                float reach = pad.HalfSize.magnitude + pad.Margin;
                Index(pad.Center - Vector2.one * reach, out int x0, out int z0);
                Index(pad.Center + Vector2.one * reach, out int x1, out int z1);
                Quaternion inverse = Quaternion.Euler(0f, -pad.Yaw, 0f);

                for (int z = Mathf.Max(z0, 0); z <= Mathf.Min(z1 + 1, Resolution - 1); z++)
                {
                    for (int x = Mathf.Max(x0, 0); x <= Mathf.Min(x1 + 1, Resolution - 1); x++)
                    {
                        Vector2 world = World(x, z);
                        Vector3 local = inverse * new Vector3(world.x - pad.Center.x, 0f, world.y - pad.Center.y);
                        float outside = pad.IsRound
                            ? new Vector2(local.x, local.z).magnitude - pad.HalfSize.x
                            : new Vector2(Mathf.Max(Mathf.Abs(local.x) - pad.HalfSize.x, 0f), Mathf.Max(Mathf.Abs(local.z) - pad.HalfSize.y, 0f)).magnitude;
                        float weight = DungeonTextureBuilder.Step(pad.Margin, 0f, outside);

                        if (weight <= 0f)
                            continue;

                        _heights[z, x] = Mathf.Lerp(_heights[z, x], pad.Height, weight);
                        _padMask[z, x] = Mathf.Max(_padMask[z, x], weight);
                    }
                }
            }
        }

        /// Normalised heights for TerrainData ([z, x]).
        public float[,] Normalized()
        {
            float[,] result = new float[Resolution, Resolution];

            for (int z = 0; z < Resolution; z++)
            {
                for (int x = 0; x < Resolution; x++)
                    result[z, x] = Mathf.Clamp01((_heights[z, x] - Bottom) / Depth);
            }

            return result;
        }

        public Vector2 World(int x, int z)
        {
            return new Vector2(x * Spacing - VillageLayout.Half, z * Spacing - VillageLayout.Half);
        }

        public void Index(Vector2 world, out int x, out int z)
        {
            x = Mathf.FloorToInt((world.x + VillageLayout.Half) / Spacing);
            z = Mathf.FloorToInt((world.y + VillageLayout.Half) / Spacing);
        }

        /// Steepness at a world point in degrees.
        public float Slope(float x, float z)
        {
            float dx = Height(x + Spacing, z) - Height(x - Spacing, z);
            float dz = Height(x, z + Spacing) - Height(x, z - Spacing);

            return Mathf.Atan(new Vector2(dx, dz).magnitude / (2f * Spacing)) * Mathf.Rad2Deg;
        }

        /// The land before anyone built on it: rolling meadows, the swamp sinking to the water, hills closing the map,
        /// the river and the stream cut into it, the fields and the pond.
        private float Natural(int z, int x)
        {
            Vector2 p = World(x, z);
            float height = 1.8f + (DungeonTextureBuilder.Noise(p.x / 540f + 3f, p.y / 540f + 7f, 6f, 4) - 0.5f) * 3.2f;
            float swamp = VillageLayout.SwampShare(p.x, p.y);
            float marsh = 0.12f + DungeonTextureBuilder.Noise(p.x / 540f + 11f, p.y / 540f + 5f, 28f, 3) * 0.95f;
            height = Mathf.Lerp(height, marsh, swamp);

            Rect yard = VillageLayout.Graveyard;
            float outsideYard = new Vector2(Mathf.Max(yard.xMin - p.x, p.x - yard.xMax, 0f), Mathf.Max(yard.yMin - p.y, p.y - yard.yMax, 0f)).magnitude;
            height += DungeonTextureBuilder.Step(8f, 0f, outsideYard) * 0.5f;

            float edge = VillageLayout.EdgeDistance(p.x, p.y);
            float hill = DungeonTextureBuilder.Step(34f, 2f, edge);
            height += hill * (12f + DungeonTextureBuilder.Noise(p.x / 540f + 1f, p.y / 540f + 9f, 9f, 4) * 22f);

            height = Carve(height, _riverDistance[z, x], VillageLayout.RiverWidth, -1.6f, swamp);
            height = Carve(height, _streamDistance[z, x], VillageLayout.StreamWidth, -1.1f, swamp);

            foreach ((Vector2 center, Vector2 size, float yaw) in VillageLayout.Fields)
            {
                Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * new Vector3(p.x - center.x, 0f, p.y - center.y);
                float outside = new Vector2(Mathf.Max(Mathf.Abs(local.x) - size.x * 0.5f, 0f), Mathf.Max(Mathf.Abs(local.z) - size.y * 0.5f, 0f)).magnitude;
                height = Mathf.Lerp(height, 1.9f, DungeonTextureBuilder.Step(8f, 0f, outside) * 0.8f);
            }

            float pond = (p - VillageLayout.Pond).magnitude;
            height = Mathf.Lerp(height, -0.6f, DungeonTextureBuilder.Step(VillageLayout.PondRadius + 3f, VillageLayout.PondRadius - 2f, pond));

            // Roads are worn a little into the ground.
            return height - DungeonTextureBuilder.Step(1.5f, -1f, _roadDistance[z, x]) * 0.12f * (1f - swamp);
        }

        /// A channel with a muddy bed and banks that rise over seven metres; in the swamp it spreads into the mire.
        private static float Carve(float height, float distance, float width, float bed, float swamp)
        {
            float bank = DungeonTextureBuilder.Step(width * 0.5f - 1.5f, width * 0.5f + 7f, distance);

            return Mathf.Lerp(Mathf.Lerp(bed, height, bank), height, swamp * 0.7f);
        }

        private float[,] Field(float value)
        {
            float[,] field = new float[Resolution, Resolution];

            for (int z = 0; z < Resolution; z++)
            {
                for (int x = 0; x < Resolution; x++)
                    field[z, x] = value;
            }

            return field;
        }

        /// Writes the distance to the line (minus its half width) into every cell within the radius, keeping the smaller value.
        private void Stamp(List<Vector2> line, float radius, float[,] field, float halfWidth)
        {
            for (int i = 0; i < line.Count - 1; i++)
            {
                Vector2 a = line[i];
                Vector2 b = line[i + 1];
                Index(Vector2.Min(a, b) - Vector2.one * radius, out int x0, out int z0);
                Index(Vector2.Max(a, b) + Vector2.one * radius, out int x1, out int z1);
                Vector2 segment = b - a;
                float length = Mathf.Max(segment.sqrMagnitude, 1e-4f);

                for (int z = Mathf.Max(z0, 0); z <= Mathf.Min(z1 + 1, Resolution - 1); z++)
                {
                    for (int x = Mathf.Max(x0, 0); x <= Mathf.Min(x1 + 1, Resolution - 1); x++)
                    {
                        Vector2 p = World(x, z);
                        float t = Mathf.Clamp01(Vector2.Dot(p - a, segment) / length);
                        float distance = (p - a - segment * t).magnitude - halfWidth;

                        if (distance < field[z, x])
                            field[z, x] = distance;
                    }
                }
            }
        }

        private float Bilinear(float[,] field, float wx, float wz)
        {
            float fx = Mathf.Clamp((wx + VillageLayout.Half) / Spacing, 0f, Resolution - 1.001f);
            float fz = Mathf.Clamp((wz + VillageLayout.Half) / Spacing, 0f, Resolution - 1.001f);
            int x = (int)fx;
            int z = (int)fz;
            float tx = fx - x;
            float tz = fz - z;
            float bottom = Mathf.Lerp(field[z, x], field[z, x + 1], tx);
            float top = Mathf.Lerp(field[z + 1, x], field[z + 1, x + 1], tx);

            return Mathf.Lerp(bottom, top, tz);
        }
    }
}
