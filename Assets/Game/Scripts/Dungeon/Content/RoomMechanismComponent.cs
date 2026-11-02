using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// The moving part of a catacomb room, one networked object spawned at the room's origin and turned with it: stone slabs
    /// hiding a secret, opened by a lever or by levers and floor plates taken in the right order, and a lift. A wrong step of a
    /// sequence starts it over and, in a room with drop points, lets a monster fall in. Every pose is a function of the
    /// networked ticks, so all peers hold the colliders in the same place each tick.
    public sealed class RoomMechanismComponent : NetworkBehaviour
    {
        public bool IsOpen => _openTick != 0;
        public bool IsLiftMoving => LiftProgress(Runner.Tick) < 1f;
        /// Steps of the sequence taken so far.
        public int Progress => _progress;

        [SerializeField]
        private RoomSlabComponent[] _slabs;

        [SerializeField]
        private float _openTime = 3f;

        [SerializeField, Tooltip("Steps of the sequence; 0 = any switch opens the secret")]
        private int _sequence;

        [SerializeField]
        private Transform _lift;

        [SerializeField, Tooltip("Local heights of the lift's upper (x) and lower (y) stops")]
        private Vector2 _liftStops;

        [SerializeField]
        private float _liftTime = 6f;

        [SerializeField]
        private Transform[] _drops;

        [SerializeField]
        private NetworkObject _dropMonster;

        [Networked]
        private int _openTick { get; set; }

        [Networked]
        private int _progress { get; set; }

        [Networked]
        private int _liftTick { get; set; }

        [Networked]
        private NetworkBool _isLiftDown { get; set; }

        [Networked]
        private int _resetCount { get; set; }

        private readonly List<NetworkObject> _dropped = new();
        private int _heardOpen;
        private int _heardLift;
        private int _heardReset;
        private int _nextDrop;

        public override void Spawned()
        {
            _heardOpen = _openTick;
            _heardLift = _liftTick;
            _heardReset = _resetCount;
            Apply(Runner.Tick);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (!runner.IsServer)
                return;

            foreach (NetworkObject monster in _dropped)
            {
                if (monster != null && monster.IsValid)
                    runner.Despawn(monster);
            }
        }

        public override void FixedUpdateNetwork()
        {
            Apply(Runner.Tick);
        }

        public override void Render()
        {
            Apply(Runner.Tick + Runner.LocalAlpha);

            if (_heardOpen != _openTick)
            {
                _heardOpen = _openTick;

                foreach (RoomSlabComponent slab in _slabs)
                    DungeonAudioComponent.Play(DungeonSound.Rattle, slab.transform.position);
            }

            if (_heardLift != _liftTick)
            {
                _heardLift = _liftTick;
                DungeonAudioComponent.Play(DungeonSound.Rattle, _lift.position);
            }

            if (_heardReset != _resetCount)
            {
                _heardReset = _resetCount;
                DungeonAudioComponent.Play(DungeonSound.Rattle, transform.position);
            }
        }

        /// A switch was used: step is its place in the sequence, 0 outside one.
        public void Press(int step)
        {
            if (IsOpen)
                return;

            if (_sequence == 0)
            {
                _openTick = Runner.Tick;

                return;
            }

            if (step != _progress + 1)
            {
                Fail(step);

                return;
            }

            _progress = step;

            if (_progress == _sequence)
                _openTick = Runner.Tick;
        }

        public void SendLift()
        {
            if (IsLiftMoving)
                return;

            _isLiftDown = !_isLiftDown;
            _liftTick = Runner.Tick;
        }

        /// Drop of the lift from the previous tick to this one, in world space.
        public Vector3 GetLiftStep(int tick)
        {
            return transform.TransformVector(Vector3.up * (LiftHeight(tick) - LiftHeight(tick - 1)));
        }

        /// The wrong step counts as the first one when it is the sequence's first.
        private void Fail(int step)
        {
            _progress = step == 1 ? 1 : 0;
            _resetCount++;

            if (_drops == null || _drops.Length == 0 || _dropMonster == null)
                return;

            Transform point = _drops[_nextDrop++ % _drops.Length];
            _dropped.Add(Runner.Spawn(_dropMonster, point.position, point.rotation, PlayerRef.None,
                (_, spawned) => spawned.GetComponent<MonsterComponent>().Setup(MatchComponent.EntryFloor)));
        }

        private float LiftProgress(float tick)
        {
            return _liftTick == 0 ? 1f : Mathf.Clamp01((tick - _liftTick) * Runner.DeltaTime / _liftTime);
        }

        private float LiftHeight(float tick)
        {
            float from = _isLiftDown ? _liftStops.x : _liftStops.y;
            float to = _isLiftDown ? _liftStops.y : _liftStops.x;

            return _liftTick == 0 ? to : Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, LiftProgress(tick)));
        }

        private void Apply(float tick)
        {
            float open = IsOpen ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tick - _openTick) * Runner.DeltaTime / _openTime)) : 0f;

            foreach (RoomSlabComponent slab in _slabs)
                slab.Set(open);

            if (_lift == null)
                return;

            Vector3 position = _lift.localPosition;
            _lift.localPosition = new Vector3(position.x, LiftHeight(tick), position.z);
        }
    }
}
