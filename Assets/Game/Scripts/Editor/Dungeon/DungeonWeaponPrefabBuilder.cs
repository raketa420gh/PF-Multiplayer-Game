using System.Collections.Generic;
using Game.Scripts.Battle;
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

        /// The écu in its own frame, the fist at the origin: 48 by 60 cm, the top rim 13 cm above the fist, the middle
        /// 5 cm to its left.
        private const float EcuHalfWidth = 0.24f;
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
        /// How far down the sides run straight before they curve in to the point.
        private const float EcuShoulder = 0.3f;

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

        public static GameObject BuildMorningStar()
        {
            const float radius = 0.058f;
            const int spikes = 18;

            Material iron = WeaponMaterials.Iron;
            Vector3 center = new Vector3(0f, 0f, 0.7f);
            GameObject root = new GameObject("MorningStar");
            WeaponMesh mesh = new WeaponMesh();

            mesh.Revolve(WeaponMaterials.DarkWood, new[]
            {
                new Vector2(-0.2f, 0.013f), new Vector2(-0.185f, 0.02f), new Vector2(-0.155f, 0.0165f), new Vector2(0.35f, 0.0155f), new Vector2(0.66f, 0.019f)
            }, 10, 0.9f);
            WeaponParts.Grip(mesh, WeaponMaterials.Leather, -0.045f, 0.11f, 0.0175f, 0.05f, 0.9f);
            WeaponParts.Band(mesh, iron, 0.57f, 0.021f, 0.008f);
            WeaponParts.Band(mesh, iron, 0.635f, 0.0225f, 0.012f);
            mesh.Revolve(iron, WeaponParts.Ball(center.z, radius, 1f, 10), 14);

            // Spikes spread evenly over the ball; the one on top is the longest.
            for (int i = 0; i < spikes; i++)
            {
                float y = 1f - (i + 0.5f) * 2f / spikes;
                float ring = Mathf.Sqrt(1f - y * y);
                float angle = i * Mathf.PI * (3f - Mathf.Sqrt(5f));
                Vector3 direction = new Vector3(Mathf.Cos(angle) * ring, Mathf.Sin(angle) * ring, y);

                if (direction.z < -0.75f)
                    continue;

                WeaponParts.Spike(mesh, iron, center + direction * (radius - 0.006f), center + direction * (radius + 0.048f), 0.011f);
            }

            WeaponParts.Spike(mesh, iron, center + Vector3.forward * (radius - 0.006f), center + Vector3.forward * (radius + 0.075f), 0.013f);
            mesh.Attach(root.transform, "MorningStar");
            root.AddComponent<WeaponVisual>();

            return Save(root);
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
            light.shadows = LightShadows.None;
            root.AddComponent<WeaponVisual>();

            return Save(root);
        }

        public static GameObject BuildCrossbow()
        {
            const int steps = 12;
            const float span = 0.36f;

            Material wood = WeaponMaterials.DarkWood;
            Material iron = WeaponMaterials.Iron;
            GameObject root = new GameObject("Crossbow");
            WeaponMesh mesh = new WeaponMesh();

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
        /// hold its top corners: the spine in the book hand, the fore-edge with the brass corners in the other, along z.
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

            // Shut, held from above at its top corners: the head of the book toward the back of the hand (+x), the front
            // cover under the thumb (-y), the fingers round the back cover, the spine in this hand.
            WeaponMesh closed = new WeaponMesh();
            const float top = 0.06f;
            const float bottom = -0.18f;
            const float width = 0.17f;
            Vector2 boards = new Vector2((top - bottom) * 0.5f, 0.004f);
            Vector2 middle = new Vector2((top + bottom) * 0.5f, 0.01f);

            foreach (float y in new[] { -0.013f, 0.033f })
                WeaponParts.Box(closed, cover, 0f, width, boards, boards, new Vector2(middle.x, y), new Vector2(middle.x, y));

            Vector2 block = new Vector2(boards.x - 0.006f, 0.019f);
            WeaponParts.Box(closed, pages, 0.006f, width - 0.006f, block, block, middle, middle, 0.5f);
            closed.Rod(cover, new Vector3(bottom, middle.y, 0.004f), new Vector3(top, middle.y, 0.004f), 0.026f, 0.026f, 10);

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
            visual.SetClosed(false);

            return Save(root);
        }

        /// The écu, a heater shield: a flat top, straight sides that curve in to a point, the board bent across, a brass
        /// rim. It is drawn in its own frame (+Z the face, +Y the top, the fist at the origin, high under the top rim and
        /// right of the middle) and turned in the shield socket (+X along the forearm, +Y the thumb), as the forearm lies
        /// in it slanting from the lower left up to the fist.
        public static GameObject BuildEcu()
        {
            Material brass = WeaponMaterials.Brass;
            Material face = WeaponMaterials.EcuFace;
            Material inside = WeaponMaterials.EcuInside;
            GameObject root = new GameObject("Ecu");
            WeaponMesh mesh = new WeaponMesh { Matrix = Matrix4x4.Rotate(EcuTurn) };
            const int across = 14;
            const int down = 18;

            for (int side = 0; side < 2; side++)
            {
                bool isFront = side == 0;

                for (int i = 0; i < across; i++)
                {
                    for (int j = 0; j < down; j++)
                    {
                        Vector3 a = EcuPoint((float)i / across, (float)j / down, isFront);
                        Vector3 b = EcuPoint((i + 1f) / across, (float)j / down, isFront);
                        Vector3 c = EcuPoint((i + 1f) / across, (j + 1f) / down, isFront);
                        Vector3 d = EcuPoint((float)i / across, (j + 1f) / down, isFront);
                        Vector2 ua = EcuUv((float)i / across, (float)j / down, isFront);
                        Vector2 ub = EcuUv((i + 1f) / across, (float)j / down, isFront);
                        Vector2 uc = EcuUv((i + 1f) / across, (j + 1f) / down, isFront);
                        Vector2 ud = EcuUv((float)i / across, (j + 1f) / down, isFront);
                        Vector3 outward = isFront ? Vector3.forward : Vector3.back;
                        mesh.Triangle(isFront ? face : inside, a, b, c, ua, ub, uc, outward, WeaponMesh.Soft);
                        mesh.Triangle(isFront ? face : inside, a, c, d, ua, uc, ud, outward, WeaponMesh.Soft);
                    }
                }
            }

            // The binding round the outline: down the left side to the point, up the right one and back along the top.
            List<Vector3> rim = new();
            List<Vector2> rimRadii = new();

            for (int j = 0; j < down; j++)
                rim.Add(EcuEdge(0f, (float)j / down));

            for (int j = down; j > 0; j--)
                rim.Add(EcuEdge(1f, (float)j / down));

            for (int i = across; i > 0; i--)
                rim.Add(EcuEdge((float)i / across, 0f));

            foreach (Vector3 _ in rim)
                rimRadii.Add(new Vector2(0.012f, 0.014f));

            mesh.Tube(brass, rim, rimRadii, 6, Vector3.forward, WeaponMesh.Soft, true);

            // Domed nails through the binding, on the face and on the inside: six along the top, three down each side.
            for (int i = 1; i < 7; i++)
            {
                WeaponParts.Rivet(mesh, brass, EcuPoint(i / 7f, 0.03f, true), Vector3.forward, 0.007f);
                WeaponParts.Rivet(mesh, brass, EcuPoint(i / 7f, 0.03f, false), Vector3.back, 0.007f);
            }

            for (int j = 1; j < 4; j++)
            {
                foreach (float u in new[] { 0.035f, 0.965f })
                {
                    WeaponParts.Rivet(mesh, brass, EcuPoint(u, j * 0.12f, true), Vector3.forward, 0.007f);
                    WeaponParts.Rivet(mesh, brass, EcuPoint(u, j * 0.12f, false), Vector3.back, 0.007f);
                }
            }

            // Inside: the leather pad under the line of the forearm and the strap the fist closes on, across it.
            Vector3 slant = Quaternion.Euler(0f, 0f, EcuSlant) * Vector3.right;
            Vector3 strapAcross = Vector3.Cross(Vector3.forward, slant);
            Vector3 pad = slant * -0.17f;
            mesh.Matrix = Matrix4x4.Rotate(EcuTurn) * Matrix4x4.TRS(new Vector3(pad.x, pad.y, EcuBack(pad.x) - 0.004f), Quaternion.LookRotation(slant, Vector3.forward), Vector3.one);
            WeaponParts.Box(mesh, WeaponMaterials.Hide, -0.12f, 0.12f, new Vector2(0.055f, 0.004f), new Vector2(0.055f, 0.004f));
            mesh.Matrix = Matrix4x4.Rotate(EcuTurn);
            float strap = EcuBack(0f);
            mesh.Tube(WeaponMaterials.Hide, new[]
            {
                strapAcross * -0.07f + Vector3.forward * (strap - 0.002f), strapAcross * -0.04f + Vector3.forward * (strap - 0.03f),
                strapAcross * 0.04f + Vector3.forward * (strap - 0.03f), strapAcross * 0.07f + Vector3.forward * (strap - 0.002f)
            }, new[] { new Vector2(0.016f, 0.003f), new Vector2(0.016f, 0.003f), new Vector2(0.016f, 0.003f), new Vector2(0.016f, 0.003f) }, 6, Vector3.back);

            mesh.Attach(root.transform, "Ecu");
            root.AddComponent<WeaponVisual>();

            return Save(root);
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

        /// A point of the écu's face (or of its back) at u across, v from the top to the point.
        private static Vector3 EcuPoint(float u, float v, bool isFront)
        {
            Vector3 edge = EcuEdge(u, v);

            return new Vector3(edge.x, edge.y, EcuBack(edge.x) + (isFront ? EcuThickness : 0f));
        }

        private static Vector3 EcuEdge(float u, float v)
        {
            float s = Mathf.Clamp01((v - EcuShoulder) / (1f - EcuShoulder));
            float halfWidth = EcuHalfWidth * (1f - s * s);
            float x = EcuCenter + (u * 2f - 1f) * halfWidth;

            return new Vector3(x, EcuTop - v * EcuHeight, EcuBack(x) + EcuThickness * 0.5f);
        }

        /// The board is bent across: its middle stands out from the arm.
        private static float EcuBack(float x)
        {
            float t = (x - EcuCenter) / EcuHalfWidth;

            return EcuGap + EcuBend * (1f - t * t);
        }

        /// The painted face is stretched once over the shield's box; the back tiles with the wood.
        private static Vector2 EcuUv(float u, float v, bool isFront)
        {
            Vector3 point = EcuEdge(u, v);
            Vector2 box = new Vector2((point.x - EcuCenter) / (EcuHalfWidth * 2f) + 0.5f, (point.y - EcuTop) / EcuHeight + 1f);

            return isFront ? box : new Vector2(point.x, point.y) / WeaponMesh.TileSize;
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
