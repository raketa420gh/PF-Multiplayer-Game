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
        /// Combat configs of the weapon with a shield in the off hand, indexed by the shield's ShieldIndex; null = none.
        public WeaponConfig[] WeaponsWithShield => _weaponsWithShield;
        /// Which entry of the main weapon's WeaponsWithShield a shield selects: shields differ in model and block.
        public int ShieldIndex => _shieldIndex;
        public WeaponClass WeaponClass => _weaponClass;
        public bool IsTwoHanded => _isTwoHanded;
        public bool IsOffHand => _isOffHand;
        public float MoveSpeedPenalty => _moveSpeedPenalty;
        public DamageType DamageType => _damageType;
        public float LightRange => _lightRange;
        /// Held to cast readied spells; a staff is a focus only when it is a magical one.
        public bool IsFocus => _isFocus;
        /// Classes that may wield the weapon on top of its weapon class; empty means every class allowed that weapon class.
        public ClassConfig[] Classes => _classes;
        /// Bolts a crossbow loads from the bag; none means it never runs out.
        public ItemConfig Ammo => _ammo;

        [SerializeField]
        private WeaponConfig _weapon;

        [SerializeField]
        private WeaponConfig[] _weaponsWithShield = Array.Empty<WeaponConfig>();

        [SerializeField]
        private int _shieldIndex;

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

        [SerializeField]
        private bool _isFocus;

        [SerializeField]
        private ClassConfig[] _classes = Array.Empty<ClassConfig>();

        [SerializeField]
        private ItemConfig _ammo;

        public override bool CanEquip(EquipSlot slot)
        {
            bool isMain = slot is EquipSlot.Weapon1Main or EquipSlot.Weapon2Main;
            bool isOff = slot is EquipSlot.Weapon1Off or EquipSlot.Weapon2Off;

            return _isOffHand ? isOff : isMain;
        }

        /// The combat config the weapon fights with when the off hand holds that item.
        public WeaponConfig GetWeapon(WeaponItemConfig off)
        {
            bool hasShield = off != null && off.WeaponClass == WeaponClass.Shield && off.ShieldIndex < _weaponsWithShield.Length;

            return hasShield && _weaponsWithShield[off.ShieldIndex] != null ? _weaponsWithShield[off.ShieldIndex] : _weapon;
        }

        public bool Fits(ClassConfig config)
        {
            return config.CanUseWeapon(_weaponClass) && (_classes.Length == 0 || Array.IndexOf(_classes, config) >= 0);
        }
    }
}
