using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public class MoveAnimComponent : NetworkBehaviour
    {
        private static readonly int IsMoving = Animator.StringToHash(nameof(IsMoving));

        [SerializeField]
        private MoveComponent _moveComponent;
        
        [SerializeField]
        private Animator _animator;

        public override void Spawned()
        {
            _moveComponent.OnMovingStateChanged += OnMovingStateChanged;
            OnMovingStateChanged(_moveComponent.IsMoving);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _moveComponent.OnMovingStateChanged -= OnMovingStateChanged;
        }

        private void OnMovingStateChanged(bool isMoving)
        {
            _animator.SetBool(IsMoving, isMoving);
        }
    }
}