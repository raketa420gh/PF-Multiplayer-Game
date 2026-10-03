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
        Spellbook,
        CrystalBall,
        Instrument
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
        public Sprite Icon => _icon;
        public GameObject WorldModel => _worldModel;

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

        [SerializeField]
        private Sprite _icon;

        [SerializeField]
        private GameObject _worldModel;

        public virtual bool CanEquip(EquipSlot slot) => false;
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
        Cloak,
        Skull,
        Ribcage
    }

    public enum ConsumableEffect : byte
    {
        HealOverTime,
        HealInstant,
        Protection,
        Haste
    }

    public enum UtilityKind : byte
    {
        ThrowingWeapon,
        Campfire,
        Lockpick
    }
}
