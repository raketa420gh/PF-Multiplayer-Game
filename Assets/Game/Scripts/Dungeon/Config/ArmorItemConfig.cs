using System;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    [CreateAssetMenu(menuName = "Game/Dungeon/Armor Item")]
    public sealed class ArmorItemConfig : ItemConfig
    {
        public override ItemKind Kind => ItemKind.Armor;
        public EquipSlot Slot => _slot;
        public ArmorType ArmorType => _armorType;
        public float ArmorRating => _armorRating;
        public float MagicResistance => _magicResistance;
        public float MoveSpeedPenalty => _moveSpeedPenalty;
        public ArmorVisual Visual => _visual;
        public Color VisualColor => _visualColor;

        [SerializeField]
        private EquipSlot _slot;

        [SerializeField]
        private ArmorType _armorType;

        [SerializeField]
        private float _armorRating;

        [SerializeField]
        private float _magicResistance;

        [SerializeField]
        private float _moveSpeedPenalty;

        [SerializeField]
        private ArmorVisual _visual;

        [SerializeField]
        private Color _visualColor = Color.gray;

        public override bool CanEquip(EquipSlot slot)
        {
            if (_slot == EquipSlot.Ring1)
                return slot is EquipSlot.Ring1 or EquipSlot.Ring2;

            return slot == _slot;
        }
    }
}
