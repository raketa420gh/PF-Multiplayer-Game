using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class TrainingDummyComponent : NetworkBehaviour, DamageReceiverComponent.IOwner
    {
        [SerializeField]
        private HealthComponent _health;

        [SerializeField]
        private DamageReceiverComponent _receiver;

        [SerializeField]
        private float _restoreDelay = 3f;

        [Header("Optional static shield")]
        [SerializeField]
        private WeaponConfig _blockWeapon;

        [SerializeField]
        private Hitbox _blockHitbox;

        [Networked]
        private TickTimer _restoreTimer { get; set; }

        [Networked]
        private TickTimer _blockLockout { get; set; }

        private bool IsBlocking => _blockWeapon != null && _blockLockout.ExpiredOrNotRunning(Runner);

        public override void Spawned()
        {
            _receiver.SetOwner(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (_restoreTimer.Expired(Runner))
            {
                _restoreTimer = TickTimer.None;
                _health.Restore(_health.MaxHealth);
            }

            if (_blockHitbox != null)
                _receiver.HitboxRoot.SetHitboxActive(_blockHitbox, IsBlocking);
        }

        BlockConfig DamageReceiverComponent.IOwner.ActiveBlock => IsBlocking ? _blockWeapon.Block : null;

        Vector3 DamageReceiverComponent.IOwner.BlockDirection => transform.forward;

        void DamageReceiverComponent.IOwner.OnHitReceived(HitResult result, float staggerDuration)
        {
            _restoreTimer = TickTimer.CreateFromSeconds(Runner, _restoreDelay);

            if (result == HitResult.Hit)
                return;

            BlockConfig block = _blockWeapon.Block;
            _blockLockout = TickTimer.CreateFromSeconds(Runner, block.ImpactDuration + block.RecoveryDuration);
        }
    }
}
