using System;
using System.IO;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Procedural sprites of the tavern screens: worn panels, the doll arch, perk and skill frames, the rank shield.
    internal static class DungeonUiSpriteBuilder
    {
        public const string Folder = "Assets/Game/Textures/UI";

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
