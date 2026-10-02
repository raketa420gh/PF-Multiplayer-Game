using Game.Scripts.Battle;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Extra weapon visuals for the dungeon catalog, built from primitives like the battle weapons.
    internal static class DungeonWeaponPrefabBuilder
    {
        public static GameObject BuildAxe(string name, float handleLength, float headSize)
        {
            Material steel = BattleEditorUtility.GetMaterial("Steel", new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.7f);
            Material wood = BattleEditorUtility.GetMaterial("BowWood", new Color(0.4f, 0.26f, 0.13f));
            GameObject root = new GameObject(name);
            Transform parent = root.transform;

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Handle", parent, new Vector3(0f, 0f, handleLength * 0.5f - 0.15f),
                new Vector3(90f, 0f, 0f), new Vector3(0.035f, handleLength * 0.5f + 0.08f, 0.035f), wood);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Blade", parent, new Vector3(0f, headSize * 0.45f, handleLength - 0.12f),
                Vector3.zero, new Vector3(0.02f, headSize, headSize * 0.9f), steel);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Spike", parent, new Vector3(0f, -0.06f, handleLength - 0.12f),
                Vector3.zero, new Vector3(0.025f, 0.14f, 0.05f), steel);
            AddTrail(parent, handleLength);

            return Save(root);
        }

        public static GameObject BuildMace()
        {
            Material steel = BattleEditorUtility.GetMaterial("DarkSteel", new Color(0.25f, 0.25f, 0.28f), 0.8f, 0.5f);
            Material leather = BattleEditorUtility.GetMaterial("Leather", new Color(0.25f, 0.15f, 0.08f));
            GameObject root = new GameObject("Mace");
            Transform parent = root.transform;

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Handle", parent, new Vector3(0f, 0f, 0.22f),
                new Vector3(90f, 0f, 0f), new Vector3(0.035f, 0.3f, 0.035f), leather);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Head", parent, new Vector3(0f, 0f, 0.6f), Vector3.zero, Vector3.one * 0.14f, steel);

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Flange" + i, parent, new Vector3(0f, 0f, 0.6f),
                    new Vector3(0f, 0f, angle), new Vector3(0.03f, 0.2f, 0.12f), steel);
            }

            AddTrail(parent, 0.7f);

            return Save(root);
        }

        public static GameObject BuildSpear()
        {
            Material steel = BattleEditorUtility.GetMaterial("Steel", new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.7f);
            Material wood = BattleEditorUtility.GetMaterial("BowWood", new Color(0.4f, 0.26f, 0.13f));
            GameObject root = new GameObject("Spear");
            Transform parent = root.transform;

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Shaft", parent, new Vector3(0f, 0f, 0.65f),
                new Vector3(90f, 0f, 0f), new Vector3(0.03f, 1.15f, 0.03f), wood);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Head", parent, new Vector3(0f, 0f, 1.85f),
                new Vector3(0f, 0f, 45f), new Vector3(0.04f, 0.04f, 0.3f), steel);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Blade", parent, new Vector3(0f, 0f, 1.78f),
                Vector3.zero, new Vector3(0.012f, 0.08f, 0.4f), steel);
            AddTrail(parent, 2f);

            return Save(root);
        }

        public static GameObject BuildStaff()
        {
            Material wood = BattleEditorUtility.GetMaterial("DarkWood", new Color(0.22f, 0.14f, 0.08f));
            Material crystal = BattleEditorUtility.GetUnlitMaterial("Crystal", new Color(0.5f, 0.75f, 1f, 0.9f));
            GameObject root = new GameObject("Staff");
            Transform parent = root.transform;

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Shaft", parent, new Vector3(0f, 0f, 0.5f),
                new Vector3(90f, 0f, 0f), new Vector3(0.04f, 0.85f, 0.04f), wood);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Crystal", parent, new Vector3(0f, 0f, 1.4f), Vector3.zero, Vector3.one * 0.12f, crystal);

            Light light = BattleEditorUtility.CreateChild("Glow", parent, new Vector3(0f, 0f, 1.4f)).AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.5f, 0.75f, 1f);
            light.range = 3f;
            light.intensity = 1.2f;
            light.shadows = LightShadows.None;
            AddTrail(parent, 1.35f);

            return Save(root);
        }

        public static GameObject BuildTorch()
        {
            Material wood = BattleEditorUtility.GetMaterial("DarkWood", new Color(0.22f, 0.14f, 0.08f));
            Material fire = DungeonPropBuilder.Fire;
            GameObject root = new GameObject("Torch");
            Transform parent = root.transform;

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Handle", parent, new Vector3(0f, 0f, 0.22f),
                new Vector3(90f, 0f, 0f), new Vector3(0.035f, 0.3f, 0.035f), wood);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Wrap", parent, new Vector3(0f, 0f, 0.5f),
                new Vector3(90f, 0f, 0f), new Vector3(0.06f, 0.06f, 0.06f), wood);

            ParticleSystem flame = BattleEditorUtility.CreateChild("Flame", parent, new Vector3(0f, 0f, 0.58f)).AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = flame.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.2f), new Color(1f, 0.3f, 0.05f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            ParticleSystem.EmissionModule emission = flame.emission;
            emission.rateOverTime = 45f;
            ParticleSystem.ShapeModule shape = flame.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.03f;
            ParticleSystem.ColorOverLifetimeModule color = flame.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            ParticleSystem.VelocityOverLifetimeModule velocity = flame.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(1.2f);
            ParticleSystemRenderer renderer = flame.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = fire;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flame.transform.rotation = Quaternion.LookRotation(Vector3.up);

            return Save(root);
        }

        public static GameObject BuildCrossbow()
        {
            Material wood = BattleEditorUtility.GetMaterial("BowWood", new Color(0.4f, 0.26f, 0.13f));
            Material steel = BattleEditorUtility.GetMaterial("DarkSteel", new Color(0.25f, 0.25f, 0.28f), 0.8f, 0.5f);
            GameObject root = new GameObject("Crossbow");
            Transform parent = root.transform;

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Stock", parent, new Vector3(0f, -0.02f, 0.2f),
                Vector3.zero, new Vector3(0.05f, 0.07f, 0.7f), wood);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Prod", parent, new Vector3(0f, 0.01f, 0.5f),
                Vector3.zero, new Vector3(0.7f, 0.025f, 0.03f), steel);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "String", parent, new Vector3(0f, 0.02f, 0.3f),
                Vector3.zero, new Vector3(0.66f, 0.006f, 0.006f), steel);
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

        private static void AddTrail(Transform parent, float tip)
        {
            TrailRenderer trail = BattleEditorUtility.CreateChild("Trail", parent, new Vector3(0f, 0f, tip - 0.05f)).AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.minVertexDistance = 0.02f;
            trail.widthCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 0f);
            trail.sharedMaterial = BattleEditorUtility.GetUnlitMaterial("Trail", new Color(1f, 1f, 1f, 0.6f));
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;
            BattleEditorUtility.Set(parent.gameObject.AddComponent<WeaponVisual>(), "_trail", trail);
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
