using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class MoveComponent : NetworkBehaviour
    {
        [SerializeField]
        private float _moveSpeed = 5;

        [SerializeField] 
        private float _speedMultiplier = 2;

        [SerializeField]
        private float _angularSpeed = 720;
        
        public void Move(Vector3 direction, bool isSprint)
        {
            if (direction == Vector3.zero)
                return;
            
            UpdateRotation(direction, Runner.DeltaTime);
            UpdatePosition(direction, isSprint, Runner.DeltaTime);
        }

        private void UpdateRotation(Vector2 direction, float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion currentRotation = transform.rotation;
            
            transform.rotation = Quaternion.RotateTowards(currentRotation, targetRotation, _angularSpeed * deltaTime);
        }

        private void UpdatePosition(Vector3 direction, bool isSprint, float deltaTime)
        {
            float moveSpeed = _moveSpeed;
            
            if (isSprint)
                moveSpeed *= _speedMultiplier;
            
            transform.position += direction * deltaTime * moveSpeed;
        }
    }
}