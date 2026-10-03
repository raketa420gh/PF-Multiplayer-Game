using Fusion;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Dungeon
{
    /// Moves the local player between the tavern scene and the test ground; every scene starts its own Fusion session.
    public static class SceneTravel
    {
        public const string LobbyScene = "DungeonScene";
        public const string SandboxScene = "BattleScene";

        private static bool s_isTraveling;

        public static async void Load(NetworkRunner runner, string scene)
        {
            if (s_isTraveling)
                return;

            s_isTraveling = true;

            try
            {
                if (runner != null)
                    await runner.Shutdown(false);
            }
            finally
            {
                s_isTraveling = false;
            }

            SceneManager.LoadScene(scene);
        }
    }
}
