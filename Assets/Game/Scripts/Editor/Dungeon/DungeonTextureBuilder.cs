using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Procedural albedo + normal textures for the crypt: stone, flagstones, wood, metal, bone, flesh and cloth.
    internal static class DungeonTextureBuilder
    {
        public const string Folder = "Assets/Game/Textures/Dungeon";
        public const int Size = 512;
        private const int EnvironmentSize = 1024;
        private const int OcclusionRadius = 6;

        public static void Build()
        {
            Directory.CreateDirectory(Folder);

            Write("StoneWall", StoneWall, 0.8f, EnvironmentSize, true);
            Write("StoneFloor", StoneFloor, 0.8f, EnvironmentSize, true);
            Write("Cobble", Cobble, 1f, EnvironmentSize, true);
            Write("WoodPlanks", WoodPlanks, 0.6f, EnvironmentSize, true);
            Write("DarkWood", DarkWood, 0.4f);
            Write("RustyMetal", RustyMetal, 0.35f);
            Write("Bone", Bone, 0.5f);
            Write("ClothRed", ClothRed, 0.25f);
            Write("Gold", Gold, 0.3f);
            Write("Dirt", Dirt, 0.6f);
            WriteFlame();
            WriteCobweb();

            AssetDatabase.Refresh();
            Debug.Log($"[{nameof(DungeonTextureBuilder)}] Textures built in {Folder}");
        }

        public static Texture2D Load(string name, bool isNormal)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{name}{(isNormal ? "_n" : string.Empty)}.png");
        }

        /// Cavity map of the environment textures; null for the ones that have none.
        public static Texture2D LoadOcclusion(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{name}_o.png");
        }

        private delegate Color Sampler(float u, float v, out float height);

        /// Soft radial sprite for fire and glow particles (RGBA, alpha fades to the edge).
        private static void WriteFlame()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size - 0.5f;
                    float dy = (y + 0.5f) / size - 0.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            string path = $"{Folder}/Flame.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            UnityEngine.Object.DestroyImmediate(texture);
        }

        /// Radial web with concentric strands, alpha outside the strands (RGBA, clamped).
        private static void WriteCobweb()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            System.Random random = new System.Random(7);
            float[] ringJitter = new float[12];

            for (int i = 0; i < ringJitter.Length; i++)
                ringJitter[i] = (float)random.NextDouble() * 0.03f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size;
                    float dy = (y + 0.5f) / size;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float spokes = Mathf.Abs(Mathf.Sin(angle * 9f));
                    float spoke = Step(0.985f, 1f, 1f - spokes);
                    int ring = Mathf.FloorToInt(radius * 11f);
                    float ringPos = Mathf.Repeat(radius * 11f + (ring < ringJitter.Length ? ringJitter[ring] : 0f) + Mathf.Sin(angle * 9f) * 0.12f, 1f);
                    float strand = Step(0.9f, 1f, 1f - Mathf.Abs(ringPos - 0.5f) * 2f);
                    float fade = Mathf.Clamp01(1.15f - radius);
                    float alpha = Mathf.Max(spoke, strand) * fade * (0.55f + 0.45f * Noise(dx, dy, 6f, 2));
                    pixels[y * size + x] = new Color(0.85f, 0.85f, 0.8f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            string path = $"{Folder}/Cobweb.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void Write(string name, Sampler sampler, float normalStrength, int size = Size, bool hasOcclusion = false)
        {
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            Texture2D normal = new Texture2D(size, size, TextureFormat.RGB24, false);
            float[,] heights = new float[size, size];
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = sampler(x / (float)size, y / (float)size, out heights[x, y]);
                }
            }

            albedo.SetPixels(pixels);
            albedo.Apply();

            // The slope is per texel: a larger texture of the same surface needs a proportionally stronger gain.
            float gain = normalStrength * 8f * size / Size;
            Color[] normals = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float left = heights[(x + size - 1) % size, y];
                    float right = heights[(x + 1) % size, y];
                    float down = heights[x, (y + size - 1) % size];
                    float up = heights[x, (y + 1) % size];
                    Vector3 n = new Vector3((left - right) * gain, (down - up) * gain, 1f).normalized;
                    normals[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
                }
            }

            normal.SetPixels(normals);
            normal.Apply();

            Save(albedo, $"{Folder}/{name}.png", TextureImporterType.Default, true);
            Save(normal, $"{Folder}/{name}_n.png", TextureImporterType.NormalMap, false);
            UnityEngine.Object.DestroyImmediate(albedo);
            UnityEngine.Object.DestroyImmediate(normal);

            if (hasOcclusion)
                WriteOcclusion(name, heights, size);
        }

        /// Cavities are the texels that lie below their surroundings: joints, cracks and the gaps between stones.
        private static void WriteOcclusion(string name, float[,] heights, int size)
        {
            float[,] rows = new float[size, size];
            float[,] blurred = new float[size, size];
            const int taps = OcclusionRadius * 2 + 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;

                    for (int i = -OcclusionRadius; i <= OcclusionRadius; i++)
                        sum += heights[(x + i + size) % size, y];

                    rows[x, y] = sum / taps;
                }
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;

                    for (int i = -OcclusionRadius; i <= OcclusionRadius; i++)
                        sum += rows[x, (y + i + size) % size];

                    blurred[x, y] = sum / taps;
                }
            }

            Texture2D occlusion = new Texture2D(size, size, TextureFormat.RGB24, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float value = Mathf.Clamp01(1f - Mathf.Max(0f, blurred[x, y] - heights[x, y]) * 2.2f);
                    pixels[y * size + x] = new Color(value, value, value);
                }
            }

            occlusion.SetPixels(pixels);
            occlusion.Apply();
            Save(occlusion, $"{Folder}/{name}_o.png", TextureImporterType.Default, false);
            UnityEngine.Object.DestroyImmediate(occlusion);
        }

        private static void Save(Texture2D texture, string path, TextureImporterType type, bool isColor)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = type;
            importer.sRGBTexture = isColor;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 8;
            importer.SaveAndReimport();
        }

        /// GLSL-style smoothstep: 0 below edge0, 1 above edge1 (Mathf.SmoothStep interpolates instead).
        public static float Step(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));

            return t * t * (3f - 2f * t);
        }

        public static float Noise(float u, float v, float scale, int octaves = 4, float persistence = 0.5f)
        {
            float total = 0f;
            float amplitude = 1f;
            float max = 0f;
            float frequency = scale;

            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(u * frequency + i * 17.3f, v * frequency + i * 31.7f) * amplitude;
                max += amplitude;
                amplitude *= persistence;
                frequency *= 2f;
            }

            return total / max;
        }

        /// Tileable noise: the field and its copy shifted by one tile are cross-faded, so both borders meet.
        private static float TileNoise(float u, float v, float scale, int octaves = 4)
        {
            return TileNoise(u, v, scale, scale, octaves);
        }

        private static float TileNoise(float u, float v, float scaleU, float scaleV, int octaves)
        {
            float a = Fractal((u + 1f) * scaleU, (v + 1f) * scaleV, octaves);
            float b = Fractal(u * scaleU, (v + 1f) * scaleV, octaves);
            float c = Fractal((u + 1f) * scaleU, v * scaleV, octaves);
            float d = Fractal(u * scaleU, v * scaleV, octaves);

            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        private static float Fractal(float x, float y, int octaves)
        {
            float total = 0f;
            float amplitude = 1f;
            float max = 0f;

            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(x + i * 17.3f, y + i * 31.7f) * amplitude;
                max += amplitude;
                amplitude *= 0.5f;
                x *= 2f;
                y *= 2f;
            }

            return total / max;
        }

        private static int Wrap(int value, int count)
        {
            return (value % count + count) % count;
        }

        /// Courses of blocks with randomly shifted joints, as masons lay them. Returns the distance to the nearest joint
        /// in tile units, so joints are equally wide in both directions.
        private static float Blocks(float u, float v, int rows, int columns, float jitter, out int id)
        {
            float y = v * rows;
            int row = Mathf.FloorToInt(y);
            float fy = y - row;
            row = Wrap(row, rows);
            float x = u * columns + Hash(row, 91) * columns;
            int column = Mathf.FloorToInt(x);

            if (x < Joint(column, row, columns, jitter))
                column--;
            else if (x >= Joint(column + 1, row, columns, jitter))
                column++;

            float left = Joint(column, row, columns, jitter);
            float right = Joint(column + 1, row, columns, jitter);
            id = Wrap(column, columns) + row * 64;

            return Mathf.Min(Mathf.Min(x - left, right - x) / columns, Mathf.Min(fy, 1f - fy) / rows);
        }

        private static float Joint(int column, int row, int columns, float jitter)
        {
            return column + (Hash(Wrap(column, columns), row) - 0.5f) * jitter;
        }

        private static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;

            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }

        /// Cross-fading flattens tileable noise; this stretches it back around the middle.
        private static float Grain(float u, float v, float scale, float contrast)
        {
            return Mathf.Clamp01((TileNoise(u, v, scale, 4) - 0.5f) * contrast + 0.5f);
        }

        /// Dressed stone in courses, 16 x 8 blocks per tile: a quarter of a metre tall and half a metre long on a 4 m tile.
        private static Color StoneWall(float u, float v, out float height)
        {
            const float joint = 0.0016f;
            float edge = Blocks(u, v, 16, 8, 0.5f, out int id);
            float grain = Grain(u, v, 70f, 2.6f);
            float patches = Grain(u + 0.21f, v + 0.63f, 14f, 2.2f);
            float damp = TileNoise(u + 0.37f, v + 0.11f, 5f, 3);
            float chip = Grain(u + 0.7f, v + 0.2f, 30f, 3f);
            float pit = Step(0.79f, 0.86f, Grain(u + 0.4f, v + 0.5f, 110f, 2.4f));
            float face = Step(joint, joint + 0.0025f + chip * chip * 0.01f, edge);
            height = face * (0.6f + Hash(id, 5) * 0.2f + grain * 0.14f + patches * 0.08f - pit * 0.18f);

            Color stone = Color.Lerp(new Color(0.5f, 0.49f, 0.47f), new Color(0.56f, 0.52f, 0.45f), Hash(id, 3));
            stone *= (0.8f + Hash(id, 7) * 0.3f) * (0.74f + grain * 0.36f + patches * 0.16f) * (1f - pit * 0.35f);
            stone = Color.Lerp(stone, stone * new Color(0.62f, 0.68f, 0.6f), Step(0.55f, 0.8f, damp));
            stone *= Mathf.Lerp(0.72f, 1f, Step(joint, joint + 0.012f, edge));
            Color mortar = new Color(0.3f, 0.28f, 0.25f) * (0.6f + grain * 0.7f);

            return Color.Lerp(mortar, stone, face);
        }

        /// Worn flagstones, 6 x 6 per tile: two thirds of a metre on a 4 m tile.
        private static Color StoneFloor(float u, float v, out float height)
        {
            const float joint = 0.0016f;
            float edge = Blocks(u, v, 6, 6, 0.7f, out int id);
            float grain = Grain(u, v, 64f, 2.6f);
            float patches = Grain(u + 0.21f, v + 0.63f, 12f, 2.2f);
            float wear = TileNoise(u + 0.3f, v + 0.6f, 6f, 3);
            float chip = Grain(u + 0.1f, v + 0.8f, 26f, 3f);
            float pit = Step(0.79f, 0.86f, Grain(u + 0.4f, v + 0.5f, 100f, 2.4f));
            float face = Step(joint, joint + 0.003f + chip * chip * 0.012f, edge);
            height = face * (0.7f + Hash(id, 5) * 0.1f + grain * 0.12f + patches * 0.08f - pit * 0.15f);

            Color stone = Color.Lerp(new Color(0.5f, 0.49f, 0.47f), new Color(0.47f, 0.44f, 0.4f), Hash(id, 3));
            stone *= (0.85f + Hash(id, 7) * 0.22f) * (0.76f + grain * 0.32f + patches * 0.16f) * (1f - pit * 0.3f);
            stone = Color.Lerp(stone, stone * 0.74f, Step(0.5f, 0.8f, wear));
            stone *= Mathf.Lerp(0.74f, 1f, Step(joint, joint + 0.016f, edge));
            Color gap = new Color(0.17f, 0.15f, 0.12f) * (0.6f + grain * 0.7f);

            return Color.Lerp(gap, stone, face);
        }

        /// Rounded cobbles bedded in dirt, 16 x 16 per tile: a quarter of a metre on a 4 m tile.
        private static Color Cobble(float u, float v, out float height)
        {
            const int cells = 16;
            float nearest = 9f;
            float second = 9f;
            float id = 0f;
            Vector2 point = new Vector2(u * cells, v * cells);

            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = Mathf.FloorToInt(point.x) + ox;
                    int cy = Mathf.FloorToInt(point.y) + oy;
                    int wx = Wrap(cx, cells);
                    int wy = Wrap(cy, cells);
                    float distance = Vector2.Distance(point, new Vector2(cx + 0.2f + Hash(wx, wy) * 0.6f, cy + 0.2f + Hash(wy, wx + 3) * 0.6f));

                    if (distance < nearest)
                    {
                        second = nearest;
                        nearest = distance;
                        id = Hash(wx + 11, wy + 5);
                    }
                    else if (distance < second)
                    {
                        second = distance;
                    }
                }
            }

            // A stone is its cell cut by a disc around the centre: the corners round off and dirt fills the wedges.
            float inside = Mathf.Min((second - nearest) * 0.9f, 0.6f + id * 0.12f - nearest);
            float grain = Grain(u, v, 80f, 2.6f);
            float stone = Step(0.05f, 0.12f, inside);
            float dome = Mathf.Sqrt(Mathf.Clamp01(inside * 2.2f));
            height = stone * (0.25f + dome * 0.6f + grain * 0.12f);

            Color color = Color.Lerp(new Color(0.47f, 0.46f, 0.45f), new Color(0.54f, 0.5f, 0.44f), id);
            color *= (0.72f + grain * 0.42f) * Mathf.Lerp(0.55f, 1.08f, dome);
            Color dirt = new Color(0.21f, 0.18f, 0.14f) * (0.6f + grain * 0.7f);

            return Color.Lerp(dirt, color, stone);
        }

        /// Sawn boards, 12 across the tile (a third of a metre on a 4 m tile), butted every two metres and nailed at the ends.
        private static Color WoodPlanks(float u, float v, out float height)
        {
            const int planks = 12;
            const float width = 1f / planks;
            float across = u * planks;
            int index = Mathf.FloorToInt(across);
            float fx = across - index;
            index = Wrap(index, planks);
            float along = Mathf.Repeat(v + Hash(index, 4), 1f);
            float end = Mathf.Min(Mathf.Repeat(along, 0.5f), 0.5f - Mathf.Repeat(along, 0.5f));
            float edge = Mathf.Min(Mathf.Min(fx, 1f - fx) * width, end);
            float board = Step(0.0012f, 0.004f, edge);
            float grain = TileNoise(fx, along, 6f, 2f, 4);
            float fibre = TileNoise(fx, along, 40f, 3f, 2);
            float rings = Mathf.Abs(Mathf.Sin((fx * 2.5f + grain * 5f) * Mathf.PI));
            Vector2 toKnot = new Vector2((fx - 0.2f - Hash(index, 6) * 0.6f) * width, Mathf.Repeat(along - Hash(index, 8) + 0.5f, 1f) - 0.5f);
            float knot = Hash(index, 10) < 0.6f ? 1f - Step(0.004f, 0.012f, toKnot.magnitude) : 0f;
            Vector2 toNail = new Vector2((Mathf.Abs(fx - 0.5f) - 0.3f) * width, end - 0.012f);
            float nail = 1f - Step(0.0018f, 0.003f, toNail.magnitude);
            height = board * (0.55f + rings * 0.14f + fibre * 0.2f - knot * 0.1f) + nail * 0.15f;

            Color wood = Color.Lerp(new Color(0.52f, 0.38f, 0.23f), new Color(0.34f, 0.23f, 0.13f), rings * 0.55f + fibre * 0.45f);
            wood *= (0.78f + Hash(index, 2) * 0.4f) * (1f - knot * 0.55f);
            wood *= Mathf.Lerp(0.7f, 1f, Step(0.0012f, 0.012f, edge));
            wood = Color.Lerp(wood, new Color(0.13f, 0.12f, 0.12f), nail);

            return Color.Lerp(new Color(0.07f, 0.05f, 0.03f), wood, board);
        }

        private static Color DarkWood(float u, float v, out float height)
        {
            float grain = Mathf.PerlinNoise(u * 6f, v * 50f);
            float rings = Mathf.Abs(Mathf.Sin((v * 14f + grain * 3f) * Mathf.PI));
            height = 0.5f + rings * 0.3f;
            Color wood = Color.Lerp(new Color(0.24f, 0.15f, 0.08f), new Color(0.15f, 0.09f, 0.05f), rings) * (0.85f + grain * 0.3f);

            return wood;
        }

        private static Color RustyMetal(float u, float v, out float height)
        {
            float rust = TileNoise(u, v, 6f, 4);
            float scratches = Mathf.PerlinNoise(u * 90f, v * 3f);
            height = 0.5f + (rust - 0.5f) * 0.4f + scratches * 0.1f;
            Color metal = new Color(0.32f, 0.31f, 0.3f) * (0.8f + scratches * 0.3f);
            Color rustColor = new Color(0.45f, 0.22f, 0.1f);

            return Color.Lerp(metal, rustColor, Step(0.45f, 0.7f, rust));
        }

        private static Color Bone(float u, float v, out float height)
        {
            float grain = TileNoise(u, v, 10f, 3);
            float cracks = Step(0.008f, 0.018f, Mathf.Abs(TileNoise(u + 0.2f, v, 4f, 2) - 0.5f));
            height = 0.6f + grain * 0.2f - (1f - cracks) * 0.3f;
            Color bone = Color.Lerp(new Color(0.78f, 0.74f, 0.62f), new Color(0.6f, 0.55f, 0.42f), grain);

            return Color.Lerp(new Color(0.3f, 0.25f, 0.18f), bone, cracks);
        }

        private static Color ClothRed(float u, float v, out float height)
        {
            float weave = (Mathf.Sin(u * Mathf.PI * 160f) * Mathf.Sin(v * Mathf.PI * 160f)) * 0.5f + 0.5f;
            float wear = TileNoise(u, v, 4f, 3);
            float emblem = Mathf.Abs(u - 0.5f) < 0.18f && Mathf.Abs(v - 0.5f) < 0.22f
                ? Mathf.Clamp01(1f - (Mathf.Abs(Mathf.Abs(u - 0.5f) - Mathf.Abs(v - 0.5f) * 0.6f)) * 12f)
                : 0f;
            height = 0.5f + weave * 0.1f;
            Color cloth = Color.Lerp(new Color(0.45f, 0.08f, 0.08f), new Color(0.3f, 0.05f, 0.05f), wear) * (0.9f + weave * 0.15f);

            return Color.Lerp(cloth, new Color(0.85f, 0.7f, 0.3f), emblem * 0.9f);
        }

        private static Color Gold(float u, float v, out float height)
        {
            float coins = 0f;
            float best = 1f;
            const int cells = 12;

            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = Mathf.FloorToInt(u * cells) + ox;
                    int cy = Mathf.FloorToInt(v * cells) + oy;
                    Vector2 center = new Vector2(cx + Hash((cx + cells) % cells, (cy + cells) % cells), cy + Hash((cy + cells) % cells, (cx + cells) % cells + 9));
                    float distance = Vector2.Distance(new Vector2(u * cells, v * cells), center);
                    best = Mathf.Min(best, distance);
                }
            }

            coins = Mathf.Clamp01(1f - best * 2.2f);
            height = coins * coins;
            float shine = TileNoise(u, v, 20f, 2);

            return Color.Lerp(new Color(0.35f, 0.25f, 0.08f), new Color(0.95f, 0.78f, 0.3f), coins * (0.7f + shine * 0.3f));
        }

        private static Color Dirt(float u, float v, out float height)
        {
            float lumps = TileNoise(u, v, 9f, 4);
            float pebbles = Step(0.6f, 0.75f, TileNoise(u + 0.5f, v + 0.1f, 25f, 2));
            height = lumps * 0.5f + pebbles * 0.4f;

            return Color.Lerp(new Color(0.22f, 0.17f, 0.12f), new Color(0.35f, 0.28f, 0.2f), lumps) * (1f + pebbles * 0.3f);
        }
    }
}
