using System;
using System.Linq;
using Fusion;
using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Editor.Dungeon
{
    /// Assembles HubScene, the settlement the player walks between runs: the blockout prefab of the mockup under a grey sky,
    /// the Fusion bootstrap of a single-player session (a party plan replaces it), the director that gives every player a
    /// session and a body, and the UI whose pages open from the stations of the blockout.
    internal static class HubSceneBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/HubScene.unity";
        public const string Title = SceneTravel.HubTitle;
        /// Share of physical and magical damage each dummy of the fencing hall ignores, by its spot in the blockout.
        private static readonly (string id, float reduction)[] s_dummies = { ("E2", 0f), ("E3", 0.5f) };

        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Camera camera = BuildAtmosphere();
            GameObject system = new GameObject("[System]");
            NetworkEvents events = system.AddComponent<NetworkEvents>();
            SerializedObject so = new SerializedObject(system.AddComponent<BattleBootstrapper>());
            BattleEditorUtility.Set(so, "_gameMode", GameMode.Single);
            BattleEditorUtility.Set(so, "_sessionName", "Hub");
            BattleEditorUtility.Set(so, "_playerCount", 1);
            so.ApplyModifiedPropertiesWithoutUndo();

            BattleInputPolling input = system.AddComponent<BattleInputPolling>();
            BattleEditorUtility.Set(input, "_networkEvents", events);
            BattleContext battle = system.AddComponent<BattleContext>();
            so = new SerializedObject(battle);
            BattleEditorUtility.Set(so, "_camera", camera);
            BattleEditorUtility.Set(so, "_input", input);
            BattleEditorUtility.Set(so, "_feedback", DungeonSceneBuilder.BuildFeedback(system.transform));
            so.ApplyModifiedPropertiesWithoutUndo();

            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DungeonContentBuilder.DatabasePath);
            DungeonContext context = system.AddComponent<DungeonContext>();
            so = new SerializedObject(context);
            BattleEditorUtility.Set(so, "_battle", battle);
            BattleEditorUtility.Set(so, "_items", database);
            BattleEditorUtility.Set(so, "_classes", DungeonSceneBuilder.LoadClasses());
            BattleEditorUtility.Set(so, "_config", AssetDatabase.LoadAssetAtPath<DungeonConfig>(DungeonContentBuilder.DungeonConfigPath));
            so.ApplyModifiedPropertiesWithoutUndo();

            Transform blockout = ((GameObject)PrefabUtility.InstantiatePrefab(HubBlockoutBuilder.Load())).transform;
            Transform spawns = Child(blockout, HubBlockoutBuilder.Spawns);
            Transform dummies = Child(blockout, HubBlockoutBuilder.Dummies);

            so = new SerializedObject(system.AddComponent<HubDirector>());
            BattleEditorUtility.Set(so, "_networkEvents", events);
            BattleEditorUtility.Set(so, "_sessionPrefab", DungeonSceneBuilder.LoadNetworkObject("PlayerSession"));
            BattleEditorUtility.Set(so, "_adventurerPrefab", DungeonSceneBuilder.LoadNetworkObject("Adventurer"));
            BattleEditorUtility.Set(so, "_dummyPrefab", DungeonSceneBuilder.LoadNetworkObject("TrainingDummy"));
            BattleEditorUtility.Set(so, "_arrivals", Enum.GetNames(typeof(HubArrival)).Select(name => Child(spawns, name)).ToArray());
            BattleEditorUtility.Set(so, "_gate", Child(spawns, "Gate"));
            BattleEditorUtility.Set(so, "_hall", Child(blockout, HubBlockoutBuilder.FencingHall).GetComponent<BoxCollider>());
            SerializedProperty dummyList = so.FindProperty("_dummies");
            dummyList.arraySize = s_dummies.Length;

            for (int i = 0; i < s_dummies.Length; i++)
            {
                dummyList.GetArrayElementAtIndex(i).FindPropertyRelative("Point").objectReferenceValue = Child(dummies, s_dummies[i].id);
                dummyList.GetArrayElementAtIndex(i).FindPropertyRelative("Reduction").floatValue = s_dummies[i].reduction;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            DungeonUiBuilder.BuildHubScene(new DungeonUiBuilder.Inputs
            {
                Context = context,
                Database = database,
                Camera = camera,
                PreviewRig = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab("PreviewRig")),
                PieceSet = AssetDatabase.LoadAssetAtPath<ArmorPieceSetConfig>($"{DungeonContentBuilder.ConfigsFolder}/ArmorPieces.asset"),
                FloorMaps = new[] { AssetDatabase.LoadAssetAtPath<Texture2D>($"{DungeonMinimapBuilder.Folder}/Floor{MatchComponent.EntryFloor}.png") },
                ModuleNames = new string[0],
                Title = Title
            });
            DungeonSceneBuilder.BuildAudio(system, context);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            CharacterSelectSceneBuilder.OrderBuildSettings();
            Debug.Log($"[{nameof(HubSceneBuilder)}] Scene built: {ScenePath}");
        }

        /// The mockup's day: an overcast sky that lights from above and casts soft shadows, grey haze that hides the far slopes,
        /// fires for warmth. Far brighter than the dungeon, so the grade adds no exposure.
        private static Camera BuildAtmosphere()
        {
            Light sun = DungeonSceneBuilder.SetupLighting();
            sun.name = "[Sun]";
            sun.color = new Color(0.74f, 0.77f, 0.83f);
            sun.intensity = 0.9f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(-16f, -60f, 30f));
            RenderSettings.ambientSkyColor = new Color(0.64f, 0.67f, 0.73f) * 0.5f;
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.42f, 0.45f) * 0.5f;
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.15f, 0.13f) * 0.5f;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.275f, 0.278f, 0.31f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 170f;
            DungeonSceneBuilder.BuildVolume($"{DungeonContentBuilder.ConfigsFolder}/HubVolume.asset", 0f);

            Camera camera = DungeonSceneBuilder.BuildCamera();
            camera.backgroundColor = new Color(0.353f, 0.361f, 0.396f);
            camera.farClipPlane = 260f;

            return camera;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);

            return child != null ? child : throw new InvalidOperationException($"{parent.name} has no '{name}': rebuild the blockout (Tools/Game/Dungeon/Hub/Apply Mockup) or restore the child");
        }
    }
}
