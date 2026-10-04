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
        BotMode = 11,
        Interact = 12,
        Skill1 = 13,
        Skill2 = 14,
        Spell1 = 15,
        Spell2 = 16,
        Spell3 = 17,
        Spell4 = 18,
        Spell5 = 19,
        Holster = 20,
        Rest = 24
    }
}
