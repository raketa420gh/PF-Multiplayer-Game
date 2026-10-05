using System;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum CastFocus : byte
    {
        Magic,
        Instrument,
        BareHands
    }

    /// The six attributes of the hexagram, in ring order: each one neighbours the next and the last neighbours the first.
    [Serializable]
    public struct ClassStats
    {
        public int Flesh;
        public int Grip;
        public int Reflex;
        public int Craft;
        public int Insight;
        public int Resonance;

        public ClassStats(int flesh, int grip, int reflex, int craft, int insight, int resonance)
        {
            Flesh = flesh;
            Grip = grip;
            Reflex = reflex;
            Craft = craft;
            Insight = insight;
            Resonance = resonance;
        }

        public int Get(StatType stat)
        {
            return stat switch
            {
                StatType.Flesh => Flesh,
                StatType.Grip => Grip,
                StatType.Reflex => Reflex,
                StatType.Craft => Craft,
                StatType.Insight => Insight,
                StatType.Resonance => Resonance,
                _ => 0
            };
        }
    }

    [Serializable]
    public sealed class PerkDefinition
    {
        public string Name => _name;
        public string Description => _description;
        public StatModifier[] Modifiers => _modifiers;
        public Sprite Icon => _icon;

        [SerializeField]
        private string _name;

        [SerializeField]
        private string _description;

        [SerializeField]
        private StatModifier[] _modifiers = Array.Empty<StatModifier>();

        [SerializeField]
        private Sprite _icon;
    }

    [Serializable]
    public sealed class StartingItem
    {
        public ItemConfig Item => _item;
        public EquipSlot Slot => _slot;
        public int Count => _count;
        public bool IsEquipped => _isEquipped;

        [SerializeField]
        private ItemConfig _item;

        [SerializeField]
        private EquipSlot _slot;

        [SerializeField]
        private int _count = 1;

        [SerializeField]
        private bool _isEquipped;
    }

    [CreateAssetMenu(menuName = "Game/Dungeon/Class Config")]
    public sealed class ClassConfig : ScriptableObject
    {
        public byte Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public ClassStats BaseStats => _baseStats;
        public Color Color => _color;
        public Color BodyColor => _bodyColor;
        public AbilityConfig[] Skills => _skills;
        public AbilityConfig[] Spells => _spells;
        public PerkDefinition[] Perks => _perks;
        public StartingItem[] StartingKit => _startingKit;
        public WeaponClass[] AllowedWeapons => _allowedWeapons;
        public ArmorType[] AllowedArmor => _allowedArmor;
        public const int SpellWheelSize = 5;
        /// The spell mask keeps a wheel in each half: bit i is spell i in wheel I, bit WheelBits + i is spell i in wheel II.
        public const int WheelBits = 16;
        public const int WheelCount = 2;
        /// The first five spells in wheel I, the next five in wheel II.
        public const int DefaultSpellMask = ((1 << SpellWheelSize) - 1) | (((1 << SpellWheelSize) - 1) << (SpellWheelSize + WheelBits));

        public bool CanCastBareHanded => _castFocus == CastFocus.BareHands;
        public CastFocus Focus => _castFocus;

        [SerializeField]
        private byte _id;

        [SerializeField]
        private string _displayName;

        [SerializeField, TextArea]
        private string _description;

        [SerializeField]
        private ClassStats _baseStats = new(15, 15, 15, 15, 15, 15);

        [SerializeField]
        private Color _color = Color.white;

        [SerializeField]
        private Color _bodyColor = new(0.62f, 0.66f, 0.72f);

        [SerializeField]
        private AbilityConfig[] _skills = Array.Empty<AbilityConfig>();

        [SerializeField]
        private AbilityConfig[] _spells = Array.Empty<AbilityConfig>();

        [SerializeField]
        private PerkDefinition[] _perks = Array.Empty<PerkDefinition>();

        [SerializeField]
        private StartingItem[] _startingKit = Array.Empty<StartingItem>();

        [SerializeField]
        private WeaponClass[] _allowedWeapons = Array.Empty<WeaponClass>();

        [SerializeField]
        private ArmorType[] _allowedArmor = Array.Empty<ArmorType>();

        [SerializeField]
        private CastFocus _castFocus;

        public static int WheelBit(int wheel, int spell)
        {
            return 1 << (wheel * WheelBits + spell);
        }

        public static bool IsInWheel(int mask, int wheel, int spell)
        {
            return (mask & WheelBit(wheel, spell)) != 0;
        }

        public bool CanUseWeapon(WeaponClass weaponClass)
        {
            return Array.IndexOf(_allowedWeapons, weaponClass) >= 0;
        }

        public bool CanWearArmor(ArmorType armorType)
        {
            return Array.IndexOf(_allowedArmor, armorType) >= 0;
        }

        /// Perks unlock at levels 1, 5, 10, 15.
        public static int PerkCountForLevel(int level)
        {
            return Mathf.Clamp(1 + level / 5, 1, 4);
        }

        public static int PerkSlotLevel(int slot)
        {
            return Mathf.Max(1, slot * 5);
        }
    }
}
