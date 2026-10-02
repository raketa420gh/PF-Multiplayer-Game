using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class HitFeedbackComponent : NetworkBehaviour
    {
        [SerializeField]
        private DamageReceiverComponent _receiver;

        [SerializeField]
        private CombatComponent _combat;

        public override void Spawned()
        {
            _receiver.OnHitEvent += OnHitEvent;

            if (_combat != null)
                _combat.OnWorldHit += OnWorldHit;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _receiver.OnHitEvent -= OnHitEvent;

            if (_combat != null)
                _combat.OnWorldHit -= OnWorldHit;
        }

        private void OnHitEvent(HitEventData hit)
        {
            if (BattleContext.Instance != null)
                BattleContext.Instance.Feedback.PlayHit(hit);
        }

        private void OnWorldHit(Vector3 point, Vector3 normal)
        {
            if (BattleContext.Instance != null)
                BattleContext.Instance.Feedback.PlayWorldHit(point, normal);
        }
    }
}
