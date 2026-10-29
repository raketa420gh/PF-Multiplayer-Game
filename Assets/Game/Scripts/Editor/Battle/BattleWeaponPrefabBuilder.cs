using System.Collections.Generic;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    internal enum SwordStyle
    {
        Arming,
        Falchion,
        Longsword,
        Greatsword,
        Rondel,
        Short,
        Rapier,
        Viking,
        Castillon,
        Stiletto
    }

    /// Weapon visuals as generated meshes. Local axes: +Z blade / stave, +Y leading edge (knuckles), origin at the grip.
    internal static class BattleWeaponPrefabBuilder
    {
        private const float BowStep = 0.01f;

        private enum Guard
        {
            Cross,
            Disc,
            Cup
        }

        private enum Pommel
        {
            Wheel,
            Pear,
            Disc,
            Ball,
            Lobed
        }

        private struct SwordShape
        {
            public float Thickness;
            public float Taper;
            public float Point;
            public float Ridge;
            public float Fuller;
            public float FullerLength;
            public Guard Guard;
            public float GuardBend;
            public float GuardThickness;
            public Pommel Pommel;
            public float PommelSize;
            public bool IsSingleEdged;
            public bool HasLugs;
        }

        public static GameObject BuildSword(string name, float bladeBase, float bladeTip, float bladeWidth, float guardWidth, float gripBack, SwordStyle style)
        {
            SwordShape shape = GetShape(style);
            Material steel = WeaponMaterials.Steel;
            Material iron = WeaponMaterials.Iron;
            Material fittings = style == SwordStyle.Rapier ? WeaponMaterials.Brass : iron;
            GameObject root = new GameObject(name);
            WeaponMesh mesh = new WeaponMesh();
            float guardZ = bladeBase - 0.02f;
            float gripFront = guardZ - shape.GuardThickness * 0.5f;
            float halfWidth = bladeWidth * 0.5f;

            if (shape.IsSingleEdged)
            {
                WeaponParts.Cleaver(mesh, steel, guardZ, bladeTip, bladeWidth, shape.Thickness);
            }
            else
            {
                WeaponParts.Blade(mesh, steel, WeaponParts.SwordStations(guardZ, bladeTip, halfWidth, shape.Thickness, shape.Taper, shape.Point),
                    shape.Ridge, shape.Fuller, shape.FullerLength);
            }

            if (shape.HasLugs)
            {
                // Parrying hooks above the ricasso of a two-hander.
                float lugZ = bladeBase + 0.16f;
                Vector2[] lug = { new(halfWidth * 0.8f, lugZ - 0.03f), new(halfWidth + 0.035f, lugZ + 0.012f), new(halfWidth * 0.8f, lugZ + 0.03f) };
                float[] lugThickness = { shape.Thickness, 0f, shape.Thickness };

                for (int side = 0; side < 2; side++)
                {
                    mesh.Matrix = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, side * 180f));
                    mesh.Plate(steel, lug, lugThickness, new Vector2(halfWidth * 0.9f, lugZ), shape.Thickness);
                }

                mesh.Matrix = Matrix4x4.identity;
            }

            switch (shape.Guard)
            {
                case Guard.Disc:
                    Disc(mesh, fittings, guardZ, guardWidth * 0.5f, 0.006f);
                    break;
                case Guard.Cup:
                    mesh.Revolve(fittings, new[]
                    {
                        new Vector2(guardZ - 0.03f, guardWidth * 0.36f), new Vector2(guardZ - 0.012f, guardWidth * 0.33f), new Vector2(guardZ + 0.012f, guardWidth * 0.22f),
                        new Vector2(guardZ + 0.024f, guardWidth * 0.08f), new Vector2(guardZ + 0.026f, 0f)
                    }, 14);
                    WeaponParts.Crossbar(mesh, fittings, guardZ - 0.034f, guardWidth * 0.5f, 0.005f, 0f, 1.8f);
                    KnuckleBow(mesh, fittings, guardZ - 0.034f, -gripBack, guardWidth * 0.36f);
                    break;
                default:
                    WeaponParts.Box(mesh, fittings, guardZ - shape.GuardThickness * 0.5f, guardZ + shape.GuardThickness * 0.5f,
                        new Vector2(shape.Thickness + 0.007f, halfWidth + 0.006f), new Vector2(shape.Thickness + 0.004f, halfWidth + 0.003f));
                    WeaponParts.Crossbar(mesh, fittings, guardZ, guardWidth * 0.5f, shape.GuardThickness * 0.5f, shape.GuardBend, 1.5f);
                    break;
            }

            WeaponParts.Grip(mesh, style == SwordStyle.Rapier ? WeaponMaterials.DarkLeather : WeaponMaterials.Leather, -gripBack, gripFront, 0.0155f);

            // Long two-handed grips are bound with a riser in the middle.
            if (gripBack > 0.15f)
                WeaponParts.Band(mesh, fittings, (gripFront - gripBack) * 0.5f, 0.019f, 0.006f, 0.003f, 0.82f);

            WeaponParts.Band(mesh, fittings, gripFront - 0.004f, 0.018f, 0.004f, 0.002f, 0.82f);
            WeaponParts.Band(mesh, fittings, -gripBack + 0.004f, 0.018f, 0.004f, 0.002f, 0.82f);
            BuildPommel(mesh, fittings, shape, -gripBack);

            mesh.Attach(root.transform, name);
            root.AddComponent<WeaponVisual>();

            return Save(root);
        }

        public static GameObject BuildShield()
        {
            const float radius = 0.34f;
            const float front = 0.036f;
            const float back = 0.02f;
            const float bulge = 0.03f;

            Material iron = WeaponMaterials.Iron;
            Material wood = WeaponMaterials.DarkWood;
            GameObject root = new GameObject("Shield");
            WeaponMesh mesh = new WeaponMesh();

            mesh.Face(WeaponMaterials.ShieldFace, radius, front, bulge, 36, 5, true);
            mesh.Face(wood, radius, back, bulge, 36, 5, false, 5f);

            List<Vector3> rim = new();
            List<Vector2> rimRadii = new();

            for (int i = 0; i < 36; i++)
            {
                float angle = i * Mathf.PI * 2f / 36f;
                rim.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, (front + back) * 0.5f));
                rimRadii.Add(new Vector2(0.011f, 0.014f));
            }

            mesh.Tube(iron, rim, rimRadii, 6, Vector3.forward, WeaponMesh.Soft, true);

            // Boss: a flange nailed to the board and the dome over the fist.
            float bossZ = front + bulge;
            mesh.Revolve(WeaponMaterials.Steel, new[]
            {
                new Vector2(bossZ - 0.012f, 0.088f), new Vector2(bossZ - 0.004f, 0.088f), new Vector2(bossZ, 0.064f), new Vector2(bossZ + 0.022f, 0.056f),
                new Vector2(bossZ + 0.044f, 0.036f), new Vector2(bossZ + 0.054f, 0.014f), new Vector2(bossZ + 0.056f, 0f)
            }, 16);

            for (int i = 0; i < 8; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 2f / 8f;
                WeaponParts.Rivet(mesh, iron, new Vector3(Mathf.Cos(angle) * 0.076f, Mathf.Sin(angle) * 0.076f, bossZ - 0.004f), Vector3.forward, 0.006f);
            }

            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16f;
                float t = 0.3f / radius;
                WeaponParts.Rivet(mesh, iron, new Vector3(Mathf.Cos(angle) * 0.3f, Mathf.Sin(angle) * 0.3f, front + bulge * (1f - t * t)), Vector3.forward, 0.006f);
            }

            // Back: the grip bar across the boss hole, an iron brace and the forearm strap.
            mesh.Tube(wood, new[] { new Vector3(0f, -0.27f, 0.03f), new Vector3(0f, -0.09f, 0.012f), new Vector3(0f, 0.09f, 0.012f), new Vector3(0f, 0.27f, 0.03f) },
                new[] { new Vector2(0.016f, 0.008f), new Vector2(0.014f, 0.013f), new Vector2(0.014f, 0.013f), new Vector2(0.016f, 0.008f) }, 8, Vector3.back);
            mesh.Tube(WeaponMaterials.Hide, new[] { new Vector3(-0.15f, -0.07f, 0.034f), new Vector3(-0.15f, -0.035f, 0.004f), new Vector3(-0.15f, 0.035f, 0.004f), new Vector3(-0.15f, 0.07f, 0.034f) },
                new[] { new Vector2(0.02f, 0.0025f), new Vector2(0.02f, 0.0025f), new Vector2(0.02f, 0.0025f), new Vector2(0.02f, 0.0025f) }, 6, Vector3.back);

            mesh.Attach(root.transform, "Shield");
            root.AddComponent<WeaponVisual>();

            return Save(root);
        }

        public static GameObject BuildArrow()
        {
            GameObject root = new GameObject("Arrow");
            BuildArrowParts(root.transform);

            return Save(root);
        }

        /// The recurve bow of DaD: flat dark limbs bending back from a thick riser and curling forward again at the tips, the string
        /// lying on the curl. Cord wraps on the grip and halfway up each limb, brass collars at the ends of the grip.
        public static GameObject BuildBow()
        {
            const float brace = 0.2f;

            GameObject root = new GameObject("Bow");
            Transform parent = root.transform;
            WeaponMesh mesh = new WeaponMesh();
            List<Vector3> limb = BowLimb(out int contact);
            float scale = (BowHalf(contact * BowStep).y - brace) / limb[contact].y;

            for (int i = 0; i < limb.Count; i++)
                limb[i] = Vector3.Scale(limb[i], new Vector3(1f, scale, 1f));

            BowSweep(mesh, WeaponMaterials.DarkWood, limb, 0, limb.Count - 1, 0f);
            BowSweep(mesh, WeaponMaterials.String, limb, 0, 8, 0.0015f);
            BowSweep(mesh, WeaponMaterials.Brass, limb, 8, 9, 0.0025f);
            BowSweep(mesh, WeaponMaterials.String, limb, 30, 35, 0.0012f);

            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 tip = Vector3.Scale(limb[^1], new Vector3(1f, 1f, side));
                Vector3 end = Vector3.Scale(limb[^2], new Vector3(1f, 1f, side));
                mesh.Rod(WeaponMaterials.Horn, end, tip + (tip - end) * 1.5f, 0.006f, 0.0035f, 6);
            }

            mesh.Attach(parent, "Bow");

            Vector3 contactPoint = limb[contact] - Vector3.up * BowHalf(contact * BowStep).y;
            Transform top = BattleEditorUtility.CreateChild("StringTop", parent, contactPoint).transform;
            Transform bottom = BattleEditorUtility.CreateChild("StringBottom", parent, Vector3.Scale(contactPoint, new Vector3(1f, 1f, -1f))).transform;
            Transform nock = BattleEditorUtility.CreateChild("NockRest", parent, new Vector3(0f, -0.2f, 0f)).transform;
            Transform arrow = BattleEditorUtility.CreateChild("Arrow", parent, nock.localPosition).transform;
            arrow.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            BuildArrowParts(arrow);

            LineRenderer line = root.AddComponent<LineRenderer>();
            line.positionCount = 3;
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = 0.004f;
            line.sharedMaterial = BattleEditorUtility.GetUnlitMaterial("BowString", new Color(0.85f, 0.8f, 0.66f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.SetPositions(new[] { top.localPosition, nock.localPosition, bottom.localPosition });

            SerializedObject visual = new SerializedObject(root.AddComponent<WeaponVisual>());
            BattleEditorUtility.Set(visual, "_string", line);
            BattleEditorUtility.Set(visual, "_stringTop", top);
            BattleEditorUtility.Set(visual, "_stringBottom", bottom);
            BattleEditorUtility.Set(visual, "_nockRest", nock);
            BattleEditorUtility.Set(visual, "_arrow", arrow);
            visual.ApplyModifiedPropertiesWithoutUndo();

            return Save(root);
        }

        /// Centre line of the upper half, one point per centimetre of arc from the grip: straight riser, the limb bending back
        /// toward the string, the tip curling forward again. 'contact' is where the curl turns parallel to the string.
        private static List<Vector3> BowLimb(out int contact)
        {
            List<Vector3> points = new() { Vector3.zero };
            float angle = 0f;
            contact = 0;

            for (int i = 1; i <= 68; i++)
            {
                float s = i * BowStep;
                float previous = angle;
                angle += (s <= 0.1f ? 0f : s <= 0.5f ? 1.9f : -6.5f) * BowStep;
                points.Add(points[^1] + new Vector3(0f, -Mathf.Sin(angle), Mathf.Cos(angle)) * BowStep);

                if (previous > 0f && angle <= 0f)
                    contact = i;
            }

            return points;
        }

        /// Half width across the bow and half thickness in its plane: a deep riser, then flat limbs tapering to the tips.
        private static Vector2 BowHalf(float s)
        {
            float riser = Mathf.InverseLerp(0.2f, 0.09f, s);
            float limb = Mathf.InverseLerp(0.2f, 0.68f, s);

            return Vector2.Lerp(new Vector2(Mathf.Lerp(0.018f, 0.009f, limb), Mathf.Lerp(0.0065f, 0.0042f, limb)), new Vector2(0.015f, 0.017f), riser);
        }

        /// Sweeps the chamfered limb section along stations 'from'..'to' of both halves, grown by 'grow' for wraps and collars.
        private static void BowSweep(WeaponMesh mesh, Material material, IReadOnlyList<Vector3> limb, int from, int to, float grow)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                List<Vector3[]> rings = new(to - from + 1);

                for (int i = from; i <= to; i++)
                {
                    Vector3 tangent = (limb[Mathf.Min(i + 1, limb.Count - 1)] - limb[Mathf.Max(i - 1, 0)]).normalized;
                    Vector3 normal = new Vector3(0f, tangent.z, -tangent.y);
                    Vector3 center = limb[i];
                    Vector3[] ring = WeaponParts.BoxRing(0f, BowHalf(i * BowStep) + Vector2.one * grow, Vector2.zero, 0.35f);

                    for (int j = 0; j < ring.Length; j++)
                        ring[j] = Vector3.Scale(center + Vector3.right * ring[j].x + normal * ring[j].y, new Vector3(1f, 1f, side));

                    rings.Add(ring);
                }

                if (side < 0f)
                    rings.Reverse();

                mesh.Loft(material, rings, WeaponMesh.Hard);
            }
        }

        private static SwordShape GetShape(SwordStyle style)
        {
            SwordShape shape = new SwordShape
            {
                Thickness = 0.0026f, Taper = 0.62f, Point = 0.16f, Ridge = 0.42f, Fuller = 0.5f, FullerLength = 0.62f,
                Guard = Guard.Cross, GuardThickness = 0.014f, Pommel = Pommel.Wheel, PommelSize = 0.024f
            };

            switch (style)
            {
                case SwordStyle.Falchion:
                    shape.IsSingleEdged = true;
                    shape.Thickness = 0.003f;
                    shape.GuardBend = 0.012f;
                    break;
                case SwordStyle.Longsword:
                    shape.Taper = 0.5f;
                    shape.Point = 0.2f;
                    shape.FullerLength = 0.5f;
                    shape.GuardBend = 0.014f;
                    shape.Pommel = Pommel.Pear;
                    break;
                case SwordStyle.Greatsword:
                    shape.Thickness = 0.0032f;
                    shape.Taper = 0.7f;
                    shape.Point = 0.13f;
                    shape.Ridge = 0.06f;
                    shape.Fuller = 0f;
                    shape.GuardBend = 0.02f;
                    shape.GuardThickness = 0.018f;
                    shape.Pommel = Pommel.Pear;
                    shape.PommelSize = 0.03f;
                    shape.HasLugs = true;
                    break;
                case SwordStyle.Rondel:
                    shape.Thickness = 0.0036f;
                    shape.Taper = 0.45f;
                    shape.Point = 0.3f;
                    shape.Ridge = 0.05f;
                    shape.Fuller = 0f;
                    shape.Guard = Guard.Disc;
                    shape.Pommel = Pommel.Disc;
                    break;
                case SwordStyle.Short:
                    shape.Taper = 0.75f;
                    shape.Point = 0.22f;
                    shape.FullerLength = 0.7f;
                    break;
                case SwordStyle.Rapier:
                    shape.Thickness = 0.003f;
                    shape.Taper = 0.4f;
                    shape.Point = 0.12f;
                    shape.Ridge = 0.05f;
                    shape.Fuller = 0f;
                    shape.Guard = Guard.Cup;
                    shape.Pommel = Pommel.Ball;
                    shape.PommelSize = 0.018f;
                    break;
                case SwordStyle.Viking:
                    shape.Taper = 0.8f;
                    shape.Point = 0.12f;
                    shape.Fuller = 0.6f;
                    shape.FullerLength = 0.9f;
                    shape.GuardThickness = 0.018f;
                    shape.Pommel = Pommel.Lobed;
                    break;
                case SwordStyle.Castillon:
                    shape.Thickness = 0.003f;
                    shape.Taper = 0.3f;
                    shape.Point = 0.35f;
                    shape.FullerLength = 0.55f;
                    shape.PommelSize = 0.02f;
                    break;
                case SwordStyle.Stiletto:
                    shape.Thickness = 0.0045f;
                    shape.Taper = 0.45f;
                    shape.Point = 0.3f;
                    shape.Ridge = 0.05f;
                    shape.Fuller = 0f;
                    shape.GuardThickness = 0.01f;
                    shape.Pommel = Pommel.Ball;
                    shape.PommelSize = 0.014f;
                    break;
            }

            return shape;
        }

        private static void BuildPommel(WeaponMesh mesh, Material material, SwordShape shape, float z)
        {
            float size = shape.PommelSize;

            switch (shape.Pommel)
            {
                case Pommel.Pear:
                    mesh.Revolve(material, new[]
                    {
                        new Vector2(z + 0.004f, size * 0.5f), new Vector2(z - size * 0.5f, size * 0.72f), new Vector2(z - size * 1.2f, size),
                        new Vector2(z - size * 1.75f, size * 0.8f), new Vector2(z - size * 2f, size * 0.35f), new Vector2(z - size * 2.1f, 0f)
                    }, 8, 1f, WeaponMesh.Hard);
                    break;
                case Pommel.Disc:
                    Disc(mesh, material, z - 0.006f, size * 1.25f, 0.007f);
                    break;
                case Pommel.Ball:
                    mesh.Revolve(material, WeaponParts.Ball(z - size * 0.8f, size), 10);
                    break;
                case Pommel.Lobed:
                    WeaponParts.Box(mesh, material, z - 0.012f, z, new Vector2(0.011f, size * 1.25f), new Vector2(0.011f, size * 1.25f));

                    for (int i = -1; i <= 1; i++)
                    {
                        float lobe = i == 0 ? size * 0.62f : size * 0.46f;
                        mesh.Matrix = Matrix4x4.Translate(new Vector3(0f, i * size * 0.78f, 0f));
                        mesh.Revolve(material, WeaponParts.Ball(z - 0.012f - lobe * 0.5f, lobe, 1.1f), 8, 0.6f);
                    }

                    mesh.Matrix = Matrix4x4.identity;
                    break;
                default:
                    // A wheel: flat across the blade, with a peened button on the end.
                    mesh.Revolve(material, WeaponParts.Ball(z - size * 0.85f, size), 12, 0.5f);
                    mesh.Revolve(material, WeaponParts.Ball(z - size * 1.85f, 0.005f), 6);
                    break;
            }
        }

        private static void Disc(WeaponMesh mesh, Material material, float z, float radius, float halfThickness)
        {
            mesh.Revolve(material, new[]
            {
                new Vector2(z - halfThickness, 0f), new Vector2(z - halfThickness, radius * 0.9f), new Vector2(z, radius),
                new Vector2(z + halfThickness, radius * 0.9f), new Vector2(z + halfThickness, 0f)
            }, 14, 1f, WeaponMesh.Hard);
        }

        /// Bar arching from the guard over the knuckles to the pommel.
        private static void KnuckleBow(WeaponMesh mesh, Material material, float from, float to, float reach)
        {
            const int steps = 8;
            List<Vector3> path = new(steps + 1);
            List<Vector2> radii = new(steps + 1);

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                path.Add(new Vector3(0f, reach * (0.35f + 0.65f * Mathf.Sin(t * Mathf.PI * 0.9f + 0.3f)), Mathf.Lerp(from, to, t)));
                radii.Add(new Vector2(0.005f, 0.003f));
            }

            mesh.Tube(material, path, radii, 6, Vector3.up);
        }

        private static void BuildArrowParts(Transform parent)
        {
            WeaponMesh mesh = new WeaponMesh();
            Material steel = WeaponMaterials.Steel;

            WeaponParts.Shaft(mesh, WeaponMaterials.Wood, 0f, 0.75f, 0.0042f, 0.0042f, 6);
            WeaponParts.Shaft(mesh, WeaponMaterials.Horn, -0.006f, 0.012f, 0.005f, 0.0046f, 6);
            WeaponParts.Shaft(mesh, steel, 0.735f, 0.765f, 0.0052f, 0.004f, 6);
            WeaponParts.Blade(mesh, steel, new[]
            {
                new Vector3(0.755f, 0.004f, 0.003f), new Vector3(0.775f, 0.012f, 0.003f), new Vector3(0.8f, 0.007f, 0.002f), new Vector3(0.83f, 0f, 0f)
            });
            WeaponParts.Band(mesh, WeaponMaterials.String, 0.03f, 0.0052f, 0.006f, 0.0008f);
            WeaponParts.Band(mesh, WeaponMaterials.String, 0.14f, 0.0052f, 0.006f, 0.0008f);

            Vector2[] vane = { new(0.004f, 0.034f), new(0.02f, 0.044f), new(0.024f, 0.1f), new(0.004f, 0.136f) };
            float[] thickness = { 0.0005f, 0.0005f, 0.0005f, 0.0005f };

            for (int i = 0; i < 3; i++)
            {
                mesh.Matrix = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, i * 120f));
                mesh.Plate(WeaponMaterials.Feather, vane, thickness, new Vector2(0.012f, 0.08f), 0.0005f);
            }

            mesh.Attach(parent, "Arrow");
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
