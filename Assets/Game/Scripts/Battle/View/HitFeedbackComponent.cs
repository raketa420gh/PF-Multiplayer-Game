using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class HitFeedbackComponent : NetworkBehaviour
    {
        [SerializeField]
        private DamageReceiverComponent _receiver;

        public override void Spawned()
        {
            _receiver.OnHitEvent += OnHitEvent;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _receiver.OnHitEvent -= OnHitEvent;
        }

        private void OnHitEvent(HitEventData hit)
        {
            if (BattleContext.Instance != null)
                BattleContext.Instance.Feedback.PlayHit(hit);
        }
    }
}
