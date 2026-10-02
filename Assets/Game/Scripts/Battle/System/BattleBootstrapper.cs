using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Battle
{
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
            NetworkRunner networkRunner = gameObject.AddComponent<NetworkRunner>();

            NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

            StartGameResult result = await networkRunner.StartGame(new StartGameArgs
            {
                GameMode = _gameMode,
                SessionName = _sessionName,
                PlayerCount = _playerCount,
                Scene = sceneInfo,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
                Debug.Log($"Battle started as {networkRunner.GameMode}");
            else
                Debug.LogError($"Battle failed to start: {result.ShutdownReason}");
        }
    }
}
