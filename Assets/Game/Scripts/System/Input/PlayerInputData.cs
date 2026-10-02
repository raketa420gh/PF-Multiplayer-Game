using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public struct PlayerInputData : INetworkInput
    {
        public Vector2 MoveDirection;
        public Vector2 LookRotation; //x - pitch, y - yaw
        public NetworkButtons Buttons; //32
    }
}
