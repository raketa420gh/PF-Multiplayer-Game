using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Training target of the Great Hall: blows land on it as on an adventurer, and it is whole again the next tick.
    public sealed class TrainingDummyComponent : NetworkBehaviour
    {
        [SerializeField]
        private HealthComponent _health;

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && _health.CurrentHealth < _health.MaxHealth)
                _health.CurrentHealth = _health.MaxHealth;
        }
    }
}
