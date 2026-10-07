using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class FighterAnimComponent : NetworkBehaviour
    {
        public const string IdleSuffix = "_Idle";
        public const string AttackSuffix = "_Attack";
        public const string AttackLegsSuffix = "_AttackLegs";
        public const string RiposteSuffix = AttackSuffix + "Riposte";
        public const string RiposteLegsSuffix = AttackLegsSuffix + "Riposte";
        public const string BlockSuffix = "_Block";
        public const string BlockImpactSuffix = "_BlockImpact";
        public const string BlockLowerSuffix = "_BlockLower";
        public const string DeflectSuffix = "_Deflect";
        public const string DrawSuffix = "_Draw";
        public const string ReleaseSuffix = "_Release";
        public const string LocomotionState = "Locomotion";
        public const string AirState = "Air";
        public const string JumpState = "Jump";
        public const string LandState = "Land";
        public const string RestState = "Rest";
        public const string DeathState = "Death";
        public const string MoveXParam = "MoveX";
        public const string MoveYParam = "MoveY";
        public const string CrouchParam = "Crouch";
        public const string MirrorParam = "Mirror";
        /// Off-hand swings play mirrored while it is on (its default); the animation editor turns it off to edit them as authored.
        public const string FlipParam = "Flip";
        public const string ActionSpeedParam = "ActionSpeed";
        public const string CastState = "Cast";
        public const string UseState = "Use";
        public const string InteractState = "Interact";
        public const string InteractFirstPersonState = "InteractFp";
        public const string CastFirstPersonState = "CastFp";
        public const string UseFirstPersonState = "UseFp";
        public const string HoldState = "Hold";
        public const string ThrowState = "Throw";
        public const string OpenState = "Open";
        public const string PickUpState = "PickUp";
        public const string BandageState = "Bandage";
        public const string BandageFirstPersonState = "BandageFp";
        public const string CastReleaseState = "CastRelease";
        public const string CastReleaseFirstPersonState = "CastReleaseFp";
        public const string HitChestState = "HitChest";
        public const string HitHeadState = "HitHead";
        public const string HitStaggerState = "HitStagger";
        /// How far the standing legs clips lower the hips below the bind pose.
        public const float IdleDrop = 0.03f;

        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private Transform[] _spineBones;

        [SerializeField]
        private Transform _headBone;

        [SerializeField]
        private float _fadeTime = 0.1f;

        [SerializeField]
        private float _moveDamping = 0.1f;

        [SerializeField]
        private float _airDelay = 0.15f;

        [SerializeField]
        private float _hitFlinch = 10f;

        [SerializeField]
        private float _staggerFlinch = 22f;

        [SerializeField]
        private float _flinchDecay = 9f;

        [SerializeField]
        private float _hitReactionTime = 0.35f;

        [SerializeField]
        private float _hitReactionWeight = 0.7f;

        [SerializeField]
        private float _staggerReactionTime = 0.6f;

        [SerializeField, Tooltip("Upward speed that counts as a jump: the legs push off at once instead of waiting for the air delay")]
        private float _jumpSpeed = 2f;

        [SerializeField]
        private float _landTime = 0.35f;

        [SerializeField]
        private float _landFadeTime = 0.2f;

        [SerializeField]
        private float _restFadeTime = 0.3f;

        private const int BaseLayer = 0;
        private const int UpperLayer = 1;
        private const int HitLayer = 2;
        private const float RestSpeed = 0.3f;

        private static readonly int s_moveX = Animator.StringToHash(MoveXParam);
        private static readonly int s_moveY = Animator.StringToHash(MoveYParam);
        private static readonly int s_crouch = Animator.StringToHash(CrouchParam);
        private static readonly int s_mirror = Animator.StringToHash(MirrorParam);
        private static readonly int s_locomotion = Animator.StringToHash(LocomotionState);
        private static readonly int s_air = Animator.StringToHash(AirState);
        private static readonly int s_jump = Animator.StringToHash(JumpState);
        private static readonly int s_land = Animator.StringToHash(LandState);
        private static readonly int s_rest = Animator.StringToHash(RestState);
        private static readonly int s_death = Animator.StringToHash(DeathState);
        private static readonly int s_actionSpeed = Animator.StringToHash(ActionSpeedParam);
        private static readonly int s_hitChest = Animator.StringToHash(HitChestState);
        private static readonly int s_hitHead = Animator.StringToHash(HitHeadState);
        private static readonly int s_hitStagger = Animator.StringToHash(HitStaggerState);
        private static readonly int s_hold = Animator.StringToHash(HoldState);

        /// Indexed by CombatComponent.BusyKind.
        private static readonly int[] s_busy =
        {
            Animator.StringToHash(CastState), Animator.StringToHash(UseState), Animator.StringToHash(InteractState),
            Animator.StringToHash(ThrowState), Animator.StringToHash(OpenState), Animator.StringToHash(PickUpState),
            Animator.StringToHash(BandageState), Animator.StringToHash(CastReleaseState)
        };

        /// Own-eyes variants of the busy states whose library motion stays outside the first-person view; 0 = there is none.
        /// Everyone plays them: a fighter moves the same in own eyes and for those who watch.
        private static readonly int[] s_busyFirstPerson =
        {
            Animator.StringToHash(CastFirstPersonState), Animator.StringToHash(UseFirstPersonState), Animator.StringToHash(InteractFirstPersonState), 0, 0, 0,
            Animator.StringToHash(BandageFirstPersonState), Animator.StringToHash(CastReleaseFirstPersonState)
        };

        private struct WeaponStates
        {
            public int Idle;
            public int Block;
            public int BlockImpact;
            public int BlockLower;
            public int Deflect;
            public int Draw;
            public int Release;
            public int[] Attacks;
            public int[] AttackLegs;
            public int Riposte;
            public int RiposteLegs;
        }

        private WeaponStates[] _weaponStates;
        private Transform[] _sockets;
        private int _baseState;
        private int _baseToken;
        private int _upperState;
        private int _upperToken;
        private float _airTime;
        private float _pitch;
        private float _flinch;
        private float _hitTime;
        private float _upperWeight = 1f;
        private float _lowerLeft;
        private float _landLeft;
        private bool _isHeadHidden;
        private bool _isKneeling;
        private bool _isHolding;
        private float _crouch;
        private float _spineHeight;

        private void Awake()
        {
            if (_spineBones.Length > 0)
                _spineHeight = transform.InverseTransformPoint(_spineBones[0].position).y;

            Transform right = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform left = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            _sockets = new Transform[4];
            _sockets[(int)WeaponSocket.RightHand] = right != null ? right.Find("RightHandSocket") : null;
            _sockets[(int)WeaponSocket.LeftHand] = left != null ? left.Find("LeftHandSocket") : null;
            _sockets[(int)WeaponSocket.RightShield] = right != null ? right.Find("RightHandShieldSocket") : null;
            _sockets[(int)WeaponSocket.LeftShield] = left != null ? left.Find("LeftHandShieldSocket") : null;
        }

        public override void Spawned()
        {
            WeaponConfig[] loadout = _fighter.Combat.Catalog;
            _weaponStates = new WeaponStates[loadout.Length];

            for (int i = 0; i < loadout.Length; i++)
                _weaponStates[i] = CreateStates(loadout[i]);

            _isHeadHidden = HasInputAuthority;
            _fighter.Receiver.OnHitEvent += OnHitEvent;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _fighter.Receiver.OnHitEvent -= OnHitEvent;
        }

        public override void Render()
        {
            float deltaTime = Time.deltaTime;
            bool isAlive = _fighter.Health.IsAlive;

            UpdateLocomotion(isAlive, deltaTime);
            UpdateUpperBody(isAlive, deltaTime);
            UpdatePitch();
        }

        /// While set, a fighter who stands still goes down on one knee.
        public void SetKneeling(bool isKneeling)
        {
            _isKneeling = isKneeling;
        }

        /// While set, the idle hands hold an item instead of the weapon.
        public void SetHolding(bool isHolding)
        {
            _isHolding = isHolding;
        }

        private void LateUpdate()
        {
            if (Object != null && Object.IsValid && _fighter.Combat.Weapon.IsMirrored)
                SocketMirror.Apply(_sockets);

            if (_spineBones.Length == 0)
                return;

            _flinch = Mathf.Lerp(_flinch, 0f, _flinchDecay * Time.deltaTime);

            Vector3 axis = transform.right;
            Quaternion step = Quaternion.AngleAxis(_pitch * _upperWeight / _spineBones.Length, axis);

            foreach (Transform bone in _spineBones)
                bone.rotation = step * bone.rotation;

            _spineBones[0].rotation = Quaternion.AngleAxis(-_flinch, axis) * _spineBones[0].rotation;

            if (_isHeadHidden)
                _headBone.localScale = Vector3.zero;

            HoldUpperBody();
        }

        private static WeaponStates CreateStates(WeaponConfig weapon)
        {
            string prefix = weapon.AnimationPrefix;
            int[] attacks = new int[weapon.Attacks.Length];
            int[] attackLegs = new int[attacks.Length];

            for (int i = 0; i < attacks.Length; i++)
            {
                attacks[i] = Animator.StringToHash(prefix + AttackSuffix + i);
                attackLegs[i] = Animator.StringToHash(prefix + AttackLegsSuffix + i);
            }

            return new WeaponStates
            {
                Idle = Animator.StringToHash(prefix + IdleSuffix),
                Block = Animator.StringToHash(prefix + BlockSuffix),
                BlockImpact = Animator.StringToHash(prefix + BlockImpactSuffix),
                BlockLower = Animator.StringToHash(prefix + BlockLowerSuffix),
                Deflect = Animator.StringToHash(prefix + DeflectSuffix),
                Draw = Animator.StringToHash(prefix + DrawSuffix),
                Release = Animator.StringToHash(prefix + ReleaseSuffix),
                Attacks = attacks,
                AttackLegs = attackLegs,
                Riposte = Animator.StringToHash(prefix + RiposteSuffix),
                RiposteLegs = Animator.StringToHash(prefix + RiposteLegsSuffix)
            };
        }

        private void UpdateLocomotion(bool isAlive, float deltaTime)
        {
            FighterMoveComponent move = _fighter.Move;
            Vector3 velocity = Quaternion.Inverse(transform.rotation) * move.Velocity;
            float walkSpeed = move.Config.RunSpeed * move.Config.WalkMultiplier;
            _crouch = new NetworkBehaviourBufferInterpolator(move).Float(nameof(FighterMoveComponent.CrouchAmount));

            _animator.SetFloat(s_moveX, velocity.x / walkSpeed, _moveDamping, deltaTime);
            _animator.SetFloat(s_moveY, velocity.z / walkSpeed, _moveDamping, deltaTime);
            _animator.SetFloat(s_crouch, _crouch);

            _airTime = move.IsGrounded ? 0f : _airTime + deltaTime;
            _landLeft -= deltaTime;

            bool wasInAir = _baseState == s_air || _baseState == s_jump;
            bool isRising = !move.IsGrounded && move.Velocity.y > _jumpSpeed;
            float fadeTime = _fadeTime;
            float time = 0f;
            int token = 0;
            int state;

            if (!isAlive)
            {
                state = s_death;
            }
            else if (_airTime > _airDelay || isRising)
            {
                // The push-off plays once on the way up; from the top of the jump, or when walking off a ledge, the legs hang.
                state = move.Velocity.y > 0f && (isRising || _baseState == s_jump) ? s_jump : s_air;
            }
            else if (wasInAir)
            {
                state = s_land;
                _landLeft = _landTime;
            }
            else if (_baseState == s_land && _landLeft > 0f)
            {
                state = s_land;
            }
            else
            {
                CombatComponent combat = _fighter.Combat;
                bool isStill = Mathf.Abs(velocity.x) + Mathf.Abs(velocity.z) < RestSpeed;
                state = _isKneeling && isStill ? s_rest : s_locomotion;
                fadeTime = _baseState == s_land ? _landFadeTime : state == s_rest || _baseState == s_rest ? _restFadeTime : _fadeTime;

                // One who strikes without walking steps into the swing.
                if (isStill && combat.State == CombatState.Attack)
                {
                    WeaponStates states = _weaponStates[combat.WeaponIndex];
                    state = combat.IsRiposte ? states.RiposteLegs : states.AttackLegs[combat.AttackIndex];
                    time = combat.StateTime;
                    token = combat.StateTick;
                }
            }

            if (state == _baseState && token == _baseToken)
                return;

            _baseState = state;
            _baseToken = token;
            _animator.CrossFadeInFixedTime(state, fadeTime, BaseLayer, time);
        }

        private void UpdateUpperBody(bool isAlive, float deltaTime)
        {
            _upperWeight = Mathf.MoveTowards(_upperWeight, isAlive ? 1f : 0f, deltaTime * 5f);
            _animator.SetLayerWeight(UpperLayer, _upperWeight);

            _hitTime = Mathf.Max(0f, _hitTime - deltaTime);
            _animator.SetLayerWeight(HitLayer, Mathf.Clamp01(_hitTime / _fadeTime) * _hitReactionWeight * _upperWeight);

            CombatComponent combat = _fighter.Combat;
            WeaponConfig weapon = combat.Weapon;
            WeaponStates states = _weaponStates[combat.WeaponIndex];
            float time = combat.StateTime;
            int token = 0;
            int state;

            switch (combat.State)
            {
                case CombatState.Attack:
                    state = combat.IsRiposte ? states.Riposte : states.Attacks[combat.AttackIndex];
                    token = combat.StateTick;
                    break;
                case CombatState.BlockRaise:
                    state = states.Block;
                    _lowerLeft = weapon.Block.LowerTime;
                    break;
                case CombatState.Block:
                    state = states.Block;
                    time += weapon.Block.RaiseTime;
                    _lowerLeft = weapon.Block.LowerTime;
                    break;
                case CombatState.BlockImpact:
                    state = states.BlockImpact;
                    token = combat.StateTick;
                    break;
                case CombatState.Deflected:
                    state = states.Deflect;
                    token = combat.StateTick;
                    break;
                case CombatState.Draw:
                    state = states.Draw;
                    token = combat.StateTick;
                    break;
                case CombatState.Reload:
                    state = states.Release;
                    token = combat.StateTick;
                    break;
                case CombatState.Stagger:
                    state = states.Idle;
                    token = combat.StateTick;
                    break;
                case CombatState.Busy:
                    int kind = Mathf.Min(combat.BusyKind, s_busy.Length - 1);
                    state = s_busyFirstPerson[kind] != 0 ? s_busyFirstPerson[kind] : s_busy[kind];
                    token = combat.StateTick;
                    break;
                default:
                    // A let-go block brings the guard down along a clip of its own before the idle takes over.
                    _lowerLeft = combat.State == CombatState.Idle && !_isHolding ? _lowerLeft - deltaTime : 0f;
                    state = _lowerLeft > 0f ? states.BlockLower : _isHolding ? s_hold : states.Idle;
                    time = 0f;
                    break;
            }

            _animator.SetBool(s_mirror, weapon.IsMirrored);
            _animator.SetFloat(s_actionSpeed, combat.TimeScale);

            if (state == _upperState && token == _upperToken)
                return;

            if (combat.State == CombatState.Stagger)
            {
                _flinch = _staggerFlinch;
                _hitTime = _staggerReactionTime;
                _animator.Play(s_hitStagger, HitLayer, 0f);
            }

            _upperState = state;
            _upperToken = token;
            _animator.CrossFadeInFixedTime(state, _fadeTime, UpperLayer, time);
        }

        private void UpdatePitch()
        {
            BattleContext context = BattleContext.Instance;

            _pitch = HasInputAuthority && context != null
                ? context.Input.LookRotation.x
                : new NetworkBehaviourBufferInterpolator(_fighter.Move).Float(nameof(FighterMoveComponent.Pitch));
        }

        /// The legs clips bob the hips (breath, crouch, push-off, landing), the eye follows the body model and does not:
        /// the upper body is held at the model's height, so the arms stay put on the screen and others see the same motion.
        private void HoldUpperBody()
        {
            Transform spine = _spineBones[0];
            float height = _spineHeight - IdleDrop - _fighter.Body.Config.CrouchDrop * _crouch;
            spine.position += transform.up * ((height - transform.InverseTransformPoint(spine.position).y) * _upperWeight);
        }

        private void OnHitEvent(HitEventData hit)
        {
            if (hit.Result == HitResult.Blocked)
                return;

            _flinch = Mathf.Max(_flinch, _hitFlinch);

            // A hit that staggers is answered by the heavier recoil, whichever of the two is noticed first.
            if (_fighter.Combat.State == CombatState.Stagger)
                return;

            _hitTime = _hitReactionTime;
            _animator.Play(hit.Zone == HitZone.Head ? s_hitHead : s_hitChest, HitLayer, 0f);
        }
    }
}
