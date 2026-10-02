using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class MeleeAnimComponent : NetworkBehaviour
    {
        private static readonly int Melee =  Animator.StringToHash(nameof(Melee));

        [SerializeField]
        private MeleeAttackComponent _meleeAttackComponent;
        
        [SerializeField]
        private Animator _animator;

        public override void Spawned()
        {
            _meleeAttackComponent.OnAttack += OnAttack;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _meleeAttackComponent.OnAttack -= OnAttack;
        }

        private void OnAttack()
        {
            _animator.SetTrigger(Melee);
        }
    }
}