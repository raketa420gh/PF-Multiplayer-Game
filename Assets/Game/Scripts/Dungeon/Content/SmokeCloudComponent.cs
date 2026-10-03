using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Rogue smoke pot: adventurers inside the cloud stay invisible while it lasts.
    public sealed class SmokeCloudComponent : NetworkBehaviour
    {
        [SerializeField]
        private float _radius = 3f;

        [SerializeField]
        private float _lifetime = 8f;

        [Networked]
        private TickTimer _timer { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
                _timer = TickTimer.CreateFromSeconds(Runner, _lifetime);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (_timer.Expired(Runner))
            {
                Runner.Despawn(Object);

                return;
            }

            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if (!fighter.TryGetComponent(out AdventurerComponent adventurer))
                    continue;

                if ((fighter.transform.position - transform.position).sqrMagnitude <= _radius * _radius)
                    adventurer.Effects.Add(StatusEffectKind.Invisible, 1f, 0.6f);
            }
        }
    }
}
