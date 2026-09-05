using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts
{
    public sealed class FusionBootstraper : MonoBehaviour
    {
        private async void Start()
        {
            NetworkRunner networkRunner = gameObject.AddComponent<NetworkRunner>();

            SceneRef sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(sceneRef);

            NetworkSceneManagerDefault sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

            StartGameResult result = await networkRunner.StartGame(new StartGameArgs 
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = "Session",
                PlayerCount = 2,
                Scene = sceneInfo,
                SceneManager = sceneManager 
            });

            if (result.Ok)
                Debug.Log("Game started successfully");
        }
    }
}