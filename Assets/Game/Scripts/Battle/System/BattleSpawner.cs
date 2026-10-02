using System;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class BattleSpawner : MonoBehaviour
    {
        [Serializable]
        private sealed class BotSpawn
        {
            public Transform Point;
            public int WeaponSlot;
            public BotMode Mode;
        }

        [Serializable]
        private sealed class DummySpawn
        {
            public Transform Point;
            public NetworkObject Prefab;
        }

        [SerializeField]
        private NetworkEvents _networkEvents;

        [SerializeField]
        private NetworkObject _fighterPrefab;

        [SerializeField]
        private NetworkObject _botPrefab;

        [SerializeField]
        private Transform[] _playerPoints;

        [SerializeField]
        private BotSpawn[] _bots;

        [SerializeField]
        private DummySpawn[] _dummies;

        private bool _isWorldSpawned;

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

        private void SpawnWorld(NetworkRunner runner)
        {
            _isWorldSpawned = true;

            foreach (BotSpawn bot in _bots)
            {
                runner.Spawn(_botPrefab, bot.Point.position, bot.Point.rotation, PlayerRef.None,
                    (_, botObject) => botObject.GetComponent<BotBrainComponent>().Setup(bot.WeaponSlot, bot.Mode));
            }

            foreach (DummySpawn dummy in _dummies)
                runner.Spawn(dummy.Prefab, dummy.Point.position, dummy.Point.rotation);
        }

        private void OnPlayerJoined(NetworkRunner runner, PlayerRef playerRef)
        {
            if (!runner.IsServer)
                return;

            if (!_isWorldSpawned)
                SpawnWorld(runner);

            Transform point = _playerPoints[playerRef.AsIndex % _playerPoints.Length];
            NetworkObject fighter = runner.Spawn(_fighterPrefab, point.position, point.rotation, playerRef);
            runner.SetPlayerObject(playerRef, fighter);
        }

        private void OnPlayerLeft(NetworkRunner runner, PlayerRef playerRef)
        {
            if (runner.IsServer && runner.TryGetPlayerObject(playerRef, out NetworkObject fighter))
                runner.Despawn(fighter);
        }
    }
}
