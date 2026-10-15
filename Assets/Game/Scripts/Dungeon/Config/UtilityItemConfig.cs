using System;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    [CreateAssetMenu(menuName = "Game/Dungeon/Utility Item")]
    public sealed class UtilityItemConfig : ItemConfig
    {
        public override ItemKind Kind => ItemKind.Utility;
        public UtilityKind UtilityKind => _utilityKind;
        public int Damage => _damage;
        public float UseTime => _useTime;
        /// A knife, not an axe: short-blade perks count for it.
        public bool IsBlade => _isBlade;

        [SerializeField]
        private UtilityKind _utilityKind;

        [SerializeField]
        private int _damage;

        [SerializeField]
        private float _useTime = 0.5f;

        [SerializeField]
        private bool _isBlade;

        public override bool CanEquip(EquipSlot slot)
        {
            return slot is >= EquipSlot.Utility1 and <= EquipSlot.Utility6;
        }
    }
}
