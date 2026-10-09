using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class HitFeedbackComponent : NetworkBehaviour
    {
        /// Optional, on the same object: what a body hit lands on when equipment changes it (plate armor).
        public interface IBodySource
        {
            /// None keeps the serialized body surface.
            ImpactSurface GetBodySurface(HitZone zone);
        }

        [SerializeField]
        private DamageReceiverComponent _receiver;

        [SerializeField]
        private CombatComponent _combat;

        [SerializeField]
        private ImpactSurface _body = ImpactSurface.Flesh;

        [SerializeField, Tooltip("What a blocked blow lands on when there is no combat component to tell a shield from a weapon")]
        private ImpactSurface _blockSurface = ImpactSurface.Clash;

        private IBodySource _bodySource;

        public override void Spawned()
        {
            _bodySource = GetComponent<IBodySource>();
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

        private ImpactSurface GetSurface(HitEventData hit)
        {
            if (hit.Result != HitResult.Hit)
                return _combat != null ? _combat.Weapon.HasShield ? ImpactSurface.Shield : ImpactSurface.Clash : _blockSurface;

            ImpactSurface surface = _bodySource != null ? _bodySource.GetBodySurface(hit.Zone) : ImpactSurface.None;

            return surface == ImpactSurface.None ? _body : surface;
        }

        private void OnHitEvent(HitEventData hit)
        {
            if (BattleContext.Instance != null)
                BattleContext.Instance.Feedback.PlayHit(hit, GetSurface(hit));
        }

        private void OnWorldHit(Vector3 point, Vector3 normal, ImpactSurface surface)
        {
            if (BattleContext.Instance != null)
                BattleContext.Instance.Feedback.PlayWorldHit(point, normal, _combat.Weapon.Sounds, surface);
        }
    }
}
