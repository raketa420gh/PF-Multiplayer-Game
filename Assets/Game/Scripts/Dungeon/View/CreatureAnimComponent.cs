using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A monster on its own animator instead of the fighter rig: a speed blend, a fall loop, one attack clip stretched over the combat phases,
    /// a flinch on hits and death.
    public sealed class CreatureAnimComponent : NetworkBehaviour
    {
        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField]
        private Animator _animator;

        [SerializeField, Tooltip("Normalized time of the blow in the attack clip: it lands in the middle of the active window.")]
        private float _attackImpact = 0.4f;

        [SerializeField]
        private float _fallSpeed = 3f;

        private static readonly int s_speed = Animator.StringToHash("Speed");
        private static readonly int s_attackTime = Animator.StringToHash("AttackTime");
        private static readonly int s_locomotion = Animator.StringToHash("Locomotion");
        private static readonly int s_fall = Animator.StringToHash("Fall");
        private static readonly int s_attack = Animator.StringToHash("Attack");
        private static readonly int s_death = Animator.StringToHash("Death");
        private static readonly int s_hit = Animator.StringToHash("Hit");
        private const int HitLayer = 1;

        private int _state;
        private int _attackTick;

        public override void Spawned()
        {
            _fighter.Receiver.OnHitEvent += OnHitEvent;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _fighter.Receiver.OnHitEvent -= OnHitEvent;
        }

        public override void Render()
        {
            CombatComponent combat = _fighter.Combat;
            Vector3 velocity = _fighter.Move.Velocity;
            _animator.SetFloat(s_speed, new Vector2(velocity.x, velocity.z).magnitude, 0.1f, Time.deltaTime);
            int state = !_fighter.Health.IsAlive ? s_death
                : combat.State == CombatState.Attack ? s_attack
                : !_fighter.Move.IsGrounded && velocity.y < -_fallSpeed ? s_fall
                : s_locomotion;

            if (state == s_attack)
                _animator.SetFloat(s_attackTime, GetAttackTime(combat));

            if (state == _state && (state != s_attack || combat.StateTick == _attackTick))
                return;

            _animator.CrossFadeInFixedTime(state, state == s_attack ? 0.1f : 0.2f, 0);
            _state = state;
            _attackTick = combat.StateTick;
        }

        /// The windup plays the clip up to the blow, the rest of the attack plays what follows it.
        private float GetAttackTime(CombatComponent combat)
        {
            MeleeAttackConfig attack = combat.Attack;
            float time = combat.StateTime;
            float impact = (attack.ActiveStart + attack.ActiveEnd) * 0.5f;

            return time < impact ? _attackImpact * time / impact : Mathf.Lerp(_attackImpact, 1f, (time - impact) / (attack.Duration - impact));
        }

        private void OnHitEvent(HitEventData hit)
        {
            if (_fighter.Health.IsAlive && _fighter.Combat.State != CombatState.Attack)
                _animator.CrossFadeInFixedTime(s_hit, 0.05f, HitLayer, 0f);
        }
    }
}
