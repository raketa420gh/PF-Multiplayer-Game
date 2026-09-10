using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class Player : NetworkBehaviour
    {
        [SerializeField]
        private NetworkObject _character;
        
        [Networked]
        private NetworkButtons _previousButtons { get; set;}
        
        public override void FixedUpdateNetwork()
        {
            if (GetInput(out PlayerInputData inputData))
            {
                NetworkButtons inputButtons = inputData.Buttons;
                
                ProcessMove(inputButtons, inputData);
                ProcessAttack(inputButtons);
                _previousButtons = inputButtons;
            }
        }

        private void ProcessAttack(NetworkButtons inputButtons)
        {
            if (inputButtons.WasPressed(_previousButtons, PlayerInputButtons.Attack))
                _character.GetBehaviour<MeleeAttackComponent>().Attack();
        }

        private void ProcessMove(NetworkButtons inputButtons, PlayerInputData inputData)
        {
            bool isSprint = inputButtons.IsSet(PlayerInputButtons.Sprint);
            Vector3 moveDirection = new Vector3(inputData.MoveDirection.x, 0f, inputData.MoveDirection.y);
            _character.GetBehaviour<MoveComponent>().Move(moveDirection, isSprint);
        }
    }
}