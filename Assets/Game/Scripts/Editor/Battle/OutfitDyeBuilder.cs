using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// New outfit looks out of the pack's own ones: the base colour atlas of the peasant or the ranger with some of its cloth dyed.
    internal static class OutfitDyeBuilder
    {
        public const string Prefix = "Outfit";

        private const string Folder = "Assets/Game/Textures/Battle";
        private const int Size = 2048;
        private const float Feather = 0.04f;
        /// Smoothness that dyed metal is polished to, at full metal.
        private const float Polish = 0.62f;

        /// Pixels inside the hue, saturation and value ranges take the new hue; their saturation and value are multiplied.
        /// Metal above 0 turns the dyed cloth into polished steel in the look's own mask.
        private readonly struct Dye
        {
            public readonly Vector2 FromHue;
            public readonly Vector2 FromSaturation;
            public readonly Vector2 FromValue;
            public readonly float Hue;
            public readonly float Saturation;
            public readonly float Value;
            public readonly float Metal;

            public Dye(Vector2 fromHue, Vector2 fromSaturation, Vector2 fromValue, float hue, float saturation, float value, float metal = 0f)
            {
                FromHue = fromHue;
                FromSaturation = fromSaturation;
                FromValue = fromValue;
                Hue = hue;
                Saturation = saturation;
                Value = value;
                Metal = metal;
            }
        }

        private static readonly Vector2 s_any = new(0f, 1f);
        // The ranger atlas: green cloth and brown leather are saturated, the metal of buckles and the pauldron is grey.
        private static readonly Vector2 s_green = new(0.19f, 0.45f);
        private static readonly Vector2 s_leather = new(0.03f, 0.16f);
        private static readonly Vector2 s_coloured = new(0.3f, 1f);
        // The peasant atlas is one hue: pale linen, saturated leather, and the trousers and the vest are simply the darkest of it.
        private static readonly Vector2 s_brown = new(0.03f, 0.18f);
        private static readonly Vector2 s_pale = new(0.12f, 0.45f);
        private static readonly Vector2 s_bright = new(0.3f, 1f);
        private static readonly Vector2 s_darkest = new(0f, 0.22f);

        private static readonly (string look, string outfit, string source, Dye[] dyes)[] s_looks =
        {
            ("PeasantMystic", "Peasant", "T_Peasant_BaseColor", new[]
            {
                new Dye(s_brown, s_pale, s_bright, 0.64f, 2.3f, 0.62f),
                new Dye(s_brown, new Vector2(0.45f, 1f), s_darkest, 0.73f, 0.75f, 1.2f)
            }),
            ("RangerMystic", "Ranger", "T_Ranger_BaseColor", new[] { new Dye(s_green, s_coloured, s_any, 0.66f, 0.9f, 1.3f) }),
            ("RangerOccultist", "Ranger", "T_Ranger_BaseColor", new[]
            {
                new Dye(s_green, s_coloured, s_any, 0.79f, 0.7f, 0.65f),
                new Dye(s_leather, s_coloured, s_any, 0.75f, 0.3f, 0.55f)
            }),
            ("RangerMarauder", "Ranger", "T_Ranger_BaseColor", new[]
            {
                new Dye(s_green, s_coloured, s_any, 0.995f, 1f, 0.95f),
                new Dye(s_leather, s_coloured, s_any, 0.06f, 0.55f, 0.5f)
            }),
            ("RangerBerserker", "Ranger", "T_Ranger_BaseColor", new[]
            {
                new Dye(s_green, s_coloured, s_any, 0.58f, 0.22f, 1.7f),
                new Dye(s_leather, s_coloured, s_any, 0.02f, 1f, 0.75f)
            }),
            // Plate of the Warrior: the cloth is steel, the straps under it blackened leather.
            ("RangerIronclad", "Ranger", "T_Ranger_BaseColor", new[]
            {
                new Dye(s_green, s_coloured, s_any, 0.6f, 0.14f, 1.75f, 0.9f),
                new Dye(s_leather, s_coloured, s_any, 0.07f, 0.45f, 0.4f)
            }),
            // Vestments of the Confessor: white linen over ochre, pale hood and wraps on tan leather.
            ("PeasantDevout", "Peasant", "T_Peasant_BaseColor", new[]
            {
                new Dye(s_brown, s_pale, s_bright, 0.12f, 0.3f, 1.3f),
                new Dye(s_brown, new Vector2(0.45f, 1f), s_darkest, 0.115f, 1f, 2.8f)
            }),
            ("RangerDevout", "Ranger", "T_Ranger_BaseColor", new[]
            {
                new Dye(s_green, s_coloured, s_any, 0.13f, 0.16f, 2.1f),
                new Dye(s_leather, s_coloured, s_any, 0.105f, 0.9f, 1.25f)
            })
        };

        [MenuItem("Tools/Game/Battle/Build Outfit Dyes")]
        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(Folder);

            foreach ((string look, string outfit, string source, Dye[] dyes) in s_looks)
            {
                string path = $"{Folder}/T_{look}_BaseColor.png";
                float[] metal = Write(BattleCharacterBuilder.PackTexture(outfit, source), path, dyes);
                Material material = BattleCharacterBuilder.ClothMaterial(Prefix + look, outfit, path);

                if (System.Array.Exists(dyes, dye => dye.Metal > 0f))
                    BattleCharacterBuilder.SetMask(material, WriteMask(material, $"{Folder}/T_{look}_Mask.png", metal));
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(OutfitDyeBuilder)}] Outfit dyes built: {s_looks.Length}");
        }

        /// Returns how much of a metal every pixel of the dyed atlas became.
        private static float[] Write(string sourcePath, string path, Dye[] dyes)
        {
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(sourcePath));
            Color32[] pixels = source.GetPixels32();
            int step = source.width / Size;
            Color32[] result = new Color32[Size * Size];
            float[] metal = new float[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Color sum = Color.clear;

                    for (int i = 0; i < step * step; i++)
                        sum += pixels[(y * step + i / step) * source.width + x * step + i % step];

                    result[y * Size + x] = Apply(sum / (step * step), dyes, out metal[y * Size + x]);
                }
            }

            Object.DestroyImmediate(source);
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixels32(result);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            return metal;
        }

        /// The material's own mask (R metallic, G occlusion, A smoothness) with the dyed metal written into it.
        private static Texture2D WriteMask(Material material, string path, float[] metal)
        {
            Texture2D mask = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            mask.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(material.GetTexture("_MetallicGlossMap"))));
            Color32[] pixels = mask.GetPixels32();

            for (int y = 0; y < mask.height; y++)
            {
                for (int x = 0; x < mask.width; x++)
                {
                    float amount = metal[y * Size / mask.height * Size + x * Size / mask.width];
                    Color32 pixel = pixels[y * mask.width + x];
                    pixel.r = (byte)Mathf.Lerp(pixel.r, 255f, amount);
                    pixel.a = (byte)Mathf.Lerp(pixel.a, 255f * Polish, amount);
                    pixels[y * mask.width + x] = pixel;
                }
            }

            mask.SetPixels32(pixels);
            File.WriteAllBytes(path, mask.EncodeToPNG());
            Object.DestroyImmediate(mask);
            AssetDatabase.ImportAsset(path);
            BattleCharacterBuilder.SetLinear(path);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Color Apply(Color color, Dye[] dyes, out float metal)
        {
            metal = 0f;
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);

            foreach (Dye dye in dyes)
            {
                float weight = Band(hue, dye.FromHue) * Band(saturation, dye.FromSaturation) * Band(value, dye.FromValue);

                if (weight <= 0f)
                    continue;

                Color dyed = Color.HSVToRGB(dye.Hue, Mathf.Clamp01(saturation * dye.Saturation), Mathf.Clamp01(value * dye.Value));
                metal = weight * dye.Metal;

                return Color.Lerp(color, dyed, weight);
            }

            return color;
        }

        /// 1 inside the range, fading out over the feather outside of it.
        private static float Band(float value, Vector2 range)
        {
            return Mathf.InverseLerp(range.x - Feather, range.x, value) * Mathf.InverseLerp(range.y + Feather, range.y, value);
        }
    }
}
