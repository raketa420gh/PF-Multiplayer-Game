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

        [SerializeField]
        private UtilityKind _utilityKind;

        [SerializeField]
        private int _damage;

        [SerializeField]
        private float _useTime = 0.5f;

        public override bool CanEquip(EquipSlot slot)
        {
            return slot is >= EquipSlot.Utility1 and <= EquipSlot.Utility4;
        }
    }
}
