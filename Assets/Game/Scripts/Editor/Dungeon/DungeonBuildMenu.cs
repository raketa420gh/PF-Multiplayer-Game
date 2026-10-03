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
            BattleContentBuilder.Build();
            DungeonContentBuilder.Build();
            DungeonSceneBuilder.Build();
        }

        [MenuItem("Tools/Game/Dungeon/Build Audio")]
        public static void BuildAudio()
        {
            DungeonAudioBuilder.Build();
        }

        [MenuItem("Tools/Game/Dungeon/Build Textures")]
        public static void BuildTextures()
        {
            DungeonTextureBuilder.Build();
        }

        [MenuItem("Tools/Game/Dungeon/Build Battle Content")]
        public static void BuildBattleContent()
        {
            BattleContentBuilder.Build();
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
    }
}
