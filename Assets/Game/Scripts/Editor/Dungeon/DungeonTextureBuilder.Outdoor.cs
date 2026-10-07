using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Outdoor surfaces of the cursed village: ground layers for the terrain (grass, dirt road, swamp mud, forest litter, cliff rock),
    /// bark, and alpha cards for grass, reeds, dead crops, spruce sprays and leaf clusters. Ground tiles are 4 m like the dungeon's.
    internal static partial class DungeonTextureBuilder
    {
        private const int CardSamples = 3;

        private delegate float Coverage(float u, float v, out Color color);

        private static void BuildOutdoor()
        {
            Write("Grass", Grass, 0.7f, EnvironmentSize);
            Write("Dirt", Dirt, 0.9f, EnvironmentSize);
            Write("Mud", Mud, 0.6f, EnvironmentSize);
            Write("ForestFloor", ForestFloor, 0.8f, EnvironmentSize);
            Write("Rock", Rock, 1.4f, EnvironmentSize);
            Write("Bark", Bark, 1.6f);
            WriteCard("GrassBlades", 256, 256, Blades(0.55f, 1f, 46, 0.016f, new Color(0.06f, 0.07f, 0.035f), new Color(0.36f, 0.34f, 0.19f), 11));
            WriteCard("Reeds", 256, 512, Blades(0.75f, 1f, 22, 0.012f, new Color(0.08f, 0.08f, 0.04f), new Color(0.38f, 0.32f, 0.2f), 23, new Color(0.22f, 0.14f, 0.08f)));
            WriteCard("DeadCrops", 256, 512, Blades(0.6f, 0.95f, 26, 0.01f, new Color(0.18f, 0.15f, 0.09f), new Color(0.5f, 0.43f, 0.27f), 37, new Color(0.46f, 0.38f, 0.22f)));
            WriteCard("SpruceSpray", 256, 512, SpruceSpray);
            WriteCard("LeafCluster", 512, 512, LeafCluster);
        }

        /// Dark wet meadow: olive and dry straw patches over soil, streaked with blades.
        private static Color Grass(float u, float v, out float height)
        {
            float patches = Grain(u, v, 3f, 2.2f);
            float soil = Step(0.6f, 0.75f, Grain(u + 0.37f, v + 0.11f, 5f, 2.4f));
            float blades = Mathf.Clamp01((TileNoise(u, v, 260f, 70f, 2) - 0.5f) * 3.2f + 0.5f);
            float fine = Grain(u, v, 120f, 2f);
            height = 0.35f + blades * 0.45f + fine * 0.2f - soil * 0.3f;

            Color green = Color.Lerp(new Color(0.12f, 0.15f, 0.07f), new Color(0.27f, 0.25f, 0.14f), patches);
            green *= 0.62f + blades * 0.55f;
            Color earth = new Color(0.16f, 0.12f, 0.085f) * (0.7f + fine * 0.5f);

            return Color.Lerp(green, earth, soil * (1f - blades * 0.6f));
        }

        /// Trodden road: packed brown earth, damp ruts and scattered pebbles.
        private static Color Dirt(float u, float v, out float height)
        {
            float tone = Grain(u, v, 4f, 2f);
            float damp = Step(0.55f, 0.7f, Grain(u + 0.5f, v, 3f, 2.5f));
            float fine = Grain(u, v, 90f, 2.2f);
            float pebble = Pebbles(u, v, 44, 0.12f, out float id) * 0.8f;
            height = 0.4f + fine * 0.2f - damp * 0.15f + pebble * 0.5f;

            Color earth = Color.Lerp(new Color(0.25f, 0.19f, 0.13f), new Color(0.33f, 0.27f, 0.19f), tone) * (0.75f + fine * 0.4f);
            earth = Color.Lerp(earth, earth * 0.6f, damp);
            Color stone = Color.Lerp(new Color(0.26f, 0.25f, 0.23f), new Color(0.33f, 0.3f, 0.25f), id) * (0.6f + pebble * 0.4f);

            return Color.Lerp(earth, stone, Step(0.02f, 0.12f, pebble));
        }

        /// Black swamp mud with darker puddles and rotting straw.
        private static Color Mud(float u, float v, out float height)
        {
            float tone = Grain(u, v, 5f, 2f);
            float puddle = Step(0.58f, 0.66f, Grain(u + 0.2f, v + 0.7f, 4f, 2.6f));
            float fine = Grain(u, v, 70f, 2f);
            float straw = Mathf.Clamp01((TileNoise(u, v, 30f, 200f, 2) - 0.5f) * 4f - 0.8f);
            height = 0.5f + fine * 0.15f + tone * 0.2f - puddle * 0.3f + straw * 0.2f;

            Color mud = Color.Lerp(new Color(0.1f, 0.085f, 0.065f), new Color(0.17f, 0.14f, 0.1f), tone) * (0.8f + fine * 0.35f);
            mud = Color.Lerp(mud, new Color(0.05f, 0.05f, 0.04f), puddle);

            return Color.Lerp(mud, new Color(0.3f, 0.26f, 0.15f), straw * 0.7f);
        }

        /// Forest litter: layers of dead leaves of different colours over dark earth and needles.
        private static Color ForestFloor(float u, float v, out float height)
        {
            const int cells = 22;
            Color earth = new Color(0.1f, 0.075f, 0.05f) * (0.7f + Grain(u, v, 60f, 2f) * 0.6f);
            Color result = earth;
            float top = -1f;
            float px = u * cells;
            float py = v * cells;

            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = Mathf.FloorToInt(px) + ox;
                    int cy = Mathf.FloorToInt(py) + oy;

                    for (int k = 0; k < 3; k++)
                    {
                        int hx = Wrap(cx, cells) + k * 101;
                        int hy = Wrap(cy, cells) + k * 53;
                        Vector2 center = new Vector2(cx + Hash(hx, hy), cy + Hash(hy, hx + 7));
                        float angle = Hash(hx + 3, hy + 9) * Mathf.PI;
                        float size = 0.35f + Hash(hx + 5, hy) * 0.3f;
                        Vector2 d = new Vector2(px, py) - center;
                        float along = (d.x * Mathf.Cos(angle) + d.y * Mathf.Sin(angle)) / size;
                        float across = (-d.x * Mathf.Sin(angle) + d.y * Mathf.Cos(angle)) / (size * 0.45f);
                        float leaf = 1f - (along * along + across * across);
                        float depth = Hash(hx + 13, hy + 17);

                        if (leaf <= 0f || depth < top)
                            continue;

                        top = depth;
                        float pick = Hash(hx + 21, hy + 2);
                        Color tint = pick < 0.35f ? new Color(0.24f, 0.14f, 0.08f) : pick < 0.65f ? new Color(0.2f, 0.14f, 0.09f) : pick < 0.85f ? new Color(0.2f, 0.18f, 0.09f) : new Color(0.24f, 0.21f, 0.17f);
                        float vein = Step(0.04f, 0.0f, Mathf.Abs(across) * 0.1f);
                        result = Color.Lerp(earth * 0.6f, tint * (0.75f + Mathf.Sqrt(leaf) * 0.35f) * (1f - vein * 0.3f), Step(0f, 0.15f, leaf));
                    }
                }
            }

            height = top < 0f ? 0.2f : 0.35f + top * 0.5f;

            return result;
        }

        /// Cliff stone in tilted strata with cracks and grey-green lichen.
        private static Color Rock(float u, float v, out float height)
        {
            float warp = TileNoise(u, v, 3f, 3) - 0.5f;
            float layer = Mathf.Repeat(v * 7f + warp * 2.2f, 1f);
            int band = Mathf.FloorToInt(v * 7f + warp * 2.2f);
            float ledge = Step(0f, 0.12f, layer) * (1f - Step(0.85f, 1f, layer) * 0.5f);
            float rough = Grain(u, v, 40f, 2.4f);
            float crack = 1f - Step(0f, 0.012f, Mathf.Abs(TileNoise(u, v, 9f, 4) - 0.5f));
            float lichen = Step(0.6f, 0.72f, Grain(u + 0.3f, v + 0.6f, 6f, 2.4f));
            height = ledge * 0.6f + rough * 0.3f - crack * 0.4f;

            Color stone = Color.Lerp(new Color(0.27f, 0.26f, 0.24f), new Color(0.37f, 0.34f, 0.3f), Hash(Wrap(band, 7), 3));
            stone *= (0.62f + rough * 0.5f) * Mathf.Lerp(0.55f, 1f, ledge);
            stone = Color.Lerp(stone, new Color(0.3f, 0.32f, 0.24f) * (0.7f + rough * 0.4f), lichen * 0.6f);

            return Color.Lerp(stone, new Color(0.08f, 0.075f, 0.07f), crack * 0.5f);
        }

        /// Deep vertical furrows of old bark.
        private static Color Bark(float u, float v, out float height)
        {
            float furrow = Mathf.Abs(TileNoise(u, v, 9f, 1.5f, 4) - 0.5f) * 2f;
            float plates = Step(0.08f, 0.3f, furrow);
            float fine = Grain(u, v, 60f, 2f);
            height = plates * 0.7f + fine * 0.2f;

            Color bark = Color.Lerp(new Color(0.24f, 0.21f, 0.17f), new Color(0.38f, 0.34f, 0.28f), fine);

            return Color.Lerp(new Color(0.08f, 0.065f, 0.05f), bark, plates);
        }

        /// Small rounded stones scattered on a grid; returns their dome height (0 outside).
        private static float Pebbles(float u, float v, int cells, float chance, out float id)
        {
            float px = u * cells;
            float py = v * cells;
            float best = 0f;
            id = 0f;

            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = Mathf.FloorToInt(px) + ox;
                    int cy = Mathf.FloorToInt(py) + oy;
                    int wx = Wrap(cx, cells);
                    int wy = Wrap(cy, cells);

                    if (Hash(wx + 40, wy + 40) > chance)
                        continue;

                    Vector2 center = new Vector2(cx + 0.25f + Hash(wx, wy) * 0.5f, cy + 0.25f + Hash(wy, wx) * 0.5f);
                    float radius = 0.12f + Hash(wx + 9, wy) * 0.2f;
                    float dome = 1f - (new Vector2(px, py) - center).sqrMagnitude / (radius * radius);

                    if (dome > best)
                    {
                        best = dome;
                        id = Hash(wx + 2, wy + 5);
                    }
                }
            }

            return Mathf.Sqrt(Mathf.Max(0f, best));
        }

        /// Tufts of blades rising from the bottom edge, leaning a little; optional seed heads for reeds and crops.
        private static Coverage Blades(float minHeight, float maxHeight, int count, float width, Color root, Color tip, int seed, Color head = default)
        {
            System.Random random = new System.Random(seed);
            float[] x0 = new float[count];
            float[] lean = new float[count];
            float[] tall = new float[count];
            float[] shade = new float[count];

            for (int i = 0; i < count; i++)
            {
                x0[i] = 0.08f + (float)random.NextDouble() * 0.84f;
                lean[i] = ((float)random.NextDouble() - 0.5f) * 0.35f;
                tall[i] = Mathf.Lerp(minHeight, maxHeight, (float)random.NextDouble());
                shade[i] = 0.7f + (float)random.NextDouble() * 0.5f;
            }

            return (float u, float v, out Color color) =>
            {
                color = Color.clear;

                for (int i = count - 1; i >= 0; i--)
                {
                    if (v > tall[i])
                        continue;

                    float t = v / tall[i];
                    float centre = x0[i] + lean[i] * t * t;
                    float half = width * (1f - t * 0.9f);
                    bool isHead = head.a > 0f && t > 0.72f && t < 0.95f && i % 3 == 0;

                    if (isHead)
                        half = width * 1.8f;

                    if (Mathf.Abs(u - centre) > half)
                        continue;

                    color = (isHead ? head : Color.Lerp(root, tip, Mathf.Pow(t, 0.7f))) * shade[i];
                    color.a = 1f;

                    return 1f;
                }

                return 0f;
            };
        }

        /// A spruce branch seen from above: a twig up the middle and needles slanting forward on both sides.
        private static float SpruceSpray(float u, float v, out Color color)
        {
            color = Color.clear;
            float twig = 0.5f + Mathf.Sin(v * 5f) * 0.02f;
            float reach = 0.42f * (1f - v * 0.75f);

            if (Mathf.Abs(u - twig) < 0.012f * (1.2f - v))
            {
                color = new Color(0.12f, 0.08f, 0.05f, 1f);

                return 1f;
            }

            float side = Mathf.Abs(u - twig);

            if (side > reach || v < 0.02f || v > 0.98f)
                return 0f;

            // Needles leave the twig at 55 degrees towards the tip: they shade the spray in stripes and fray its edge.
            float along = v * 512f - side * 512f * 0.7f;
            float needle = Mathf.Abs(Mathf.Repeat(along, 5f) - 2.5f) / 2.5f;
            float clump = DungeonTextureBuilder.Noise(u * 3f, v * 3f, 6f, 3);
            float fray = side / reach;

            if (fray > 0.75f + needle * 0.25f || clump < 0.36f)
                return 0f;

            float shade = (0.6f + (1f - needle) * 0.45f) * (0.8f + Hash(Mathf.FloorToInt(along / 5f), u < twig ? 1 : 2) * 0.3f);
            color = Color.Lerp(new Color(0.05f, 0.08f, 0.055f), new Color(0.14f, 0.18f, 0.11f), fray) * shade;
            color.a = 1f;

            return 1f;
        }

        /// A twig with leaves, dark summer green going brown at the edges of the clump.
        private static float LeafCluster(float u, float v, out Color color)
        {
            color = Color.clear;
            Vector2 p = new Vector2(u - 0.5f, v - 0.5f);

            for (int i = 47; i >= 0; i--)
            {
                float angle = Hash(i, 1) * Mathf.PI * 2f;
                float distance = Mathf.Sqrt(Hash(i, 2)) * 0.36f;
                Vector2 center = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                float size = 0.07f + Hash(i, 3) * 0.04f;
                float spin = Hash(i, 4) * Mathf.PI;
                Vector2 d = p - center;
                float along = (d.x * Mathf.Cos(spin) + d.y * Mathf.Sin(spin)) / size;
                float across = (-d.x * Mathf.Sin(spin) + d.y * Mathf.Cos(spin)) / size;
                // Pointed leaf: the width narrows towards both ends faster than an ellipse.
                float width = 0.42f * (1f - along * along) * (1f - Mathf.Abs(along) * 0.3f);

                if (Mathf.Abs(along) > 1f || Mathf.Abs(across) > width)
                    continue;

                float autumn = Mathf.Clamp01(distance / 0.36f - 0.5f) * Hash(i, 5);
                Color leaf = Color.Lerp(new Color(0.08f, 0.11f, 0.05f), new Color(0.2f, 0.19f, 0.08f), Hash(i, 6));
                leaf = Color.Lerp(leaf, new Color(0.26f, 0.15f, 0.06f), autumn);
                float vein = Mathf.Abs(across) < 0.03f ? 0.75f : 1f;
                color = leaf * (0.7f + (1f - Mathf.Abs(across) / Mathf.Max(width, 0.01f)) * 0.4f) * vein;
                color.a = 1f;

                return 1f;
            }

            // Twigs joining the leaves to the middle.
            for (int i = 0; i < 48; i += 4)
            {
                float angle = Hash(i, 1) * Mathf.PI * 2f;
                Vector2 end = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Sqrt(Hash(i, 2)) * 0.36f;
                float t = Mathf.Clamp01(Vector2.Dot(p, end) / end.sqrMagnitude);

                if ((p - end * t).magnitude < 0.006f)
                {
                    color = new Color(0.1f, 0.07f, 0.05f, 1f);

                    return 1f;
                }
            }

            return 0f;
        }

        /// RGBA card with antialiased coverage; colour bleeds into the transparent texels so mipmaps do not darken the edges.
        private static void WriteCard(string name, int width, int height, Coverage coverage)
        {
            Color[] pixels = new Color[width * height];
            Color average = Color.clear;
            int filled = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color sum = Color.clear;
                    float alpha = 0f;

                    for (int sy = 0; sy < CardSamples; sy++)
                    {
                        for (int sx = 0; sx < CardSamples; sx++)
                        {
                            float a = coverage((x + (sx + 0.5f) / CardSamples) / width, (y + (sy + 0.5f) / CardSamples) / height, out Color color);
                            alpha += a;
                            sum += color * a;
                        }
                    }

                    alpha /= CardSamples * CardSamples;
                    Color pixel = alpha > 0f ? sum / (alpha * CardSamples * CardSamples) : Color.clear;
                    pixel.a = alpha;
                    pixels[y * width + x] = pixel;

                    if (alpha > 0.5f)
                    {
                        average += pixel;
                        filled++;
                    }
                }
            }

            average = filled > 0 ? average / filled : Color.gray;

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a <= 0f)
                    pixels[i] = new Color(average.r, average.g, average.b, 0f);
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipMapsPreserveCoverage = true;
            importer.alphaTestReferenceValue = 0.5f;
            importer.SaveAndReimport();
        }
    }
}
