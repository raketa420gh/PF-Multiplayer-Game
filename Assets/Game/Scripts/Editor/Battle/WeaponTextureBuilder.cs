using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Procedural PBR sets for the weapons: albedo, normal and a mask (R metallic, G occlusion, A smoothness) per surface.
    /// Every set tiles in both directions; the length of a part runs along V.
    internal static class WeaponTextureBuilder
    {
        public const string Folder = "Assets/Game/Textures/Weapons";
        public const string Steel = "Steel";
        public const string Iron = "Iron";
        public const string Wood = "Wood";
        public const string Wrap = "Wrap";
        public const string Hide = "Hide";
        public const string Cloth = "Cloth";
        public const string ShieldFace = "ShieldFace";
        public const string Feather = "Feather";

        private const int Size = 512;
        private const int OcclusionRadius = 5;

        private struct Surface
        {
            public Color Albedo;
            public float Height;
            public float Metallic;
            public float Smoothness;
        }

        private delegate Surface Sampler(float u, float v);

        private static float[,] s_scratches;

        [MenuItem("Tools/Game/Battle/Build Weapon Textures")]
        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(Folder);
            s_scratches = DrawScratches(90, 11);

            Write(Steel, SampleSteel, 0.35f);
            Write(Iron, SampleIron, 1.1f);
            Write(Wood, SampleWood, 0.7f);
            Write(Wrap, SampleWrap, 1.6f);
            Write(Hide, SampleHide, 0.8f);
            Write(Cloth, SampleCloth, 1.2f);
            Write(ShieldFace, SampleShieldFace, 1f);
            Write(Feather, SampleFeather, 0.8f);

            s_scratches = null;
            AssetDatabase.Refresh();
            Debug.Log($"[{nameof(WeaponTextureBuilder)}] Weapon textures built in {Folder}");
        }

        public static void EnsureBuilt()
        {
            if (Load(Feather, "_m") == null)
                Build();
        }

        public static Texture2D Load(string name, string suffix = "")
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{name}{suffix}.png");
        }

        private static void Write(string name, Sampler sampler, float normalStrength)
        {
            Color[] albedo = new Color[Size * Size];
            Color[] normal = new Color[Size * Size];
            Color[] mask = new Color[Size * Size];
            float[,] heights = new float[Size, Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Surface surface = sampler((x + 0.5f) / Size, (y + 0.5f) / Size);
                    heights[x, y] = surface.Height;
                    albedo[y * Size + x] = surface.Albedo;
                    mask[y * Size + x] = new Color(surface.Metallic, 1f, 0f, surface.Smoothness);
                }
            }

            float[,] blurred = Blur(heights);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float left = heights[(x + Size - 1) % Size, y];
                    float right = heights[(x + 1) % Size, y];
                    float down = heights[x, (y + Size - 1) % Size];
                    float up = heights[x, (y + 1) % Size];
                    Vector3 n = new Vector3((left - right) * normalStrength * 8f, (down - up) * normalStrength * 8f, 1f).normalized;
                    normal[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
                    mask[y * Size + x].g = Mathf.Clamp01(1f - Mathf.Max(0f, blurred[x, y] - heights[x, y]) * 2.5f);
                }
            }

            Save(albedo, $"{Folder}/{name}.png", TextureImporterType.Default, true);
            Save(normal, $"{Folder}/{name}_n.png", TextureImporterType.NormalMap, false);
            Save(mask, $"{Folder}/{name}_m.png", TextureImporterType.Default, false);
        }

        private static float[,] Blur(float[,] source)
        {
            float[,] rows = new float[Size, Size];
            float[,] result = new float[Size, Size];
            const int taps = OcclusionRadius * 2 + 1;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float sum = 0f;

                    for (int i = -OcclusionRadius; i <= OcclusionRadius; i++)
                        sum += source[(x + i + Size) % Size, y];

                    rows[x, y] = sum / taps;
                }
            }

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float sum = 0f;

                    for (int i = -OcclusionRadius; i <= OcclusionRadius; i++)
                        sum += rows[x, (y + i + Size) % Size];

                    result[x, y] = sum / taps;
                }
            }

            return result;
        }

        private static void Save(Color[] pixels, string path, TextureImporterType type, bool isColor)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = type;
            importer.sRGBTexture = isColor;
            importer.alphaSource = isColor ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 8;
            importer.SaveAndReimport();
        }

        /// Thin random lines that wrap around the tile: 1 on a scratch, 0 elsewhere.
        private static float[,] DrawScratches(int count, int seed)
        {
            float[,] buffer = new float[Size, Size];
            System.Random random = new System.Random(seed);

            for (int i = 0; i < count; i++)
            {
                float x = (float)random.NextDouble() * Size;
                float y = (float)random.NextDouble() * Size;
                // Most wear runs along the weapon, some across it.
                float angle = ((float)random.NextDouble() - 0.5f) * (random.NextDouble() < 0.7 ? 0.5f : 3f) + Mathf.PI * 0.5f;
                float length = Mathf.Lerp(20f, 220f, (float)(random.NextDouble() * random.NextDouble()));
                float strength = Mathf.Lerp(0.35f, 1f, (float)random.NextDouble());
                Vector2 step = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                for (float t = 0f; t < length; t += 0.5f)
                {
                    int px = ((Mathf.RoundToInt(x + step.x * t) % Size) + Size) % Size;
                    int py = ((Mathf.RoundToInt(y + step.y * t) % Size) + Size) % Size;
                    float fade = Mathf.Sin(t / length * Mathf.PI);
                    buffer[px, py] = Mathf.Max(buffer[px, py], strength * fade);
                }
            }

            return buffer;
        }

        private static float Scratch(float u, float v)
        {
            return s_scratches[Mathf.Min((int)(u * Size), Size - 1), Mathf.Min((int)(v * Size), Size - 1)];
        }

        private static float Step(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));

            return t * t * (3f - 2f * t);
        }

        private static float Hash(int x, int y, int seed)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 362437);
            h = (h ^ (h >> 13)) * 1274126177u;

            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }

        /// Value noise that repeats after periodU x periodV cells, summed over octaves.
        private static float Noise(float u, float v, int periodU, int periodV, int octaves = 3, int seed = 0)
        {
            float total = 0f;
            float amplitude = 1f;
            float max = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float x = u * periodU;
                float y = v * periodV;
                int ix = Mathf.FloorToInt(x);
                int iy = Mathf.FloorToInt(y);
                float fx = x - ix;
                float fy = y - iy;
                fx = fx * fx * (3f - 2f * fx);
                fy = fy * fy * (3f - 2f * fy);
                int x0 = ix % periodU;
                int y0 = iy % periodV;
                int x1 = (ix + 1) % periodU;
                int y1 = (iy + 1) % periodV;
                float value = Mathf.Lerp(
                    Mathf.Lerp(Hash(x0, y0, seed + i), Hash(x1, y0, seed + i), fx),
                    Mathf.Lerp(Hash(x0, y1, seed + i), Hash(x1, y1, seed + i), fx), fy);

                total += value * amplitude;
                max += amplitude;
                amplitude *= 0.5f;
                periodU *= 2;
                periodV *= 2;
            }

            return total / max;
        }

        /// Distance to the nearest feature point of a wrapping grid, in cells; id tells the cells apart.
        private static float Cells(float u, float v, int cells, int seed, out float id)
        {
            Vector2 point = new Vector2(u * cells, v * cells);
            float nearest = 9f;
            id = 0f;

            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = Mathf.FloorToInt(point.x) + ox;
                    int cy = Mathf.FloorToInt(point.y) + oy;
                    int wx = (cx % cells + cells) % cells;
                    int wy = (cy % cells + cells) % cells;
                    float distance = Vector2.Distance(point, new Vector2(cx + Hash(wx, wy, seed), cy + Hash(wy, wx, seed + 7)));

                    if (distance >= nearest)
                        continue;

                    nearest = distance;
                    id = Hash(wx, wy, seed + 13);
                }
            }

            return nearest;
        }

        /// Ground blade steel: fine grinding lines along the length, stains, pits and use scratches.
        private static Surface SampleSteel(float u, float v)
        {
            float grind = Noise(u, v, 180, 3, 2, 1);
            float streak = Noise(u, v, 22, 1, 2, 2);
            float stain = Step(0.58f, 0.9f, Noise(u, v, 3, 2, 4, 3));
            float pit = Step(0.83f, 0.92f, Noise(u, v, 110, 110, 2, 4));
            float scratch = Scratch(u, v);
            float tone = 0.6f + grind * 0.1f + streak * 0.08f;
            Color color = new Color(tone * 0.98f, tone, tone * 1.03f);
            color = Color.Lerp(color, new Color(0.4f, 0.36f, 0.32f), stain * 0.22f + pit * 0.5f);
            color += Color.white * (scratch * 0.14f);

            return new Surface
            {
                Albedo = color,
                Height = 0.5f + grind * 0.16f - scratch * 0.3f - pit * 0.3f - stain * 0.04f,
                Metallic = 1f - stain * 0.15f - pit * 0.4f,
                Smoothness = 0.8f - grind * 0.14f - stain * 0.2f - scratch * 0.25f - pit * 0.3f
            };
        }

        /// Forged iron: hammer marks, dark fire scale, bright worn bumps and rust settled in the hollows.
        private static Surface SampleIron(float u, float v)
        {
            float distance = Cells(u, v, 14, 21, out float id);
            float dome = 1f - Mathf.Clamp01(distance * distance * 1.4f);
            float grain = Noise(u, v, 70, 70, 3, 22);
            float patch = Noise(u, v, 4, 4, 4, 23);
            float height = dome * 0.4f + id * 0.12f + grain * 0.3f;
            float rust = Step(0.6f, 0.85f, patch + (1f - dome) * 0.3f - grain * 0.15f);
            float worn = Step(0.55f, 0.9f, height);
            float tone = 0.26f + grain * 0.08f + worn * 0.1f;
            Color color = Color.Lerp(new Color(tone, tone, tone * 1.04f), new Color(0.3f, 0.19f, 0.12f) * (0.7f + grain * 0.6f), rust * 0.6f);

            return new Surface
            {
                Albedo = color,
                Height = height - rust * 0.06f,
                Metallic = 1f - rust * 0.6f,
                Smoothness = Mathf.Lerp(0.42f + worn * 0.2f - grain * 0.1f, 0.15f, rust)
            };
        }

        /// Shaved hardwood: grain lines and fibres along the length, pores, grime from the hands.
        private static Surface SampleWood(float u, float v)
        {
            float warp = Noise(u, v, 3, 2, 3, 31);
            float lines = Mathf.Abs(Mathf.Sin((u * 9f + warp * 4f) * Mathf.PI));
            float fibre = Noise(u, v, 150, 5, 2, 32);
            float pore = Step(0.7f, 0.88f, Noise(u, v, 170, 12, 1, 33));
            float grime = Noise(u, v, 4, 3, 3, 34);
            float dark = Mathf.Clamp01(Mathf.Pow(lines, 1.6f) * 0.55f + fibre * 0.45f);
            Color color = Color.Lerp(new Color(0.66f, 0.49f, 0.3f), new Color(0.38f, 0.25f, 0.13f), dark);
            color *= (1f - pore * 0.35f) * (0.82f + grime * 0.3f);

            return new Surface
            {
                Albedo = color,
                Height = 0.5f + fibre * 0.22f - lines * 0.1f - pore * 0.3f,
                Metallic = 0f,
                Smoothness = 0.2f + (1f - fibre) * 0.22f - pore * 0.15f
            };
        }

        /// Leather strip wound around a grip: overlapping turns with a dark seam, pebbled and polished by the palm.
        private static Surface SampleWrap(float u, float v)
        {
            const int turns = 8;
            float turn = Mathf.Repeat(v * turns + u, 1f);
            float edge = Step(0f, 0.1f, turn);
            float slope = 1f - turn * 0.3f;
            float pebble = Noise(u, v, 64, 64, 3, 41);
            float crease = Step(0.6f, 0.9f, Noise(u, v, 6, 40, 2, 42));
            float polish = Mathf.Sin(turn * Mathf.PI);
            Color color = new Color(0.33f, 0.2f, 0.11f) * (0.7f + pebble * 0.45f) * Mathf.Lerp(0.35f, 1f, edge) * (1f - crease * 0.2f);
            color = Color.Lerp(color, color * 1.35f, polish * 0.3f);

            return new Surface
            {
                Albedo = color,
                Height = edge * slope * 0.6f + pebble * 0.14f - crease * 0.08f,
                Metallic = 0f,
                Smoothness = 0.16f + polish * 0.26f + (1f - pebble) * 0.1f
            };
        }

        /// Plain tanned leather for covers, straps and bindings. Kept grey-brown so the material tint picks the colour.
        private static Surface SampleHide(float u, float v)
        {
            float distance = Cells(u, v, 40, 51, out float id);
            float pebble = Mathf.Clamp01(1f - distance * 1.3f);
            float grain = Noise(u, v, 90, 90, 2, 52);
            float wear = Noise(u, v, 4, 4, 4, 53);
            float crack = Step(0.8f, 0.92f, Noise(u, v, 14, 60, 2, 54));
            float tone = (0.5f + pebble * 0.2f + grain * 0.12f + id * 0.06f) * (0.8f + wear * 0.35f) * (1f - crack * 0.4f);

            return new Surface
            {
                Albedo = new Color(tone, tone * 0.82f, tone * 0.66f),
                Height = pebble * 0.4f + grain * 0.15f - crack * 0.3f,
                Metallic = 0f,
                Smoothness = 0.22f + wear * 0.2f - crack * 0.1f
            };
        }

        /// Coarse plain weave, grey so the material tint decides between linen and tarred rags.
        private static Surface SampleCloth(float u, float v)
        {
            const int threads = 48;
            float warp = Mathf.Sin(u * threads * Mathf.PI * 2f) * 0.5f + 0.5f;
            float weft = Mathf.Sin(v * threads * Mathf.PI * 2f) * 0.5f + 0.5f;
            bool isOver = (Mathf.FloorToInt(u * threads) + Mathf.FloorToInt(v * threads)) % 2 == 0;
            float thread = isOver ? warp : weft;
            float fuzz = Noise(u, v, 120, 120, 2, 61);
            float stain = Noise(u, v, 5, 5, 3, 62);
            float tone = (0.55f + thread * 0.3f + fuzz * 0.12f) * (0.75f + stain * 0.35f);

            return new Surface
            {
                Albedo = new Color(tone, tone, tone),
                Height = thread * 0.6f + fuzz * 0.15f,
                Metallic = 0f,
                Smoothness = 0.08f
            };
        }

        /// Face of a round shield, stretched once over the board: planks under chipped paint, a pale stripe, rim grime.
        private static Surface SampleShieldFace(float u, float v)
        {
            const int planks = 7;
            float across = u * planks;
            int index = Mathf.FloorToInt(across);
            float fx = across - index;
            float gap = Step(0.012f, 0.05f, Mathf.Min(fx, 1f - fx));
            float warp = Noise(u, v, 4, 3, 3, 71 + index);
            float lines = Mathf.Abs(Mathf.Sin((fx * 2.2f + warp * 3f) * Mathf.PI));
            float fibre = Noise(u, v, 160, 6, 2, 72);
            float chip = Noise(u, v, 14, 14, 4, 73);
            float scratch = Scratch(u, v);
            float radius = Vector2.Distance(new Vector2(u, v), Vector2.one * 0.5f) * 2f;
            float bare = Mathf.Clamp01(Step(0.56f, 0.7f, chip + (1f - gap) * 0.25f + Step(0.8f, 1f, radius) * 0.2f) + scratch * 0.9f);

            Color wood = Color.Lerp(new Color(0.55f, 0.4f, 0.24f), new Color(0.3f, 0.2f, 0.11f), lines * 0.5f + fibre * 0.5f) * (0.8f + Hash(index, 3, 74) * 0.3f);
            bool isStripe = Mathf.Abs(u - 0.5f) < 0.11f;
            Color paint = (isStripe ? new Color(0.78f, 0.72f, 0.58f) : new Color(0.17f, 0.27f, 0.46f)) * (0.82f + fibre * 0.22f + chip * 0.12f);
            Color color = Color.Lerp(paint, wood, bare) * Mathf.Lerp(0.3f, 1f, gap) * Mathf.Lerp(1f, 0.72f, Step(0.7f, 1f, radius));

            return new Surface
            {
                Albedo = color,
                Height = gap * (0.5f + fibre * 0.18f - lines * 0.06f + (1f - bare) * 0.07f) - scratch * 0.15f,
                Metallic = 0f,
                Smoothness = Mathf.Lerp(0.34f, 0.18f, bare) * gap
            };
        }

        /// One vane, stretched once over it: the quill along V at u = 0, barbs slanting away from it.
        private static Surface SampleFeather(float u, float v)
        {
            float barb = Mathf.Abs(Mathf.Sin((v * 46f - u * 16f) * Mathf.PI));
            float split = Step(0.86f, 0.95f, Noise(u, v, 3, 40, 2, 81) + u * 0.12f);
            float quill = 1f - Step(0.02f, 0.06f, u);
            float tone = (0.74f + barb * 0.22f) * (1f - split * 0.45f) * (1f - u * 0.15f);

            return new Surface
            {
                Albedo = Color.Lerp(new Color(tone, tone, tone), new Color(0.9f, 0.86f, 0.74f), quill),
                Height = barb * 0.4f + quill * 0.5f - split * 0.3f,
                Metallic = 0f,
                Smoothness = 0.2f + quill * 0.2f
            };
        }
    }
}
