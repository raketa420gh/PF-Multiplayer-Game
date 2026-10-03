using System;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    [CreateAssetMenu(menuName = "Game/Dungeon/Consumable Item")]
    public sealed class ConsumableItemConfig : ItemConfig
    {
        public override ItemKind Kind => ItemKind.Consumable;
        public ConsumableEffect Effect => _effect;
        public float Magnitude => _magnitude;
        public float Duration => _duration;
        public float UseTime => _useTime;

        [SerializeField]
        private ConsumableEffect _effect;

        [SerializeField]
        private float _magnitude = 20f;

        [SerializeField]
        private float _duration = 12f;

        [SerializeField]
        private float _useTime = 1f;

        public override bool CanEquip(EquipSlot slot)
        {
            return slot is >= EquipSlot.Utility1 and <= EquipSlot.Utility6;
        }
    }
}
