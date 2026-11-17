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
        /// Classes that may wear the piece; empty means everyone.
        public ClassConfig[] Classes => _classes;

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

        [SerializeField]
        private ClassConfig[] _classes = Array.Empty<ClassConfig>();

        public override bool CanEquip(EquipSlot slot)
        {
            if (_slot == EquipSlot.Ring1)
                return slot is EquipSlot.Ring1 or EquipSlot.Ring2;

            return slot == _slot;
        }

        /// wearsPlate: a perk lets the class wear any plate.
        public bool Fits(ClassConfig config, bool wearsPlate = false)
        {
            return (wearsPlate && _armorType == ArmorType.Plate) || _classes.Length == 0 || Array.IndexOf(_classes, config) >= 0;
        }
    }
}
