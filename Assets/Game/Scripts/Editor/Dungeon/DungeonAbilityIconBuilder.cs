using System;
using System.Collections.Generic;
using System.IO;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Procedural icons of skills, spells and perks: a symbol drawn from distance fields, tinted with the ability colour.
    /// Shapes live in a -1..1 square with Y up; later layers of a symbol are drawn over the earlier ones.
    internal static class DungeonAbilityIconBuilder
    {
        public const string Folder = "Assets/Game/Textures/Icons/Abilities";
        private const int Size = 128;
        /// Symbols are drawn slightly smaller than the square so the outline and the glow fit in.
        private const float Fit = 0.86f;

        private readonly struct Layer
        {
            public readonly Func<Vector2, float> Shape;
            /// Brightness of the layer: below 1 darkens the ability colour, above 1 fades it to white.
            public readonly float Tone;

            public Layer(float tone, Func<Vector2, float> shape)
            {
                Shape = shape;
                Tone = tone;
            }
        }

        private static readonly Dictionary<string, Layer[]> s_symbols = new()
        {
            ["Maw"] = new[]
            {
                new Layer(1f, p => Mathf.Max(Circle(p, 0f, 0.05f, 0.8f), 0.2f - p.y)),
                new Layer(0.8f, p => Mathf.Max(Circle(p, 0f, -0.05f, 0.8f), p.y + 0.3f)),
                new Layer(1.75f, p => Mathf.Min(Mathf.Min(Fang(p, -0.42f, 0.2f, -0.3f), Fang(p, -0.14f, 0.2f, -0.3f)), Mathf.Min(Fang(p, 0.14f, 0.2f, -0.3f), Fang(p, 0.42f, 0.2f, -0.3f)))),
                new Layer(1.75f, p => Mathf.Min(Fang(p, -0.28f, -0.3f, 0.28f), Mathf.Min(Fang(p, 0f, -0.3f, 0.28f), Fang(p, 0.28f, -0.3f, 0.28f)))),
                new Layer(0.15f, p => Polygon(Mirror(p), new Vector2(0.14f, 0.5f), new Vector2(0.46f, 0.46f), new Vector2(0.5f, 0.68f)))
            },
            ["Shield"] = new[]
            {
                new Layer(1f, p => Heater(p)),
                new Layer(0.6f, p => Heater(p) + 0.13f),
                new Layer(1.5f, p => Mathf.Max(Heater(p) + 0.13f, Mathf.Min(Box(p, 0f, 0f, 0.075f, 1f), Box(p, 0f, 0.24f, 1f, 0.075f))))
            },
            ["Armor"] = new[]
            {
                new Layer(1f, p => Cuirass(p)),
                new Layer(0.55f, p => Mathf.Max(Cuirass(p), Box(p, 0f, -0.52f, 1f, 0.075f))),
                new Layer(1.5f, p => Segment(p, new Vector2(0f, 0.36f), new Vector2(0f, -0.36f), 0.035f)),
                new Layer(1.6f, p => Circle(Mirror(p), 0.5f, 0.42f, 0.085f))
            },
            ["Heart"] = new[]
            {
                new Layer(1f, p => Mathf.Min(Circle(Mirror(p), 0.3f, 0.25f, 0.38f), Polygon(p, new Vector2(-0.64f, 0.07f), new Vector2(0f, -0.75f), new Vector2(0.64f, 0.07f)))),
                new Layer(1.8f, p => Mathf.Min(Box(p, 0f, 0.08f, 0.07f, 0.26f), Box(p, 0f, 0.08f, 0.26f, 0.07f)))
            },
            ["Axe"] = new[]
            {
                new Layer(0.5f, p => Segment(Rotate(p, 30f), new Vector2(0f, -0.85f), new Vector2(0f, 0.62f), 0.065f)),
                new Layer(1.25f, p => Polygon(Rotate(p, 30f), new Vector2(-0.07f, 0.6f), new Vector2(0.07f, 0.6f), new Vector2(0f, 0.9f))),
                new Layer(1.1f, p => Blade(Rotate(p, 30f))),
                new Layer(1.7f, p => Mathf.Max(Blade(Rotate(p, 30f)), -Circle(Mirror(Rotate(p, 30f)), -0.05f, 0.3f, 0.6f)))
            },
            ["Blood"] = new[]
            {
                new Layer(1f, p => Mathf.Min(Drop(p, 0f, -0.2f, 1f), Mathf.Min(Drop(p, 0.62f, 0.3f, 0.36f), Drop(p, -0.6f, 0.42f, 0.28f)))),
                new Layer(1.7f, p => Circle(p, -0.17f, -0.3f, 0.1f))
            },
            ["Cross"] = new[]
            {
                new Layer(1f, p => Plus(p, 0.66f, 0.22f)),
                new Layer(1.5f, p => Plus(p, 0.66f, 0.22f) + 0.1f)
            },
            ["Chevrons"] = new[]
            {
                new Layer(0.75f, p => Chevron(p, -0.3f)),
                new Layer(1.15f, p => Chevron(p, 0.25f))
            },
            ["Dash"] = new[]
            {
                new Layer(0.6f, p => Chevron(Rotate(p, 90f) * 1.25f, -0.55f) / 1.25f),
                new Layer(0.85f, p => Chevron(Rotate(p, 90f) * 1.25f, 0f) / 1.25f),
                new Layer(1.2f, p => Chevron(Rotate(p, 90f) * 1.25f, 0.55f) / 1.25f)
            },
            ["Bolt"] = new[]
            {
                new Layer(0.65f, p => Segment(Rotate(p, 45f), new Vector2(0f, -0.75f), new Vector2(0f, 0.4f), 0.06f)),
                new Layer(1f, p => Mathf.Min(Segment(Mirror(Rotate(p, 45f)), new Vector2(0f, -0.62f), new Vector2(0.2f, -0.85f), 0.05f),
                    Segment(Mirror(Rotate(p, 45f)), new Vector2(0f, -0.44f), new Vector2(0.2f, -0.67f), 0.05f))),
                new Layer(1.35f, p => Polygon(Rotate(p, 45f), new Vector2(-0.26f, 0.32f), new Vector2(0.26f, 0.32f), new Vector2(0f, 0.9f)))
            },
            ["Burst"] = new[]
            {
                new Layer(1f, p =>
                {
                    float distance = Circle(p, 0f, 0f, 0.4f);

                    for (int i = 0; i < 8; i++)
                        distance = Mathf.Min(distance, Polygon(Rotate(p, i * 45f), new Vector2(-0.15f, 0.3f), new Vector2(0.15f, 0.3f), new Vector2(0f, i % 2 == 0 ? 0.84f : 0.62f)));

                    return distance;
                }),
                new Layer(1.75f, p => Circle(p, 0f, 0f, 0.24f))
            },
            ["Eye"] = new[]
            {
                new Layer(1f, p => Mathf.Max(Circle(p, 0f, -0.55f, 1f), Circle(p, 0f, 0.55f, 1f))),
                new Layer(0.3f, p => Circle(p, 0f, 0f, 0.32f)),
                new Layer(1.8f, p => Circle(p, 0.08f, 0.08f, 0.11f))
            },
            ["Book"] = new[]
            {
                new Layer(1f, p => Box(p, 0f, 0f, 0.56f, 0.7f, 0.08f)),
                new Layer(0.55f, p => Box(p, -0.44f, 0f, 0.1f, 0.7f, 0.04f)),
                new Layer(1.6f, p => Mathf.Min(Circle(p, 0.1f, 0.12f, 0.2f), Box(p, 0.1f, -0.38f, 0.26f, 0.045f))),
                new Layer(0.55f, p => Circle(p, 0.1f, 0.12f, 0.09f))
            },
            ["Paw"] = new[]
            {
                new Layer(1f, p => Mathf.Min(Circle(p, 0f, -0.36f, 0.34f), Circle(Mirror(p), 0.2f, -0.24f, 0.25f))),
                new Layer(1.2f, p => Mathf.Min(Circle(Mirror(p), 0.55f, 0.1f, 0.17f), Circle(Mirror(p), 0.21f, 0.45f, 0.19f)))
            },
            ["Skull"] = new[]
            {
                new Layer(1.2f, p => Mathf.Min(Circle(p, 0f, 0.16f, 0.6f), Box(p, 0f, -0.44f, 0.33f, 0.26f, 0.08f))),
                new Layer(0.12f, p => Mathf.Min(Circle(Mirror(p), 0.25f, 0.12f, 0.17f), Polygon(p, new Vector2(-0.09f, -0.22f), new Vector2(0.09f, -0.22f), new Vector2(0f, -0.04f)))),
                new Layer(0.12f, p => Mathf.Min(Box(p, 0f, -0.56f, 0.02f, 0.14f), Box(Mirror(p), 0.15f, -0.56f, 0.02f, 0.14f)))
            },
            ["Hourglass"] = new[]
            {
                new Layer(1f, p => Mathf.Min(Polygon(p, new Vector2(-0.5f, 0.7f), new Vector2(0f, 0f), new Vector2(0.5f, 0.7f)),
                    Polygon(p, new Vector2(0.5f, -0.7f), new Vector2(0f, 0f), new Vector2(-0.5f, -0.7f)))),
                new Layer(1.6f, p => Polygon(p, new Vector2(0.3f, -0.64f), new Vector2(0f, -0.28f), new Vector2(-0.3f, -0.64f))),
                new Layer(0.55f, p => Mathf.Min(Box(p, 0f, 0.76f, 0.62f, 0.075f, 0.03f), Box(p, 0f, -0.76f, 0.62f, 0.075f, 0.03f)))
            }
        };

        public static string SymbolFor(AbilityKind kind)
        {
            return kind switch
            {
                AbilityKind.Heal => "Cross",
                AbilityKind.Buff => "Chevrons",
                AbilityKind.Projectile => "Bolt",
                AbilityKind.AreaDamage => "Burst",
                AbilityKind.Dash => "Dash",
                AbilityKind.Invisibility => "Eye",
                AbilityKind.Shield => "Shield",
                AbilityKind.SpellMemory => "Book",
                AbilityKind.Shapeshift => "Paw",
                AbilityKind.Taunt => "Maw",
                AbilityKind.Spawn => "Skull",
                _ => "Hourglass"
            };
        }

        public static string SymbolFor(StatType stat)
        {
            return stat switch
            {
                StatType.ArmorRating or StatType.MagicResistance => "Armor",
                StatType.MaxHealth or StatType.Flesh => "Heart",
                StatType.PhysicalPower or StatType.PhysicalDamageBonus or StatType.Grip => "Axe",
                StatType.ActionSpeed or StatType.MoveSpeed or StatType.Reflex => "Dash",
                StatType.MagicalPower or StatType.MagicalDamageBonus or StatType.Resonance => "Burst",
                StatType.Insight => "Eye",
                StatType.Craft => "Bolt",
                _ => "Chevrons"
            };
        }

        /// Renders the symbol into `Folder/name.png` and returns it as a sprite.
        public static Sprite Build(string name, string symbol, Color color)
        {
            if (!s_symbols.TryGetValue(symbol, out Layer[] layers))
                throw new ArgumentException($"Unknown ability icon symbol '{symbol}' for {name}");

            BattleEditorUtility.EnsureFolder(Folder);
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = Shade(new Vector2(x + 0.5f, y + 0.5f) / (Size * 0.5f) - Vector2.one, layers, color);
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

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// Top-lit body with a dark rim around every layer and a soft glow of the ability colour outside the silhouette.
        private static Color Shade(Vector2 pixel, Layer[] layers, Color color)
        {
            const float pixelSize = 2f / Size / Fit;
            Vector2 p = pixel / Fit;
            float silhouette = float.MaxValue;
            float depth = 0f;
            float tone = 1f;

            foreach (Layer layer in layers)
            {
                float distance = layer.Shape(p);
                silhouette = Mathf.Min(silhouette, distance);

                if (distance < 0f)
                {
                    depth = -distance;
                    tone = layer.Tone;
                }
            }

            Color lit = tone <= 1f ? color * tone : Color.Lerp(color, Color.white, (tone - 1f) * 0.85f);
            lit *= Mathf.Lerp(0.72f, 1.18f, (p.y + 1f) * 0.5f);
            Color rim = color * 0.14f;
            Color body = Color.Lerp(rim, lit, DungeonTextureBuilder.Step(0.035f, 0.035f + pixelSize * 1.5f, depth));
            body.a = 1f;
            Color glow = color * 0.5f;
            glow.a = (1f - DungeonTextureBuilder.Step(0f, 0.16f, silhouette)) * 0.55f;

            return Color.Lerp(body, glow, DungeonTextureBuilder.Step(-pixelSize, pixelSize, silhouette));
        }

        private static float Circle(Vector2 p, float x, float y, float radius)
        {
            return (p - new Vector2(x, y)).magnitude - radius;
        }

        private static float Box(Vector2 p, float x, float y, float halfWidth, float halfHeight, float round = 0f)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x - x) - halfWidth + round, Mathf.Abs(p.y - y) - halfHeight + round);

            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - round;
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b, float radius)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);

            return (p - a - ab * t).magnitude - radius;
        }

        /// Convex polygon with counter-clockwise corners.
        private static float Polygon(Vector2 p, params Vector2[] corners)
        {
            float distance = float.MinValue;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 edge = corners[(i + 1) % corners.Length] - corners[i];
                distance = Mathf.Max(distance, Vector2.Dot(p - corners[i], new Vector2(edge.y, -edge.x).normalized));
            }

            return distance;
        }

        private static Vector2 Mirror(Vector2 p)
        {
            return new Vector2(Mathf.Abs(p.x), p.y);
        }

        /// Turns the shape clockwise by the given angle.
        private static Vector2 Rotate(Vector2 p, float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);

            return new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
        }

        /// Triangle tooth: the base lies on the jaw line, the tip reaches towards the other jaw.
        private static float Fang(Vector2 p, float x, float baseY, float length)
        {
            Vector2 left = new Vector2(x - 0.13f, baseY);
            Vector2 right = new Vector2(x + 0.13f, baseY);
            Vector2 tip = new Vector2(x, baseY + length);

            return length < 0f ? Polygon(p, left, tip, right) : Polygon(p, right, tip, left);
        }

        /// Heater shield: flat top, straight sides, pointed bottom.
        private static float Heater(Vector2 p)
        {
            float x = Mathf.Abs(p.x);

            return Mathf.Max(Mathf.Max(x - 0.62f, p.y - 0.72f), (x * 1.1f - p.y - 0.85f) * 0.67f);
        }

        /// Breastplate: torso with round shoulders and a neck cut.
        private static float Cuirass(Vector2 p)
        {
            float body = Mathf.Min(Box(p, 0f, -0.12f, 0.5f, 0.6f, 0.14f), Circle(Mirror(p), 0.5f, 0.42f, 0.3f));

            return Mathf.Max(body, -Circle(p, 0f, 0.86f, 0.32f));
        }

        /// Double-bitted axe head around a vertical handle: two fans widening outwards under a round cutting edge.
        private static float Blade(Vector2 p)
        {
            Vector2 q = Mirror(p);

            return Mathf.Max(Mathf.Max(Circle(q, -0.05f, 0.3f, 0.72f), 0.06f - q.x), (Mathf.Abs(q.y - 0.3f) - 0.1f - q.x * 0.75f) * 0.8f);
        }

        private static float Drop(Vector2 p, float x, float y, float scale)
        {
            Vector2 q = (p - new Vector2(x, y)) / scale;

            return Mathf.Min(Circle(q, 0f, 0f, 0.45f), Polygon(q, new Vector2(-0.41f, 0.19f), new Vector2(0.41f, 0.19f), new Vector2(0f, 1f))) * scale;
        }

        private static float Plus(Vector2 p, float length, float width)
        {
            return Mathf.Min(Box(p, 0f, 0f, width, length, 0.05f), Box(p, 0f, 0f, length, width, 0.05f));
        }

        /// Arrowhead pointing up, its tip at the given height.
        private static float Chevron(Vector2 p, float tip)
        {
            return Segment(Mirror(p), new Vector2(0.5f, tip - 0.45f), new Vector2(0f, tip), 0.1f);
        }
    }
}
