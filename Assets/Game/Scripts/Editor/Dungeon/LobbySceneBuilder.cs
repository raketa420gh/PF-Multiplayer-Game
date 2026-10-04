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
    /// Assembles LobbyScene: the tavern with the lobby, skills and stash pages. It runs a local single-player session
    /// that only carries the player's kit and stash; the Start button travels to the chosen gameplay scene.
    internal static class LobbySceneBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/LobbyScene.unity";
        public const string Title = SceneTravel.LobbyTitle;

        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);

            DungeonSceneBuilder.SetupLighting();
            DungeonSceneBuilder.BuildVolume($"{DungeonContentBuilder.ConfigsFolder}/LobbyVolume.asset", 0.6f);
            Camera camera = DungeonSceneBuilder.BuildCamera();
            GameObject system = new GameObject("[System]");
            NetworkEvents events = system.AddComponent<NetworkEvents>();
            SerializedObject so = new SerializedObject(system.AddComponent<BattleBootstrapper>());
            BattleEditorUtility.Set(so, "_gameMode", GameMode.Single);
            BattleEditorUtility.Set(so, "_sessionName", "Lobby");
            BattleEditorUtility.Set(so, "_playerCount", 1);
            so.ApplyModifiedPropertiesWithoutUndo();

            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DungeonContentBuilder.DatabasePath);
            DungeonContext context = system.AddComponent<DungeonContext>();
            so = new SerializedObject(context);
            BattleEditorUtility.Set(so, "_items", database);
            BattleEditorUtility.Set(so, "_classes", DungeonSceneBuilder.LoadClasses());
            BattleEditorUtility.Set(so, "_config", AssetDatabase.LoadAssetAtPath<DungeonConfig>(DungeonContentBuilder.DungeonConfigPath));
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(system.AddComponent<LobbyDirector>());
            BattleEditorUtility.Set(so, "_networkEvents", events);
            BattleEditorUtility.Set(so, "_sessionPrefab", DungeonSceneBuilder.LoadNetworkObject("PlayerSession"));
            so.ApplyModifiedPropertiesWithoutUndo();

            DungeonUiBuilder.BuildLobbyScene(new DungeonUiBuilder.Inputs
            {
                Context = context,
                Database = database,
                Camera = camera,
                PreviewRig = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab("PreviewRig")),
                PieceSet = AssetDatabase.LoadAssetAtPath<ArmorPieceSetConfig>($"{DungeonContentBuilder.ConfigsFolder}/ArmorPieces.asset"),
                FloorMaps = new[] { AssetDatabase.LoadAssetAtPath<Texture2D>($"{DungeonMinimapBuilder.Folder}/Floor1.png") },
                ModuleNames = new string[0],
                Title = Title
            });
            DungeonSceneBuilder.BuildAudio(system, context);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"[{nameof(LobbySceneBuilder)}] Scene built: {ScenePath}");
        }

        /// The tavern is where the game starts: it goes first in the build.
        private static void AddToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (scenes.Length > 0 && scenes[0].path == ScenePath)
                return;

            EditorBuildSettings.scenes = scenes.Where(scene => scene.path != ScenePath).Prepend(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
        }
    }
}
