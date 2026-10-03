using System;
using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Host-side driver of the gameplay test ground: sessions, naked adventurer (re)spawns, the weapon table and developer spawns.
    public sealed class SandboxDirector : MonoBehaviour
    {
        [Serializable]
        private sealed class TableItem
        {
            public ItemConfig Item;
            public int Count = 1;
            public Transform Point;
        }

        [Serializable]
        private sealed class FixedSpawn
        {
            public NetworkObject Prefab;
            public Transform Point;
        }

        [SerializeField]
        private NetworkEvents _networkEvents;

        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private NetworkObject _sessionPrefab;

        [SerializeField]
        private NetworkObject _adventurerPrefab;

        [SerializeField]
        private NetworkObject _worldItemPrefab;

        [SerializeField]
        private NetworkObject _botPrefab;

        [SerializeField]
        private NetworkObject[] _monsters;

        [SerializeField]
        private FixedSpawn[] _dummies;

        [SerializeField]
        private Transform[] _playerSpawns;

        [SerializeField]
        private TableItem[] _table;

        [SerializeField]
        private ContainerComponent[] _containers;

        [SerializeField, Tooltip("Class of every adventurer on the test ground (0 = Fighter, any weapon)")]
        private byte _classId;

        [SerializeField]
        private float _respawnDelay = 4f;

        [SerializeField]
        private float _spawnDistance = 6f;

        private readonly List<PlayerSessionComponent> _sessions = new();
        private readonly List<NetworkObject> _mobs = new();
        private readonly List<NetworkObject> _tableItems = new();
        private readonly Dictionary<PlayerSessionComponent, float> _deathTimes = new();
        private NetworkRunner _runner;
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

        private void Update()
        {
            if (!IsServer())
                return;

            foreach (PlayerSessionComponent session in _sessions)
            {
                if (session.State == SessionState.Lobby && session.HasLoadedKit)
                    SpawnAdventurer(session);
                else if (session.State is SessionState.Dead or SessionState.Extracted)
                    UpdateRespawn(session);
            }
        }

        public void SpawnMonster(int index)
        {
            if (IsServer() && index >= 0 && index < _monsters.Length)
                SpawnMob(_monsters[index], (_, obj) => obj.GetComponent<MonsterComponent>().Setup(1));
        }

        public void SpawnBot()
        {
            if (IsServer())
                SpawnMob(_botPrefab, (_, obj) => obj.GetComponent<BotBrainComponent>().Setup(UnityEngine.Random.Range(0, 3), BotMode.Spar));
        }

        public void ClearMobs()
        {
            if (!IsServer())
                return;

            foreach (NetworkObject mob in _mobs)
            {
                if (mob != null && mob.IsValid)
                    _runner.Despawn(mob);
            }

            _mobs.Clear();
        }

        /// Puts every table item back, removing the ones still lying there, and refills the chests.
        public void Restock()
        {
            if (!IsServer())
                return;

            foreach (NetworkObject item in _tableItems)
            {
                if (item != null && item.IsValid)
                    _runner.Despawn(item);
            }

            _tableItems.Clear();

            foreach (TableItem entry in _table)
            {
                ItemStack stack = ItemStack.Create(entry.Item, entry.Count, entry.Item.BaseRarity);
                _tableItems.Add(_runner.Spawn(_worldItemPrefab, entry.Point.position, entry.Point.rotation, PlayerRef.None,
                    (_, obj) => obj.GetComponent<WorldItemComponent>().Setup(stack, true)));
            }

            for (int i = 0; i < _containers.Length; i++)
            {
                _containers[i].ResetContainer();
                _containers[i].Fill(_containers[i].LootTable, Environment.TickCount + i);
            }
        }

        private bool IsServer()
        {
            return _runner != null && _runner.IsServer;
        }

        private void SpawnWorld()
        {
            _isWorldSpawned = true;

            foreach (FixedSpawn dummy in _dummies)
                _runner.Spawn(dummy.Prefab, dummy.Point.position, dummy.Point.rotation);

            Restock();
        }

        private void SpawnMob(NetworkObject prefab, NetworkRunner.OnBeforeSpawned setup)
        {
            AdventurerComponent adventurer = _context.LocalAdventurer;
            Transform origin = adventurer != null ? adventurer.transform : _playerSpawns[0];
            Vector3 position = origin.position + origin.forward * _spawnDistance;
            Quaternion rotation = Quaternion.LookRotation(-origin.forward);

            _mobs.RemoveAll(mob => mob == null || !mob.IsValid);
            _mobs.Add(_runner.Spawn(prefab, position, rotation, PlayerRef.None, setup));
        }

        private void UpdateRespawn(PlayerSessionComponent session)
        {
            if (!_deathTimes.TryGetValue(session, out float time))
            {
                _deathTimes[session] = Time.time;

                return;
            }

            if (Time.time - time < _respawnDelay)
                return;

            _deathTimes.Remove(session);
            SpawnAdventurer(session);
        }

        /// Everyone enters naked; gear comes from the table, corpses and monster drops.
        private void SpawnAdventurer(PlayerSessionComponent session)
        {
            // Scene objects (chests) are surely spawned once the first profile has arrived.
            if (!_isWorldSpawned)
                SpawnWorld();

            PlayerRef player = session.Object.InputAuthority;
            Transform point = _playerSpawns[player.AsIndex % _playerSpawns.Length];

            NetworkObject adventurer = _runner.Spawn(_adventurerPrefab, point.position, point.rotation, player,
                (_, obj) => obj.GetComponent<AdventurerComponent>().Setup(_classId, session));

            session.OnAdventurerSpawned(adventurer.GetComponent<AdventurerComponent>());
        }

        private void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            _runner = runner;

            if (!runner.IsServer)
                return;

            NetworkObject session = runner.Spawn(_sessionPrefab, Vector3.zero, Quaternion.identity, player);
            runner.SetPlayerObject(player, session);
            _sessions.Add(session.GetComponent<PlayerSessionComponent>());
        }

        private void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer || !runner.TryGetPlayerObject(player, out NetworkObject session))
                return;

            PlayerSessionComponent component = session.GetComponent<PlayerSessionComponent>();
            AdventurerComponent adventurer = component.Adventurer;

            if (adventurer != null)
                runner.Despawn(adventurer.Object);

            _sessions.Remove(component);
            _deathTimes.Remove(component);
            runner.Despawn(session);
        }
    }
}
