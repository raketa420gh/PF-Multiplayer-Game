using System;
using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Hub scene host: gives every player the session object that carries the kit, the stash and the build, and a body to walk
    /// the settlement with. The body wears the kit as it is packed right now and never dies here; party members hurt each
    /// other only inside the fencing hall. In a party the hub is the party session; losing its host reconnects the members.
    public sealed class HubDirector : MonoBehaviour
    {
        [Serializable]
        private sealed class Dummy
        {
            public Transform Point;
            [Range(0f, 1f), Tooltip("Share of physical and magical damage the dummy shrugs off")]
            public float Reduction;
        }

        private sealed class Resident
        {
            public PlayerSessionComponent Session;
            public AdventurerComponent Adventurer;
            public int KitVersion;
            public int Build;
            public int SeenBuild;
            public float SeenAt;
            public bool IsSparring;
        }

        [SerializeField]
        private NetworkEvents _networkEvents;

        [SerializeField]
        private NetworkObject _sessionPrefab;

        [SerializeField]
        private NetworkObject _adventurerPrefab;

        [SerializeField]
        private NetworkObject _dummyPrefab;

        [SerializeField, Tooltip("Indexed by HubArrival: the bed, the portal anchor, the infirmary")]
        private Transform[] _arrivals;

        [SerializeField, Tooltip("Where party members enter the hub of their leader")]
        private Transform _gate;

        [SerializeField]
        private Dummy[] _dummies = Array.Empty<Dummy>();

        [SerializeField, Tooltip("Fencing hall: party members hit each other only while both stand inside")]
        private BoxCollider _hall;

        [SerializeField, Tooltip("Below this height a body fell off the terrace and is put back where it arrived")]
        private float _rescueHeight = -6f;

        [SerializeField, Tooltip("Seconds a changed build has to stay as it is before the body is rebuilt with it")]
        private float _rebuildDelay = 0.6f;

        private const int Team = 1;
        private const float SpawnSpacing = 0.9f;
        private const int WorldMask = 1;

        private readonly List<Resident> _residents = new();
        private NetworkRunner _runner;
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

        private void Update()
        {
            // Every peer resolves hits by the same rule, so it is set on all of them.
            foreach (FighterComponent fighter in FighterComponent.All)
            {
                fighter.Receiver.SetTeam(Team);
                fighter.Receiver.SetSparring(IsInHall(fighter.transform.position));
            }

            if (_runner == null || !_runner.IsServer)
                return;

            for (int i = 0; i < _residents.Count; i++)
                Tend(_residents[i], i);
        }

        private void Tend(Resident resident, int index)
        {
            PlayerSessionComponent session = resident.Session;

            if (session == null || session.Object == null || !session.Object.IsValid || !session.HasLoadedKit)
                return;

            int build = HashCode.Combine(session.ClassId, session.Level, session.Subclass, session.SkillA, session.SkillB, session.PerkMask, session.SpellLayout);
            AdventurerComponent adventurer = resident.Adventurer;
            bool isAlive = adventurer != null && adventurer.Object != null && adventurer.Object.IsValid && adventurer.State == AdventurerState.Alive;

            if (!isAlive)
            {
                Transform point = ArrivalPoint(session);
                Despawn(resident);
                Spawn(resident, point.TransformPoint(new Vector3(((index + 1) % 3 - 1) * SpawnSpacing, 0f, 0f)), point.eulerAngles.y, build);

                return;
            }

            if (build != resident.SeenBuild)
            {
                resident.SeenBuild = build;
                resident.SeenAt = Time.time;
            }

            // Skills, perks and spells are fixed when the body spawns: a new build gets a new body on the same spot.
            if (resident.SeenBuild != resident.Build && Time.time - resident.SeenAt > _rebuildDelay)
            {
                Vector3 position = adventurer.transform.position;
                float yaw = adventurer.transform.eulerAngles.y;
                Despawn(resident);
                Spawn(resident, position, yaw, build);

                return;
            }

            bool isSparring = IsInHall(adventurer.transform.position);
            bool hasLeftHall = resident.IsSparring && !isSparring;
            resident.IsSparring = isSparring;

            // What was shot and thrown in the hall comes back with the kit; wounds close at the door. Nothing is drunk or eaten in the hub (AdventurerComponent.OnUseItem).
            if (session.Kit.Version != resident.KitVersion || hasLeftHall)
            {
                resident.KitVersion = session.Kit.Version;
                adventurer.Inventory.CopyFrom(session.Kit);
            }

            HealthComponent health = adventurer.Fighter.Health;

            if (!isSparring && health.CurrentHealth < health.MaxHealth)
                health.Restore(health.MaxHealth);

            if (adventurer.transform.position.y < _rescueHeight)
            {
                Transform point = ArrivalPoint(session);
                adventurer.Fighter.Move.Teleport(Ground(point.position), point.eulerAngles.y);
            }
        }

        private void Spawn(Resident resident, Vector3 position, float yaw, int build)
        {
            PlayerSessionComponent session = resident.Session;
            NetworkObject spawned = _runner.Spawn(_adventurerPrefab, Ground(position), Quaternion.Euler(0f, yaw, 0f), session.Object.InputAuthority, (_, obj) =>
            {
                AdventurerComponent component = obj.GetComponent<AdventurerComponent>();
                component.Setup(session.ClassId, session, 1, Team);
                component.Inventory.CopyFrom(session.Kit);
            });

            resident.Adventurer = spawned.GetComponent<AdventurerComponent>();
            resident.Adventurer.Fighter.Health.SetFloor(1);
            resident.KitVersion = session.Kit.Version;
            resident.Build = resident.SeenBuild = build;
            resident.IsSparring = false;
            session.SetAdventurer(resident.Adventurer);
        }

        private void Despawn(Resident resident)
        {
            if (resident.Adventurer != null && resident.Adventurer.Object != null && resident.Adventurer.Object.IsValid)
                _runner.Despawn(resident.Adventurer.Object);

            resident.Adventurer = null;
        }

        /// Back from a run: the portal anchor or the infirmary. Otherwise the host wakes in the cell and guests walk in through the gate.
        private Transform ArrivalPoint(PlayerSessionComponent session)
        {
            if (session.HubArrival != HubArrival.Home)
                return _arrivals[(int)session.HubArrival];

            return session.Object.InputAuthority == _runner.LocalPlayer ? _arrivals[(int)HubArrival.Home] : _gate;
        }

        private bool IsInHall(Vector3 position)
        {
            Vector3 local = _hall.transform.InverseTransformPoint(position) - _hall.center;
            Vector3 half = _hall.size * 0.5f;

            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private static Vector3 Ground(Vector3 position)
        {
            return Physics.Raycast(position + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5f, WorldMask, QueryTriggerInteraction.Ignore) ? hit.point : position;
        }

        private void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            _isJoined |= player == runner.LocalPlayer;

            if (!runner.IsServer)
                return;

            if (_runner == null)
            {
                _runner = runner;

                foreach (Dummy dummy in _dummies)
                    runner.Spawn(_dummyPrefab, dummy.Point.position, dummy.Point.rotation, PlayerRef.None, (_, obj) => obj.GetComponent<TrainingDummyComponent>().Setup(dummy.Reduction));
            }

            NetworkObject session = runner.Spawn(_sessionPrefab, Vector3.zero, Quaternion.identity, player);
            runner.SetPlayerObject(player, session);
            _residents.Add(new Resident { Session = session.GetComponent<PlayerSessionComponent>() });
        }

        private void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer || !runner.TryGetPlayerObject(player, out NetworkObject session))
                return;

            int index = _residents.FindIndex(resident => resident.Session != null && resident.Session.Object == session);

            if (index >= 0)
            {
                Despawn(_residents[index]);
                _residents.RemoveAt(index);
            }

            runner.Despawn(session);
        }

        private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            if (SceneTravel.IsTraveling || !PartyService.IsInParty || !_isJoined)
                return;

            NetworkLaunch.Notice = "The party host left, reconnecting...";
            SceneTravel.Load(runner, SceneTravel.HubScene, SceneTravel.HubTitle);
        }
    }
}
