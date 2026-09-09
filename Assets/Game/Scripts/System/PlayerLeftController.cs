using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class PlayerLeftController : MonoBehaviour
    {
        [SerializeField]
        private NetworkEvents _networkEvents;

        private void OnEnable()
        {
            _networkEvents.PlayerLeft.AddListener(OnPlayerLeft);
        }

        private void OnDisable()
        {
            _networkEvents.PlayerLeft.RemoveListener(OnPlayerLeft);
        }

        private void OnPlayerLeft(NetworkRunner runner, PlayerRef playerRef)
        {
            if (runner.IsServer && runner.TryGetPlayerObject(playerRef, out NetworkObject character))
            {
                runner.Despawn(character);
            }
        }
    }
}