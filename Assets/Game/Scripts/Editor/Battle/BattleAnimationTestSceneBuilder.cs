using System.Linq;
using Game.Scripts.Battle;
using Game.Scripts.Editor.Dungeon;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Editor.Battle
{
    /// Animation test ground: the bare character with the fighter controller and every weapon of the catalog, no network session.
    internal static class BattleAnimationTestSceneBuilder
    {
        private const string ScenePath = "Assets/Game/Scenes/AnimationTestScene.unity";

        [MenuItem("Tools/Game/Battle/Build Animation Test Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Light light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            Volume volume = new GameObject("[Volume]").AddComponent<Volume>();
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{DungeonContentBuilder.ConfigsFolder}/SandboxVolume.asset");

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Ground", null, new Vector3(0f, -0.5f, 0f), Vector3.zero, new Vector3(12f, 1f, 12f),
                AssetDatabase.LoadAssetAtPath<Material>($"{BattleEditorUtility.MaterialsFolder}/Ground.mat"));

            GameObject camera = new GameObject("[Camera]") { tag = "MainCamera" };
            camera.AddComponent<Camera>().nearClipPlane = 0.04f;
            camera.AddComponent<AudioListener>();
            camera.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath));
            Animator animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(BattleEditorUtility.ControllerPath);
            animator.applyRootMotion = false;

            SerializedObject so = new SerializedObject(new GameObject("[System]").AddComponent<AnimationTestView>());
            BattleEditorUtility.Set(so, "_animator", animator);
            BattleEditorUtility.Set(so, "_camera", camera.GetComponent<Camera>());
            BattleEditorUtility.Set(so, "_body", AssetDatabase.LoadAssetAtPath<BodyConfig>($"{BattleEditorUtility.ConfigsFolder}/Body.asset"));
            BattleEditorUtility.Set(so, "_sockets", BattlePoseRig.CreateSockets(animator));
            BattleEditorUtility.Set(so, "_weapons", AssetDatabase.FindAssets($"t:{nameof(WeaponConfig)}", new[] { BattleEditorUtility.ConfigsFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<WeaponConfig>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[{nameof(BattleAnimationTestSceneBuilder)}] Scene built: {ScenePath}");
        }
    }
}
