using Game.Scripts.Editor.Battle;
using UnityEditor;

namespace Game.Scripts.Editor.Dungeon
{
    internal static class DungeonBuildMenu
    {
        [MenuItem("Tools/Game/Dungeon/Build All")]
        public static void BuildAll()
        {
            BattleAnimationBuilder.Build();
            BattleAudioBuilder.Build();
            DungeonAudioBuilder.Build();
            DungeonTextureBuilder.Build();
            WeaponTextureBuilder.Build();
            DungeonUiSpriteBuilder.Build();
            BattleContentBuilder.Build();
            DungeonContentBuilder.Build();
            CombatSoundBuilder.Build();
            DungeonSceneBuilder.Build();
            HubSceneBuilder.Build();
            CharacterSelectSceneBuilder.Build();
        }

        [MenuItem("Tools/Game/Dungeon/Build Audio")]
        public static void BuildAudio()
        {
            BattleAudioBuilder.Build();
            DungeonAudioBuilder.Build();
        }

        [MenuItem("Tools/Game/Dungeon/Build Textures")]
        public static void BuildTextures()
        {
            DungeonTextureBuilder.Build();
            WeaponTextureBuilder.Build();
            DungeonUiSpriteBuilder.Build();
        }

        [MenuItem("Tools/Game/Dungeon/Build Battle Content")]
        public static void BuildBattleContent()
        {
            BattleContentBuilder.Build();
        }

        [MenuItem("Tools/Game/Dungeon/Build Kit")]
        public static void BuildKit()
        {
            DungeonKitBuilder.Build();
            DungeonMedievalBuilder.Build();
            DungeonVillageKitBuilder.Build();
            DungeonVegetationBuilder.Build();
            VillageArchitectureBuilder.BuildPieces();
        }

        [MenuItem("Tools/Game/Dungeon/Build Content")]
        public static void BuildContent()
        {
            DungeonContentBuilder.Build();
        }

        [MenuItem("Tools/Game/Dungeon/Build Scene")]
        public static void BuildScene()
        {
            DungeonSceneBuilder.Build();
        }

        [MenuItem("Tools/Game/Dungeon/Build Hub Scene")]
        public static void BuildHubScene()
        {
            HubSceneBuilder.Build();
        }

        /// The whole hub from the mockup export: content it relies on, the blockout prefab (overwritten) and the scene.
        [MenuItem("Tools/Game/Dungeon/Hub/Rebuild Hub From Mockup")]
        public static void RebuildHub()
        {
            DungeonContentBuilder.BuildHubContent();
            HubBlockoutBuilder.Build();
            HubSceneBuilder.Build();
            CharacterSelectSceneBuilder.Build();
        }

        [MenuItem("Tools/Game/Dungeon/Build Character Select Scene")]
        public static void BuildCharacterSelectScene()
        {
            CharacterSelectSceneBuilder.Build();
        }
    }
}
