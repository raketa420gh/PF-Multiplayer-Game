using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Tavern scene host: gives every player the session object that carries the kit, the stash and the build.
    public sealed class LobbyDirector : MonoBehaviour
    {
        [SerializeField]
        private NetworkEvents _networkEvents;

        [SerializeField]
        private NetworkObject _sessionPrefab;

        private void OnEnable()
        {
            _networkEvents.PlayerJoined.AddListener(OnPlayerJoined);
            _networkEvents.PlayerLeft.AddListener(OnPlayerLeft);
        }

        private void OnDisable()
        {
            _networkEvents.PlayerJoined.RemoveListener(OnPlayerJoined);
            _networkEvents.PlayerLeft.RemoveListener(OnPlayerLeft);
        }

        private void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
                return;

            NetworkObject session = runner.Spawn(_sessionPrefab, Vector3.zero, Quaternion.identity, player);
            runner.SetPlayerObject(player, session);
        }

        private void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer && runner.TryGetPlayerObject(player, out NetworkObject session))
                runner.Despawn(session);
        }
    }
}
