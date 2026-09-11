using Fusion;
using UnityEngine;

namespace Game.Scripts.GameObjects.Content
{
    public sealed class Character : NetworkBehaviour, MoveComponent.ICondition, MeleeAttackComponent.ICondition
    {
        [SerializeField]
        private HealthComponent _healthComponent;
        
        [SerializeField]
        private MoveComponent _moveComponent;

        [SerializeField]
        private MeleeAttackComponent _meleeAttackComponent;

        public override void Spawned()
        {
            _moveComponent.SetCondition(this);
            _meleeAttackComponent.SetCondition(this);
        }

        bool MoveComponent.ICondition.IsMet() => _healthComponent.IsAlive;

        bool MeleeAttackComponent.ICondition.IsMet() => _healthComponent.IsAlive;
    }
}