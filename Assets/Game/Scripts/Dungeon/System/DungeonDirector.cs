using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Host-side orchestration: sessions on join, adventurer spawns, dungeon population, portals and match reset.
    public sealed class DungeonDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class FloorLayout
        {
            public Transform[] PlayerSpawns;
            public Transform[] MonsterSpawns;
            public ContainerComponent[] Containers;
            public PortalComponent[] EscapePortals;
            public PortalComponent DescendPortal;
            public Transform DescendDestination;
            public Vector3 Center;
            public float Radius = 30f;
        }

        [Serializable]
        private sealed class MonsterKind
        {
            public NetworkObject Prefab;
            public float Weight = 1f;
        }

        public MatchComponent Match => _match;
        public IReadOnlyList<FloorLayout> Floors => _floors;

        [SerializeField]
        private NetworkEvents _networkEvents;

        [SerializeField]
        private NetworkObject _sessionPrefab;

        [SerializeField]
        private NetworkObject _adventurerPrefab;

        [SerializeField]
        private NetworkObject _matchPrefab;

        [SerializeField]
        private DungeonConfig _config;

        [SerializeField]
        private FloorLayout[] _floors = new FloorLayout[MatchComponent.FloorCount];

        [SerializeField]
        private MonsterKind[] _monsters;

        [SerializeField, Range(0f, 1f)]
        private float _monsterSpawnChance = 0.8f;

        [SerializeField]
        private float _resetDelay = 8f;

        private readonly List<NetworkObject> _spawned = new();
        private readonly List<PlayerSessionComponent> _sessions = new();
        private NetworkRunner _runner;
        private MatchComponent _match;
        private bool _isPopulated;
        private bool _portalsOpened;
        private bool _descendOpened;
        private float _finishedAt = -1f;
        private int _seed;

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
            if (_runner == null || !_runner.IsServer || _match == null)
                return;

            if (_match.State == MatchState.Running)
                UpdateRunning();
            else if (_match.State == MatchState.Finished && Time.time - _finishedAt > _resetDelay)
                ResetDungeon();
        }

        public void SpawnAdventurer(PlayerSessionComponent session)
        {
            if (_runner == null || !_runner.IsServer)
                return;

            if (_match.State == MatchState.Finished)
                ResetDungeon();

            if (!_isPopulated)
                Populate();

            if (_match.State != MatchState.Running)
                StartMatch();

            FloorLayout floor = _floors[0];
            Transform point = floor.PlayerSpawns[session.Object.InputAuthority.AsIndex % floor.PlayerSpawns.Length];
            PlayerRef player = session.Object.InputAuthority;

            NetworkObject adventurer = _runner.Spawn(_adventurerPrefab, point.position, point.rotation, player, (_, obj) =>
            {
                AdventurerComponent component = obj.GetComponent<AdventurerComponent>();
                component.Setup(session.ClassId, session);
                component.Inventory.CopyFrom(session.Kit);
            });

            session.OnAdventurerSpawned(adventurer.GetComponent<AdventurerComponent>());
        }

        private void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            _runner = runner;

            if (!runner.IsServer)
                return;

            if (_match == null)
                _match = runner.Spawn(_matchPrefab).GetComponent<MatchComponent>();

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
            runner.Despawn(session);
        }

        private void StartMatch()
        {
            _portalsOpened = false;
            _descendOpened = false;
            Vector3[] centers = new Vector3[MatchComponent.FloorCount];
            float[] radii = new float[MatchComponent.FloorCount];
            Vector3[] finals = new Vector3[MatchComponent.FloorCount];
            System.Random random = new System.Random(_seed);

            for (int i = 0; i < MatchComponent.FloorCount; i++)
            {
                FloorLayout floor = _floors[i];
                centers[i] = floor.Center;
                radii[i] = floor.Radius;
                Vector2 offset = new Vector2((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * floor.Radius * 0.8f;
                finals[i] = floor.Center + new Vector3(offset.x, 0f, offset.y);
            }

            _match.Begin(centers, radii, finals);
        }

        private void UpdateRunning()
        {
            float elapsed = _match.Elapsed;

            if (!_descendOpened && elapsed >= _config.DescendPortalTime)
            {
                _descendOpened = true;
                SetDescendPortals(true);
            }

            if (!_portalsOpened && elapsed >= _config.EscapePortalTime)
            {
                _portalsOpened = true;
                SetEscapePortals(true);
            }

            if (_match.IsTimeUp && !AnyAdventurerAlive())
            {
                _match.Finish();
                _finishedAt = Time.time;
            }
        }

        private bool AnyAdventurerAlive()
        {
            foreach (PlayerSessionComponent session in _sessions)
            {
                if (session != null && session.State == SessionState.InDungeon)
                    return true;
            }

            return false;
        }

        private void Populate()
        {
            _isPopulated = true;
            _seed = Environment.TickCount;
            System.Random random = new System.Random(_seed);

            for (int floorIndex = 0; floorIndex < _floors.Length; floorIndex++)
            {
                FloorLayout floor = _floors[floorIndex];

                foreach (ContainerComponent container in floor.Containers)
                {
                    if (container == null)
                        continue;

                    container.ResetContainer();
                    container.Fill(container.LootTable, random.Next());
                }

                foreach (Transform point in floor.MonsterSpawns)
                {
                    if (random.NextDouble() > _monsterSpawnChance)
                        continue;

                    MonsterKind kind = Pick(_monsters, random, m => m.Weight);
                    NetworkObject monster = _runner.Spawn(kind.Prefab, point.position, point.rotation, PlayerRef.None,
                        (_, obj) => obj.GetComponent<MonsterComponent>().Setup((byte)(floorIndex + 1)));
                    _spawned.Add(monster);
                }
            }

            SetEscapePortals(false);
            SetDescendPortals(false);
        }

        private void SetDescendPortals(bool isActive)
        {
            for (int i = 0; i < _floors.Length - 1; i++)
            {
                PortalComponent portal = _floors[i].DescendPortal;

                if (portal == null)
                    continue;

                if (isActive)
                    portal.Activate(_floors[i + 1].DescendDestination);
                else
                    portal.Deactivate();
            }
        }

        private void SetEscapePortals(bool isActive)
        {
            foreach (FloorLayout floor in _floors)
            {
                foreach (PortalComponent portal in floor.EscapePortals)
                {
                    if (portal == null)
                        continue;

                    if (isActive)
                        portal.Activate(null);
                    else
                        portal.Deactivate();
                }
            }
        }

        private void ResetDungeon()
        {
            foreach (NetworkObject obj in _spawned)
            {
                if (obj != null && obj.IsValid)
                    _runner.Despawn(obj);
            }

            _spawned.Clear();
            _isPopulated = false;
            _match.ResetMatch();
        }

        private static T Pick<T>(T[] options, System.Random random, Func<T, float> weight) where T : class
        {
            float total = 0f;

            foreach (T option in options)
                total += weight(option);

            float roll = (float)random.NextDouble() * total;

            foreach (T option in options)
            {
                roll -= weight(option);

                if (roll <= 0f)
                    return option;
            }

            return options[^1];
        }
    }
}
