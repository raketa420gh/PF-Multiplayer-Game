using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Dungeon
{
    /// Moves the local player between the tavern, the dungeon and the test ground. Every scene runs its own Fusion session;
    /// the loading screens of both scenes cover the trip.
    public static class SceneTravel
    {
        public const string CharacterSelectScene = "CharacterSelectScene";
        public const string LobbyScene = "LobbyScene";
        public const string DungeonScene = "DungeonScene";
        public const string SandboxScene = "BattleScene";
        public const string LobbyTitle = "The Tavern";
        public const string CharacterSelectTitle = "Characters";

        /// Destination title, status line and 0..1 progress of the trip, raised from the moment it starts.
        public static event Action<string, string, float> OnProgress;

        public static bool IsTraveling => s_isTraveling;

        private static bool s_isTraveling;

        public static async void Load(NetworkRunner runner, string scene, string title)
        {
            if (s_isTraveling)
                return;

            s_isTraveling = true;
            OnProgress?.Invoke(title, "Registering for the game...", 0f);

            try
            {
                if (runner != null)
                {
                    GameObject host = runner.gameObject;

                    if (!runner.IsShutdown)
                        await runner.Shutdown(false);

                    // Fusion keeps the runner object alive across scene loads; hand it back so it leaves with its scene.
                    if (host != null)
                        SceneManager.MoveGameObjectToScene(host, SceneManager.GetActiveScene());
                }

                AsyncOperation operation = SceneManager.LoadSceneAsync(scene);

                while (operation != null && !operation.isDone)
                {
                    OnProgress?.Invoke(title, "Loading...", operation.progress);
                    await Task.Yield();
                }
            }
            finally
            {
                s_isTraveling = false;
            }
        }
    }
}
