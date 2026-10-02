using System.Linq;
using Fusion;
using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Editor.Dungeon
{
    /// Assembles DungeonScene: dark lighting, Fusion bootstrap, context, director, the two floors and the UI.
    internal static class DungeonSceneBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/DungeonScene.unity";

        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);

            SetupLighting();
            Camera camera = BuildCamera();
            GameObject system = new GameObject("[System]");
            NetworkEvents events = system.AddComponent<NetworkEvents>();
            BattleBootstrapper bootstrapper = system.AddComponent<BattleBootstrapper>();
            SerializedObject so = new SerializedObject(bootstrapper);
            BattleEditorUtility.Set(so, "_sessionName", "Dungeon");
            BattleEditorUtility.Set(so, "_playerCount", 6);
            so.ApplyModifiedPropertiesWithoutUndo();

            BattleInputPolling input = system.AddComponent<BattleInputPolling>();
            BattleEditorUtility.Set(input, "_networkEvents", events);
            BattleContext battle = system.AddComponent<BattleContext>();
            BattleFeedback feedback = BuildFeedback(system.transform);
            so = new SerializedObject(battle);
            BattleEditorUtility.Set(so, "_camera", camera);
            BattleEditorUtility.Set(so, "_input", input);
            BattleEditorUtility.Set(so, "_feedback", feedback);
            so.ApplyModifiedPropertiesWithoutUndo();

            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DungeonContentBuilder.DatabasePath);
            DungeonConfig config = AssetDatabase.LoadAssetAtPath<DungeonConfig>(DungeonContentBuilder.DungeonConfigPath);
            ClassConfig[] classes = AssetDatabase.FindAssets("t:ClassConfig", new[] { DungeonContentBuilder.ClassesFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ClassConfig>(AssetDatabase.GUIDToAssetPath(guid)))
                .OrderBy(c => c.Id)
                .ToArray();

            DungeonDirector director = system.AddComponent<DungeonDirector>();
            so = new SerializedObject(director);
            BattleEditorUtility.Set(so, "_networkEvents", events);
            BattleEditorUtility.Set(so, "_sessionPrefab", LoadNetworkObject("PlayerSession"));
            BattleEditorUtility.Set(so, "_adventurerPrefab", LoadNetworkObject("Adventurer"));
            BattleEditorUtility.Set(so, "_matchPrefab", LoadNetworkObject("Match"));
            BattleEditorUtility.Set(so, "_config", config);
            SerializedProperty monsters = so.FindProperty("_monsters");
            (string name, float weight)[] kinds = { ("SkeletonSwordsman", 1f), ("SkeletonArcher", 0.6f), ("Zombie", 0.8f) };
            monsters.arraySize = kinds.Length;

            for (int i = 0; i < kinds.Length; i++)
            {
                monsters.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab").objectReferenceValue = LoadNetworkObject(kinds[i].name);
                monsters.GetArrayElementAtIndex(i).FindPropertyRelative("Weight").floatValue = kinds[i].weight;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            DungeonContext context = system.AddComponent<DungeonContext>();
            so = new SerializedObject(context);
            BattleEditorUtility.Set(so, "_battle", battle);
            BattleEditorUtility.Set(so, "_items", database);
            BattleEditorUtility.Set(so, "_classes", classes);
            BattleEditorUtility.Set(so, "_config", config);
            BattleEditorUtility.Set(so, "_director", director);
            so.ApplyModifiedPropertiesWithoutUndo();

            DungeonMapBuilder.Build(director);
            DungeonUiBuilder.Build(context, database);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"[{nameof(DungeonSceneBuilder)}] Scene built: {ScenePath}");
        }

        private static void SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.03f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.01f, 0.008f, 0.006f);
            RenderSettings.fogDensity = 0.045f;
            RenderSettings.skybox = null;
            RenderSettings.reflectionIntensity = 0.1f;
            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
        }

        private static Camera BuildCamera()
        {
            GameObject go = new GameObject("[Camera]") { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(new Vector3(0f, 2f, -4f), Quaternion.identity);
            Camera camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.farClipPlane = 120f;
            go.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;

            return camera;
        }

        private static NetworkObject LoadNetworkObject(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(name)).GetComponent<NetworkObject>();
        }

        private static BattleFeedback BuildFeedback(Transform parent)
        {
            GameObject go = BattleEditorUtility.CreateChild("Feedback", parent);
            BattleFeedback feedback = go.AddComponent<BattleFeedback>();
            AudioSource audio = go.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            audio.volume = 0.6f;

            Material particle = BattleEditorUtility.GetUnlitMaterial("HitParticle", Color.white);
            TMPro.TextMeshPro popup = AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.PrefabsFolder + "/DamagePopup.prefab").GetComponent<TMPro.TextMeshPro>();

            SerializedObject so = new SerializedObject(feedback);
            BattleEditorUtility.Set(so, "_hitVfx", BuildVfx(go.transform, "HitVfx", particle, new Color(0.9f, 0.15f, 0.1f), new Color(1f, 0.5f, 0.3f), 16, 3f, 0.06f));
            BattleEditorUtility.Set(so, "_blockVfx", BuildVfx(go.transform, "BlockVfx", particle, new Color(1f, 0.9f, 0.5f), Color.white, 24, 5f, 0.035f));
            BattleEditorUtility.Set(so, "_popupPrefab", popup);
            BattleEditorUtility.Set(so, "_audioSource", audio);
            BattleEditorUtility.Set(so, "_hitClip", AssetDatabase.LoadAssetAtPath<AudioClip>(BattleAudioBuilder.HitPath));
            BattleEditorUtility.Set(so, "_blockClip", AssetDatabase.LoadAssetAtPath<AudioClip>(BattleAudioBuilder.BlockPath));
            BattleEditorUtility.Set(so, "_swingClip", AssetDatabase.LoadAssetAtPath<AudioClip>(BattleAudioBuilder.SwingPath));
            BattleEditorUtility.Set(so, "_shotClip", AssetDatabase.LoadAssetAtPath<AudioClip>(BattleAudioBuilder.ShotPath));
            so.ApplyModifiedPropertiesWithoutUndo();

            return feedback;
        }

        private static ParticleSystem BuildVfx(Transform parent, string name, Material material, Color colorA, Color colorB, int count, float speed, float size)
        {
            ParticleSystem ps = BattleEditorUtility.CreateChild(name, parent).AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 50f;
            shape.radius = 0.03f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            return ps;
        }

        private static void AddToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (scenes.Any(scene => scene.path == ScenePath))
                return;

            EditorBuildSettings.scenes = scenes.Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
        }
    }
}
