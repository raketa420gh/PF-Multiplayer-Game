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

        public static void Build()
        {
            Directory.CreateDirectory(Folder);

            Write("StoneWall", StoneWall, 0.9f);
            Write("StoneFloor", StoneFloor, 0.7f);
            Write("Cobble", Cobble, 0.8f);
            Write("WoodPlanks", WoodPlanks, 0.5f);
            Write("DarkWood", DarkWood, 0.4f);
            Write("RustyMetal", RustyMetal, 0.35f);
            Write("Bone", Bone, 0.5f);
            Write("ZombieSkin", ZombieSkin, 0.45f);
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

        private static void Write(string name, Sampler sampler, float normalStrength)
        {
            Texture2D albedo = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            Texture2D normal = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            float[,] heights = new float[Size, Size];
            Color[] pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[y * Size + x] = sampler(x / (float)Size, y / (float)Size, out heights[x, y]);
                }
            }

            albedo.SetPixels(pixels);
            albedo.Apply();

            Color[] normals = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float left = heights[(x + Size - 1) % Size, y];
                    float right = heights[(x + 1) % Size, y];
                    float down = heights[x, (y + Size - 1) % Size];
                    float up = heights[x, (y + 1) % Size];
                    Vector3 n = new Vector3((left - right) * normalStrength * 8f, (down - up) * normalStrength * 8f, 1f).normalized;
                    normals[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
                }
            }

            normal.SetPixels(normals);
            normal.Apply();

            Save(albedo, $"{Folder}/{name}.png", false);
            Save(normal, $"{Folder}/{name}_n.png", true);
            UnityEngine.Object.DestroyImmediate(albedo);
            UnityEngine.Object.DestroyImmediate(normal);
        }

        private static void Save(Texture2D texture, string path, bool isNormal)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !isNormal;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        /// GLSL-style smoothstep: 0 below edge0, 1 above edge1 (Mathf.SmoothStep interpolates instead).
        private static float Step(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));

            return t * t * (3f - 2f * t);
        }

        private static float Noise(float u, float v, float scale, int octaves = 4, float persistence = 0.5f)
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

        /// Tileable noise using wrapped coordinates.
        private static float TileNoise(float u, float v, float scale, int octaves = 4)
        {
            float a = Noise(u, v, scale, octaves);
            float b = Noise(u + 1f, v, scale, octaves);
            float c = Noise(u, v + 1f, scale, octaves);
            float d = Noise(u + 1f, v + 1f, scale, octaves);

            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        private static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;

            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }

        private static Color StoneWall(float u, float v, out float height)
        {
            const int rows = 8;
            float row = v * rows;
            int rowIndex = Mathf.FloorToInt(row);
            float offset = rowIndex % 2 == 0 ? 0f : 0.5f;
            const int columns = 4;
            float column = u * columns + offset;
            int columnIndex = Mathf.FloorToInt(column);
            float fy = row - rowIndex;
            float fx = column - columnIndex;
            float mortar = 0.08f;
            float edge = Mathf.Min(Mathf.Min(fx, 1f - fx) * columns / rows, Mathf.Min(fy, 1f - fy));
            float brick = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((edge - mortar * 0.5f) / mortar));
            float grain = TileNoise(u, v, 24f, 3);
            float variation = Hash(columnIndex, rowIndex);
            height = brick * (0.7f + grain * 0.3f) - 0.2f * (1f - brick);
            Color stone = Color.Lerp(new Color(0.36f, 0.33f, 0.3f), new Color(0.5f, 0.46f, 0.4f), variation) * (0.75f + grain * 0.45f);
            Color mortarColor = new Color(0.2f, 0.18f, 0.16f) * (0.8f + grain * 0.3f);

            return Color.Lerp(mortarColor, stone, brick);
        }

        private static Color StoneFloor(float u, float v, out float height)
        {
            const int cells = 4;
            float cx = u * cells;
            float cy = v * cells;
            int ix = Mathf.FloorToInt(cx);
            int iy = Mathf.FloorToInt(cy);
            float jitterX = (Hash(ix, iy) - 0.5f) * 0.25f;
            float jitterY = (Hash(iy, ix + 7) - 0.5f) * 0.25f;
            float fx = cx - ix - jitterX;
            float fy = cy - iy - jitterY;
            float edge = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));
            float slab = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((edge - 0.03f) / 0.06f));
            float grain = TileNoise(u, v, 18f, 4);
            float dirt = TileNoise(u + 0.3f, v + 0.6f, 5f, 2);
            height = slab * (0.6f + grain * 0.4f);
            Color stone = Color.Lerp(new Color(0.42f, 0.4f, 0.37f), new Color(0.3f, 0.29f, 0.27f), dirt) * (0.75f + grain * 0.4f);
            Color gap = new Color(0.14f, 0.12f, 0.1f);

            return Color.Lerp(gap, stone, slab) * Mathf.Lerp(1f, 0.6f, Hash(ix, iy) * 0.5f);
        }

        private static Color Cobble(float u, float v, out float height)
        {
            float best = 1f;
            float id = 0f;
            const int cells = 10;

            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = Mathf.FloorToInt(u * cells) + ox;
                    int cy = Mathf.FloorToInt(v * cells) + oy;
                    int wx = (cx + cells) % cells;
                    int wy = (cy + cells) % cells;
                    Vector2 center = new Vector2(cx + Hash(wx, wy), cy + Hash(wy, wx + 3));
                    float distance = Vector2.Distance(new Vector2(u * cells, v * cells), center);

                    if (distance < best)
                    {
                        best = distance;
                        id = Hash(wx + 11, wy + 5);
                    }
                }
            }

            float stone = Mathf.Clamp01(1f - best * 1.6f);
            float grain = TileNoise(u, v, 30f, 3);
            height = stone * stone * (0.7f + grain * 0.3f);
            Color color = Color.Lerp(new Color(0.33f, 0.31f, 0.29f), new Color(0.48f, 0.44f, 0.38f), id) * (0.6f + stone * 0.5f) * (0.8f + grain * 0.3f);

            return Color.Lerp(new Color(0.1f, 0.09f, 0.08f), color, Mathf.Clamp01(stone * 2f));
        }

        private static Color WoodPlanks(float u, float v, out float height)
        {
            const int planks = 6;
            float p = u * planks;
            int index = Mathf.FloorToInt(p);
            float fx = p - index;
            float gap = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(fx, 1f - fx) / 0.04f));
            float grain = Mathf.PerlinNoise(u * 8f + index * 3.1f, v * 40f + index * 7f);
            float rings = Mathf.Abs(Mathf.Sin((v * 12f + grain * 2f + index) * Mathf.PI));
            height = gap * (0.5f + rings * 0.3f + grain * 0.2f);
            Color wood = Color.Lerp(new Color(0.42f, 0.28f, 0.15f), new Color(0.3f, 0.19f, 0.09f), rings) * (0.8f + grain * 0.3f) * (0.75f + Hash(index, 2) * 0.35f);

            return Color.Lerp(new Color(0.08f, 0.05f, 0.03f), wood, gap);
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

        private static Color ZombieSkin(float u, float v, out float height)
        {
            float mottle = TileNoise(u, v, 7f, 4);
            float veins = Step(0.01f, 0.02f, Mathf.Abs(TileNoise(u, v + 0.4f, 3f, 2) - 0.5f));
            height = 0.5f + mottle * 0.3f;
            Color skin = Color.Lerp(new Color(0.42f, 0.5f, 0.36f), new Color(0.3f, 0.32f, 0.22f), mottle);

            return Color.Lerp(new Color(0.25f, 0.1f, 0.12f), skin, veins);
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
