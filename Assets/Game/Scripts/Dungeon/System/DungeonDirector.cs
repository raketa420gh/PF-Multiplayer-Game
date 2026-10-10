using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Dungeon
{
    /// Host-side orchestration: sessions on join, adventurer spawns, dungeon population, portals and match reset.
    /// A session holds one floor (DungeonAdmission.Floor): players arrive from the tavern on the first one and from the
    /// floor above on deeper ones, and enter as soon as their kit is loaded. The floor is populated and its clock starts
    /// when the first adventurer reaches it; its ways down hand adventurers over to the next floor's session.
    public sealed class DungeonDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class FloorLayout
        {
            public string Title;
            public Transform[] PlayerSpawns;
            /// Houses adventurers start in, their children are the spots: every team gets a house of its own.
            public Transform[] SpawnHouses;
            public Transform[] MonsterSpawns;
            /// Monsters that always stand at their points, unlike the random ones of MonsterSpawns.
            public MonsterPlacement[] Monsters;
            public ContainerComponent[] Containers;
            public PortalComponent[] EscapePortals;
            /// Escape portals show up one by one at DungeonConfig.EscapePortalTimes.
            /// Half side of the square around Center where escape portals open at random; 0 keeps them in place.
            public float EscapeArea;
            /// Ways down, opened halfway through the floor's clock; they hand adventurers over to the next floor's session.
            public PortalComponent[] DescendPortals;
            /// Where adventurers coming down from the floor above appear (the spawns of a deeper floor's session).
            public Transform[] Arrivals;
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
        private DungeonAdmission _admission;

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

        [SerializeField, Tooltip("Lays out its floor anew for every run; its rooms bring the starts, monsters, containers and traps")]
        private CatacombGenerator _catacombs;

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
        private int _portalAttempts = 60;

        [SerializeField, Tooltip("Vertical reach when snapping a random escape portal point to the NavMesh; floors may be hilly")]
        private float _portalSnap = 6f;

        [SerializeField, Tooltip("A spawn point closer than this to an adventurer counts as taken")]
        private float _spawnSpacing = 12f;

        [SerializeField, Tooltip("Spawn spots inside furnished houses snap to the nearest walkable point within this distance")]
        private float _spawnSnap = 1.5f;

        [SerializeField, Tooltip("Teams start in houses at least this far apart while there are such houses left")]
        private float _houseSpacing = 45f;

        [SerializeField, Tooltip("No monster stands closer than this to an adventurer spawn spot, so nobody is aggroed while still loading in")]
        private float _spawnSafety = 18f;

        [SerializeField, Tooltip("How many houses (catacomb start rooms) are kept monster-free for team starts; one per team at most")]
        private int _safeStarts = 12;

        private readonly List<NetworkObject> _spawned = new();
        private readonly List<Vector3> _spawnSpots = new();
        private readonly List<Transform> _safeHouses = new();
        private readonly List<PlayerSessionComponent> _sessions = new();
        private readonly Dictionary<int, Transform> _teamHouses = new();
        private readonly Dictionary<int, int> _teamSpawns = new();
        private readonly bool[] _populated = new bool[MatchComponent.FloorCount];
        private readonly int[] _escapeOpened = new int[MatchComponent.FloorCount];
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

            int floor = Mathf.Clamp(_admission != null ? _admission.Floor : MatchComponent.EntryFloor, 1, _floors.Length);

            if (!_populated[floor - 1])
            {
                _seed = Environment.TickCount;

                for (int i = 0; i < _floors.Length; i++)
                {
                    CloseEscapePortals(i);
                    SetDescendPortal(i, false);
                }

                if (IsGenerated(floor - 1))
                {
                    _match.SetSeed(_seed | 1);
                    _catacombs.Generate(_seed | 1);
                    _catacombs.BakeNavMesh();
                }

                Populate(floor - 1);
            }

            if (_match.State != MatchState.Running)
                StartMatch(floor);

            session.EnsureKit();

            PlayerRef player = session.Object.InputAuthority;
            Transform point = TakeSpawn(floor - 1, _admission != null ? _admission.TeamOf(player) : -1 - player.PlayerId);
            FloorTransfer.Carry arrival = session.Arrival != null && session.Arrival.Floor == floor ? session.Arrival : null;

            Vector3 position = NavMesh.SamplePosition(point.position, out NavMeshHit hit, _spawnSnap, NavMesh.AllAreas) ? hit.position : point.position;

            NetworkObject adventurer = _runner.Spawn(_adventurerPrefab, position, point.rotation, player, (_, obj) =>
            {
                AdventurerComponent component = obj.GetComponent<AdventurerComponent>();
                component.Setup(session.ClassId, session, (byte)floor);
                component.Inventory.CopyFrom(session.Kit);

                if (arrival != null)
                    component.Arrive(arrival);
            });

            session.OnAdventurerSpawned(adventurer.GetComponent<AdventurerComponent>());
        }

        /// Teammates share a house, every team gets its own one away from the others.
        private Transform TakeSpawn(int floorIndex, int team)
        {
            if (_safeHouses.Count == 0)
                return TakeSpawn(_floors[floorIndex]);

            if (!_teamHouses.TryGetValue(team, out Transform house))
            {
                List<Transform> free = new();
                List<Transform> far = new();

                foreach (Transform candidate in _safeHouses)
                {
                    if (_teamHouses.ContainsValue(candidate))
                        continue;

                    free.Add(candidate);

                    if (IsFarFrom(_teamHouses.Values, candidate.position))
                        far.Add(candidate);
                }

                List<Transform> pool = far.Count > 0 ? far : free.Count > 0 ? free : _safeHouses;
                house = pool[UnityEngine.Random.Range(0, pool.Count)];
                _teamHouses[team] = house;
            }

            _teamSpawns.TryGetValue(team, out int index);
            _teamSpawns[team] = index + 1;

            return house.childCount > 0 ? house.GetChild(index % house.childCount) : house;
        }

        private bool IsFarFrom(IEnumerable<Transform> houses, Vector3 position)
        {
            foreach (Transform taken in houses)
            {
                if ((taken.position - position).sqrMagnitude < _houseSpacing * _houseSpacing)
                    return false;
            }

            return true;
        }

        /// A random spawn point of the floor (its arrivals on a deeper floor) that no adventurer stands near yet; any if all are taken.
        private Transform TakeSpawn(FloorLayout floor)
        {
            Transform[] points = floor.PlayerSpawns.Length > 0 ? floor.PlayerSpawns : floor.Arrivals;
            List<Transform> free = new();

            foreach (Transform point in points)
            {
                if (!IsTaken(point.position))
                    free.Add(point);
            }

            if (free.Count == 0)
                free.AddRange(points);

            return free[UnityEngine.Random.Range(0, free.Count)];
        }

        private bool IsTaken(Vector3 point)
        {
            foreach (PlayerSessionComponent session in _sessions)
            {
                if (session != null && session.Adventurer != null && (session.Adventurer.transform.position - point).sqrMagnitude < _spawnSpacing * _spawnSpacing)
                    return true;
            }

            return false;
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

        /// The server or host left: the run is over for everyone, back to the tavern with the kit the player came in with.
        private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            if (reason != ShutdownReason.Ok && _runner != null && !GameServer.IsDedicated)
                SceneTravel.Load(runner, SceneTravel.LobbyScene, SceneTravel.LobbyTitle);
        }

        private void StartMatch(int firstFloor)
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

            _match.Begin(centers, radii, finals, firstFloor);
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

                float[] times = _config.EscapePortalTimes;

                while (_escapeOpened[i] < _floors[i].EscapePortals.Length && elapsed >= times[Mathf.Min(_escapeOpened[i], times.Length - 1)])
                    OpenEscapePortal(i, _escapeOpened[i]++);
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
            CollectSpawnSpots(floorIndex);

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

            if (!IsGenerated(floorIndex))
                return;

            foreach (CatacombRoomComponent room in _catacombs.Rooms)
            {
                foreach (MonsterPlacement placement in room.Containers)
                {
                    NetworkObject spawned = _runner.Spawn(placement.Prefab, placement.Point.position, placement.Point.rotation);
                    ContainerComponent container = spawned.GetComponent<ContainerComponent>();
                    container.Fill(container.LootTable, random.Next());
                    _spawned.Add(spawned);
                }

                foreach (MonsterPlacement placement in room.Traps)
                    _spawned.Add(_runner.Spawn(placement.Prefab, placement.Point.position, placement.Point.rotation));

                foreach (MonsterPlacement placement in room.Monsters)
                {
                    if (random.NextDouble() <= _monsterSpawnChance)
                        SpawnMonster(placement.Prefab != null ? placement.Prefab : Pick(_monsters, random, m => m.Weight).Prefab, placement.Point, level);
                }
            }
        }

        private bool IsGenerated(int floorIndex)
        {
            return _catacombs != null && _catacombs.Floor == floorIndex + 1;
        }

        private Transform[] Starts()
        {
            List<Transform> starts = new();

            foreach (CatacombRoomComponent room in _catacombs.Rooms)
            {
                if (room.StartSpots != null)
                    starts.Add(room.StartSpots);
            }

            return starts.ToArray();
        }

        /// Reserves the houses teams may start in, spread apart, and every place an adventurer may appear: monsters keep clear of them.
        private void CollectSpawnSpots(int floorIndex)
        {
            FloorLayout floor = _floors[floorIndex];
            List<Transform> pool = new(IsGenerated(floorIndex) ? Starts() : floor.SpawnHouses ?? Array.Empty<Transform>());
            pool.RemoveAll(house => house == null);
            _safeHouses.Clear();

            while (_safeHouses.Count < _safeStarts && pool.Count > 0)
            {
                List<Transform> far = pool.FindAll(house => IsFarFrom(_safeHouses, house.position));
                List<Transform> from = far.Count > 0 ? far : pool;
                Transform pick = from[UnityEngine.Random.Range(0, from.Count)];
                _safeHouses.Add(pick);
                pool.Remove(pick);
            }

            _spawnSpots.Clear();
            AddSpawnSpots(_safeHouses.ToArray());
            AddSpawnSpots(floor.PlayerSpawns);
            AddSpawnSpots(floor.Arrivals);
        }

        private void AddSpawnSpots(Transform[] points)
        {
            if (points == null)
                return;

            foreach (Transform point in points)
            {
                if (point == null)
                    continue;

                _spawnSpots.Add(point.position);

                foreach (Transform spot in point)
                    _spawnSpots.Add(spot.position);
            }
        }

        private bool IsNearSpawn(Vector3 position)
        {
            foreach (Vector3 spot in _spawnSpots)
            {
                if ((spot - position).sqrMagnitude < _spawnSafety * _spawnSafety)
                    return true;
            }

            return false;
        }

        private void SpawnMonster(NetworkObject prefab, Transform point, byte level)
        {
            if (IsNearSpawn(point.position))
                return;

            _spawned.Add(_runner.Spawn(prefab, point.position, point.rotation, PlayerRef.None, (_, obj) => obj.GetComponent<MonsterComponent>().Setup(level)));
        }

        private void SetDescendPortal(int floorIndex, bool isActive)
        {
            if (floorIndex + 1 >= _floors.Length)
                return;

            foreach (PortalComponent portal in _floors[floorIndex].DescendPortals)
            {
                if (portal == null)
                    continue;

                if (isActive)
                    portal.Activate();
                else
                    portal.Deactivate();
            }
        }

        private void CloseEscapePortals(int floorIndex)
        {
            foreach (PortalComponent portal in _floors[floorIndex].EscapePortals)
            {
                if (portal != null)
                    portal.Deactivate();
            }
        }

        private void OpenEscapePortal(int floorIndex, int index)
        {
            FloorLayout floor = _floors[floorIndex];
            PortalComponent portal = floor.EscapePortals[index];

            if (portal == null)
                return;

            if (floor.EscapeArea > 0f)
                portal.GetComponent<NetworkTransform>().Teleport(RandomPortalPoint(floor, portal.transform.position), Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));

            portal.Activate();
        }

        /// Random walkable point of the floor square that keeps clear of walls and props.
        private Vector3 RandomPortalPoint(FloorLayout floor, Vector3 fallback)
        {
            for (int i = 0; i < _portalAttempts; i++)
            {
                Vector3 point = floor.Center + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)) * floor.EscapeArea;

                if (NavMesh.SamplePosition(point, out NavMeshHit hit, _portalSnap, NavMesh.AllAreas) && Mathf.Abs(hit.position.x - point.x) + Mathf.Abs(hit.position.z - point.z) < 1f
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
            _teamHouses.Clear();
            _teamSpawns.Clear();
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
