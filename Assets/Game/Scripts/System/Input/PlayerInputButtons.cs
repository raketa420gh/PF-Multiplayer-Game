using System;

namespace Game.Scripts
{
    [Flags]
    public enum PlayerInputButtons : int
    {
        Sprint = 1,
        Attack = 2,
        Primary = 3,
        Secondary = 4,
        Crouch = 5,
        Jump = 6,
        Weapon1 = 7,
        Weapon2 = 8,
        Weapon3 = 9,
        Weapon4 = 10,
        BotMode = 11
    }
}
