using System;
using System.IO;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Procedural sprites of the UI: worn panels, the doll arch, perk and skill frames, the rank shield, the eye of unsearched loot,
    /// the hexagram of the character sheet.
    internal static class DungeonUiSpriteBuilder
    {
        public const string Folder = "Assets/Game/Textures/UI";
        /// Corner distance of the hexagram web as a share of the half size of its sprite.
        public const float HexagramRadius = 0.84f;

        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(Folder);

            Write("Panel", 256, 512, Panel);
            Write("Arch", 576, 740, Arch);
            Write("Diamond", 128, 128, (p, _) => Frame(Mathf.Abs(p.x) + Mathf.Abs(p.y)));
            Write("Square", 128, 128, (p, _) => Frame(Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y))));
            Write("Shield", 128, 160, Shield);
            Write("Vignette", 256, 256, (p, half) => new Color(0f, 0f, 0f, DungeonTextureBuilder.Step(0.5f, 1.35f, (p / half).magnitude) * 0.92f));
            Write("Glow", 128, 64, (p, half) => new Color(1f, 0.8f, 0.45f, (1f - DungeonTextureBuilder.Step(0f, 1f, (p / half).magnitude)) * 0.55f));
            Write("Eye", 64, 64, Eye);
            Write("Hexagram", 512, 512, Hexagram);
            Write("Pill", 128, 56, Pill);

            Debug.Log($"[{nameof(DungeonUiSpriteBuilder)}] Sprites built in {Folder}");
        }

        public static Sprite Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");
        }

        /// Samplers get the pixel position relative to the texture center and the half size, both in pixels.
        private static void Write(string name, int width, int height, Func<Vector2, Vector2, Color> sampler)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            Vector2 half = new Vector2(width, height) * 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = sampler(new Vector2(x + 0.5f, y + 0.5f) - half, half);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        /// Grey-brown parchment with a torn, darkened border.
        private static Color Panel(Vector2 p, Vector2 half)
        {
            Vector2 uv = (p + half) / 256f;
            float edge = Mathf.Min(half.x - Mathf.Abs(p.x), half.y - Mathf.Abs(p.y));
            float tear = edge - 4f - 22f * DungeonTextureBuilder.Noise(uv.x, uv.y, 9f, 3);
            float tone = (0.3f + 0.2f * DungeonTextureBuilder.Noise(uv.x, uv.y, 3f)) * Mathf.Lerp(0.45f, 1f, DungeonTextureBuilder.Step(0f, 70f, edge));

            return new Color(tone, tone * 0.91f, tone * 0.8f, DungeonTextureBuilder.Step(0f, 7f, tear));
        }

        /// Double outline of a round-topped doorway, open at the bottom.
        private static Color Arch(Vector2 p, Vector2 half)
        {
            const float radius = 284f;
            float centerY = half.y - 4f - radius;
            float distance = p.y >= centerY ? new Vector2(p.x, p.y - centerY).magnitude : Mathf.Abs(p.x);
            float outer = 1f - DungeonTextureBuilder.Step(0.8f, 2.2f, Mathf.Abs(distance - radius));
            float inner = 1f - DungeonTextureBuilder.Step(0.3f, 1.5f, Mathf.Abs(distance - radius + 9f));

            return new Color(1f, 1f, 1f, Mathf.Max(outer, inner * 0.45f));
        }

        /// White border, thin inner line and a dark plate; tinted by the icon. The distance metric gives the shape.
        private static Color Frame(float distance)
        {
            float border = DungeonTextureBuilder.Step(52.5f, 54.5f, distance);
            float line = 1f - DungeonTextureBuilder.Step(0f, 1.5f, Mathf.Abs(distance - 47f));
            float tone = Mathf.Lerp(Mathf.Lerp(0.12f, 0.6f, line), 1f, border);

            return new Color(tone, tone, tone, 1f - DungeonTextureBuilder.Step(60.5f, 62.5f, distance));
        }

        /// Pointy-top hexagon web: the outer ring is the attribute threshold, the middle one the neutral value, spokes run to the corners.
        private static Color Hexagram(Vector2 p, Vector2 half)
        {
            Vector2 q = p / (half.x * HexagramRadius);
            float radius = Mathf.Max(Mathf.Abs(q.x), Mathf.Abs(q.x) * 0.5f + Mathf.Abs(q.y) * 0.8660254f) / 0.8660254f;
            float pixel = 1f / (half.x * HexagramRadius);
            float outer = 1f - DungeonTextureBuilder.Step(1.2f * pixel, 2.8f * pixel, Mathf.Abs(radius - 1f));
            float neutral = (1f - DungeonTextureBuilder.Step(0.6f * pixel, 2f * pixel, Mathf.Abs(radius - 0.5f))) * 0.6f;
            float quarters = (1f - DungeonTextureBuilder.Step(0.4f * pixel, 1.6f * pixel, Mathf.Min(Mathf.Abs(radius - 0.25f), Mathf.Abs(radius - 0.75f)))) * 0.22f;
            float spokes = 0f;

            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI / 3f;
                float distance = Mathf.Abs(q.x * Mathf.Cos(angle) - q.y * Mathf.Sin(angle));
                spokes = Mathf.Max(spokes, (1f - DungeonTextureBuilder.Step(0.4f * pixel, 1.6f * pixel, distance)) * 0.3f);
            }

            float inside = 1f - DungeonTextureBuilder.Step(1f, 1f + 2f * pixel, radius);
            float line = Mathf.Max(outer, Mathf.Max(neutral, Mathf.Max(quarters, spokes)) * inside);
            float glow = (1f - DungeonTextureBuilder.Step(1f, 1.14f, radius)) * (1f - inside) * 0.35f;
            Color plate = new Color(0.07f, 0.06f, 0.05f, Mathf.Lerp(0.82f, 0.6f, radius) * inside);
            Color color = Color.Lerp(plate, new Color(0.86f, 0.76f, 0.55f, 1f), line);
            color.a = Mathf.Max(Mathf.Max(plate.a, line), glow);

            if (inside <= 0f && line <= 0f)
                color = new Color(0.85f, 0.65f, 0.3f, glow);

            return color;
        }

        /// Rounded plate with a white border, tinted by the stat it holds.
        private static Color Pill(Vector2 p, Vector2 half)
        {
            float radius = half.y - 2f;
            Vector2 d = new Vector2(Mathf.Max(Mathf.Abs(p.x) - (half.x - 2f - radius), 0f), p.y);
            float distance = d.magnitude - radius;
            float border = DungeonTextureBuilder.Step(-5f, -3.5f, distance);
            float tone = Mathf.Lerp(0.1f, 1f, border);

            return new Color(tone, tone, tone, (1f - DungeonTextureBuilder.Step(-1f, 0.5f, distance)) * Mathf.Lerp(0.92f, 1f, border));
        }

        /// Almond outline with an iris and a hollow pupil: marks loot nobody has searched yet.
        private static Color Eye(Vector2 p, Vector2 half)
        {
            float x = p.x / (half.x - 3f);
            float lid = (1f - x * x) * half.y * 0.52f;
            float outline = Mathf.Abs(x) < 1f ? 1f - DungeonTextureBuilder.Step(1.2f, 2.6f, Mathf.Abs(lid - Mathf.Abs(p.y))) : 0f;
            float iris = 1f - DungeonTextureBuilder.Step(half.y * 0.3f, half.y * 0.3f + 1.5f, p.magnitude);
            float pupil = 1f - DungeonTextureBuilder.Step(half.y * 0.1f, half.y * 0.1f + 1.5f, p.magnitude);

            return new Color(1f, 1f, 1f, Mathf.Max(outline, iris * (1f - pupil)));
        }

        /// Golden heater shield: flat top, pointed bottom.
        private static Color Shield(Vector2 p, Vector2 half)
        {
            float point = (p.y + half.y - 6f - Mathf.Abs(p.x) * 0.72f) * 0.81f;
            float edge = Mathf.Min(Mathf.Min(half.x - 8f - Mathf.Abs(p.x), half.y - 4f - p.y), point);
            Color fill = Color.Lerp(new Color(0.5f, 0.32f, 0.08f), new Color(0.95f, 0.72f, 0.25f), (p.y + half.y) / (half.y * 2f));
            Color color = Color.Lerp(fill, new Color(0.22f, 0.17f, 0.12f), 1f - DungeonTextureBuilder.Step(4f, 6f, edge));
            color.a = DungeonTextureBuilder.Step(0f, 2f, edge);

            return color;
        }
    }
}
