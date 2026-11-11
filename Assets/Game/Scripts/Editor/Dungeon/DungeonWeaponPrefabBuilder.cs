using System.Collections.Generic;
using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    internal enum AxeStyle
    {
        Battle,
        Hatchet,
        Felling
    }

    /// Extra weapon visuals for the dungeon catalog, generated meshes like the battle weapons.
    internal static class DungeonWeaponPrefabBuilder
    {
        /// The écu's frame in the shield socket: the fist takes the grip like a handle, its palm to the board and its back to
        /// the bearer; the forearm slants up across the board from the lower left and comes in to it from behind.
        public static readonly Quaternion EcuTurn = EcuSocket();
        /// The fist on the écu's grip from the middle of its face, in the écu's frame.
        public static readonly Vector3 EcuFist = new(-EcuCenter, EcuHeight * 0.5f - EcuTop, -EcuGap - EcuBend - EcuThickness);
        /// How far the thumb on the shut book points up from across it, in degrees: the hands pinch its sides with the
        /// thumbs slanting up and in over the front cover.
        public const float BookThumb = 45f;

        /// The écu in its own frame, the fist at the origin: 48 by 60 cm, the top rim 13 cm above the fist, the middle
        /// 5 cm to its left.
        private const float EcuTop = 0.13f;
        private const float EcuHeight = 0.6f;
        private const float EcuCenter = -0.05f;
        private const float EcuBend = 0.045f;
        private const float EcuThickness = 0.014f;
        /// From the shield socket, 7 cm off the back of the hand, through the fist to the board behind the edges.
        private const float EcuGap = 0.07f;
        /// The forearm across the board, in degrees up from its level, and in from behind it: an arm long enough to lie
        /// flat on a board held before the eyes would not reach the grip.
        private const float EcuSlant = 62f;
        private const float EcuRise = 38f;

        public static GameObject BuildAxe(string name, float handleLength, float headSize, AxeStyle style)
        {
            Material iron = WeaponMaterials.Iron;
            Material steel = WeaponMaterials.Steel;
            GameObject root = new GameObject(name);
            WeaponMesh mesh = new WeaponMesh();
            float head = handleLength - 0.12f;
            float h = headSize;
            // A long axe is held in both hands, the off hand at the butt. The haft of the double axe is a stout one.
            float butt = style == AxeStyle.Hatchet ? -0.13f : -0.48f;
            float g = style == AxeStyle.Battle ? 1.25f : 1f;

            // Oval haft with a knob at the butt, so the hand does not slip off.
            mesh.Revolve(style == AxeStyle.Battle ? WeaponMaterials.DarkWood : WeaponMaterials.Wood, new[]
            {
                new Vector2(butt, 0.014f * g), new Vector2(butt + 0.012f, 0.0215f * g), new Vector2(butt + 0.04f, 0.0185f * g), new Vector2(head - h * 0.4f, 0.017f * g),
                new Vector2(head + h * 0.2f, 0.019f * g), new Vector2(head + h * 0.2f + 0.012f, 0.014f * g)
            }, 10, 0.78f);

            if (style != AxeStyle.Battle)
                WeaponParts.Grip(mesh, WeaponMaterials.Leather, butt + 0.045f, style == AxeStyle.Hatchet ? 0.1f : butt + 0.19f, 0.0195f, 0.03f, 0.8f);

            // The eye wraps the haft; the blade is a wedge drawn from it down to the cutting edge.
            mesh.Revolve(iron, new[]
            {
                new Vector2(head - h * 0.2f, 0.021f * g), new Vector2(head - h * 0.17f, 0.025f * g), new Vector2(head + h * 0.17f, 0.025f * g), new Vector2(head + h * 0.2f, 0.021f * g)
            }, 10, 0.8f);

            Vector2[] blade = style switch
            {
                AxeStyle.Felling => new Vector2[]
                {
                    new(0.012f, head + h * 0.17f), new(h * 0.45f, head + h * 0.2f), new(h * 0.92f, head + h * 0.36f), new(h * 1.02f, head + h * 0.16f),
                    new(h * 1.04f, head - h * 0.04f), new(h * 1.0f, head - h * 0.24f), new(h * 0.9f, head - h * 0.42f), new(h * 0.45f, head - h * 0.22f), new(0.012f, head - h * 0.17f)
                },
                // A crescent on a narrow neck, the same above and below: the bit of a double axe.
                AxeStyle.Battle => new Vector2[]
                {
                    new(0.012f, head + h * 0.15f), new(h * 0.2f, head + h * 0.15f), new(h * 0.42f, head + h * 0.27f), new(h * 0.6f, head + h * 0.6f),
                    new(h * 0.7f, head + h * 0.34f), new(h * 0.745f, head + h * 0.115f), new(h * 0.745f, head - h * 0.115f), new(h * 0.7f, head - h * 0.34f),
                    new(h * 0.6f, head - h * 0.6f), new(h * 0.42f, head - h * 0.27f), new(h * 0.2f, head - h * 0.15f), new(0.012f, head - h * 0.15f)
                },
                _ => new Vector2[]
                {
                    new(0.012f, head + h * 0.17f), new(h * 0.35f, head + h * 0.2f), new(h * 0.8f, head + h * 0.44f), new(h * 0.93f, head + h * 0.22f),
                    new(h * 0.98f, head - h * 0.05f), new(h * 0.93f, head - h * 0.32f), new(h * 0.78f, head - h * 0.56f), new(h * 0.45f, head - h * 0.24f), new(0.012f, head - h * 0.17f)
                }
            };
            float[] wedge = style == AxeStyle.Battle
                ? new[] { 0.017f, 0.012f, 0.006f, 0f, 0f, 0f, 0f, 0f, 0f, 0.006f, 0.012f, 0.017f }
                : new[] { 0.017f, 0.009f, 0f, 0f, 0f, 0f, 0f, 0.009f, 0.017f };
            Vector2 middle = new Vector2(h * 0.42f, style == AxeStyle.Battle ? head : head - h * 0.02f);
            mesh.Plate(steel, blade, wedge, middle, 0.0095f);

            switch (style)
            {
                case AxeStyle.Battle:
                    // The second bit on the back of the eye, a finial on top, a pair of rings under the langets and an iron shoe on the butt.
                    mesh.Matrix = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 180f));
                    mesh.Plate(steel, blade, wedge, middle, 0.0095f);
                    mesh.Matrix = Matrix4x4.identity;
                    WeaponParts.Spike(mesh, iron, new Vector3(0f, 0f, head + h * 0.2f + 0.008f), new Vector3(0f, 0f, head + h * 0.2f + 0.06f), 0.015f, 6);
                    Langets(mesh, iron, head - h * 0.2f - 0.14f, head - h * 0.2f, 0.0135f * g);
                    WeaponParts.Band(mesh, iron, head - h * 0.2f - 0.17f, 0.0225f * g, 0.009f, 0.003f, 0.8f);
                    WeaponParts.Band(mesh, iron, head - h * 0.2f - 0.2f, 0.0225f * g, 0.009f, 0.003f, 0.8f);
                    mesh.Revolve(iron, new[]
                    {
                        new Vector2(butt - 0.004f, 0.012f * g), new Vector2(butt + 0.006f, 0.023f * g), new Vector2(butt + 0.085f, 0.0215f * g), new Vector2(butt + 0.092f, 0.0185f * g)
                    }, 10, 0.8f);
                    break;
                case AxeStyle.Felling:
                    WeaponParts.Box(mesh, iron, head - h * 0.16f, head + h * 0.16f, new Vector2(0.02f, 0.022f), new Vector2(0.02f, 0.022f), new Vector2(0f, -0.036f), new Vector2(0f, -0.036f));
                    break;
            }

            mesh.Attach(root.transform, name);
            root.AddComponent<WeaponVisual>();

            return Save(root);
        }

        /// The Tripo morning star: the butt 8.5 cm behind the fist, the ball's middle 55 cm ahead of it.
        public static GameObject BuildMorningStar()
        {
            return BattleWeaponPrefabBuilder.BuildTripo("MorningStar", "MorningStar", BattleWeaponPrefabBuilder.TripoRod, 0.737f, Vector3.zero, new Vector3(0f, 0f, -0.085f));
        }

        public static GameObject BuildStaff()
        {
            const int steps = 16;
            const float crystalZ = 0.33f;

            Material wood = WeaponMaterials.DarkWood;
            Material crystal = BattleEditorUtility.GetUnlitMaterial("Crystal", new Color(0.5f, 0.75f, 1f, 0.9f));
            GameObject root = new GameObject("Staff");
            // Gripped a third of the way down from the head, like a walking stick.
            Transform parent = BattleEditorUtility.CreateChild("Grip", root.transform, Vector3.zero).transform;
            WeaponMesh mesh = new WeaponMesh();
            List<Vector3> path = new(steps + 1);
            List<Vector2> radii = new(steps + 1);

            // A grown stick: it wanders a little and thickens into a knotted head.
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float z = Mathf.Lerp(-1.2f, 0.23f, t);
                float knot = 1f + 0.16f * Mathf.Max(0f, Mathf.Sin(t * 23f)) * Mathf.Max(0f, Mathf.Sin(t * 7f + 1f));
                float radius = Mathf.Lerp(0.0165f, 0.021f, t * t) * knot;
                float sway = Mathf.Clamp01((t - 0.42f) / 0.4f);
                path.Add(new Vector3(Mathf.Sin(t * 9f) * 0.014f * sway, Mathf.Sin(t * 6f + 0.6f) * 0.016f * sway, z));
                radii.Add(new Vector2(radius, radius * 0.94f));
            }

            mesh.Tube(wood, path, radii, 8, Vector3.up);
            mesh.Revolve(WeaponMaterials.Iron, new[] { new Vector2(-1.24f, 0.008f), new Vector2(-1.22f, 0.018f), new Vector2(-1.17f, 0.0185f) }, 8);
            WeaponParts.Grip(mesh, WeaponMaterials.DarkLeather, -0.07f, 0.09f, 0.019f, 0.03f, 0.96f);

            // Four roots of the head close around the crystal.
            Vector3 top = path[steps];

            for (int i = 0; i < 4; i++)
            {
                Quaternion turn = Quaternion.Euler(0f, 0f, 45f + i * 90f);
                mesh.Tube(wood, new[]
                {
                    top + turn * new Vector3(0.012f, 0f, -0.03f), top + turn * new Vector3(0.05f, 0f, 0.03f), top + turn * new Vector3(0.066f, 0f, 0.1f),
                    top + turn * new Vector3(0.05f, 0f, 0.16f), top + turn * new Vector3(0.026f, 0f, 0.19f)
                }, new[] { Vector2.one * 0.011f, Vector2.one * 0.01f, Vector2.one * 0.008f, Vector2.one * 0.0055f, Vector2.one * 0.002f }, 6, turn * Vector3.right);
            }

            Vector3 gem = new Vector3(top.x, top.y, crystalZ);
            mesh.Loft(crystal, new[]
            {
                WeaponMesh.Ellipse(gem + Vector3.back * 0.062f, Vector3.right, Vector3.up, 0.001f, 0.001f, 6),
                WeaponMesh.Ellipse(gem + Vector3.back * 0.018f, Vector3.right, Vector3.up, 0.04f, 0.04f, 6),
                WeaponMesh.Ellipse(gem + Vector3.forward * 0.02f, Vector3.right, Vector3.up, 0.034f, 0.034f, 6, 0.5f),
                WeaponMesh.Ellipse(gem + Vector3.forward * 0.075f, Vector3.right, Vector3.up, 0.001f, 0.001f, 6, 0.5f)
            }, WeaponMesh.Hard);
            mesh.Attach(parent, "Staff");

            Light light = BattleEditorUtility.CreateChild("Glow", parent, gem).AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.5f, 0.75f, 1f);
            light.range = 3f;
            light.intensity = 1.2f;
            DungeonLightLayers.Set(light, DungeonLightLayers.Everything);
            light.shadows = LightShadows.None;
            root.AddComponent<WeaponVisual>();

            return Save(root);
        }

        /// A hewn club for one hand: slim and leather-wrapped in the fist, thick for most of its length (the head 10 cm), 0.98 m long.
        public static GameObject BuildClub()
        {
            const int steps = 14;

            Material wood = WeaponMaterials.DarkWood;
            GameObject root = new GameObject("Club");
            Transform parent = BattleEditorUtility.CreateChild("Grip", root.transform, Vector3.zero).transform;
            WeaponMesh mesh = new WeaponMesh();
            List<Vector3> path = new(steps + 1);
            List<Vector2> radii = new(steps + 1);

            // A grown branch: it wanders a little, knots swell it, and its end is rounded off.
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float z = Mathf.Lerp(ClubMotions.Back, ClubMotions.Tip, t);
                float swell = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.06f, 0.4f, z));
                float radius = (Mathf.Lerp(0.019f, 0.046f, swell) + 0.008f * Mathf.InverseLerp(0.4f, ClubMotions.Tip, z)) * (1f + 0.06f * Mathf.Sin(t * 19f) * swell) * (i == steps ? 0.62f : 1f);
                path.Add(new Vector3(Mathf.Sin(t * 7f) * 0.006f * swell, Mathf.Sin(t * 5f + 1f) * 0.005f * swell, z));
                radii.Add(new Vector2(radius, radius * 0.93f));
            }

            mesh.Tube(wood, path, radii, 8, Vector3.up, WeaponMesh.Hard);
            mesh.Revolve(wood, new[]
            {
                new Vector2(ClubMotions.Back - 0.032f, 0.01f), new Vector2(ClubMotions.Back - 0.022f, 0.027f), new Vector2(ClubMotions.Back - 0.004f, 0.028f),
                new Vector2(ClubMotions.Back + 0.012f, 0.02f)
            }, 8);
            WeaponParts.Grip(mesh, WeaponMaterials.DarkLeather, ClubMotions.Back + 0.01f, 0.1f, 0.0205f, 0.06f, 0.92f);
            mesh.Attach(parent, "Club");
            root.AddComponent<WeaponVisual>();

            return Save(root);
        }

        /// Below the muzzle line, how far the knuckles of the fist on the crossbow's grip point (degrees): the fist holds the
        /// wrist of the stock as a pistol grip, the stock over the thumb and running on past the knuckles, so a level stock
        /// leaves the wrist straight on a forearm that rises to it.
        public const float CrossbowGrip = 40f;

        /// Socket space of the right hand (y = the knuckles, z = the thumb) from the crossbow's own (z = the muzzle, y = the top).
        public static Matrix4x4 CrossbowInHand
        {
            get
            {
                Quaternion turn = Quaternion.AngleAxis(-CrossbowGrip, Vector3.right);

                return Matrix4x4.TRS(turn * new Vector3(0f, 0f, 0.035f), Quaternion.LookRotation(turn * Vector3.up, turn * Vector3.forward), Vector3.one);
            }
        }

        public static GameObject BuildCrossbow()
        {
            const int steps = 12;
            const float span = 0.36f;

            Material wood = WeaponMaterials.DarkWood;
            Material iron = WeaponMaterials.Iron;
            GameObject root = new GameObject("Crossbow");
            WeaponMesh mesh = new WeaponMesh { Matrix = CrossbowInHand };

            // Tiller: slim at the butt, deepest under the lock, with a groove rail on top for the bolt.
            mesh.Loft(wood, new[]
            {
                WeaponParts.BoxRing(-0.18f, new Vector2(0.017f, 0.026f), new Vector2(0f, -0.034f)), WeaponParts.BoxRing(-0.02f, new Vector2(0.02f, 0.03f), new Vector2(0f, -0.026f)),
                WeaponParts.BoxRing(0.2f, new Vector2(0.023f, 0.03f), new Vector2(0f, -0.02f)), WeaponParts.BoxRing(0.48f, new Vector2(0.022f, 0.022f), new Vector2(0f, -0.012f)),
                WeaponParts.BoxRing(0.56f, new Vector2(0.018f, 0.016f), new Vector2(0f, -0.008f))
            }, WeaponMesh.Hard);
            WeaponParts.Box(mesh, WeaponMaterials.Horn, 0.2f, 0.55f, new Vector2(0.006f, 0.002f), new Vector2(0.006f, 0.002f), new Vector2(0f, 0.011f), new Vector2(0f, 0.009f));

            // Steel prod swept back to the nocks, braced by the string.
            List<Vector3> prod = new(steps + 1);
            List<Vector2> radii = new(steps + 1);

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps * 2f - 1f;
                prod.Add(new Vector3(t * span, 0.004f, 0.5f - 0.12f * t * t));
                radii.Add(new Vector2(Mathf.Lerp(0.015f, 0.009f, Mathf.Abs(t)), Mathf.Lerp(0.0065f, 0.004f, Mathf.Abs(t))));
            }

            mesh.Tube(WeaponMaterials.Steel, prod, radii, 6, Vector3.forward, WeaponMesh.Hard);
            mesh.Rod(WeaponMaterials.String, prod[0] + Vector3.back * 0.004f, prod[steps] + Vector3.back * 0.004f, 0.0022f, 0.0022f, 5);
            WeaponParts.Box(mesh, WeaponMaterials.String, 0.47f, 0.53f, new Vector2(0.027f, 0.022f), new Vector2(0.027f, 0.022f), new Vector2(0f, -0.006f), new Vector2(0f, -0.006f), 0.5f);

            // Stirrup, lock nut and the long trigger lever under the tiller.
            List<Vector3> stirrup = new();
            List<Vector2> stirrupRadii = new();

            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                stirrup.Add(new Vector3(Mathf.Cos(angle) * 0.045f, -0.004f, 0.6f + Mathf.Sin(angle) * 0.05f));
                stirrupRadii.Add(Vector2.one * 0.004f);
            }

            mesh.Tube(iron, stirrup, stirrupRadii, 5, Vector3.up, WeaponMesh.Soft, true);
            mesh.Rod(WeaponMaterials.Horn, new Vector3(-0.012f, 0.008f, 0.2f), new Vector3(0.012f, 0.008f, 0.2f), 0.011f, 0.011f, 8);
            mesh.Tube(iron, new[] { new Vector3(0f, -0.05f, 0.19f), new Vector3(0f, -0.066f, 0.1f), new Vector3(0f, -0.07f, -0.08f) },
                new[] { new Vector2(0.004f, 0.005f), new Vector2(0.004f, 0.004f), new Vector2(0.004f, 0.003f) }, 5, Vector3.up);

            mesh.Attach(root.transform, "Crossbow");
            root.AddComponent<WeaponVisual>();

            return Save(root);
        }

        /// The spellbook rides in the left hand socket (x = the back of the hand, y = the fingers, z = the thumb). Open, it
        /// lies on the palm with its spine along the fingers and the pages up; closed, it stands between both hands, which
        /// pinch its sides below the middle: the spine in the book hand, the fore-edge with the brass corners in the other,
        /// across the book from the thumb turned BookThumb down.
        public static GameObject BuildBook()
        {
            Material cover = WeaponMaterials.BookCover;
            Material pages = BattleEditorUtility.GetMaterial("BookPages", new Color(0.85f, 0.8f, 0.65f), 0f, 0.1f);
            Material brass = WeaponMaterials.Brass;
            Material glow = BattleEditorUtility.GetUnlitMaterial("Crystal", new Color(0.5f, 0.75f, 1f, 0.9f));
            GameObject root = new GameObject("Book");
            WeaponMesh open = new WeaponMesh();
            Matrix4x4 palm = Matrix4x4.TRS(new Vector3(-0.04f, -0.16f, 0f), Quaternion.LookRotation(Vector3.up, Vector3.left), Vector3.one);

            // Two boards hinged on the spine, each with its block of pages and brass corners.
            for (int side = -1; side <= 1; side += 2)
            {
                open.Matrix = palm * Matrix4x4.TRS(new Vector3(0f, -0.012f, 0f), Quaternion.Euler(0f, 0f, side * 12f), Vector3.one);
                WeaponParts.Box(open, cover, 0f, 0.24f, new Vector2(0.078f, 0.005f), new Vector2(0.078f, 0.005f), new Vector2(side * 0.082f, 0f), new Vector2(side * 0.082f, 0f));
                WeaponParts.Box(open, pages, 0.008f, 0.232f, new Vector2(0.07f, 0.011f), new Vector2(0.07f, 0.011f), new Vector2(side * 0.076f, 0.016f), new Vector2(side * 0.076f, 0.016f), 0.5f);

                foreach (float z in new[] { 0.004f, 0.212f })
                    WeaponParts.Box(open, brass, z, z + 0.024f, new Vector2(0.014f, 0.0065f), new Vector2(0.014f, 0.0065f), new Vector2(side * 0.148f, 0f), new Vector2(side * 0.148f, 0f));
            }

            open.Matrix = palm;
            WeaponParts.Shaft(open, cover, -0.002f, 0.242f, 0.012f, 0.012f, 8);
            open.Loft(glow, new[]
            {
                WeaponMesh.Ellipse(new Vector3(0f, 0.035f, 0.12f), Vector3.right, Vector3.forward, 0.001f, 0.001f, 6),
                WeaponMesh.Ellipse(new Vector3(0f, 0.06f, 0.12f), Vector3.right, Vector3.forward, 0.022f, 0.022f, 6),
                WeaponMesh.Ellipse(new Vector3(0f, 0.095f, 0.12f), Vector3.right, Vector3.forward, 0.001f, 0.001f, 6)
            }, WeaponMesh.Hard);

            // Shut, pinched at its side a quarter of the way up: the head of the book toward +x, the front cover under the
            // thumb (-y), the fingers round the back cover, the spine in this hand.
            WeaponMesh closed = new WeaponMesh { Matrix = Matrix4x4.Rotate(Quaternion.Euler(0f, -BookThumb, 0f)) };
            const float top = 0.15f;
            const float bottom = -0.05f;
            const float width = 0.17f;
            Vector2 boards = new Vector2((top - bottom) * 0.5f, 0.004f);
            Vector2 middle = new Vector2((top + bottom) * 0.5f, 0.01f);

            foreach (float y in new[] { -0.013f, 0.033f })
                WeaponParts.Box(closed, cover, 0f, width, boards, boards, new Vector2(middle.x, y), new Vector2(middle.x, y));

            Vector2 block = new Vector2(boards.x - 0.006f, 0.019f);
            WeaponParts.Box(closed, pages, 0.006f, width - 0.006f, block, block, middle, middle, 0.5f);
            // The spine sits nearly flush with the boards: one standing out by its radius showed its round side to the eye in
            // the block and read as a book turned sideways.
            closed.Rod(cover, new Vector3(bottom, middle.y, 0.02f), new Vector3(top, middle.y, 0.02f), 0.024f, 0.024f, 10);

            foreach (float x in new[] { bottom + 0.012f, top - 0.012f })
            {
                foreach (float y in new[] { -0.013f, 0.033f })
                    WeaponParts.Box(closed, brass, width - 0.024f, width + 0.002f, new Vector2(0.014f, 0.0065f), new Vector2(0.014f, 0.0065f), new Vector2(x, y), new Vector2(x, y));
            }

            closed.Loft(glow, new[]
            {
                WeaponMesh.Ellipse(new Vector3(middle.x, -0.016f, width * 0.5f), Vector3.right, Vector3.forward, 0.001f, 0.001f, 6),
                WeaponMesh.Ellipse(new Vector3(middle.x, -0.034f, width * 0.5f), Vector3.right, Vector3.forward, 0.022f, 0.022f, 6),
                WeaponMesh.Ellipse(new Vector3(middle.x, -0.052f, width * 0.5f), Vector3.right, Vector3.forward, 0.001f, 0.001f, 6)
            }, WeaponMesh.Hard);

            WeaponVisual visual = root.AddComponent<WeaponVisual>();
            BattleEditorUtility.Set(visual, "_bookOpen", open.Attach(root.transform, "Book", "Open"));
            BattleEditorUtility.Set(visual, "_bookClosed", closed.Attach(root.transform, "BookClosed", "Closed"));
            // The strike of the footage folds the book shut by video frame 7 and opens it again from frame 90 (clip = x0.75).
            BattleEditorUtility.Set(visual, "_attackShut", new Vector2(7f, 90f) * 0.75f / BattleAnimationBuilder.FrameRate);
            BattleEditorUtility.Set(visual, "_equipOpen", DungeonWeaponLibrary.BookEquipOpen);
            BattleEditorUtility.Set(visual, "_unequipShut", DungeonWeaponLibrary.BookUnequipShut);
            visual.SetClosed(false);

            return Save(root);
        }

        /// The Tripo écu, 48 cm across: in its own frame (+Z the face, +Y the top, the fist at the origin) its top rim
        /// 13 cm above the fist and its middle 5 cm to the left, the back of the board's middle 11.5 cm off the hand; turned
        /// in the shield socket (+X along the forearm, +Y the thumb) as the forearm lies in it slanting up to the fist.
        public static GameObject BuildEcu()
        {
            return BattleWeaponPrefabBuilder.BuildTripo("Ecu", "Ecu", EcuTurn, 0.664f, new Vector3(0f, 0.977f, 0.03f), EcuTurn * new Vector3(EcuCenter, EcuTop, EcuGap + EcuBend));
        }

        public static GameObject BuildMagicOrb()
        {
            Material glow = BattleEditorUtility.GetUnlitMaterial("MagicOrb", new Color(0.6f, 0.7f, 1f, 1f));
            GameObject root = new GameObject("MagicOrb");
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Core", root.transform, Vector3.zero, Vector3.zero, Vector3.one * 0.22f, glow);

            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 2.5f;
            DungeonLightLayers.Set(light, DungeonLightLayers.Everything);
            light.shadows = LightShadows.None;

            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.widthCurve = AnimationCurve.Linear(0f, 0.12f, 1f, 0f);
            trail.sharedMaterial = glow;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return Save(root);
        }

        /// Iron straps nailed along both flats of a haft to keep the head on and the wood from being cut.
        private static void Langets(WeaponMesh mesh, Material material, float from, float to, float offset)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                WeaponParts.Box(mesh, material, from, to, new Vector2(0.0018f, 0.007f), new Vector2(0.0018f, 0.008f), new Vector2(side * offset, 0f), new Vector2(side * offset, 0f));

                for (float z = from + 0.03f; z < to - 0.01f; z += 0.07f)
                    WeaponParts.Rivet(mesh, material, new Vector3(side * (offset + 0.0018f), 0f, z), Vector3.right * side, 0.0035f);
            }
        }

        /// The shield socket in the écu's frame, turned into the écu's frame in the socket: +x along the forearm toward the
        /// fingers, +z the back of the hand, toward the bearer.
        private static Quaternion EcuSocket()
        {
            Vector3 slant = Quaternion.Euler(0f, 0f, EcuSlant) * Vector3.right;
            Vector3 forearm = slant * Mathf.Cos(EcuRise * Mathf.Deg2Rad) + Vector3.forward * Mathf.Sin(EcuRise * Mathf.Deg2Rad);
            Vector3 back = Vector3.ProjectOnPlane(Vector3.back, forearm).normalized;

            return Quaternion.Inverse(Quaternion.LookRotation(back, Vector3.Cross(back, forearm)));
        }

        private static GameObject Save(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{BattleEditorUtility.PrefabsFolder}/{root.name}.prefab");
            Object.DestroyImmediate(root);

            return prefab;
        }
    }
}
