using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Tavern scene host: gives every player the session object that carries the kit, the stash and the build. In a party the
    /// tavern is the party session; losing its host reconnects the members, one of them takes over.
    public sealed class LobbyDirector : MonoBehaviour
    {
        [SerializeField]
        private NetworkEvents _networkEvents;

        [SerializeField]
        private NetworkObject _sessionPrefab;

        private bool _isJoined;

        private void OnEnable()
        {
            _networkEvents.PlayerJoined.AddListener(OnPlayerJoined);
            _networkEvents.PlayerLeft.AddListener(OnPlayerLeft);
            _networkEvents.OnShutdown.AddListener(OnShutdown);
        }

        private void OnDisable()
        {
            _networkEvents.PlayerJoined.RemoveListener(OnPlayerJoined);
            _networkEvents.PlayerLeft.RemoveListener(OnPlayerLeft);
            _networkEvents.OnShutdown.RemoveListener(OnShutdown);
        }

        private void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            _isJoined |= player == runner.LocalPlayer;

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

        private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            if (SceneTravel.IsTraveling || !PartyService.IsInParty || !_isJoined)
                return;

            NetworkLaunch.Notice = "The party host left, reconnecting...";
            SceneTravel.Load(runner, SceneTravel.LobbyScene, SceneTravel.LobbyTitle);
        }
    }
}
