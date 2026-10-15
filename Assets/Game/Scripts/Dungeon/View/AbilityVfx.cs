using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Client-side ability visuals built from particle systems in code: bursts, cast flourishes, zones, beams, projectiles and status looks.
    /// Materials come from AbilityVfxConfig.Instance; nothing here is networked.
    public static class AbilityVfx
    {
        private const float Forever = 100000f;
        private const string EruptionName = "Eruption";
        private const string WarningName = "Warning";
        private const string BaseColor = "_BaseColor";

        private static readonly Vector3 Flat = new(90f, 0f, 0f);
        private static readonly Vector3 Up = new(-90f, 0f, 0f);

        /// Longest life of the one-shot layers spawned since the last Root(): the root is destroyed after it.
        private static float s_longest;
        private static MaterialPropertyBlock s_block;

        /// One particle system: every field has a sensible default, so a layer names only what it changes.
        private sealed class Layer
        {
            public Material Material;
            public Vector3 Position;
            /// Rotation of the emission shape; the system itself stays upright so Velocity stays world-like.
            public Vector3 Rotation;
            public int Burst;
            /// Particles per second; with no Duration the system loops.
            public float Rate;
            public float PerMetre;
            /// Emission time of a non-looping system; 0 with a rate means it loops.
            public float Duration;
            public float Delay;
            public Vector2 Life = new(1f, 1f);
            public Vector2 Speed;
            public Vector2 Size = new(0.2f, 0.2f);
            public Color From = Color.white;
            /// Color.clear fades From to transparent.
            public Color To = Color.clear;
            public float FadeIn = 0.1f;
            public Gradient Colors;
            public AnimationCurve Grow;
            public float Gravity;
            public ParticleSystemShapeType Shape = ParticleSystemShapeType.Sphere;
            public float Radius = 0.01f;
            public float Thickness = 1f;
            public float Angle = 25f;
            public Vector3 Scale = Vector3.one;
            public bool Spread;
            public Vector3 Velocity;
            public float Orbit;
            public float Radial;
            public float Noise;
            public float Frequency = 0.8f;
            public float Drag;
            public ParticleSystemRenderMode Render = ParticleSystemRenderMode.Billboard;
            public float Streak = 0.05f;
            public float Stretch = 1f;
            /// Billboards face along the system's +Z instead of the camera.
            public bool Facing;
            public bool Spin = true;
            public bool Local;
            /// Built stopped: played later (the pyre eruption).
            public bool Hold;
            public int Max = 64;
            /// Range of a particle light per particle; 0 = no light. Intensity fades with the particle's alpha.
            public float Light;
            public float Intensity = 3f;
            public int Lights = 1;
        }

        /// One-shot explosion / nova / flash sized to the radius, destroyed when it is over.
        public static void Burst(VfxStyle style, Vector3 position, float radius, Color color)
        {
            AbilityVfxConfig c = AbilityVfxConfig.Instance;

            if (c == null || style == VfxStyle.None)
                return;

            float r = Mathf.Max(0.3f, radius);
            Transform root = Root($"Burst{style}", position);
            Color tint = Tint(Palette(style), color);
            Color hot = Color.Lerp(tint, Color.white, 0.4f);

            switch (style)
            {
                case VfxStyle.Fire:
                    Flash(root, c, new Color(1f, 0.72f, 0.38f, 1f), 2.2f * r, 2.5f * r + 4f, 9f, 0.4f);
                    Spawn(root, new Layer { Material = c.Flame, Burst = 16, Life = new(0.35f, 0.75f), Speed = new(1.2f * r, 2.6f * r), Size = new(0.6f * r, 1.1f * r), Radius = 0.25f * r, Colors = FireRamp(), Grow = Curve(0.5f, 1.15f), Drag = 4f, Noise = 0.6f, Gravity = -0.15f });
                    Puff(root, c, new Color(0.1f, 0.08f, 0.07f, 0.65f), 8, r, 0.1f, 2.4f);
                    Spawn(root, new Layer { Material = c.Ember, Burst = 24, Life = new(0.8f, 1.6f), Speed = new(0.8f * r, 2.4f * r), Size = new(0.03f, 0.07f), Radius = 0.2f * r, From = new Color(1f, 0.6f, 0.2f, 1f), To = new Color(0.6f, 0.1f, 0.02f, 0f), FadeIn = 0f, Gravity = -0.1f, Noise = 0.8f, Drag = 1f });
                    Sparks(root, c, new Color(1f, 0.65f, 0.3f, 1f), 20, new Vector2(2f * r, 5f * r), 0.6f);
                    Shock(root, new Color(1f, 0.45f, 0.12f, 0.45f), r, c.Ring);
                    break;
                case VfxStyle.Frost:
                    Flash(root, c, new Color(0.65f, 0.85f, 1f, 0.9f), 1.8f * r, 2f * r + 3f, 5f, 0.3f);
                    Spawn(root, new Layer { Material = c.Crystal, Burst = 24, Life = new(0.5f, 1.1f), Speed = new(2f * r, 4.5f * r), Size = new(0.06f, 0.2f), Radius = 0.15f * r, From = new Color(0.88f, 0.96f, 1f, 1f), FadeIn = 0f, Gravity = 1.2f, Drag = 1f });
                    Spawn(root, new Layer { Material = c.Smoke, Burst = 12, Life = new(1.2f, 2.4f), Speed = new(0.3f * r, 0.9f * r), Size = new(0.8f * r, 1.5f * r), Radius = 0.4f * r, From = new Color(0.78f, 0.9f, 1f, 0.45f), Grow = Curve(0.5f, 1.4f), Drag = 2f, Noise = 0.2f, Gravity = -0.02f });
                    Spawn(root, new Layer { Material = c.Spark, Burst = 30, Life = new(0.6f, 1.4f), Speed = new(0.2f * r, 1f * r), Size = new(0.015f, 0.035f), Radius = 0.6f * r, From = new Color(0.9f, 0.97f, 1f, 1f), Noise = 0.5f, Drag = 1f });
                    Shock(root, new Color(0.7f, 0.9f, 1f, 0.7f), r, c.Ring);
                    break;
                case VfxStyle.Holy:
                case VfxStyle.Light:
                    bool isHoly = style == VfxStyle.Holy;
                    Flash(root, c, Alpha(hot, 1f), (isHoly ? 2.8f : 3f) * r, (isHoly ? 3f : 4f) * r + 5f, isHoly ? 8f : 10f, isHoly ? 0.5f : 0.7f);
                    Spawn(root, new Layer { Material = c.Spark, Burst = 14, Life = new(0.25f, 0.4f), Speed = new(5f * r, 8f * r), Size = new(0.12f, 0.22f), From = Alpha(hot, 0.9f), FadeIn = 0f, Drag = 6f, Render = ParticleSystemRenderMode.Stretch, Streak = 0.12f, Stretch = 2f, Spin = false });
                    Spawn(root, new Layer { Material = c.Glow, Burst = 30, Life = new(1f, 2f), Speed = new(0.2f * r, 0.8f * r), Size = new(0.04f, 0.09f), Radius = 0.7f * r, From = Alpha(hot, 1f), Velocity = new Vector3(0f, 0.8f, 0f), Noise = 0.4f, Drag = 1f });
                    Shock(root, Alpha(tint, 0.8f), r, c.Ring);

                    if (isHoly)
                        Spawn(root, new Layer { Material = c.Rune, Burst = 1, Life = new(0.9f, 0.9f), Size = new(2.2f * r, 2.2f * r), From = Alpha(tint, 0.6f), Grow = Curve(0.9f, 1.05f), Render = ParticleSystemRenderMode.HorizontalBillboard, Spin = false, Max = 1 });
                    else
                        Spawn(root, new Layer { Material = c.Glow, Burst = 1, Life = new(1.2f, 1.2f), Size = new(1.5f * r, 1.5f * r), From = Alpha(tint, 0.5f), FadeIn = 0.05f, Spin = false, Max = 1 });
                    break;
                case VfxStyle.Dark:
                    Spawn(root, new Layer { Material = c.Smoke, Burst = 18, Life = new(0.45f, 0.7f), Radius = 1.2f * r, Thickness = 0f, Radial = -2.2f * r, Size = new(0.5f * r, 0.9f * r), From = new Color(0.06f, 0.02f, 0.09f, 0.75f), To = new Color(0.12f, 0.04f, 0.18f, 0f), FadeIn = 0.2f, Grow = Curve(1f, 0.3f) });
                    Spawn(root, new Layer { Material = c.Glow, Burst = 1, Life = new(0.6f, 0.6f), Size = new(1.6f * r, 1.6f * r), From = new Color(0.5f, 0.2f, 0.8f, 0.8f), Grow = Curve(1.2f, 0.1f), Spin = false, Max = 1, Light = 2f * r + 3f, Intensity = 4f });
                    Spawn(root, new Layer { Material = c.Spark, Burst = 24, Life = new(0.35f, 0.6f), Radius = 1.4f * r, Thickness = 0f, Radial = -3f * r, Size = new(0.03f, 0.06f), From = new Color(0.6f, 0.3f, 0.9f, 0.9f), Render = ParticleSystemRenderMode.Stretch, Streak = 0.1f, Spin = false });
                    Spawn(root, new Layer { Material = c.Smoke, Burst = 10, Delay = 0.45f, Life = new(1f, 1.8f), Speed = new(1f * r, 2f * r), Size = new(0.5f * r, 1f * r), From = new Color(0.05f, 0.02f, 0.07f, 0.6f), Drag = 3f, Grow = Curve(0.6f, 1.4f) });
                    Spawn(root, new Layer { Material = c.Glow, Burst = 1, Delay = 0.45f, Life = new(0.3f, 0.3f), Size = new(1.2f * r, 1.2f * r), From = new Color(0.45f, 0.15f, 0.7f, 0.6f), FadeIn = 0f, Spin = false, Max = 1 });
                    break;
                case VfxStyle.Poison:
                    Flash(root, c, new Color(0.5f, 0.8f, 0.2f, 0.5f), r, 2f * r + 2f, 2.5f);
                    Spawn(root, new Layer { Material = c.Drop, Burst = 28, Life = new(0.5f, 1f), Speed = new(2f * r, 4f * r), Size = new(0.04f, 0.1f), Shape = ParticleSystemShapeType.Cone, Rotation = Up, Angle = 55f, Radius = 0.2f * r, From = new Color(0.45f, 0.7f, 0.12f, 0.95f), To = new Color(0.3f, 0.5f, 0.08f, 0f), FadeIn = 0f, Gravity = 1.4f });
                    Spawn(root, new Layer { Material = c.Smoke, Burst = 12, Life = new(1.5f, 3f), Speed = new(0.3f * r, 0.9f * r), Size = new(1f * r, 1.8f * r), Radius = 0.5f * r, From = new Color(0.35f, 0.58f, 0.14f, 0.45f), To = new Color(0.3f, 0.45f, 0.15f, 0f), Grow = Curve(0.5f, 1.4f), Drag = 1.8f, Noise = 0.3f, Gravity = -0.02f });
                    break;
                case VfxStyle.Heal:
                    Flash(root, c, new Color(0.7f, 1f, 0.55f, 0.7f), 1.6f * r, 2f * r + 3f, 4f, 0.5f);
                    Spawn(root, new Layer { Material = c.Glow, Burst = 36, Life = new(0.8f, 1.6f), Speed = new(0.3f * r, 1.2f * r), Radius = 0.6f * r, Size = new(0.04f, 0.1f), From = new Color(0.8f, 1f, 0.6f, 1f), To = new Color(1f, 0.85f, 0.4f, 0f), Velocity = new Vector3(0f, 1f, 0f), Noise = 0.4f, Drag = 1.5f });
                    Shock(root, new Color(0.6f, 1f, 0.5f, 0.6f), r, c.Ring);
                    break;
                case VfxStyle.Blood:
                    Spawn(root, new Layer { Material = c.Drop, Burst = 30, Life = new(0.4f, 0.9f), Speed = new(1.5f * r, 4f * r), Size = new(0.03f, 0.08f), Radius = 0.15f * r, From = new Color(0.4f, 0.02f, 0.02f, 1f), To = new Color(0.25f, 0.01f, 0.01f, 0f), FadeIn = 0f, Gravity = 1.6f, Render = ParticleSystemRenderMode.Stretch, Streak = 0.04f, Spin = false });
                    Spawn(root, new Layer { Material = c.Smoke, Burst = 6, Life = new(0.6f, 1.2f), Speed = new(0.5f * r, 1.2f * r), Size = new(0.4f * r, 0.8f * r), From = new Color(0.35f, 0.02f, 0.02f, 0.45f), Grow = Curve(0.5f, 1.3f), Drag = 3f });
                    break;
                case VfxStyle.Alchemy:
                    Flash(root, c, Alpha(hot, 0.8f), 1.6f * r, 2f * r + 3f, 5f, 0.3f);
                    Spawn(root, new Layer { Material = c.Crystal, Burst = 18, Life = new(0.4f, 0.9f), Speed = new(2f * r, 4f * r), Size = new(0.03f, 0.08f), From = new Color(0.9f, 0.95f, 1f, 0.9f), FadeIn = 0f, Gravity = 1.5f });
                    Spawn(root, new Layer { Material = c.Drop, Burst = 26, Life = new(0.4f, 0.8f), Speed = new(1.5f * r, 3.5f * r), Size = new(0.04f, 0.1f), Shape = ParticleSystemShapeType.Cone, Rotation = Up, Angle = 60f, Radius = 0.2f * r, From = Alpha(tint, 0.95f), FadeIn = 0f, Gravity = 1.3f });
                    Puff(root, c, Alpha(Color.Lerp(tint, new Color(0.4f, 0.4f, 0.4f), 0.5f), 0.45f), 10, r, 0.05f, 2.2f);
                    Sparks(root, c, Alpha(hot, 1f), 12, new Vector2(2f * r, 4f * r), 0.8f);
                    break;
                case VfxStyle.Shout:
                    Spawn(root, new Layer { Material = c.Smoke, Burst = 30, Life = new(0.8f, 1.6f), Speed = new(3f * r, 5f * r), Size = new(0.25f * r, 0.45f * r), Shape = ParticleSystemShapeType.Circle, Rotation = Flat, Radius = 0.3f * r, Thickness = 0f, Position = new Vector3(0f, 0.15f, 0f), From = new Color(0.45f, 0.4f, 0.33f, 0.5f), Grow = Curve(0.5f, 1.5f), Drag = 4f });
                    Shock(root, new Color(0.8f, 0.75f, 0.65f, 0.35f), r, c.Ring);
                    Spawn(root, new Layer { Material = c.Ring, Burst = 1, Delay = 0.1f, Life = new(0.4f, 0.4f), Size = new(1.4f * r, 1.4f * r), Position = new Vector3(0f, 0.05f, 0f), From = new Color(0.8f, 0.75f, 0.65f, 0.25f), FadeIn = 0f, Grow = Expand(0.15f), Render = ParticleSystemRenderMode.HorizontalBillboard, Spin = false, Max = 1 });
                    break;
                case VfxStyle.Lightning:
                    Flash(root, c, new Color(0.7f, 0.8f, 1f, 1f), 1.8f * r, 3f * r + 4f, 9f, 0.2f);
                    Spawn(root, new Layer { Material = c.Spark, Burst = 40, Life = new(0.06f, 0.18f), Speed = new(4f * r, 10f * r), Size = new(0.02f, 0.05f), From = new Color(0.85f, 0.9f, 1f, 1f), FadeIn = 0f, Render = ParticleSystemRenderMode.Stretch, Streak = 0.05f, Drag = 2f, Spin = false });
                    Spawn(root, new Layer { Material = c.Spark, Rate = 60f, Duration = 0.4f, Life = new(0.05f, 0.12f), Speed = new(2f * r, 5f * r), Radius = 0.8f * r, Size = new(0.015f, 0.03f), From = new Color(0.75f, 0.85f, 1f, 1f), FadeIn = 0f, Render = ParticleSystemRenderMode.Stretch, Streak = 0.05f, Spin = false });
                    Shock(root, new Color(0.6f, 0.75f, 1f, 0.6f), r, c.Ring);
                    Puff(root, c, new Color(0.25f, 0.27f, 0.32f, 0.35f), 6, r, 0f, 1.6f);
                    break;
                case VfxStyle.Nature:
                    Flash(root, c, new Color(0.6f, 0.9f, 0.4f, 0.6f), r, 2f * r + 2f, 3f);
                    Spawn(root, new Layer { Material = c.Crystal, Burst = 20, Life = new(1f, 2f), Speed = new(1f * r, 2.5f * r), Size = new(0.06f, 0.12f), From = new Color(0.35f, 0.55f, 0.18f, 1f), To = new Color(0.3f, 0.4f, 0.15f, 0f), FadeIn = 0f, Gravity = 0.3f, Drag = 2f, Noise = 0.6f });
                    Spawn(root, new Layer { Material = c.Glow, Burst = 25, Life = new(1f, 2f), Speed = new(0.2f * r, 0.8f * r), Size = new(0.03f, 0.06f), Radius = 0.5f * r, From = new Color(0.7f, 0.95f, 0.4f, 1f), Velocity = new Vector3(0f, 0.4f, 0f), Noise = 0.4f, Drag = 1f });
                    break;
                case VfxStyle.Arcane:
                    Flash(root, c, Alpha(hot, 1f), 1.8f * r, 2.5f * r + 3f, 6f, 0.35f);
                    Shock(root, Alpha(tint, 0.7f), r, c.Ring);
                    Spawn(root, new Layer { Material = c.Rune, Burst = 1, Life = new(0.7f, 0.7f), Size = new(2f * r, 2f * r), From = Alpha(tint, 0.6f), Grow = Curve(0.8f, 1.1f), Render = ParticleSystemRenderMode.HorizontalBillboard, Max = 1 });
                    Spawn(root, new Layer { Material = c.Glow, Burst = 30, Life = new(0.6f, 1.2f), Speed = new(0.5f * r, 2f * r), Radius = 0.3f * r, Orbit = 2f, Drag = 2f, Size = new(0.03f, 0.07f), From = Alpha(hot, 1f) });
                    Sparks(root, c, Alpha(hot, 1f), 14, new Vector2(2f * r, 4f * r), 0f);
                    break;
                case VfxStyle.Smoke:
                    Puff(root, c, new Color(0.32f, 0.32f, 0.34f, 0.7f), 16, 1.2f * r, 0f, 4f);
                    Spawn(root, new Layer { Material = c.Smoke, Burst = 14, Life = new(1f, 2f), Speed = new(2f * r, 3f * r), Size = new(0.3f * r, 0.5f * r), Shape = ParticleSystemShapeType.Circle, Rotation = Flat, Radius = 0.2f * r, Thickness = 0f, From = new Color(0.3f, 0.3f, 0.31f, 0.45f), Grow = Curve(0.6f, 1.6f), Drag = 3f });
                    break;
                default:
                    Puff(root, c, new Color(0.45f, 0.4f, 0.33f, 0.5f), 8, r, 0f, 1.4f);
                    Sparks(root, c, new Color(1f, 0.75f, 0.4f, 1f), 16, new Vector2(2f * r, 5f * r), 0.8f, 0.04f, 0.5f);
                    Spawn(root, new Layer { Material = c.Crystal, Burst = 10, Life = new(0.4f, 0.8f), Speed = new(1.5f * r, 3f * r), Size = new(0.03f, 0.07f), From = new Color(0.35f, 0.32f, 0.28f, 1f), FadeIn = 0f, Gravity = 1.6f });
                    break;
            }

            Expire(root);
        }

        /// Short flourish around a body (the character root, chest at about 1.2 m); follows it and is gone in about a second.
        public static void Cast(VfxStyle style, Transform caster, Color color)
        {
            AbilityVfxConfig c = AbilityVfxConfig.Instance;

            if (c == null || caster == null || style == VfxStyle.None)
                return;

            Transform root = Root($"Cast{style}", caster.position);
            root.SetParent(caster, true);
            Color tint = Tint(Palette(style), color);
            Color hot = Color.Lerp(tint, Color.white, 0.35f);
            Vector3 chest = new(0f, 1.2f, 0.2f);
            bool isDark = style is VfxStyle.Dark or VfxStyle.Blood or VfxStyle.Smoke;

            if (!isDark)
                Spawn(root, new Layer { Material = c.Glow, Position = chest, Burst = 1, Life = new(0.5f, 0.5f), Size = new(1.1f, 1.1f), From = Alpha(hot, 0.5f), Grow = Curve(0.4f, 1.1f), Spin = false, Local = true, Max = 1, Light = 4f, Intensity = 2.5f });

            Spawn(root, new Layer { Material = c.Glow, Rate = 40f, Duration = 0.5f, Life = new(0.6f, 1f), Size = new(0.03f, 0.07f), Shape = ParticleSystemShapeType.Circle, Rotation = Flat, Radius = 0.55f, Thickness = 0f, Position = new Vector3(0f, 0.15f, 0f), Velocity = new Vector3(0f, 1.6f, 0f), Orbit = 3f, From = Alpha(hot, isDark ? 0.5f : 1f), To = Alpha(tint, 0f), Local = true, Noise = 0.2f });

            switch (style)
            {
                case VfxStyle.Fire:
                    Spawn(root, new Layer { Material = c.Flame, Position = chest, Rate = 30f, Duration = 0.5f, Life = new(0.3f, 0.5f), Radius = 0.3f, Size = new(0.15f, 0.3f), Velocity = new Vector3(0f, 1f, 0f), Colors = FireRamp(), Grow = Curve(1f, 0.3f), Noise = 0.3f });
                    Spawn(root, new Layer { Material = c.Ember, Position = chest, Burst = 12, Life = new(0.6f, 1.2f), Speed = new(0.3f, 1f), Radius = 0.3f, Size = new(0.02f, 0.04f), From = new Color(1f, 0.6f, 0.2f, 1f), Velocity = new Vector3(0f, 0.6f, 0f), Noise = 0.8f });
                    break;
                case VfxStyle.Frost:
                    Spawn(root, new Layer { Material = c.Crystal, Position = chest, Burst = 14, Life = new(0.5f, 0.9f), Speed = new(0.5f, 1.5f), Radius = 0.3f, Size = new(0.04f, 0.09f), From = new Color(0.88f, 0.96f, 1f, 0.9f), FadeIn = 0f, Gravity = 0.4f, Drag = 1f });
                    Spawn(root, new Layer { Material = c.Smoke, Burst = 6, Position = new Vector3(0f, 0.2f, 0f), Life = new(0.8f, 1.2f), Speed = new(0.3f, 0.8f), Radius = 0.3f, Size = new(0.5f, 0.9f), From = new Color(0.8f, 0.9f, 1f, 0.3f), Drag = 2f });
                    break;
                case VfxStyle.Dark:
                case VfxStyle.Smoke:
                    Spawn(root, new Layer { Material = c.Smoke, Rate = 24f, Duration = 0.5f, Life = new(0.6f, 1f), Shape = ParticleSystemShapeType.Circle, Rotation = Flat, Radius = 0.5f, Thickness = 0f, Position = new Vector3(0f, 0.1f, 0f), Size = new(0.3f, 0.6f), Velocity = new Vector3(0f, 0.9f, 0f), Orbit = 1.5f, From = style == VfxStyle.Dark ? new Color(0.08f, 0.03f, 0.12f, 0.6f) : new Color(0.3f, 0.3f, 0.32f, 0.5f), Local = true, Grow = Curve(0.6f, 1.4f) });
                    break;
                case VfxStyle.Poison:
                    Spawn(root, new Layer { Material = c.Drop, Position = chest, Burst = 14, Life = new(0.5f, 0.9f), Speed = new(0.2f, 0.6f), Radius = 0.3f, Size = new(0.03f, 0.07f), From = new Color(0.5f, 0.8f, 0.15f, 0.9f), Velocity = new Vector3(0f, 0.5f, 0f) });
                    break;
                case VfxStyle.Lightning:
                    Spawn(root, new Layer { Material = c.Spark, Position = chest, Rate = 50f, Duration = 0.5f, Life = new(0.05f, 0.12f), Speed = new(2f, 5f), Radius = 0.35f, Size = new(0.015f, 0.03f), From = new Color(0.8f, 0.88f, 1f, 1f), FadeIn = 0f, Render = ParticleSystemRenderMode.Stretch, Streak = 0.05f, Spin = false });
                    break;
                case VfxStyle.Holy:
                case VfxStyle.Heal:
                case VfxStyle.Light:
                case VfxStyle.Arcane:
                    Spawn(root, new Layer { Material = style == VfxStyle.Arcane ? c.Rune : c.Ring, Position = new Vector3(0f, 0.04f, 0f), Burst = 1, Life = new(0.8f, 0.8f), Size = new(1.6f, 1.6f), From = Alpha(tint, 0.55f), Grow = Expand(0.6f), Render = ParticleSystemRenderMode.HorizontalBillboard, Local = true, Max = 1 });
                    break;
                case VfxStyle.Blood:
                    Spawn(root, new Layer { Material = c.Smoke, Position = chest, Burst = 8, Life = new(0.6f, 1f), Speed = new(0.2f, 0.6f), Radius = 0.3f, Size = new(0.3f, 0.5f), From = new Color(0.4f, 0.03f, 0.03f, 0.4f), Grow = Curve(0.5f, 1.3f) });
                    break;
                case VfxStyle.Shout:
                case VfxStyle.Physical:
                    Spawn(root, new Layer { Material = c.Smoke, Burst = 14, Life = new(0.6f, 1.1f), Speed = new(1.5f, 3f), Size = new(0.3f, 0.5f), Shape = ParticleSystemShapeType.Circle, Rotation = Flat, Radius = 0.3f, Thickness = 0f, Position = new Vector3(0f, 0.1f, 0f), From = new Color(0.45f, 0.4f, 0.33f, 0.4f), Grow = Curve(0.5f, 1.5f), Drag = 3f });
                    break;
                case VfxStyle.Nature:
                    Spawn(root, new Layer { Material = c.Crystal, Rate = 20f, Duration = 0.5f, Life = new(0.6f, 1f), Shape = ParticleSystemShapeType.Circle, Rotation = Flat, Radius = 0.5f, Thickness = 0f, Size = new(0.04f, 0.08f), Velocity = new Vector3(0f, 1.2f, 0f), Orbit = 2f, From = new Color(0.35f, 0.55f, 0.18f, 1f), FadeIn = 0f, Local = true });
                    break;
            }

            Expire(root);
        }

        /// Looping visual of a zone, parented to the zone at local zero (ground level). Size is used only by the fire wall (length, height, thickness).
        public static GameObject Zone(ZoneKind kind, Transform parent, float radius, Vector3 size, Color color)
        {
            Transform root = new GameObject($"Zone{kind}").transform;
            root.SetParent(parent, false);
            AbilityVfxConfig c = AbilityVfxConfig.Instance;

            if (c == null)
                return root.gameObject;

            float r = Mathf.Max(0.5f, radius);
            float area = Mathf.Clamp(r * r, 1f, 36f);
            Vector3 low = new(0f, 0.04f, 0f);

            switch (kind)
            {
                case ZoneKind.HealPool:
                    Color heal = Tint(new Color(0.55f, 0.9f, 0.4f), color);
                    Color gold = new(1f, 0.85f, 0.45f);
                    Steady(root, c.Decal, low, 2f * r, Alpha(Color.Lerp(heal, gold, 0.3f), 0.45f));
                    Steady(root, c.Ring, low + new Vector3(0f, 0.01f, 0f), 2f * r, Alpha(gold, 0.35f));
                    Spawn(root, Disc(new Layer { Material = c.Glow, Rate = Mathf.Min(6f * area, 40f), Life = new(1.2f, 2.2f), Velocity = new Vector3(0f, 0.6f, 0f), Size = new(0.03f, 0.07f), From = Alpha(Color.Lerp(heal, Color.white, 0.3f), 1f), To = Alpha(gold, 0f), FadeIn = 0.2f, Noise = 0.2f, Max = 120 }, r));
                    Spawn(root, Disc(new Layer { Material = c.Smoke, Rate = Mathf.Min(1.5f * area, 12f), Position = new Vector3(0f, 0.15f, 0f), Life = new(2.5f, 4f), Size = new(0.8f, 1.4f), Velocity = new Vector3(0f, 0.1f, 0f), From = Alpha(heal, 0.12f), FadeIn = 0.3f }, r));
                    Lamp(root, c, heal, r + 3f, 1.4f, new Vector3(0f, 0.5f, 0f), false);
                    break;
                case ZoneKind.FirePool:
                    Steady(root, c.Decal, low, 2f * r, new Color(0.06f, 0.04f, 0.03f, 0.75f));
                    Steady(root, c.Glow, low + new Vector3(0f, 0.01f, 0f), 2.2f * r, new Color(1f, 0.4f, 0.1f, 0.25f));
                    Spawn(root, Disc(new Layer { Material = c.Flame, Rate = Mathf.Min(18f * area, 90f), Life = new(0.5f, 1f), Velocity = new Vector3(0f, 1.4f, 0f), Size = new(0.35f, 0.8f), Colors = FireRamp(), Grow = Curve(1f, 0.3f), Noise = 0.4f, Max = 150 }, 0.9f * r));
                    Spawn(root, Disc(new Layer { Material = c.Ember, Rate = Mathf.Min(4f * area, 30f), Life = new(1f, 2f), Velocity = new Vector3(0f, 1.2f, 0f), Size = new(0.02f, 0.05f), From = new Color(1f, 0.55f, 0.15f, 1f), Noise = 0.8f }, r));
                    Spawn(root, Disc(new Layer { Material = c.Smoke, Rate = Mathf.Min(1.5f * area, 12f), Position = new Vector3(0f, 0.8f, 0f), Life = new(2f, 3.5f), Velocity = new Vector3(0f, 0.8f, 0f), Size = new(0.8f, 1.6f), From = new Color(0.08f, 0.07f, 0.06f, 0.4f), Grow = Curve(0.6f, 1.6f), Noise = 0.3f }, 0.7f * r));
                    Lamp(root, c, new Color(1f, 0.55f, 0.2f), 2f * r + 6f, 2.6f, new Vector3(0f, 0.8f, 0f), true);
                    break;
                case ZoneKind.PoisonCloud:
                    Color venom = Tint(new Color(0.35f, 0.55f, 0.12f), color, 0.4f);
                    Steady(root, c.Decal, low, 2f * r, new Color(0.2f, 0.32f, 0.06f, 0.35f));
                    Spawn(root, Disc(new Layer { Material = c.Smoke, Rate = Mathf.Min(3f * area, 30f), Position = new Vector3(0f, 0.6f, 0f), Life = new(3f, 5f), Speed = new(0.05f, 0.2f), Velocity = new Vector3(0f, 0.08f, 0f), Orbit = 0.15f, Size = new(1.4f, 2.4f), From = Alpha(venom, 0.32f), To = new Color(0.25f, 0.4f, 0.12f, 0f), FadeIn = 0.3f, Grow = Curve(0.6f, 1.3f), Noise = 0.25f, Frequency = 0.4f, Max = 120 }, 0.85f * r));
                    Spawn(root, Disc(new Layer { Material = c.Drop, Rate = Mathf.Min(area, 12f), Position = new Vector3(0f, 0.2f, 0f), Life = new(1f, 2f), Velocity = new Vector3(0f, 0.4f, 0f), Size = new(0.04f, 0.08f), From = new Color(0.5f, 0.8f, 0.15f, 0.8f) }, r));
                    Lamp(root, c, new Color(0.45f, 0.7f, 0.2f), r + 2f, 0.6f, new Vector3(0f, 0.6f, 0f), false);
                    break;
                case ZoneKind.Frost:
                    Steady(root, c.Decal, low, 2f * r, new Color(0.8f, 0.9f, 1f, 0.45f));
                    Steady(root, c.Ring, low + new Vector3(0f, 0.01f, 0f), 2f * r, new Color(0.6f, 0.85f, 1f, 0.25f));
                    int crystals = Mathf.Clamp(Mathf.RoundToInt(8f * area), 8, 60);
                    Spawn(root, Disc(new Layer { Material = c.Crystal, Burst = crystals, Position = new Vector3(0f, 0.1f, 0f), Life = new(Forever, Forever), Size = new(0.12f, 0.35f), From = new Color(0.85f, 0.95f, 1f, 0.85f), To = new Color(0.85f, 0.95f, 1f, 0.85f), FadeIn = 0f, Max = crystals }, r, 1f));
                    Spawn(root, Disc(new Layer { Material = c.Smoke, Rate = Mathf.Min(2f * area, 20f), Position = new Vector3(0f, 0.15f, 0f), Life = new(2.5f, 4f), Velocity = new Vector3(0f, 0.05f, 0f), Size = new(1f, 1.8f), From = new Color(0.8f, 0.9f, 1f, 0.22f), FadeIn = 0.3f, Grow = Curve(0.6f, 1.3f), Noise = 0.15f }, r));
                    Spawn(root, Disc(new Layer { Material = c.Spark, Rate = Mathf.Min(3f * area, 30f), Position = new Vector3(0f, 0.3f, 0f), Life = new(0.4f, 0.9f), Velocity = new Vector3(0f, 0.15f, 0f), Size = new(0.015f, 0.03f), From = new Color(0.9f, 0.97f, 1f, 1f) }, r));
                    Lamp(root, c, new Color(0.6f, 0.8f, 1f), r + 2f, 0.8f, new Vector3(0f, 0.5f, 0f), false);
                    break;
                case ZoneKind.Banner:
                    Model(root, c.BannerMesh, new[] { c.Solid, c.Solid, c.Solid }, new Color(0.32f, 0.21f, 0.12f), Tint(new Color(0.42f, 0.06f, 0.05f), color, 0.7f), new Color(0.35f, 0.35f, 0.37f));
                    Steady(root, c.Ring, low, 2f * r, new Color(1f, 0.82f, 0.4f, 0.22f));
                    Spawn(root, Disc(new Layer { Material = c.Glow, Rate = Mathf.Min(2f * area, 20f), Life = new(1.5f, 2.5f), Velocity = new Vector3(0f, 0.5f, 0f), Size = new(0.025f, 0.05f), From = new Color(1f, 0.85f, 0.5f, 0.9f), FadeIn = 0.2f, Noise = 0.2f }, r));
                    Lamp(root, c, new Color(1f, 0.8f, 0.45f), 4f, 0.8f, new Vector3(0f, 2f, 0f), false);
                    break;
                case ZoneKind.FireWall:
                    float length = Mathf.Max(1f, size.x);
                    float height = Mathf.Max(0.5f, size.y);
                    Vector3 box = new(length, 0.05f, Mathf.Max(0.2f, size.z));
                    float scale = Mathf.Clamp(height * 0.5f, 0.6f, 1.5f);
                    Spawn(root, new Layer { Material = c.Flame, Rate = Mathf.Min(30f * length, 200f), Shape = ParticleSystemShapeType.Box, Scale = box, Position = new Vector3(0f, 0.1f, 0f), Life = new(0.5f, 0.9f), Velocity = new Vector3(0f, height * 1.4f, 0f), Size = new(0.6f * scale, 1.1f * scale), Colors = FireRamp(), Grow = Curve(1f, 0.35f), Noise = 0.5f, Max = 300 });
                    Spawn(root, new Layer { Material = c.Glow, Rate = Mathf.Min(6f * length, 60f), Shape = ParticleSystemShapeType.Box, Scale = box, Position = new Vector3(0f, 0.3f, 0f), Life = new(0.4f, 0.7f), Size = new(0.8f, 1.4f), From = new Color(1f, 0.45f, 0.1f, 0.35f), Max = 60 });
                    Spawn(root, new Layer { Material = c.Ember, Rate = Mathf.Min(4f * length, 40f), Shape = ParticleSystemShapeType.Box, Scale = box, Life = new(1f, 1.8f), Velocity = new Vector3(0f, height * 1.2f, 0f), Size = new(0.02f, 0.05f), From = new Color(1f, 0.55f, 0.15f, 1f), Noise = 1f });
                    Spawn(root, new Layer { Material = c.Smoke, Rate = Mathf.Min(1.2f * length, 16f), Shape = ParticleSystemShapeType.Box, Scale = box, Position = new Vector3(0f, height, 0f), Life = new(2f, 3f), Velocity = new Vector3(0f, 0.8f, 0f), Size = new(1f, 2f), From = new Color(0.08f, 0.07f, 0.06f, 0.35f), Grow = Curve(0.6f, 1.5f), Noise = 0.3f });
                    Lamp(root, c, new Color(1f, 0.55f, 0.2f), length * 0.5f + 5f, 2.6f, new Vector3(0f, height * 0.5f, 0f), true);
                    break;
                case ZoneKind.Pyre:
                    Steady(root, c.Decal, low, 2f * r, new Color(0.08f, 0.05f, 0.04f, 0.6f));
                    Transform warning = Child(root, WarningName);
                    Spawn(warning, new Layer { Material = c.Rune, Rate = 1.2f, Position = low + new Vector3(0f, 0.01f, 0f), Life = new(1.2f, 1.2f), Size = new(2f * r, 2f * r), From = new Color(1f, 0.35f, 0.08f, 0.7f), FadeIn = 0.3f, Grow = Curve(0.95f, 1.05f), Render = ParticleSystemRenderMode.HorizontalBillboard, Spin = false, Local = true, Max = 3 });
                    Spawn(warning, Disc(new Layer { Material = c.Ember, Rate = Mathf.Min(3f * area, 25f), Life = new(1f, 2f), Velocity = new Vector3(0f, 0.9f, 0f), Size = new(0.02f, 0.05f), From = new Color(1f, 0.5f, 0.15f, 1f), Noise = 0.6f }, r));
                    Spawn(warning, Disc(new Layer { Material = c.Smoke, Rate = Mathf.Min(area, 8f), Life = new(2f, 3f), Velocity = new Vector3(0f, 0.5f, 0f), Size = new(0.6f, 1.1f), From = new Color(0.1f, 0.09f, 0.08f, 0.25f), Grow = Curve(0.6f, 1.4f) }, r));
                    Lamp(warning, c, new Color(1f, 0.35f, 0.1f), r + 3f, 1.2f, new Vector3(0f, 0.4f, 0f), true);
                    Transform eruption = Child(root, EruptionName);
                    Spawn(eruption, Disc(new Layer { Material = c.Flame, Hold = true, Burst = 30, Rate = 120f, Duration = 0.9f, Life = new(0.5f, 0.9f), Velocity = new Vector3(0f, 9f, 0f), Size = new(0.8f * Mathf.Min(r, 2f), 1.6f * Mathf.Min(r, 2f)), Colors = FireRamp(), Grow = Curve(1f, 0.4f), Noise = 0.6f, Max = 200 }, 0.7f * r));
                    Spawn(eruption, Disc(new Layer { Material = c.Glow, Hold = true, Rate = 40f, Duration = 0.8f, Life = new(0.3f, 0.5f), Velocity = new Vector3(0f, 12f, 0f), Size = new(1f, 1.8f), From = new Color(1f, 0.8f, 0.5f, 0.6f), Render = ParticleSystemRenderMode.Stretch, Streak = 0.08f, Spin = false }, 0.4f * r));
                    Spawn(eruption, new Layer { Material = c.Glow, Hold = true, Burst = 1, Position = new Vector3(0f, 1.5f, 0f), Life = new(1.6f, 1.6f), Size = new(3f * r, 3f * r), From = new Color(1f, 0.6f, 0.25f, 0.8f), FadeIn = 0f, Spin = false, Max = 1, Light = 4f * r + 8f, Intensity = 10f });
                    Spawn(eruption, new Layer { Material = c.Smoke, Hold = true, Burst = 14, Delay = 0.3f, Position = new Vector3(0f, 4f, 0f), Radius = 0.5f * r, Life = new(2.5f, 4f), Speed = new(0.5f, 1.5f), Velocity = new Vector3(0f, 1.5f, 0f), Size = new(1.5f, 3f), From = new Color(0.07f, 0.06f, 0.05f, 0.55f), Grow = Curve(0.6f, 1.6f), Noise = 0.3f });
                    Spawn(eruption, new Layer { Material = c.Ember, Hold = true, Burst = 60, Shape = ParticleSystemShapeType.Cone, Rotation = Up, Angle = 25f, Radius = 0.5f * r, Life = new(1.5f, 3f), Speed = new(2f, 6f), Size = new(0.03f, 0.06f), From = new Color(1f, 0.6f, 0.2f, 1f), FadeIn = 0f, Gravity = 0.2f, Noise = 1f });
                    Spawn(eruption, new Layer { Material = c.Ring, Hold = true, Burst = 1, Position = low, Life = new(0.5f, 0.5f), Size = new(2.6f * r, 2.6f * r), From = new Color(1f, 0.5f, 0.15f, 0.7f), FadeIn = 0f, Grow = Expand(0.2f), Render = ParticleSystemRenderMode.HorizontalBillboard, Spin = false, Max = 1 });
                    break;
                case ZoneKind.Storm:
                    Steady(root, c.Decal, low, 2f * r, new Color(0.05f, 0.06f, 0.09f, 0.55f));
                    Steady(root, c.Ring, low + new Vector3(0f, 0.01f, 0f), 2f * r, new Color(0.4f, 0.5f, 0.9f, 0.25f));
                    Spawn(root, Disc(new Layer { Material = c.Smoke, Rate = Mathf.Min(2f * area, 20f), Position = new Vector3(0f, 0.2f, 0f), Life = new(2f, 3.5f), Orbit = 0.4f, Size = new(1f, 1.8f), From = new Color(0.1f, 0.11f, 0.16f, 0.35f), FadeIn = 0.3f, Noise = 0.2f }, 0.9f * r));
                    Spawn(root, Disc(new Layer { Material = c.Spark, Rate = Mathf.Min(6f * area, 60f), Position = new Vector3(0f, 0.1f, 0f), Life = new(0.05f, 0.15f), Speed = new(1f, 4f), Size = new(0.015f, 0.035f), From = new Color(0.75f, 0.85f, 1f, 1f), FadeIn = 0f, Noise = 2f, Frequency = 3f, Render = ParticleSystemRenderMode.Stretch, Streak = 0.05f, Spin = false }, r));
                    Spawn(root, Disc(new Layer { Material = c.Glow, Rate = 3f, Position = new Vector3(0f, 0.2f, 0f), Life = new(0.06f, 0.14f), Size = new(0.3f, 0.6f), From = new Color(0.6f, 0.7f, 1f, 0.8f), FadeIn = 0f, Spin = false, Light = 4f, Intensity = 3f, Lights = 2, Max = 4 }, r));
                    break;
                case ZoneKind.Sanctuary:
                    Color holy = Tint(new Color(1f, 0.85f, 0.5f), color, 0.3f);
                    Steady(root, c.Ring, low, 2f * r, Alpha(holy, 0.4f));
                    Steady(root, c.Rune, low + new Vector3(0f, 0.01f, 0f), 1.9f * r, Alpha(holy, 0.18f), true);
                    Spawn(root, Disc(new Layer { Material = c.Glow, Rate = Mathf.Min(4f * area, 40f), Life = new(1.5f, 3f), Velocity = new Vector3(0f, 0.7f, 0f), Size = new(0.03f, 0.07f), From = new Color(1f, 0.92f, 0.7f, 1f), To = new Color(1f, 0.8f, 0.45f, 0f), FadeIn = 0.2f, Noise = 0.2f, Max = 120 }, r));
                    Spawn(root, Disc(new Layer { Material = c.Glow, Rate = Mathf.Min(area, 10f), Life = new(1.5f, 2.5f), Velocity = new Vector3(0f, 0.6f, 0f), Size = new(0.25f, 0.4f), From = Alpha(holy, 0.15f), FadeIn = 0.4f, Render = ParticleSystemRenderMode.Stretch, Streak = 2f, Spin = false }, 0.9f * r));
                    Lamp(root, c, new Color(1f, 0.88f, 0.6f), r + 3f, 1.5f, new Vector3(0f, 0.8f, 0f), false);
                    break;
                case ZoneKind.Whisper:
                    Steady(root, c.Decal, low, 2f * r, new Color(0.08f, 0.03f, 0.1f, 0.5f));
                    Spawn(root, Disc(new Layer { Material = c.Smoke, Rate = Mathf.Min(2.5f * area, 24f), Position = new Vector3(0f, 0.4f, 0f), Life = new(2.5f, 4f), Orbit = 0.35f, Velocity = new Vector3(0f, 0.12f, 0f), Size = new(1f, 1.8f), From = new Color(0.12f, 0.04f, 0.16f, 0.4f), FadeIn = 0.3f, Noise = 0.25f }, 0.85f * r));
                    Spawn(root, Disc(new Layer { Material = c.Spark, Rate = Mathf.Min(3f * area, 30f), Life = new(1.5f, 2.5f), Orbit = 1.2f, Radial = -0.2f, Velocity = new Vector3(0f, 0.5f, 0f), Size = new(0.03f, 0.05f), From = Tint(new Color(0.55f, 0.25f, 0.8f, 0.7f), color, 0.3f), FadeIn = 0.2f, Noise = 0.3f, Render = ParticleSystemRenderMode.Stretch, Streak = 0.4f, Spin = false }, r));
                    Spawn(root, Disc(new Layer { Material = c.Glow, Rate = Mathf.Min(area, 10f), Life = new(1f, 2f), Velocity = new Vector3(0f, 0.3f, 0f), Size = new(0.03f, 0.06f), From = new Color(0.6f, 0.3f, 0.9f, 0.8f), Noise = 0.3f }, r));
                    Lamp(root, c, new Color(0.5f, 0.2f, 0.75f), r + 2f, 0.8f, new Vector3(0f, 0.5f, 0f), false);
                    break;
                case ZoneKind.Flare:
                    Vector3 flame = new(0f, 0.15f, 0f);
                    Spawn(root, new Layer { Material = c.Glow, Rate = 12f, Position = flame, Life = new(0.15f, 0.3f), Size = new(0.4f, 0.7f), From = new Color(1f, 0.85f, 0.6f, 1f), FadeIn = 0f, Local = true });
                    Steady(root, c.Glow, flame + new Vector3(0f, 0.05f, 0f), 1.6f, new Color(1f, 0.6f, 0.3f, 0.25f), false, ParticleSystemRenderMode.Billboard);
                    Spawn(root, new Layer { Material = c.Spark, Rate = 40f, Position = flame, Shape = ParticleSystemShapeType.Cone, Rotation = Up, Angle = 30f, Radius = 0.03f, Life = new(0.3f, 0.8f), Speed = new(1.5f, 4f), Gravity = 0.8f, Size = new(0.02f, 0.04f), From = new Color(1f, 0.8f, 0.5f, 1f), FadeIn = 0f, Render = ParticleSystemRenderMode.Stretch, Streak = 0.05f, Spin = false });
                    Spawn(root, new Layer { Material = c.Smoke, Rate = 4f, Position = new Vector3(0f, 0.4f, 0f), Life = new(2.5f, 4f), Velocity = new Vector3(0f, 0.9f, 0f), Size = new(0.4f, 1.2f), From = new Color(0.35f, 0.25f, 0.22f, 0.35f), Grow = Curve(0.5f, 1.8f), Noise = 0.3f });
                    Lamp(root, c, new Color(1f, 0.72f, 0.42f), 14f, 4f, new Vector3(0f, 0.6f, 0f), true);
                    break;
            }

            return root.gameObject;
        }

        /// Pyre eruption on a visual made by Zone(ZoneKind.Pyre): the warning glyph dies down and the fire pillar bursts up.
        public static void Erupt(GameObject zoneVisual)
        {
            if (zoneVisual == null)
                return;

            Transform warning = zoneVisual.transform.Find(WarningName);
            Transform eruption = zoneVisual.transform.Find(EruptionName);

            if (warning != null)
            {
                foreach (ParticleSystem system in warning.GetComponentsInChildren<ParticleSystem>())
                    system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (eruption == null)
                return;

            foreach (ParticleSystem system in eruption.GetComponentsInChildren<ParticleSystem>())
                system.Play(false);
        }

        /// Short-lived beam between two points (holy lance, dark drain tether, frost ray), about 0.35 s.
        public static void Beam(VfxStyle style, Vector3 from, Vector3 to, Color color)
        {
            AbilityVfxConfig c = AbilityVfxConfig.Instance;
            Vector3 delta = to - from;
            float length = delta.magnitude;

            if (c == null || style == VfxStyle.None || length < 0.05f)
                return;

            Transform root = Root($"Beam{style}", (from + to) * 0.5f);
            root.rotation = Quaternion.FromToRotation(Vector3.right, delta / length);
            Color tint = Tint(Palette(style), color);
            Color hot = Color.Lerp(tint, Color.white, 0.5f);
            float width = style switch
            {
                VfxStyle.Holy or VfxStyle.Light => 0.18f,
                VfxStyle.Lightning => 0.08f,
                VfxStyle.Frost => 0.14f,
                _ => 0.12f
            };
            int count = Mathf.Clamp(Mathf.RoundToInt(length / (width * 0.35f)), 8, 300);
            Vector3 end = new(length * 0.5f, 0f, 0f);

            Spawn(root, Line(new Layer { Material = c.Glow, Burst = count, Life = new(0.3f, 0.35f), Size = new(width, width * 1.2f), From = Alpha(hot, style == VfxStyle.Dark ? 0.5f : 0.9f), FadeIn = 0f, Max = count }, length));
            Spawn(root, Line(new Layer { Material = c.Glow, Burst = count / 3, Life = new(0.3f, 0.35f), Size = new(width * 3f, width * 3.5f), From = Alpha(tint, 0.25f), FadeIn = 0f, Max = count }, length));
            Spawn(root, new Layer { Material = c.Glow, Burst = 1, Position = end, Life = new(0.35f, 0.35f), Size = new(width * 6f, width * 6f), From = Alpha(hot, 0.8f), FadeIn = 0f, Spin = false, Local = true, Max = 1, Light = 4f, Intensity = 3f });

            switch (style)
            {
                case VfxStyle.Dark:
                case VfxStyle.Blood:
                    Spawn(root, Line(new Layer { Material = c.Smoke, Burst = count / 3, Life = new(0.35f, 0.6f), Size = new(width * 2f, width * 4f), From = style == VfxStyle.Dark ? new Color(0.06f, 0.02f, 0.09f, 0.6f) : new Color(0.3f, 0.02f, 0.02f, 0.5f), Velocity = new Vector3(-length * 1.5f, 0f, 0f), Noise = 0.3f, Max = count }, length));
                    Spawn(root, Line(new Layer { Material = c.Spark, Burst = count / 4, Life = new(0.2f, 0.35f), Size = new(0.02f, 0.04f), From = Alpha(hot, 1f), Velocity = new Vector3(-length * 2.5f, 0f, 0f), Render = ParticleSystemRenderMode.Stretch, Streak = 0.03f, Spin = false, Max = count }, length));
                    break;
                case VfxStyle.Frost:
                    Spawn(root, Line(new Layer { Material = c.Crystal, Burst = count / 4, Life = new(0.3f, 0.6f), Speed = new(0.2f, 0.8f), Size = new(0.04f, 0.1f), From = new Color(0.88f, 0.96f, 1f, 0.9f), FadeIn = 0f, Gravity = 0.5f, Max = count }, length));
                    Spawn(root, Line(new Layer { Material = c.Smoke, Burst = count / 5, Life = new(0.5f, 0.9f), Size = new(0.3f, 0.5f), From = new Color(0.8f, 0.9f, 1f, 0.3f), Grow = Curve(0.5f, 1.4f), Max = count }, length));
                    break;
                default:
                    Spawn(root, Line(new Layer { Material = c.Glow, Burst = count / 4, Life = new(0.4f, 0.8f), Speed = new(0.1f, 0.5f), Size = new(0.02f, 0.05f), From = Alpha(hot, 1f), Velocity = new Vector3(0f, 0.4f, 0f), Noise = 0.3f, Max = count }, length));
                    break;
            }

            Expire(root);
        }

        /// Looping in-flight look of a spell projectile (+Z flies forward); restart it with Restart() after moving it to a new shot.
        public static GameObject Projectile(VfxStyle style, Transform parent, Color color)
        {
            Transform root = new GameObject($"Projectile{style}").transform;
            root.SetParent(parent, false);
            AbilityVfxConfig c = AbilityVfxConfig.Instance;

            if (c == null)
                return root.gameObject;

            Color tint = Tint(Palette(style), color);
            Color hot = Color.Lerp(tint, Color.white, 0.45f);
            bool isDark = style == VfxStyle.Dark;
            Spawn(root, new Layer { Material = c.Glow, Rate = 20f, Life = new(0.12f, 0.2f), Size = isDark ? new Vector2(0.5f, 0.6f) : new Vector2(0.35f, 0.45f), From = Alpha(isDark ? tint : hot, isDark ? 0.5f : 0.9f), FadeIn = 0f, Local = true, Max = 8 });
            Lamp(root, c, tint, 5f, isDark ? 1.2f : 2.2f, Vector3.zero, true);

            TrailRenderer trail = root.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = c.Beam;
            trail.time = style == VfxStyle.Holy ? 0.3f : 0.18f;
            trail.widthCurve = AnimationCurve.Linear(0f, style == VfxStyle.Holy ? 0.22f : 0.14f, 1f, 0f);
            trail.colorGradient = Fade(Alpha(hot, isDark ? 0.4f : 0.8f), Alpha(tint, 0f), 0f);
            trail.minVertexDistance = 0.1f;
            trail.numCapVertices = 2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            switch (style)
            {
                case VfxStyle.Fire:
                    Spawn(root, new Layer { Material = c.Flame, Rate = 30f, Life = new(0.15f, 0.25f), Speed = new(0f, 0.3f), Radius = 0.05f, Size = new(0.3f, 0.4f), Colors = FireRamp(), Local = true, Max = 10 });
                    Spawn(root, new Layer { Material = c.Flame, Rate = 70f, Life = new(0.2f, 0.4f), Speed = new(0f, 0.3f), Radius = 0.08f, Size = new(0.25f, 0.45f), Colors = FireRamp(), Grow = Curve(1f, 0.3f), Noise = 0.5f });
                    Spawn(root, new Layer { Material = c.Ember, Rate = 25f, Life = new(0.4f, 0.8f), Radius = 0.08f, Size = new(0.02f, 0.04f), From = new Color(1f, 0.6f, 0.2f, 1f), Gravity = -0.1f, Noise = 1f });
                    Spawn(root, new Layer { Material = c.Smoke, Rate = 12f, Life = new(0.6f, 1f), Radius = 0.08f, Size = new(0.2f, 0.4f), From = new Color(0.1f, 0.08f, 0.07f, 0.3f), Grow = Curve(0.5f, 1.8f), Noise = 0.3f });
                    break;
                case VfxStyle.Frost:
                    Spawn(root, new Layer { Material = c.Crystal, Rate = 8f, Life = new(0.25f, 0.25f), Size = new(0.35f, 0.35f), From = new Color(0.85f, 0.95f, 1f, 0.95f), FadeIn = 0.5f, Spin = false, Local = true, Max = 3 });
                    Spawn(root, new Layer { Material = c.Smoke, Rate = 30f, Life = new(0.4f, 0.8f), Radius = 0.06f, Size = new(0.15f, 0.35f), From = new Color(0.8f, 0.92f, 1f, 0.35f), Grow = Curve(0.5f, 1.6f), Noise = 0.2f });
                    Spawn(root, new Layer { Material = c.Spark, Rate = 25f, Life = new(0.3f, 0.6f), Radius = 0.1f, Size = new(0.015f, 0.03f), From = new Color(0.9f, 0.97f, 1f, 1f) });
                    break;
                case VfxStyle.Holy:
                case VfxStyle.Light:
                    Spawn(root, new Layer { Material = c.Glow, Rate = 30f, Life = new(0.3f, 0.7f), Radius = 0.08f, Size = new(0.03f, 0.06f), From = Alpha(hot, 1f), Velocity = new Vector3(0f, 0.3f, 0f), Noise = 0.3f });
                    Spawn(root, new Layer { Material = c.Glow, Rate = 8f, Life = new(0.25f, 0.25f), Size = new(0.9f, 0.9f), From = Alpha(tint, 0.25f), FadeIn = 0.5f, Spin = false, Local = true, Max = 3 });
                    break;
                case VfxStyle.Dark:
                    Spawn(root, new Layer { Material = c.Smoke, Rate = 25f, Life = new(0.2f, 0.35f), Radius = 0.04f, Size = new(0.3f, 0.45f), From = new Color(0.05f, 0.02f, 0.08f, 0.85f), FadeIn = 0f, Local = true, Max = 10 });
                    Spawn(root, new Layer { Material = c.Smoke, Rate = 40f, Life = new(0.4f, 0.8f), Radius = 0.08f, Size = new(0.15f, 0.35f), From = new Color(0.06f, 0.02f, 0.09f, 0.6f), Grow = Curve(0.6f, 1.5f), Noise = 0.4f });
                    Spawn(root, new Layer { Material = c.Spark, Rate = 20f, Life = new(0.3f, 0.5f), Radius = 0.12f, Size = new(0.02f, 0.035f), From = new Color(0.6f, 0.3f, 0.9f, 0.9f), Noise = 0.6f });
                    break;
                default:
                    Spawn(root, new Layer { Material = c.Glow, Rate = 30f, Life = new(0.3f, 0.6f), Radius = 0.1f, Size = new(0.025f, 0.05f), From = Alpha(hot, 1f), Noise = 0.5f });
                    Spawn(root, new Layer { Material = c.Spark, Rate = 15f, Life = new(0.2f, 0.4f), Radius = 0.12f, Size = new(0.015f, 0.03f), From = Alpha(hot, 1f) });
                    break;
            }

            return root.gameObject;
        }

        /// Clears and restarts every particle system and trail under the root: a pooled visual moved to a new shot.
        public static void Restart(GameObject visual)
        {
            foreach (ParticleSystem system in visual.GetComponentsInChildren<ParticleSystem>())
            {
                system.Clear(false);
                system.Play(false);
            }

            foreach (TrailRenderer trail in visual.GetComponentsInChildren<TrailRenderer>())
                trail.Clear();
        }

        /// Small glass bottle (+Y up the neck) with tinted liquid; spin it yourself.
        public static GameObject Flask(Transform parent, Color liquid)
        {
            Transform root = new GameObject("Flask").transform;
            root.SetParent(parent, false);
            AbilityVfxConfig c = AbilityVfxConfig.Instance;

            if (c != null)
                Model(root, c.FlaskMesh, new[] { c.Glass, c.Solid, c.Solid }, Color.clear, liquid, new Color(0.42f, 0.3f, 0.18f));

            return root.gameObject;
        }

        /// Small weighted net lying in the XZ plane; spin it yourself.
        public static GameObject Net(Transform parent)
        {
            Transform root = new GameObject("Net").transform;
            root.SetParent(parent, false);
            AbilityVfxConfig c = AbilityVfxConfig.Instance;

            if (c != null)
                Model(root, c.NetMesh, new[] { c.Solid, c.Solid }, new Color(0.45f, 0.36f, 0.22f), new Color(0.25f, 0.25f, 0.27f));

            return root.gameObject;
        }

        /// Looping look of one status effect on a body of the given height, playing; null when the effect has no look on the body.
        public static GameObject Status(StatusEffectKind kind, Transform body, float height)
        {
            AbilityVfxConfig c = AbilityVfxConfig.Instance;

            if (c == null || body == null)
                return null;

            Transform root = new GameObject($"Status{kind}").transform;
            root.SetParent(body, false);

            if (BuildStatus(kind, root, height, c))
                return root.gameObject;

            Object.Destroy(root.gameObject);

            return null;
        }

        private static bool BuildStatus(StatusEffectKind kind, Transform root, float height, AbilityVfxConfig c)
        {
            Vector3 chest = new(0f, height * 0.55f, 0f);
            Vector3 torso = new(0.5f, height * 0.75f, 0.35f);
            Vector3 above = new(0f, height + 0.35f, 0f);
            Vector3 hand = new(0.3f, height * 0.55f, 0.45f);
            Color aura = AuraColor(kind);

            switch (kind)
            {
                case StatusEffectKind.Burn:
                    Spawn(root, Body(new Layer { Material = c.Flame, Rate = 22f, Life = new(0.3f, 0.6f), Velocity = new Vector3(0f, 1.2f, 0f), Size = new(0.15f, 0.35f), Colors = FireRamp(), Grow = Curve(1f, 0.3f), Noise = 0.4f, Max = 40 }, chest, torso));
                    Spawn(root, Body(new Layer { Material = c.Ember, Rate = 6f, Life = new(0.6f, 1.2f), Velocity = new Vector3(0f, 1f, 0f), Size = new(0.015f, 0.035f), From = new Color(1f, 0.6f, 0.2f, 1f), Noise = 0.8f, Max = 15 }, chest, torso));
                    Spawn(root, new Layer { Material = c.Smoke, Rate = 3f, Position = new Vector3(0f, height, 0f), Radius = 0.2f, Life = new(1f, 1.8f), Velocity = new Vector3(0f, 0.6f, 0f), Size = new(0.3f, 0.6f), From = new Color(0.08f, 0.07f, 0.06f, 0.3f), Grow = Curve(0.6f, 1.6f), Max = 8 });
                    Lamp(root, c, new Color(1f, 0.5f, 0.15f), 4f, 1.6f, chest, true);

                    return true;
                case StatusEffectKind.Poison:
                    Spawn(root, Body(new Layer { Material = c.Drop, Rate = 6f, Life = new(0.6f, 1.2f), Velocity = new Vector3(0f, 0.3f, 0f), Size = new(0.03f, 0.07f), From = new Color(0.5f, 0.8f, 0.15f, 0.9f), Max = 10 }, chest, torso));
                    Spawn(root, Body(new Layer { Material = c.Drop, Rate = 4f, Life = new(0.5f, 0.9f), Gravity = 0.8f, Size = new(0.025f, 0.045f), From = new Color(0.4f, 0.7f, 0.1f, 0.9f), Render = ParticleSystemRenderMode.Stretch, Streak = 0.05f, Spin = false, Max = 8 }, chest, torso));
                    Spawn(root, Body(new Layer { Material = c.Smoke, Rate = 2.5f, Life = new(1.5f, 2.5f), Velocity = new Vector3(0f, 0.15f, 0f), Size = new(0.5f, 0.9f), From = new Color(0.35f, 0.6f, 0.12f, 0.16f), Max = 6 }, chest, torso));

                    return true;
                case StatusEffectKind.Slow:
                    Spawn(root, Body(new Layer { Material = c.Crystal, Rate = 5f, Life = new(1f, 1.6f), Size = new(0.05f, 0.12f), From = new Color(0.85f, 0.95f, 1f, 0.8f), FadeIn = 0.2f, Local = true, Max = 10 }, new Vector3(0f, height * 0.3f, 0f), new Vector3(0.5f, height * 0.6f, 0.4f)));
                    Spawn(root, Feet(new Layer { Material = c.Smoke, Rate = 3f, Life = new(1.5f, 2.2f), Size = new(0.5f, 0.8f), From = new Color(0.75f, 0.88f, 1f, 0.25f), Velocity = new Vector3(0f, 0.05f, 0f), Max = 6 }, 0.4f));

                    return true;
                case StatusEffectKind.Root:
                    Spawn(root, Feet(new Layer { Material = c.Crystal, Rate = 10f, Life = new(0.8f, 1.2f), Size = new(0.18f, 0.32f), From = new Color(0.8f, 0.92f, 1f, 0.9f), FadeIn = 0.15f, Spin = false, Local = true, Max = 14 }, 0.35f));
                    Hover(root, c.Ring, new Vector3(0f, 0.03f, 0f), 1f, new Color(0.6f, 0.85f, 1f, 0.4f), ParticleSystemRenderMode.HorizontalBillboard);
                    Spawn(root, Feet(new Layer { Material = c.Smoke, Rate = 2f, Life = new(1.2f, 1.8f), Size = new(0.4f, 0.6f), From = new Color(0.8f, 0.9f, 1f, 0.2f), Max = 4 }, 0.3f));

                    return true;
                case StatusEffectKind.Cursed:
                    Spawn(root, new Layer { Material = c.Glow, Rate = 10f, Shape = ParticleSystemShapeType.Circle, Rotation = Flat, Radius = 0.5f, Thickness = 0f, Position = chest, Orbit = 2.5f, Velocity = new Vector3(0f, 0.15f, 0f), Life = new(1f, 1.6f), Size = new(0.03f, 0.06f), From = new Color(0.6f, 0.25f, 0.9f, 0.9f), Local = true, Max = 18 });
                    Spawn(root, Body(new Layer { Material = c.Smoke, Rate = 3f, Life = new(1f, 1.6f), Size = new(0.25f, 0.45f), From = new Color(0.1f, 0.03f, 0.14f, 0.4f), Velocity = new Vector3(0f, 0.3f, 0f), Max = 6 }, chest, torso));

                    return true;
                case StatusEffectKind.Marked:
                    Hover(root, c.Sigil, above, 0.4f, new Color(1f, 0.15f, 0.08f, 0.9f), ParticleSystemRenderMode.Billboard);
                    Spawn(root, new Layer { Material = c.Sigil, Rate = 1f, Position = above, Life = new(1f, 1f), Size = new(0.4f, 0.4f), Grow = Curve(1f, 1.8f), From = new Color(1f, 0.2f, 0.1f, 0.5f), FadeIn = 0f, Spin = false, Local = true, Max = 2 });

                    return true;
                case StatusEffectKind.Vulnerable:
                    Spawn(root, Body(new Layer { Material = c.Drop, Rate = 5f, Life = new(0.5f, 0.9f), Gravity = 0.8f, Size = new(0.025f, 0.05f), From = new Color(0.75f, 0.8f, 0.2f, 0.9f), Render = ParticleSystemRenderMode.Stretch, Streak = 0.05f, Spin = false, Max = 8 }, chest, torso));
                    Spawn(root, Body(new Layer { Material = c.Smoke, Rate = 3f, Life = new(0.8f, 1.4f), Velocity = new Vector3(0f, 0.5f, 0f), Size = new(0.25f, 0.5f), From = new Color(0.75f, 0.78f, 0.55f, 0.2f), Grow = Curve(0.5f, 1.5f), Max = 6 }, chest, torso));

                    return true;
                case StatusEffectKind.Weakened:
                    Spawn(root, Body(new Layer { Material = c.Smoke, Rate = 4f, Life = new(1.5f, 2.2f), Velocity = new Vector3(0f, 0.2f, 0f), Size = new(0.3f, 0.6f), From = new Color(0.45f, 0.47f, 0.42f, 0.25f), Noise = 0.3f, Max = 10 }, chest, torso));

                    return true;
                case StatusEffectKind.Bulwark:
                    Vector3 shield = new(0f, height * 0.55f, 0.5f);
                    Hover(root, c.Ring, shield, 1.2f, new Color(1f, 0.82f, 0.4f, 0.35f), ParticleSystemRenderMode.Billboard, true);
                    Hover(root, c.Glow, shield, 1f, new Color(1f, 0.85f, 0.5f, 0.12f), ParticleSystemRenderMode.Billboard, true);
                    Spawn(root, new Layer { Material = c.Glow, Rate = 8f, Shape = ParticleSystemShapeType.Circle, Radius = 0.55f, Position = shield, Life = new(0.4f, 0.8f), Size = new(0.02f, 0.04f), From = new Color(1f, 0.9f, 0.6f, 1f), Local = true, Max = 8 });

                    return true;
                case StatusEffectKind.Guard:
                case StatusEffectKind.Protection:
                    Spawn(root, Body(new Layer { Material = c.Glow, Rate = 6f, Life = new(0.6f, 1.2f), Size = new(0.02f, 0.05f), From = new Color(0.9f, 0.9f, 1f, 0.8f), Local = true, Max = 10 }, chest, torso));
                    Hover(root, c.Ring, new Vector3(0f, 0.03f, 0f), 1.1f, new Color(0.85f, 0.88f, 1f, 0.18f), ParticleSystemRenderMode.HorizontalBillboard);

                    return true;
                case StatusEffectKind.HealOverTime:
                    Spawn(root, Feet(new Layer { Material = c.Glow, Rate = 10f, Velocity = new Vector3(0f, 0.9f, 0f), Life = new(1f, 1.6f), Size = new(0.03f, 0.06f), From = new Color(0.75f, 1f, 0.55f, 1f), To = new Color(1f, 0.85f, 0.4f, 0f), Noise = 0.3f, Local = true, Max = 18 }, 0.4f));

                    return true;
                case StatusEffectKind.AllAttributes:
                case StatusEffectKind.Strength:
                case StatusEffectKind.BullBlood:
                case StatusEffectKind.Vitality:
                case StatusEffectKind.Agility:
                case StatusEffectKind.Dexterity:
                case StatusEffectKind.Spirit:
                case StatusEffectKind.Knowledge:
                case StatusEffectKind.Armor:
                case StatusEffectKind.ArmorPercent:
                case StatusEffectKind.DamageBonus:
                    Spawn(root, Body(new Layer { Material = c.Glow, Rate = 6f, Velocity = new Vector3(0f, 0.4f, 0f), Life = new(1f, 1.6f), Size = new(0.025f, 0.05f), From = Alpha(aura, 0.9f), Local = true, Max = 12 }, chest, torso));
                    Hover(root, c.Ring, new Vector3(0f, 0.03f, 0f), 1.1f, Alpha(aura, 0.2f), ParticleSystemRenderMode.HorizontalBillboard);

                    return true;
                case StatusEffectKind.Rage:
                case StatusEffectKind.Mercury:
                case StatusEffectKind.Onslaught:
                case StatusEffectKind.Haste:
                    Spawn(root, Body(new Layer { Material = c.Spark, Rate = 14f, Velocity = new Vector3(0f, 2.2f, 0f), Life = new(0.2f, 0.35f), Size = new(0.02f, 0.035f), From = Alpha(aura, 0.55f), Render = ParticleSystemRenderMode.Stretch, Streak = 0.12f, Spin = false, Local = true, Max = 10 }, chest, torso));

                    return true;
                case StatusEffectKind.Lightbearer:
                    Vector3 orb = new(0.2f, height + 0.35f, 0.4f);
                    Hover(root, c.Glow, orb, 0.22f, new Color(1f, 0.93f, 0.78f, 1f), ParticleSystemRenderMode.Billboard);
                    Hover(root, c.Glow, orb, 0.7f, new Color(1f, 0.85f, 0.6f, 0.25f), ParticleSystemRenderMode.Billboard);
                    Spawn(root, new Layer { Material = c.Glow, Rate = 6f, Position = orb, Radius = 0.15f, Speed = new(0.05f, 0.15f), Life = new(0.8f, 1.4f), Size = new(0.015f, 0.03f), From = new Color(1f, 0.9f, 0.7f, 1f), Max = 10 });
                    Light light = Child(root, "Light").gameObject.AddComponent<Light>();
                    light.transform.localPosition = orb;
                    light.type = LightType.Point;
                    light.color = new Color(1f, 0.88f, 0.72f);
                    light.range = 10f;
                    light.intensity = 2.6f;
                    light.shadows = LightShadows.None;

                    return true;
                case StatusEffectKind.FireWeapon:
                    Hover(root, c.Glow, hand, 0.3f, Alpha(aura, 0.5f), ParticleSystemRenderMode.Billboard);
                    Spawn(root, new Layer { Material = c.Flame, Rate = 12f, Position = hand, Radius = 0.12f, Life = new(0.2f, 0.4f), Velocity = new Vector3(0f, 0.8f, 0f), Size = new(0.08f, 0.16f), Colors = FireRamp(), Grow = Curve(1f, 0.3f), Max = 8 });

                    return true;
                case StatusEffectKind.FrostWeapon:
                    Hover(root, c.Glow, hand, 0.3f, Alpha(aura, 0.5f), ParticleSystemRenderMode.Billboard);
                    Spawn(root, new Layer { Material = c.Crystal, Rate = 6f, Position = hand, Radius = 0.12f, Life = new(0.4f, 0.8f), Gravity = 0.2f, Size = new(0.03f, 0.06f), From = new Color(0.88f, 0.96f, 1f, 0.9f), Max = 8 });

                    return true;
                case StatusEffectKind.HolyWeapon:
                    Hover(root, c.Glow, hand, 0.3f, Alpha(aura, 0.5f), ParticleSystemRenderMode.Billboard);
                    Spawn(root, new Layer { Material = c.Glow, Rate = 10f, Position = hand, Radius = 0.12f, Life = new(0.5f, 0.9f), Velocity = new Vector3(0f, 0.4f, 0f), Size = new(0.02f, 0.04f), From = new Color(1f, 0.9f, 0.6f, 1f), Max = 10 });

                    return true;
                case StatusEffectKind.VenomBlade:
                    Hover(root, c.Glow, hand, 0.3f, Alpha(aura, 0.4f), ParticleSystemRenderMode.Billboard);
                    Spawn(root, new Layer { Material = c.Drop, Rate = 6f, Position = hand, Radius = 0.12f, Life = new(0.4f, 0.8f), Gravity = 0.8f, Size = new(0.02f, 0.04f), From = new Color(0.45f, 0.75f, 0.12f, 0.9f), Max = 6 });

                    return true;
                case StatusEffectKind.Silence:
                    Hover(root, c.RuneDark, above, 0.32f, new Color(0.14f, 0.05f, 0.2f, 0.9f), ParticleSystemRenderMode.Billboard);

                    return true;
                case StatusEffectKind.Festering:
                    Spawn(root, Body(new Layer { Material = c.Drop, Rate = 5f, Life = new(0.5f, 0.9f), Gravity = 0.8f, Size = new(0.025f, 0.05f), From = new Color(0.3f, 0.03f, 0.02f, 0.95f), Render = ParticleSystemRenderMode.Stretch, Streak = 0.05f, Spin = false, Max = 8 }, chest, torso));
                    Spawn(root, Body(new Layer { Material = c.Smoke, Rate = 2f, Life = new(1f, 1.6f), Velocity = new Vector3(0f, 0.2f, 0f), Size = new(0.25f, 0.45f), From = new Color(0.15f, 0.03f, 0.02f, 0.2f), Max = 4 }, chest, torso));

                    return true;
                case StatusEffectKind.Drain:
                    Spawn(root, Body(new Layer { Material = c.Smoke, Rate = 5f, Life = new(1f, 1.8f), Velocity = new Vector3(0f, 0.35f, 0f), Size = new(0.3f, 0.6f), From = new Color(0.55f, 0.04f, 0.04f, 0.2f), Noise = 0.3f, Max = 10 }, chest, torso));

                    return true;
                default:
                    return false;
            }
        }

        private static Color AuraColor(StatusEffectKind kind)
        {
            return kind switch
            {
                StatusEffectKind.Strength or StatusEffectKind.DamageBonus => new Color(1f, 0.35f, 0.15f),
                StatusEffectKind.BullBlood or StatusEffectKind.Rage => new Color(0.85f, 0.12f, 0.08f),
                StatusEffectKind.Vitality => new Color(0.95f, 0.45f, 0.35f),
                StatusEffectKind.Agility => new Color(0.5f, 0.9f, 0.4f),
                StatusEffectKind.Dexterity => new Color(0.4f, 0.85f, 0.75f),
                StatusEffectKind.Spirit => new Color(0.65f, 0.6f, 1f),
                StatusEffectKind.Knowledge => new Color(0.4f, 0.6f, 1f),
                StatusEffectKind.Armor or StatusEffectKind.ArmorPercent => new Color(0.65f, 0.72f, 0.8f),
                StatusEffectKind.Mercury => new Color(0.8f, 0.85f, 0.9f),
                StatusEffectKind.Onslaught => new Color(1f, 0.55f, 0.2f),
                StatusEffectKind.Haste => new Color(0.75f, 0.9f, 1f),
                StatusEffectKind.FireWeapon => new Color(1f, 0.5f, 0.15f),
                StatusEffectKind.FrostWeapon => new Color(0.6f, 0.82f, 1f),
                StatusEffectKind.VenomBlade => new Color(0.45f, 0.8f, 0.15f),
                _ => new Color(1f, 0.85f, 0.45f)
            };
        }

        private static Color Palette(VfxStyle style)
        {
            return style switch
            {
                VfxStyle.Physical => new Color(0.55f, 0.48f, 0.38f),
                VfxStyle.Shout => new Color(0.6f, 0.55f, 0.45f),
                VfxStyle.Fire => new Color(1f, 0.5f, 0.15f),
                VfxStyle.Frost => new Color(0.6f, 0.82f, 1f),
                VfxStyle.Lightning => new Color(0.62f, 0.72f, 1f),
                VfxStyle.Holy => new Color(1f, 0.82f, 0.48f),
                VfxStyle.Dark => new Color(0.45f, 0.18f, 0.7f),
                VfxStyle.Poison => new Color(0.45f, 0.75f, 0.15f),
                VfxStyle.Heal => new Color(0.6f, 0.95f, 0.45f),
                VfxStyle.Blood => new Color(0.55f, 0.04f, 0.04f),
                VfxStyle.Alchemy => new Color(1f, 0.62f, 0.22f),
                VfxStyle.Nature => new Color(0.45f, 0.72f, 0.28f),
                VfxStyle.Arcane => new Color(0.45f, 0.55f, 1f),
                VfxStyle.Light => new Color(1f, 0.92f, 0.75f),
                VfxStyle.Smoke => new Color(0.4f, 0.4f, 0.42f),
                _ => Color.white
            };
        }

        /// Pulls the natural palette towards the caller's color; a transparent color keeps the palette.
        private static Color Tint(Color natural, Color color, float amount = 0.5f)
        {
            if (color.a <= 0f)
                return natural;

            Color tinted = Color.Lerp(natural, color, amount);
            tinted.a = natural.a;

            return tinted;
        }

        private static Color Alpha(Color color, float alpha)
        {
            color.a = alpha;

            return color;
        }

        private static Transform Root(string name, Vector3 position)
        {
            s_longest = 0f;
            Transform root = new GameObject(name).transform;
            root.position = position;

            return root;
        }

        private static void Expire(Transform root)
        {
            Object.Destroy(root.gameObject, s_longest + 0.2f);
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);

            return child;
        }

        /// Mesh with one material per submesh; a clear tint leaves that submesh's material as it is.
        private static void Model(Transform parent, Mesh mesh, Material[] materials, params Color[] tints)
        {
            if (mesh == null)
                return;

            GameObject model = Child(parent, "Model").gameObject;
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = model.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s_block ??= new MaterialPropertyBlock();

            for (int i = 0; i < tints.Length && i < materials.Length; i++)
            {
                if (tints[i].a <= 0f)
                    continue;

                s_block.Clear();
                s_block.SetColor(BaseColor, tints[i]);
                renderer.SetPropertyBlock(s_block, i);
            }
        }

        private static void Flash(Transform root, AbilityVfxConfig c, Color color, float size, float range, float intensity, float life = 0.35f)
        {
            Spawn(root, new Layer { Material = c.Glow, Burst = 1, Life = new(life, life), Size = new(size, size), From = color, FadeIn = 0f, Grow = Curve(0.6f, 1.1f), Spin = false, Max = 1, Light = range, Intensity = intensity });
        }

        private static void Sparks(Transform root, AbilityVfxConfig c, Color color, int count, Vector2 speed, float gravity, float size = 0.04f, float life = 0.6f)
        {
            Spawn(root, new Layer { Material = c.Spark, Burst = count, Life = new(life * 0.5f, life), Speed = speed, Size = new(size * 0.6f, size), From = color, FadeIn = 0f, Gravity = gravity, Drag = 1.5f, Render = ParticleSystemRenderMode.Stretch, Streak = 0.06f, Spin = false, Max = count });
        }

        private static void Puff(Transform root, AbilityVfxConfig c, Color color, int count, float r, float delay, float life)
        {
            Spawn(root, new Layer { Material = c.Smoke, Burst = count, Delay = delay, Life = new(life * 0.6f, life), Speed = new(0.4f * r, 1.1f * r), Size = new(0.7f * r, 1.4f * r), Radius = 0.3f * r, From = color, FadeIn = 0.15f, Grow = Curve(0.5f, 1.5f), Drag = 2.5f, Gravity = -0.03f, Noise = 0.3f, Max = count });
        }

        /// Ground shockwave ring racing out to the radius.
        private static void Shock(Transform root, Color color, float r, Material material)
        {
            Spawn(root, new Layer { Material = material, Burst = 1, Position = new Vector3(0f, 0.05f, 0f), Life = new(0.45f, 0.45f), Size = new(2f * r, 2f * r), From = color, FadeIn = 0f, Grow = Expand(0.15f), Render = ParticleSystemRenderMode.HorizontalBillboard, Spin = false, Max = 1 });
        }

        /// Ground-hugging flat particle that never dies: puddles, rings, glyphs of a zone.
        private static void Steady(Transform root, Material material, Vector3 position, float size, Color color, bool isTurning = false,
            ParticleSystemRenderMode render = ParticleSystemRenderMode.HorizontalBillboard)
        {
            Spawn(root, new Layer { Material = material, Burst = 1, Position = position, Life = new(Forever, Forever), Size = new(size, size), From = color, To = color, FadeIn = 0f, Spin = isTurning, Render = render, Local = true, Max = 1 });
        }

        /// A constant-looking particle made of two overlapping fades, so stopping it fades it out smoothly.
        private static void Hover(Transform root, Material material, Vector3 position, float size, Color color, ParticleSystemRenderMode render, bool isFacing = false)
        {
            Spawn(root, new Layer { Material = material, Rate = 4f, Position = position, Life = new(0.5f, 0.5f), Size = new(size, size), From = color, FadeIn = 0.5f, Spin = false, Local = true, Render = render, Facing = isFacing, Max = 4 });
        }

        /// Particle light made of overlapping fading lights (a flickering one when asked), with no visible particle.
        private static void Lamp(Transform root, AbilityVfxConfig c, Color color, float range, float intensity, Vector3 position, bool isFlickering)
        {
            Spawn(root, new Layer
            {
                Material = c.Glow, Position = position, Rate = isFlickering ? 5f : 2f, Life = isFlickering ? new Vector2(0.3f, 0.5f) : new Vector2(1f, 1f), Size = new(0.01f, 0.01f),
                From = Alpha(color, 1f), FadeIn = 0.5f, Spin = false, Local = true, Light = range, Intensity = intensity, Lights = 3, Max = 4
            });
        }

        private static Layer Disc(Layer layer, float radius, float thickness = 1f)
        {
            layer.Shape = ParticleSystemShapeType.Circle;
            layer.Rotation = Flat;
            layer.Radius = radius;
            layer.Thickness = thickness;

            return layer;
        }

        private static Layer Feet(Layer layer, float radius)
        {
            layer.Position = new Vector3(0f, 0.1f, 0f);

            return Disc(layer, radius, 0.4f);
        }

        private static Layer Body(Layer layer, Vector3 center, Vector3 size)
        {
            layer.Shape = ParticleSystemShapeType.Box;
            layer.Position = center;
            layer.Scale = size;

            return layer;
        }

        /// Particles spread evenly along the system's X axis over the given length, held in place.
        private static Layer Line(Layer layer, float length)
        {
            layer.Shape = ParticleSystemShapeType.SingleSidedEdge;
            layer.Radius = length * 0.5f;
            layer.Spread = true;
            layer.Local = true;

            return layer;
        }

        private static Gradient Fade(Color from, Color to, float fadeIn)
        {
            Color end = to == Color.clear ? Alpha(from, 0f) : to;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(fadeIn > 0f ? 0f : from.a, 0f), new GradientAlphaKey(from.a, Mathf.Max(fadeIn, 0.01f)), new GradientAlphaKey(end.a, 1f) });

            return gradient;
        }

        private static Gradient FireRamp()
        {
            Color hot = new(1f, 0.85f, 0.55f, 1f);
            Color mid = new(1f, 0.42f, 0.08f, 0.8f);
            Color cold = new(0.3f, 0.05f, 0.02f, 0f);
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(hot, 0f), new GradientColorKey(mid, 0.4f), new GradientColorKey(cold, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(hot.a, 0.08f), new GradientAlphaKey(mid.a, 0.5f), new GradientAlphaKey(cold.a, 1f) });

            return gradient;
        }

        private static AnimationCurve Curve(float from, float to)
        {
            return AnimationCurve.EaseInOut(0f, from, 1f, to);
        }

        /// Fast-then-slow growth from a fraction to full size: shockwaves.
        private static AnimationCurve Expand(float from)
        {
            return new AnimationCurve(new Keyframe(0f, from, 0f, 2.5f * (1f - from)), new Keyframe(1f, 1f, 0f, 0f));
        }

        private static ParticleSystem Spawn(Transform parent, Layer layer)
        {
            AbilityVfxConfig config = AbilityVfxConfig.Instance;
            GameObject go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = layer.Position;
            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            bool isLooping = layer.Duration <= 0f && (layer.Rate > 0f || layer.PerMetre > 0f);

            ParticleSystem.MainModule main = system.main;
            main.duration = layer.Duration > 0f ? layer.Duration : 1f;
            main.loop = isLooping;
            main.playOnAwake = !layer.Hold;
            main.startDelay = layer.Delay;
            main.startLifetime = new ParticleSystem.MinMaxCurve(layer.Life.x, layer.Life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(layer.Speed.x, layer.Speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(layer.Size.x, layer.Size.y);
            main.startRotation = layer.Spin ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f) : new ParticleSystem.MinMaxCurve(0f);
            main.startColor = Color.white;
            main.gravityModifier = layer.Gravity;
            main.simulationSpace = layer.Local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = layer.Max;

            if (layer.Light > 0f)
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = layer.Rate;
            emission.rateOverDistance = layer.PerMetre;

            if (layer.Burst > 0)
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)layer.Burst) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = layer.Shape;
            shape.radius = Mathf.Max(0.001f, layer.Radius);
            shape.radiusThickness = layer.Thickness;
            shape.angle = layer.Angle;
            shape.scale = layer.Scale;
            shape.rotation = layer.Rotation;

            if (layer.Spread)
                shape.radiusMode = ParticleSystemShapeMultiModeValue.BurstSpread;

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = layer.Colors ?? Fade(layer.From, layer.To, layer.FadeIn);

            if (layer.Grow != null)
            {
                ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, layer.Grow);
            }

            if (layer.Velocity != Vector3.zero || layer.Orbit != 0f || layer.Radial != 0f)
            {
                ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.x = layer.Velocity.x;
                velocity.y = layer.Velocity.y;
                velocity.z = layer.Velocity.z;
                velocity.orbitalX = 0f;
                velocity.orbitalY = layer.Orbit;
                velocity.orbitalZ = 0f;
                velocity.radial = layer.Radial;
            }

            if (layer.Spin)
            {
                ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
            }

            if (layer.Noise > 0f)
            {
                ParticleSystem.NoiseModule noise = system.noise;
                noise.enabled = true;
                noise.strength = layer.Noise;
                noise.frequency = layer.Frequency;
                noise.scrollSpeed = 0.4f;
                noise.quality = ParticleSystemNoiseQuality.Medium;
            }

            if (layer.Drag > 0f)
            {
                ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.limit = 1000f;
                limit.drag = layer.Drag;
                limit.multiplyDragByParticleSize = false;
            }

            if (layer.Light > 0f && config != null && config.Light != null)
            {
                ParticleSystem.LightsModule lights = system.lights;
                lights.enabled = true;
                lights.light = config.Light;
                lights.ratio = 1f;
                lights.maxLights = layer.Lights;
                lights.useParticleColor = true;
                lights.sizeAffectsRange = false;
                lights.alphaAffectsIntensity = true;
                lights.rangeMultiplier = layer.Light;
                lights.intensityMultiplier = layer.Intensity;
            }

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = layer.Material;
            renderer.renderMode = layer.Render;
            renderer.alignment = layer.Facing ? ParticleSystemRenderSpace.Local : ParticleSystemRenderSpace.View;
            renderer.velocityScale = layer.Streak;
            renderer.lengthScale = layer.Stretch;
            renderer.maxParticleSize = 2f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (!layer.Hold)
                system.Play(false);

            if (!isLooping)
                s_longest = Mathf.Max(s_longest, layer.Delay + layer.Duration + layer.Life.y);

            return system;
        }
    }
}
