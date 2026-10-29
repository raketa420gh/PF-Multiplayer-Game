using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// The moving parts of the Great Hall, one networked object spawned at the hall's origin. Two cages hang on one chain: when
    /// the ore cage is up the coffin cage is down and the other way round, and the coffin cage at the top holds the sliding wall
    /// of the winch room open. Three seals hold counterweights over the shaft floor; a broken seal drops its weight (crushing
    /// whoever stands under it) and lifts the treasury slab by a third. Every pose is a function of the networked ticks, so all
    /// peers hold the colliders in the same place each tick.
    public sealed class GreatHallMachineryComponent : NetworkBehaviour
    {
        public const int SealCount = 3;

        public bool IsMoving => Progress(Runner.Tick) < 1f;
        public bool IsOreCageUp => _isOreCageUp;

        [SerializeField]
        private Transform[] _cages = new Transform[2];

        [SerializeField, Tooltip("Local heights of each cage while the ore cage is down (x) and up (y)")]
        private Vector2[] _cageHeights = { new(0f, 12f), new(18f, 6f) };

        [SerializeField]
        private float _travelTime = 6f;

        [SerializeField]
        private Transform _wall;

        [SerializeField]
        private Vector3 _wallClosed;

        [SerializeField]
        private Vector3 _wallOpen;

        [SerializeField, Tooltip("The wall opens over the last metres of the coffin cage's climb, from this height to its top")]
        private float _wallFrom = 15f;

        [SerializeField]
        private Transform[] _weights = new Transform[SealCount];

        [SerializeField]
        private float _weightTop = 16f;

        [SerializeField, Tooltip("Chain screech and falling dust before the weight lets go")]
        private float _warning = 1.5f;

        [SerializeField]
        private float _gravity = 20f;

        [SerializeField]
        private Vector3 _crushHalfExtents = new(1.2f, 1.2f, 1.2f);

        [SerializeField]
        private Transform _slab;

        [SerializeField]
        private float _slabRise = 3.2f;

        [SerializeField, Tooltip("Seconds the slab takes to rise by one seal's third")]
        private float _slabTime = 4f;

        [SerializeField]
        private Transform _wheel;

        [SerializeField]
        private LayerMask _victimMask;

        [Networked]
        private NetworkBool _isOreCageUp { get; set; }

        [Networked]
        private int _switchTick { get; set; }

        [Networked, Capacity(SealCount)]
        private NetworkArray<int> _sealTicks => default;

        private static readonly Collider[] s_colliders = new Collider[16];
        private int _heardSwitch;
        private readonly int[] _heardSeals = new int[SealCount];

        public override void Spawned()
        {
            if (HasStateAuthority)
                _isOreCageUp = true;

            _heardSwitch = _switchTick;

            for (int i = 0; i < SealCount; i++)
                _heardSeals[i] = _sealTicks[i];

            Apply(Runner.Tick);
        }

        public override void FixedUpdateNetwork()
        {
            Apply(Runner.Tick);

            if (!HasStateAuthority)
                return;

            for (int i = 0; i < SealCount; i++)
            {
                if (_sealTicks[i] != 0 && Runner.Tick == _sealTicks[i] + Mathf.CeilToInt(ImpactTime() / Runner.DeltaTime))
                    Crush(_weights[i].position);
            }
        }

        public override void Render()
        {
            Apply(Runner.Tick + Runner.LocalAlpha);

            if (_heardSwitch != _switchTick)
            {
                _heardSwitch = _switchTick;
                DungeonAudioComponent.Play(DungeonSound.Rattle, _cages[0].position);
                DungeonAudioComponent.Play(DungeonSound.Rattle, _cages[1].position);
            }

            for (int i = 0; i < SealCount; i++)
            {
                if (_heardSeals[i] == _sealTicks[i])
                    continue;

                _heardSeals[i] = _sealTicks[i];
                DungeonAudioComponent.Play(DungeonSound.Rattle, _weights[i].position);
            }
        }

        public bool IsSealBroken(int seal)
        {
            return _sealTicks[seal] != 0;
        }

        /// Sends the cages on their way unless they are travelling or already there.
        public void SetOreCageUp(bool isUp)
        {
            if (IsMoving || isUp == _isOreCageUp)
                return;

            _isOreCageUp = isUp;
            _switchTick = Runner.Tick;
        }

        public void BreakSeal(int seal)
        {
            if (!IsSealBroken(seal))
                _sealTicks.Set(seal, Runner.Tick);
        }

        /// Rise of a cage from the previous tick to this one, in world space.
        public Vector3 GetCageStep(int cage, int tick)
        {
            return transform.TransformVector(Vector3.up * (CageHeight(cage, tick) - CageHeight(cage, tick - 1)));
        }

        private float Progress(float tick)
        {
            return _switchTick == 0 ? 1f : Mathf.Clamp01((tick - _switchTick) * Runner.DeltaTime / _travelTime);
        }

        private float CageHeight(int cage, float tick)
        {
            Vector2 heights = _cageHeights[cage];
            float from = _isOreCageUp ? heights.x : heights.y;
            float to = _isOreCageUp ? heights.y : heights.x;

            return Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Progress(tick)));
        }

        private float ImpactTime()
        {
            return _warning + Mathf.Sqrt(2f * _weightTop / _gravity);
        }

        private float Since(int start, float tick)
        {
            return start == 0 ? -1f : (tick - start) * Runner.DeltaTime;
        }

        private void Apply(float tick)
        {
            for (int i = 0; i < _cages.Length; i++)
            {
                Vector3 position = _cages[i].localPosition;
                _cages[i].localPosition = new Vector3(position.x, CageHeight(i, tick), position.z);
            }

            float coffin = CageHeight(1, tick);
            _wall.localPosition = Vector3.Lerp(_wallClosed, _wallOpen, Mathf.InverseLerp(_wallFrom, _cageHeights[1].x, coffin));
            _wheel.localRotation = Quaternion.Euler(0f, CageHeight(0, tick) * 25f, 0f);
            float slab = 0f;

            for (int i = 0; i < SealCount; i++)
            {
                float fall = Mathf.Max(0f, Since(_sealTicks[i], tick) - _warning);
                Vector3 position = _weights[i].localPosition;
                _weights[i].localPosition = new Vector3(position.x, Mathf.Max(0f, _weightTop - 0.5f * _gravity * fall * fall), position.z);
                slab += Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Since(_sealTicks[i], tick) - ImpactTime()) / _slabTime));
            }

            _slab.localPosition = new Vector3(_slab.localPosition.x, _slabRise * slab / SealCount, _slab.localPosition.z);
        }

        private void Crush(Vector3 spot)
        {
            int count = Runner.GetPhysicsScene().OverlapBox(spot + Vector3.up * _crushHalfExtents.y, _crushHalfExtents, s_colliders, transform.rotation, _victimMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                DamageReceiverComponent receiver = s_colliders[i].GetComponentInParent<DamageReceiverComponent>();

                if (receiver == null || !receiver.IsAlive)
                    continue;

                receiver.ApplyHit(new HitRequest
                {
                    BaseDamage = 1000,
                    BodyRays = 1,
                    Zone = HitZone.Head,
                    Point = s_colliders[i].bounds.center,
                    Normal = Vector3.up,
                    AttackerPosition = spot + Vector3.up * 3f,
                    StaggerDuration = 0.5f,
                    DamageType = DamageType.True,
                    Tag = DamageTag.Trap
                });
            }
        }
    }
}
