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
        public Sprite Icon => _icon;

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

        [SerializeField]
        private Sprite _icon;
    }

    /// What a perk does beyond its stat modifiers; the meaning of its value is told per entry.
    public enum PerkEffect : byte
    {
        None,
        /// Share of the armor move penalty taken off.
        HeavyArmorTraining,
        /// Share more healing from bandages.
        BandageHealing,
        /// A raised block does not slow.
        SteadyBlock,
        /// Share more of a hit the shield stops.
        ShieldTraining,
        /// Percent faster attacks for a moment after a blocked hit.
        Retaliation,
        /// Nothing throws the body back.
        KnockbackImmune,
        /// Every weapon can be equipped.
        AnyWeapon,
        /// Move speed rating added while no plate is worn.
        LightStep,
        /// Percent faster attacks per hit in a row on one body.
        Onslaught,
        /// Allies around: flat armor.
        BannerAura,
        /// Allies around: percent move speed.
        ExampleAura,
        /// Allies around: percent damage.
        FormationAura,
        /// Share wider war cries and auras.
        Voice,
        /// Share longer potions.
        PotionDuration,
        /// More potions in a stack.
        PotionStack,
        /// Share less poison damage.
        PoisonResistance,
        /// Allies around: Vitality.
        VitalityAura,
        /// Share of a drunk healing potion that heals the allies around.
        GenerousFlask,
        /// Allies around: percent faster drinking and bandaging.
        CalmHandsAura,
        /// Every heal of his cleanses poison.
        PureBlood,
        /// Percent more damage taken by the ones his flasks hit.
        CausticMix,
        /// Share more flask damage.
        StrongCharge,
        /// Share wider flasks and pools.
        WideSpread,
        /// Share less damage from his own flasks and fire.
        BlastHabit,
        /// Share shorter side effects.
        Accustomed,
        /// Share longer mixture buffs.
        DoubleDose,
        /// Share less damage while a mixture works.
        Buzz,
        /// Share more damage to monsters.
        BeastHabit,
        /// Share more ranged damage at 20 m and beyond.
        LongShot,
        /// Share more damage to bodies below 30% health.
        FinishOff,
        /// Share farther torch light.
        CatEye,
        /// Share less damage from traps.
        TrapSense,
        /// Share faster searching of chests and bodies.
        VeinExpert,
        /// Percent more damage the marked body takes.
        HuntersMark,
        /// Seconds longer trap hold.
        HeavyJaws,
        /// Seconds of poison from a trap.
        PoisonTeeth,
        /// More traps at once.
        IronStock,
        /// Share more damage to rooted and slowed bodies.
        SnaredPrey,
        /// Chance not to spend a charge.
        FrugalWord,
        /// Share more fire damage.
        FireMastery,
        /// Seconds longer burning.
        LongSmoulder,
        /// Damage of the blaze a body killed by his fire bursts into.
        FuneralHeat,
        /// Share stronger slows.
        DeepCold,
        /// Percent more damage taken by bodies he slowed.
        Brittle,
        /// More charges of frost spells.
        WinterMemory,
        /// Share more lightning damage.
        DryAir,
        /// Share more lightning damage to plate.
        IronCalls,
        /// More chain jumps.
        LongChain,
        /// Share faster lightning casting.
        QuickWord,
        /// Share more damage to the undead.
        Requiem,
        /// Share faster prayers.
        Acolyte,
        /// Share more healing done.
        WarmHands,
        /// Percent less damage for a moment taken by the one he heals.
        Shroud,
        /// More charges of healing prayers.
        Patience,
        /// Share more healing on allies below 30% health.
        LastRites,
        /// Share more damage of blunt weapons.
        HeavyHand,
        /// Percent more damage of the next attack after a hit taken.
        RighteousWrath,
        /// Percent of maximum health every weapon hit heals.
        BloodTithe,
        /// Percent of maximum health a prayer without charges costs.
        BloodPrayer,
        /// Share more prayer damage below half health.
        DarkVoice,
        /// Percent more damage taken by the ones he cursed.
        Rot,
        /// Percent of maximum health a kill of a cursed body brings back.
        TakeBack,
        /// Share faster searching of bodies.
        GraveSense,
        /// Share more damage of the first hit on a body at full health.
        FirstBlood,
        /// Share more dagger damage.
        ShortBlade,
        /// Share more damage of hits on the head.
        Throat,
        /// Seconds off the skill cooldowns per kill.
        CleanWork,
        /// Seconds longer poison.
        ThickVenom,
        /// Share more poison damage.
        CausticVenom,
        /// Percent less damage dealt by the ones he poisoned.
        Weakness,
        /// Percent less healing for the ones he poisoned.
        RotBlood,
        /// More throwing weapons in a stack.
        KnifeBundle,
        /// Share more thrown damage.
        TrueThrow,
        /// Share faster throws.
        QuickThrow,
        /// The venom on the blade goes on thrown weapons too.
        OiledBlades,
        /// Share more armor rating.
        FrostSkin,
        Count
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
        public PerkEffect Effect => _effect;
        public float Value => _value;

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

        [SerializeField]
        private PerkEffect _effect;

        [SerializeField]
        private float _value;
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
        /// The old saved spell mask kept a wheel in each half: bit i is spell i in wheel I, bit WheelBits + i is spell i in wheel II.
        public const int WheelBits = 16;
        public const int WheelCount = 2;
        public const int SlotCount = WheelCount * SpellWheelSize;
        /// The spell layout keeps SlotBits per wheel slot, wheel I first, clockwise from the top: 0 is a free slot, n is spell n - 1.
        public const int SlotBits = 5;
        /// The first five spells in wheel I, the next five in wheel II.
        public static readonly long DefaultSpellLayout = DefaultLayout();
        /// Every class picks one of its three subclasses; its own perks, skills and spells join the shared ones.
        public static readonly bool AreSubclassesEnabled = true;

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

        /// Spell in the slot of the layout, -1 for a free slot.
        public static int GetSlotSpell(long layout, int slot)
        {
            return (int)((layout >> (slot * SlotBits)) & ((1 << SlotBits) - 1)) - 1;
        }

        public static long SetSlotSpell(long layout, int slot, int spell)
        {
            long mask = ((1L << SlotBits) - 1) << (slot * SlotBits);

            return (layout & ~mask) | ((long)(spell + 1) << (slot * SlotBits));
        }

        /// First slot from `first` on holding the spell (-1: a free slot), -1 when none does.
        public static int FindSlot(long layout, int spell, int first = 0, int count = SlotCount)
        {
            for (int slot = first; slot < first + count; slot++)
            {
                if (GetSlotSpell(layout, slot) == spell)
                    return slot;
            }

            return -1;
        }

        public static bool IsInWheel(long layout, int wheel, int spell)
        {
            return FindSlot(layout, spell, wheel * SpellWheelSize, SpellWheelSize) >= 0;
        }

        /// The wheels of a class are opened by its spell memory skill, from the skill slot (Q: wheel I, E: wheel II) it is put in.
        public bool HasSpellWheels => Array.Exists(_skills, skill => skill.Kind == AbilityKind.SpellMemory);

        private static long DefaultLayout()
        {
            long layout = 0;

            for (int slot = 0; slot < SlotCount; slot++)
                layout = SetSlotSpell(layout, slot, slot);

            return layout;
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

        public bool IsSpellAvailable(int index, int subclass)
        {
            return index >= 0 && index < _spells.Length && IsAvailable(_spells[index].Subclass, subclass);
        }

        /// Summed values of the perks in the mask, indexed by PerkEffect; perkCount caps how many count, as in the stats.
        public void SumPerkEffects(int perkMask, int perkCount, float[] values)
        {
            Array.Clear(values, 0, values.Length);
            int applied = 0;

            for (int i = 0; i < _perks.Length && applied < perkCount; i++)
            {
                if ((perkMask & (1 << i)) == 0)
                    continue;

                applied++;
                values[(int)_perks[i].Effect] += _perks[i].Value;
            }
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
