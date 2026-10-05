using System.Linq;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Editor.Dungeon
{
    /// Assembles CharacterSelectScene, where the game starts: the account's character slots and the creation page in front
    /// of the tavern backdrop. No network session runs here; Enter travels to LobbyScene with the picked slot.
    internal static class CharacterSelectSceneBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/CharacterSelectScene.unity";

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
            ClassConfig[] classes = DungeonSceneBuilder.LoadClasses();
            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DungeonContentBuilder.DatabasePath);

            DungeonContext context = system.AddComponent<DungeonContext>();
            SerializedObject so = new SerializedObject(context);
            BattleEditorUtility.Set(so, "_items", database);
            BattleEditorUtility.Set(so, "_classes", classes);
            BattleEditorUtility.Set(so, "_config", AssetDatabase.LoadAssetAtPath<DungeonConfig>(DungeonContentBuilder.DungeonConfigPath));
            so.ApplyModifiedPropertiesWithoutUndo();

            DungeonUiBuilder.BuildCharacterSelectScene(new DungeonUiBuilder.Inputs
            {
                Context = context,
                Database = database,
                Camera = camera,
                PreviewRig = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab("PreviewRig")),
                PieceSet = AssetDatabase.LoadAssetAtPath<ArmorPieceSetConfig>($"{DungeonContentBuilder.ConfigsFolder}/ArmorPieces.asset"),
                FloorMaps = new Texture2D[0],
                ModuleNames = new string[0],
                Title = SceneTravel.CharacterSelectTitle
            }, classes);
            DungeonSceneBuilder.BuildAudio(system, context);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            OrderBuildSettings();
            Debug.Log($"[{nameof(CharacterSelectSceneBuilder)}] Scene built: {ScenePath}");
        }

        /// The game starts at the character select, the tavern comes right after it.
        internal static void OrderBuildSettings()
        {
            string[] first = new[] { ScenePath, LobbySceneBuilder.ScenePath }.Where(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null).ToArray();
            EditorBuildSettings.scenes = first.Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(EditorBuildSettings.scenes.Where(scene => !first.Contains(scene.path))).ToArray();
        }
    }
}
