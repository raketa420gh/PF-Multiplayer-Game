using UnityEditor;

namespace Game.Scripts.Editor.Battle
{
    internal static class BattleBuildMenu
    {
        [MenuItem("Tools/Game/Battle/Build All")]
        public static void BuildAll()
        {
            BattleAnimationBuilder.Build();
            BattleAudioBuilder.Build();
            BattleContentBuilder.Build();
            BattleSceneBuilder.Build();
        }

        [MenuItem("Tools/Game/Battle/Rebuild Animations And Content")]
        public static void BuildAnimationsAndContent()
        {
            BattleAnimationBuilder.Build();
            BattleContentBuilder.Build();
        }
    }
}
