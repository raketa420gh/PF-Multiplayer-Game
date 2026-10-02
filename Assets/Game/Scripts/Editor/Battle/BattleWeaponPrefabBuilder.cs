using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Weapon visuals from primitives. Local axes: +Z blade / stave, +Y leading edge (knuckles), origin at the grip.
    internal static class BattleWeaponPrefabBuilder
    {
        public static GameObject BuildSword(string name, float bladeBase, float bladeTip, float bladeWidth, float guardWidth, float gripBack)
        {
            Material steel = BattleEditorUtility.GetMaterial("Steel", new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.7f);
            Material darkSteel = BattleEditorUtility.GetMaterial("DarkSteel", new Color(0.25f, 0.25f, 0.28f), 0.8f, 0.5f);
            Material leather = BattleEditorUtility.GetMaterial("Leather", new Color(0.25f, 0.15f, 0.08f));

            GameObject root = new GameObject(name);
            Transform parent = root.transform;
            float gripFront = bladeBase - 0.04f;

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Blade", parent, new Vector3(0f, 0f, (bladeBase + bladeTip) * 0.5f),
                Vector3.zero, new Vector3(0.016f, bladeWidth, bladeTip - bladeBase), steel);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Guard", parent, new Vector3(0f, 0f, bladeBase - 0.02f),
                Vector3.zero, new Vector3(0.03f, guardWidth, 0.03f), darkSteel);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Grip", parent, new Vector3(0f, 0f, (gripFront - gripBack) * 0.5f),
                new Vector3(90f, 0f, 0f), new Vector3(0.035f, (gripFront + gripBack) * 0.5f, 0.035f), leather);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Pommel", parent, new Vector3(0f, 0f, -gripBack - 0.02f),
                Vector3.zero, Vector3.one * 0.055f, darkSteel);

            TrailRenderer trail = BattleEditorUtility.CreateChild("Trail", parent, new Vector3(0f, 0f, bladeTip - 0.05f)).AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.minVertexDistance = 0.02f;
            trail.widthCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 0f);
            trail.sharedMaterial = BattleEditorUtility.GetUnlitMaterial("Trail", new Color(1f, 1f, 1f, 0.6f));
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;

            BattleEditorUtility.Set(root.AddComponent<WeaponVisual>(), "_trail", trail);

            return Save(root);
        }

        public static GameObject BuildShield()
        {
            Material wood = BattleEditorUtility.GetMaterial("ShieldWood", new Color(0.2f, 0.32f, 0.55f));
            Material steel = BattleEditorUtility.GetMaterial("Steel", new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.7f);

            GameObject root = new GameObject("Shield");
            Transform parent = root.transform;

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Board", parent, new Vector3(0f, 0f, 0.03f),
                new Vector3(90f, 0f, 0f), new Vector3(0.68f, 0.015f, 0.68f), wood);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Rim", parent, new Vector3(0f, 0f, 0.025f),
                new Vector3(90f, 0f, 0f), new Vector3(0.71f, 0.01f, 0.71f), steel);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Boss", parent, new Vector3(0f, 0f, 0.05f),
                Vector3.zero, new Vector3(0.16f, 0.16f, 0.08f), steel);
            root.AddComponent<WeaponVisual>();

            return Save(root);
        }

        public static GameObject BuildArrow()
        {
            GameObject root = new GameObject("Arrow");
            BuildArrowParts(root.transform);

            return Save(root);
        }

        public static GameObject BuildBow()
        {
            Material wood = BattleEditorUtility.GetMaterial("BowWood", new Color(0.4f, 0.26f, 0.13f));
            GameObject root = new GameObject("Bow");
            Transform parent = root.transform;

            for (int i = -3; i <= 3; i++)
            {
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Stave" + (i + 3), parent,
                    new Vector3(0f, -0.018f * i * i, i * 0.2f),
                    new Vector3(Mathf.Atan(0.18f * i) * Mathf.Rad2Deg, 0f, 0f),
                    new Vector3(0.025f, 0.03f, 0.22f), wood);
            }

            Transform top = BattleEditorUtility.CreateChild("StringTop", parent, new Vector3(0f, -0.2f, 0.69f)).transform;
            Transform bottom = BattleEditorUtility.CreateChild("StringBottom", parent, new Vector3(0f, -0.2f, -0.69f)).transform;
            Transform nock = BattleEditorUtility.CreateChild("NockRest", parent, new Vector3(0f, -0.2f, 0f)).transform;
            Transform arrow = BattleEditorUtility.CreateChild("Arrow", parent, nock.localPosition).transform;
            arrow.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            BuildArrowParts(arrow);

            LineRenderer line = root.AddComponent<LineRenderer>();
            line.positionCount = 3;
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = 0.006f;
            line.sharedMaterial = BattleEditorUtility.GetUnlitMaterial("BowString", Color.white);
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

        private static void BuildArrowParts(Transform parent)
        {
            Material wood = BattleEditorUtility.GetMaterial("ArrowWood", new Color(0.75f, 0.62f, 0.4f));
            Material steel = BattleEditorUtility.GetMaterial("Steel", new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.7f);
            Material feather = BattleEditorUtility.GetMaterial("Feather", new Color(0.85f, 0.2f, 0.15f));

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Shaft", parent, new Vector3(0f, 0f, 0.375f),
                new Vector3(90f, 0f, 0f), new Vector3(0.012f, 0.375f, 0.012f), wood);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Head", parent, new Vector3(0f, 0f, 0.77f),
                new Vector3(0f, 0f, 45f), new Vector3(0.025f, 0.025f, 0.06f), steel);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "FletchA", parent, new Vector3(0f, 0f, 0.08f),
                Vector3.zero, new Vector3(0.003f, 0.05f, 0.1f), feather);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "FletchB", parent, new Vector3(0f, 0f, 0.08f),
                new Vector3(0f, 0f, 90f), new Vector3(0.003f, 0.05f, 0.1f), feather);
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
