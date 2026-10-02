using System;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum ItemKind : byte
    {
        Weapon,
        Armor,
        Consumable,
        Utility,
        Treasure
    }

    public enum ItemRarity : byte
    {
        Poor,
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Unique
    }

    public enum EquipSlot : byte
    {
        Head,
        Chest,
        Hands,
        Legs,
        Feet,
        Back,
        Necklace,
        Ring1,
        Ring2,
        Weapon1Main,
        Weapon1Off,
        Weapon2Main,
        Weapon2Off,
        Utility1,
        Utility2,
        Utility3,
        Utility4,
        Count
    }

    public enum ArmorType : byte
    {
        Cloth,
        Leather,
        Chain,
        Plate
    }

    public enum WeaponClass : byte
    {
        Sword,
        Axe,
        Mace,
        Dagger,
        Spear,
        Bow,
        Crossbow,
        Staff,
        Shield,
        Torch,
        Spellbook
    }

    public enum StatType : byte
    {
        Strength,
        Vigor,
        Agility,
        Dexterity,
        Will,
        Knowledge,
        Resourcefulness,
        MaxHealth,
        ArmorRating,
        MagicResistance,
        MoveSpeed,
        PhysicalPower,
        MagicalPower,
        ActionSpeed,
        PhysicalDamageBonus,
        MagicalDamageBonus,
        Count
    }

    [Serializable]
    public struct StatModifier
    {
        public StatType Stat;
        public float Value;

        public StatModifier(StatType stat, float value)
        {
            Stat = stat;
            Value = value;
        }
    }

    public abstract class ItemConfig : ScriptableObject
    {
        public short Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public abstract ItemKind Kind { get; }
        public ItemRarity BaseRarity => _baseRarity;
        public int Width => _width;
        public int Height => _height;
        public int MaxStack => _maxStack;
        public int Value => _value;
        public Color IconColor => _iconColor;
        public string IconGlyph => _iconGlyph;
        public StatModifier[] Modifiers => _modifiers;
        public bool CanRollRarity => _canRollRarity;

        [SerializeField]
        private short _id;

        [SerializeField]
        private string _displayName;

        [SerializeField, TextArea]
        private string _description;

        [SerializeField]
        private ItemRarity _baseRarity = ItemRarity.Common;

        [SerializeField]
        private int _width = 1;

        [SerializeField]
        private int _height = 1;

        [SerializeField]
        private int _maxStack = 1;

        [SerializeField]
        private int _value = 10;

        [SerializeField]
        private Color _iconColor = Color.white;

        [SerializeField]
        private string _iconGlyph = "?";

        [SerializeField]
        private StatModifier[] _modifiers = Array.Empty<StatModifier>();

        [SerializeField]
        private bool _canRollRarity;

        public virtual bool CanEquip(EquipSlot slot) => false;
    }

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

    public enum ArmorVisual : byte
    {
        None,
        Hood,
        Cap,
        Helmet,
        GreatHelm,
        Tunic,
        LeatherChest,
        ChainChest,
        PlateChest,
        Gloves,
        Gauntlets,
        Pants,
        Greaves,
        Boots,
        PlateBoots,
        Cloak
    }

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

    public enum ConsumableEffect : byte
    {
        HealOverTime,
        HealInstant,
        Protection,
        Haste
    }

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
            return slot is >= EquipSlot.Utility1 and <= EquipSlot.Utility4;
        }
    }

    public enum UtilityKind : byte
    {
        ThrowingWeapon,
        Campfire,
        Lockpick
    }

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

    [CreateAssetMenu(menuName = "Game/Dungeon/Treasure Item")]
    public sealed class TreasureItemConfig : ItemConfig
    {
        public override ItemKind Kind => ItemKind.Treasure;
    }

    [CreateAssetMenu(menuName = "Game/Dungeon/Item Database")]
    public sealed class ItemDatabase : ScriptableObject
    {
        public ItemConfig[] Items => _items;

        [SerializeField]
        private ItemConfig[] _items = Array.Empty<ItemConfig>();

        [SerializeField]
        private Color[] _rarityColors =
        {
            new(0.55f, 0.55f, 0.55f), new(0.85f, 0.85f, 0.85f), new(0.35f, 0.75f, 0.35f), new(0.3f, 0.5f, 0.95f),
            new(0.65f, 0.35f, 0.9f), new(0.95f, 0.65f, 0.2f), new(0.95f, 0.35f, 0.25f)
        };

        public ItemConfig Get(int id)
        {
            return id > 0 && id <= _items.Length ? _items[id - 1] : null;
        }

        public T Get<T>(int id) where T : ItemConfig
        {
            return Get(id) as T;
        }

        public ItemConfig Find(string displayName)
        {
            foreach (ItemConfig item in _items)
            {
                if (item.DisplayName == displayName)
                    return item;
            }

            return null;
        }

        public Color GetRarityColor(ItemRarity rarity)
        {
            return _rarityColors[Mathf.Clamp((int)rarity, 0, _rarityColors.Length - 1)];
        }
    }
}
