using System;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    [CreateAssetMenu(menuName = "Game/Dungeon/Weapon Item")]
    public sealed class WeaponItemConfig : ItemConfig
    {
        public override ItemKind Kind => ItemKind.Weapon;
        public WeaponConfig Weapon => _weapon;
        public WeaponConfig WeaponWithShield => _weaponWithShield;
        public WeaponClass WeaponClass => _weaponClass;
        public bool IsTwoHanded => _isTwoHanded;
        public bool IsOffHand => _isOffHand;
        public float MoveSpeedPenalty => _moveSpeedPenalty;
        public DamageType DamageType => _damageType;
        public float LightRange => _lightRange;

        [SerializeField]
        private WeaponConfig _weapon;

        [SerializeField]
        private WeaponConfig _weaponWithShield;

        [SerializeField]
        private WeaponClass _weaponClass;

        [SerializeField]
        private bool _isTwoHanded;

        [SerializeField]
        private bool _isOffHand;

        [SerializeField]
        private float _moveSpeedPenalty;

        [SerializeField]
        private DamageType _damageType = DamageType.Physical;

        [SerializeField]
        private float _lightRange;

        public override bool CanEquip(EquipSlot slot)
        {
            bool isMain = slot is EquipSlot.Weapon1Main or EquipSlot.Weapon2Main;
            bool isOff = slot is EquipSlot.Weapon1Off or EquipSlot.Weapon2Off;

            return _isOffHand ? isOff : isMain;
        }
    }
}
