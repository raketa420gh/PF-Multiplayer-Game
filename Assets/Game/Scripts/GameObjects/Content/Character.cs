using Fusion;
using UnityEngine;

namespace Game.Scripts.GameObjects.Content
{
    public sealed class Character : NetworkBehaviour, MoveComponent.ICondition
    {
        [SerializeField]
        private HealthComponent _healthComponent;
        
        [SerializeField]
        private MoveComponent _moveComponent;

        public override void Spawned()
        {
            _moveComponent.SetCondition(this);
        }

        bool MoveComponent.ICondition.IsMet() => _healthComponent.IsAlive;
    }
}