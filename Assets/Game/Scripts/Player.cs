using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class Player : NetworkBehaviour
    {
        [SerializeField]
        private float _moveSpeed;

        [SerializeField] 
        private float _speedMultiplier = 2;
        
        public override void FixedUpdateNetwork()
        {
            if (GetInput(out PlayerInputData inputData))
            {
                bool isSprint = inputData.Buttons.IsSet(PlayerInputButtons.Sprint);
                Move(inputData.MoveDirection, isSprint);
            }
        }

        private void Move(Vector2 moveDirection, bool isSprint)
        {
            Vector3 direction = new Vector3(moveDirection.x, 0, moveDirection.y);
            
            float moveSpeed = _moveSpeed;
            
            if (isSprint)
                moveSpeed *= _speedMultiplier;
            
            transform.position += direction * Runner.DeltaTime * moveSpeed;
        }
    }
}