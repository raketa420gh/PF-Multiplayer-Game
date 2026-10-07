using System.Threading.Tasks;
using Fusion;
using Game.Scripts.Dungeon;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Battle
{
    /// Starts the scene's Fusion session: with the launch plan handed over by the previous scene (matchmaking, party, dedicated
    /// server) or with the scene's own defaults.
    public sealed class BattleBootstrapper : MonoBehaviour
    {
        [SerializeField]
        private GameMode _gameMode = GameMode.AutoHostOrClient;

        [SerializeField]
        private string _sessionName = "Battle";

        [SerializeField]
        private int _playerCount = 4;

        private async void Start()
        {
            Scene scene = SceneManager.GetActiveScene();
            NetworkRunner networkRunner = gameObject.AddComponent<NetworkRunner>();
            LaunchPlan plan = NetworkLaunch.Take(scene.name);

            NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(scene.buildIndex));

            StartGameArgs args = new StartGameArgs
            {
                GameMode = _gameMode,
                SessionName = _sessionName,
                PlayerCount = _playerCount,
                Scene = sceneInfo,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            };

            string error = plan != null ? await plan.Start(networkRunner, GetComponent<NetworkEvents>(), args) : await StartDefault(networkRunner, args);

            if (error == null)
            {
                Debug.Log($"Session '{networkRunner.SessionInfo.Name}' started as {networkRunner.GameMode}");

                return;
            }

            Debug.LogError($"Session failed to start: {error}");
            plan?.Fail(networkRunner, error);
        }

        private static async Task<string> StartDefault(NetworkRunner runner, StartGameArgs args)
        {
            StartGameResult result = await runner.StartGame(args);

            return result.Ok ? null : result.ShutdownReason.ToString();
        }
    }
}
