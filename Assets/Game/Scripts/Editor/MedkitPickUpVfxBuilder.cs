using Game.Scripts.GameObjects.Content;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    public static class MedkitPickUpVfxBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/MedkitPickUp.prefab";
        private const string MaterialPath = "Assets/Game/Materials/MedkitPickUpVfx.mat";
        private const string VfxName = "PickUpVfx";

        [MenuItem("Tools/Game/Build Medkit PickUp VFX")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

            Transform existing = root.transform.Find(VfxName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject go = new GameObject(VfxName);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Setup(ps);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetOrCreateMaterial();

            SerializedObject view = new SerializedObject(root.GetComponent<MedkitPickUpView>());
            view.FindProperty("_vfx").objectReferenceValue = ps;
            view.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[{nameof(MedkitPickUpVfxBuilder)}] VFX built in {PrefabPath}");
        }

        private static void Setup(ParticleSystem ps)
        {
            Color green = new Color(0.3f, 1f, 0.4f, 1f);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(green, Color.white);
            main.gravityModifier = -0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.3f;

            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = fade;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.5f;
            limit.dampen = 0.15f;
        }

        private static Material GetOrCreateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_BLENDMODE_ADD");
            material.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));

            AssetDatabase.CreateAsset(material, MaterialPath);

            return material;
        }
    }
}
