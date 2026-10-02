using System;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    [CreateAssetMenu(menuName = "Game/Dungeon/Treasure Item")]
    public sealed class TreasureItemConfig : ItemConfig
    {
        public override ItemKind Kind => ItemKind.Treasure;
    }
}
