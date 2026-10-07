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
        public int Strength;
        public int Vitality;
        public int Spirit;
        public int Knowledge;
        public int Agility;
        public int Dexterity;

        public ClassStats(int strength, int vitality, int spirit, int knowledge, int agility, int dexterity)
        {
            Strength = strength;
            Vitality = vitality;
            Spirit = spirit;
            Knowledge = knowledge;
            Agility = agility;
            Dexterity = dexterity;
        }

        public int Get(StatType stat)
        {
            return stat switch
            {
                StatType.Strength => Strength,
                StatType.Vitality => Vitality,
                StatType.Spirit => Spirit,
                StatType.Knowledge => Knowledge,
                StatType.Agility => Agility,
                StatType.Dexterity => Dexterity,
                _ => 0
            };
        }
    }

    /// Events that earn stacks of a subclass resource.
    [Flags]
    public enum ResourceSource
    {
        None = 0,
        /// A weapon hit lands on a body.
        WeaponHit = 1,
        /// A riposte lands.
        Riposte = 2,
        /// The weapon struck a raised block.
        HitOnBlock = 4,
        /// Own block stops a hit.
        BlockedHit = 8,
        /// An unblocked hit is taken.
        DamageTaken = 16,
        /// A spell or a magical skill deals damage.
        SpellHit = 32,
        /// A heal, a shield or a blessing is given.
        Support = 64,
        /// An arrow, a bolt or a thrown weapon lands on a body.
        RangedHit = 128,
        /// An arrow, a bolt or a thrown weapon lands on a head.
        Headshot = 256
    }

    /// Specialisation of a class: its own skills, perks and spells on top of the shared ones, and a resource of stacks that
    /// its play earns and its skills spend.
    [Serializable]
    public sealed class SubclassDefinition
    {
        public string Name => _name;
        public string Description => _description;
        public Color Color => _color;
        public string ResourceName => _resourceName;
        public int ResourceMax => _resourceMax;
        public ResourceSource ResourceSources => _resourceSources;
        public float ResourceDecay => _resourceDecay;

        [SerializeField]
        private string _name;

        [SerializeField, TextArea]
        private string _description;

        [SerializeField]
        private Color _color = Color.white;

        [SerializeField]
        private string _resourceName;

        [SerializeField]
        private int _resourceMax = 3;

        [SerializeField]
        private ResourceSource _resourceSources;

        [SerializeField, Tooltip("Seconds without a new stack before all of them are lost")]
        private float _resourceDecay = 8f;
    }

    [Serializable]
    public sealed class PerkDefinition
    {
        public string Name => _name;
        public string Description => _description;
        public StatModifier[] Modifiers => _modifiers;
        public Sprite Icon => _icon;
        /// Subclass the perk belongs to, -1 for the whole class.
        public int Subclass => _subclass;
        /// The modifiers count once per stack of the subclass resource.
        public bool IsPerStack => _isPerStack;

        [SerializeField]
        private string _name;

        [SerializeField]
        private string _description;

        [SerializeField]
        private StatModifier[] _modifiers = Array.Empty<StatModifier>();

        [SerializeField]
        private Sprite _icon;

        [SerializeField]
        private int _subclass = -1;

        [SerializeField]
        private bool _isPerStack;
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
        public SubclassDefinition[] Subclasses => _subclasses;
        public StartingItem[] StartingKit => _startingKit;
        public WeaponClass[] AllowedWeapons => _allowedWeapons;
        public const int SpellWheelSize = 5;
        /// The spell mask keeps a wheel in each half: bit i is spell i in wheel I, bit WheelBits + i is spell i in wheel II.
        public const int WheelBits = 16;
        public const int WheelCount = 2;
        /// The first five spells in wheel I, the next five in wheel II.
        public const int DefaultSpellMask = ((1 << SpellWheelSize) - 1) | (((1 << SpellWheelSize) - 1) << (SpellWheelSize + WheelBits));
        /// Subclasses are hidden from the player for now: only the shared skills, perks and spells are offered and no subclass resource is kept.
        public static readonly bool AreSubclassesEnabled = false;

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
        private SubclassDefinition[] _subclasses = Array.Empty<SubclassDefinition>();

        [SerializeField]
        private StartingItem[] _startingKit = Array.Empty<StartingItem>();

        [SerializeField]
        private WeaponClass[] _allowedWeapons = Array.Empty<WeaponClass>();

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

        /// A class has a spell wheel when one of its skills opens it.
        public bool HasWheel(int wheel)
        {
            return Array.Exists(_skills, skill => skill.Kind == AbilityKind.SpellMemory && skill.Wheel == wheel);
        }

        public SubclassDefinition GetSubclass(int subclass)
        {
            return AreSubclassesEnabled && _subclasses.Length > 0 ? _subclasses[Mathf.Clamp(subclass, 0, _subclasses.Length - 1)] : null;
        }

        /// Shared entries (-1) belong to every subclass.
        public static bool IsAvailable(int owner, int subclass)
        {
            return owner < 0 || (AreSubclassesEnabled && owner == subclass);
        }

        public bool IsSkillAvailable(int index, int subclass)
        {
            return index < _skills.Length && IsAvailable(_skills[index].Subclass, subclass);
        }

        public int AvailablePerkMask(int subclass)
        {
            int mask = 0;

            for (int i = 0; i < _perks.Length; i++)
            {
                if (IsAvailable(_perks[i].Subclass, subclass))
                    mask |= 1 << i;
            }

            return mask;
        }

        /// Spell bits of both wheels the subclass may hold.
        public int AvailableSpellMask(int subclass)
        {
            int mask = 0;

            for (int i = 0; i < _spells.Length; i++)
            {
                if (!IsAvailable(_spells[i].Subclass, subclass))
                    continue;

                for (int wheel = 0; wheel < WheelCount; wheel++)
                    mask |= WheelBit(wheel, i);
            }

            return mask;
        }

        public bool CanUseWeapon(WeaponClass weaponClass)
        {
            return Array.IndexOf(_allowedWeapons, weaponClass) >= 0;
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
