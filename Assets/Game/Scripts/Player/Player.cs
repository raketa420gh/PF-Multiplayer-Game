using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class Player : NetworkBehaviour
    {
        [SerializeField]
        private NetworkObject _character;
        
        public override void FixedUpdateNetwork()
        {
            if (GetInput(out PlayerInputData inputData))
            {
                _character.GetBehaviour<MoveComponent>()
                    .Move(inputData.MoveDirection, inputData.Buttons.IsSet(PlayerInputButtons.Sprint));
            }
        }
    }
}