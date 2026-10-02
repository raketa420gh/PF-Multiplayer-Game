using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class FighterAnimComponent : NetworkBehaviour
    {
        public const string IdleSuffix = "_Idle";
        public const string AttackSuffix = "_Attack";
        public const string BlockSuffix = "_Block";
        public const string BlockImpactSuffix = "_BlockImpact";
        public const string DeflectSuffix = "_Deflect";
        public const string DrawSuffix = "_Draw";
        public const string ReleaseSuffix = "_Release";
        public const string LocomotionState = "Locomotion";
        public const string AirState = "Air";
        public const string DeathState = "Death";
        public const string MoveXParam = "MoveX";
        public const string MoveYParam = "MoveY";
        public const string CrouchParam = "Crouch";
        public const string MirrorParam = "Mirror";

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

        private const int BaseLayer = 0;
        private const int UpperLayer = 1;

        private static readonly int s_moveX = Animator.StringToHash(MoveXParam);
        private static readonly int s_moveY = Animator.StringToHash(MoveYParam);
        private static readonly int s_crouch = Animator.StringToHash(CrouchParam);
        private static readonly int s_mirror = Animator.StringToHash(MirrorParam);
        private static readonly int s_locomotion = Animator.StringToHash(LocomotionState);
        private static readonly int s_air = Animator.StringToHash(AirState);
        private static readonly int s_death = Animator.StringToHash(DeathState);

        private struct WeaponStates
        {
            public int Idle;
            public int Block;
            public int BlockImpact;
            public int Deflect;
            public int Draw;
            public int Release;
            public int[] Attacks;
        }

        private WeaponStates[] _weaponStates;
        private int _baseState;
        private int _upperState;
        private int _upperToken;
        private float _airTime;
        private float _pitch;
        private float _flinch;
        private float _upperWeight = 1f;
        private bool _isHeadHidden;

        public override void Spawned()
        {
            WeaponConfig[] loadout = _fighter.Combat.Loadout;
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

        private void LateUpdate()
        {
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
        }

        private static WeaponStates CreateStates(WeaponConfig weapon)
        {
            string prefix = weapon.AnimationPrefix;
            int[] attacks = new int[weapon.Attacks.Length];

            for (int i = 0; i < attacks.Length; i++)
                attacks[i] = Animator.StringToHash(prefix + AttackSuffix + i);

            return new WeaponStates
            {
                Idle = Animator.StringToHash(prefix + IdleSuffix),
                Block = Animator.StringToHash(prefix + BlockSuffix),
                BlockImpact = Animator.StringToHash(prefix + BlockImpactSuffix),
                Deflect = Animator.StringToHash(prefix + DeflectSuffix),
                Draw = Animator.StringToHash(prefix + DrawSuffix),
                Release = Animator.StringToHash(prefix + ReleaseSuffix),
                Attacks = attacks
            };
        }

        private void UpdateLocomotion(bool isAlive, float deltaTime)
        {
            FighterMoveComponent move = _fighter.Move;
            Vector3 velocity = Quaternion.Inverse(transform.rotation) * move.Velocity;
            float walkSpeed = move.Config.WalkSpeed;
            float crouch = new NetworkBehaviourBufferInterpolator(move).Float(nameof(FighterMoveComponent.CrouchAmount));

            _animator.SetFloat(s_moveX, velocity.x / walkSpeed, _moveDamping, deltaTime);
            _animator.SetFloat(s_moveY, velocity.z / walkSpeed, _moveDamping, deltaTime);
            _animator.SetFloat(s_crouch, crouch);

            _airTime = move.IsGrounded ? 0f : _airTime + deltaTime;

            int state = !isAlive ? s_death : _airTime > _airDelay ? s_air : s_locomotion;

            if (state == _baseState)
                return;

            _baseState = state;
            _animator.CrossFadeInFixedTime(state, _fadeTime, BaseLayer);
        }

        private void UpdateUpperBody(bool isAlive, float deltaTime)
        {
            _upperWeight = Mathf.MoveTowards(_upperWeight, isAlive ? 1f : 0f, deltaTime * 5f);
            _animator.SetLayerWeight(UpperLayer, _upperWeight);

            CombatComponent combat = _fighter.Combat;
            WeaponConfig weapon = combat.Weapon;
            WeaponStates states = _weaponStates[combat.WeaponSlot];
            float time = combat.StateTime;
            int token = 0;
            int state;

            switch (combat.State)
            {
                case CombatState.Attack:
                    state = states.Attacks[combat.AttackIndex];
                    token = combat.StateTick;
                    break;
                case CombatState.BlockRaise:
                    state = states.Block;
                    break;
                case CombatState.Block:
                    state = states.Block;
                    time += weapon.Block.RaiseTime;
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
                default:
                    state = states.Idle;
                    time = 0f;
                    break;
            }

            _animator.SetBool(s_mirror, weapon.IsMirrored);

            if (state == _upperState && token == _upperToken)
                return;

            if (combat.State == CombatState.Stagger)
                _flinch = _staggerFlinch;

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

        private void OnHitEvent(HitEventData hit)
        {
            if (hit.Result != HitResult.Blocked)
                _flinch = Mathf.Max(_flinch, _hitFlinch);
        }
    }
}
