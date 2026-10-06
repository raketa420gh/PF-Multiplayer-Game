using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Dungeon
{
    /// Host-side orchestration: sessions on join, adventurer spawns, dungeon population, portals and match reset.
    /// Players arrive from the tavern scene and enter as soon as their kit is loaded. A floor is populated and its
    /// clock starts only when the first adventurer reaches it.
    public sealed class DungeonDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class FloorLayout
        {
            public Transform[] PlayerSpawns;
            public Transform[] MonsterSpawns;
            /// Monsters that always stand at their points, unlike the random ones of MonsterSpawns.
            public MonsterPlacement[] Monsters;
            public ContainerComponent[] Containers;
            public PortalComponent[] EscapePortals;
            /// Half side of the square around Center where escape portals open at random; 0 keeps them in place.
            public float EscapeArea;
            public PortalComponent DescendPortal;
            public Transform DescendDestination;
            public Transform BossSpawn;
            public Vector3 Center;
            public float Radius = 30f;
        }

        [Serializable]
        public sealed class MonsterPlacement
        {
            public Transform Point;
            public NetworkObject Prefab;
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

        [SerializeField]
        private NetworkObject _bossPrefab;

        [SerializeField, Range(0f, 1f)]
        private float _monsterSpawnChance = 0.8f;

        [SerializeField]
        private float _resetDelay = 8f;

        [SerializeField, Tooltip("Distance from walls and obstacles a randomly placed escape portal keeps")]
        private float _portalClearance = 1.2f;

        [SerializeField]
        private int _portalAttempts = 40;

        private readonly List<NetworkObject> _spawned = new();
        private readonly List<PlayerSessionComponent> _sessions = new();
        private readonly bool[] _populated = new bool[MatchComponent.FloorCount];
        private readonly bool[] _escapeOpened = new bool[MatchComponent.FloorCount];
        private readonly bool[] _descendOpened = new bool[MatchComponent.FloorCount];
        private NetworkRunner _runner;
        private MatchComponent _match;
        private float _finishedAt = -1f;
        private int _seed;

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

        private void Update()
        {
            if (_runner == null || !_runner.IsServer || _match == null)
                return;

            foreach (PlayerSessionComponent session in _sessions)
            {
                if (session.State == SessionState.Lobby && session.HasLoadedKit)
                    SpawnAdventurer(session);
            }

            if (_match.State == MatchState.Running)
                UpdateRunning();
            else if (_match.State == MatchState.Finished && Time.time - _finishedAt > _resetDelay)
                ResetDungeon();
        }

        private void SpawnAdventurer(PlayerSessionComponent session)
        {
            if (_match.State == MatchState.Finished)
                ResetDungeon();

            if (!_populated[0])
            {
                _seed = Environment.TickCount;

                for (int i = 0; i < _floors.Length; i++)
                {
                    SetEscapePortals(i, false);
                    SetDescendPortal(i, false);
                }

                Populate(0);
            }

            if (_match.State != MatchState.Running)
                StartMatch();

            session.EnsureKit();

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

        /// The host left: the run is over for everyone, back to the tavern with the kit the player came in with.
        private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            if (reason != ShutdownReason.Ok)
                SceneTravel.Load(runner, SceneTravel.LobbyScene, SceneTravel.LobbyTitle);
        }

        private void StartMatch()
        {
            Array.Clear(_escapeOpened, 0, _escapeOpened.Length);
            Array.Clear(_descendOpened, 0, _descendOpened.Length);
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
            for (int i = 0; i < _floors.Length; i++)
            {
                // A deeper floor is a new dungeon: fresh loot, monsters and a full swarm timer from the first arrival.
                if (!_populated[i] && AnyAdventurerOnFloor(i + 1))
                {
                    Populate(i);
                    _match.BeginFloor(i + 1);
                }

                float elapsed = _match.GetElapsed(i + 1);

                if (!_descendOpened[i] && elapsed >= _config.DescendPortalTime)
                {
                    _descendOpened[i] = true;
                    SetDescendPortal(i, true);
                }

                if (!_escapeOpened[i] && elapsed >= _config.EscapePortalTime)
                {
                    _escapeOpened[i] = true;
                    SetEscapePortals(i, true);
                }
            }

            // A run ends as soon as nobody is left inside, so the next arrival gets a fresh dungeon and a full swarm timer.
            if (_match.Elapsed > 5f && !AnyAdventurerAlive())
            {
                _match.Finish();
                _finishedAt = Time.time;
            }
        }

        private bool AnyAdventurerOnFloor(int floor)
        {
            foreach (PlayerSessionComponent session in _sessions)
            {
                if (session != null && session.State == SessionState.InDungeon && session.Adventurer != null && session.Adventurer.Floor == floor)
                    return true;
            }

            return false;
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

        private void Populate(int floorIndex)
        {
            _populated[floorIndex] = true;
            System.Random random = new System.Random(_seed + floorIndex);
            FloorLayout floor = _floors[floorIndex];
            byte level = (byte)(floorIndex + 1);

            foreach (ContainerComponent container in floor.Containers)
            {
                if (container == null)
                    continue;

                container.ResetContainer();
                container.Fill(container.LootTable, random.Next());
            }

            if (floor.BossSpawn != null && _bossPrefab != null)
            {
                NetworkObject boss = _runner.Spawn(_bossPrefab, floor.BossSpawn.position, floor.BossSpawn.rotation, PlayerRef.None,
                    (_, obj) => obj.GetComponent<MonsterComponent>().Setup(level));
                _spawned.Add(boss);
            }

            foreach (Transform point in floor.MonsterSpawns)
            {
                if (random.NextDouble() <= _monsterSpawnChance)
                    SpawnMonster(Pick(_monsters, random, m => m.Weight).Prefab, point, level);
            }

            foreach (MonsterPlacement placement in floor.Monsters)
                SpawnMonster(placement.Prefab, placement.Point, level);
        }

        private void SpawnMonster(NetworkObject prefab, Transform point, byte level)
        {
            _spawned.Add(_runner.Spawn(prefab, point.position, point.rotation, PlayerRef.None, (_, obj) => obj.GetComponent<MonsterComponent>().Setup(level)));
        }

        private void SetDescendPortal(int floorIndex, bool isActive)
        {
            PortalComponent portal = _floors[floorIndex].DescendPortal;

            if (portal == null || floorIndex + 1 >= _floors.Length)
                return;

            if (isActive)
                portal.Activate(_floors[floorIndex + 1].DescendDestination);
            else
                portal.Deactivate();
        }

        private void SetEscapePortals(int floorIndex, bool isActive)
        {
            FloorLayout floor = _floors[floorIndex];

            foreach (PortalComponent portal in floor.EscapePortals)
            {
                if (portal == null)
                    continue;

                if (isActive && floor.EscapeArea > 0f)
                    portal.GetComponent<NetworkTransform>().Teleport(RandomPortalPoint(floor, portal.transform.position), Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));

                if (isActive)
                    portal.Activate(null);
                else
                    portal.Deactivate();
            }
        }

        /// Random walkable point of the floor square that keeps clear of walls and props.
        private Vector3 RandomPortalPoint(FloorLayout floor, Vector3 fallback)
        {
            for (int i = 0; i < _portalAttempts; i++)
            {
                Vector3 point = floor.Center + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)) * floor.EscapeArea;

                if (NavMesh.SamplePosition(point, out NavMeshHit hit, 1f, NavMesh.AllAreas)
                    && NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, NavMesh.AllAreas) && edge.distance >= _portalClearance)
                    return hit.position;
            }

            return fallback;
        }

        private void ResetDungeon()
        {
            foreach (NetworkObject obj in _spawned)
            {
                if (obj != null && obj.IsValid)
                    _runner.Despawn(obj);
            }

            _spawned.Clear();
            Array.Clear(_populated, 0, _populated.Length);
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
