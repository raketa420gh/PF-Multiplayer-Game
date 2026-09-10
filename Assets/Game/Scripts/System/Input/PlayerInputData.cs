using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public struct PlayerInputData : INetworkInput
    {
        public Vector2 MoveDirection;
        public NetworkButtons Buttons; //32
    }
}