using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum AbilityKind : byte
    {
        Heal,
        Buff,
        Projectile,
        AreaDamage,
        Dash,
        Invisibility,
        Shield,
        SpellMemory,
        Shapeshift,
        Taunt,
        Spawn,
        RestoreCharges,
        /// Fire or frost on the weapon of the adventurer under the crosshair, or on the caster when the aim misses.
        WeaponEnchant,
        /// Hitscan bolt that jumps to the nearest bodies around the struck one.
        ChainLightning,
        /// Hitscan: a struck body is hit by lightning from the sky.
        LightningStrike,
        /// Very fast dash along the view that stops at the first obstacle.
        Blink,
        /// The next spell is cast instantly.
        QuickCast,
        /// Heal over time for every living adventurer around the caster, the caster included.
        AreaHeal,
        /// Status effect on the caster and every ally around.
        AreaBuff,
        /// A flask or a net thrown in an arc: it bursts and leaves its zone where it lands.
        Throw,
        /// A lasting zone on the marked spot or around the caster.
        Zone,
        /// Hitscan on the body under the crosshair: damage and a status effect.
        TargetEffect,
        /// Puts items on the belt.
        GiveItem
    }

    [System.Flags]
    public enum AbilityFlags
    {
        None = 0,
        /// A war cry: the Voice perk widens it.
        Shout = 1,
        /// A flask of the alchemist: the demolitionist perks change it.
        Flask = 2,
        /// A self-made mixture of the tester: side effects and buff time follow his perks.
        Mixture = 4,
        Fire = 8,
        Frost = 16,
        Lightning = 32,
        Holy = 64,
        Dark = 128,
        /// Needs a shield in hand.
        RequiresShield = 256,
        /// Harms only the undead.
        UndeadOnly = 512,
        /// Half damage to the living.
        HalfVsLiving = 1024,
        /// A healing prayer: patience adds charges to it.
        Healing = 2048,
        Poison = 4096,
        /// A curse: the godless perks follow it.
        Curse = 8192
    }

    /// The lasting zone an ability leaves.
    public enum ZoneKind : byte
    {
        None,
        HealPool,
        FirePool,
        PoisonCloud,
        Frost,
        Banner,
        FireWall,
        Pyre,
        Storm,
        Sanctuary,
        Whisper,
        Flare
    }

    /// Look of a cast, a projectile, a zone or a status effect.
    public enum VfxStyle : byte
    {
        None,
        Physical,
        Shout,
        Fire,
        Frost,
        Lightning,
        Holy,
        Dark,
        Poison,
        Heal,
        Blood,
        Alchemy,
        Nature,
        Arcane,
        Light,
        Smoke
    }

    /// How a spell finds what it affects.
    public enum AbilityTargeting : byte
    {
        /// The caster only.
        Self,
        /// Instant ray from the crosshair: the body it strikes, the caster (or nothing) when it misses; reaches Range metres.
        Hitscan,
        /// A circle on the ground under the crosshair, held at Range metres when the aim goes farther.
        Ground,
        /// A missile that flies until it breaks or runs out of Range.
        Projectile,
        /// A burst or an aura around the caster.
        Aura
    }

    public enum ShapeshiftForm : byte
    {
        None,
        Bear,
        Panther,
        Rat
    }

    /// Skills and spells share one definition. Skills recharge by cooldown; spells carry a limited charge count.
    [CreateAssetMenu(menuName = "Game/Dungeon/Ability Config")]
    public sealed class AbilityConfig : ScriptableObject
    {
        public string DisplayName => _displayName;
        public string Description => _description;
        public AbilityKind Kind => _kind;
        public bool IsSpell => _isSpell;
        public int Charges => _charges;
        public float Cooldown => _cooldown;
        public float CastTime => _castTime;
        public float Magnitude => _magnitude;
        public float Duration => _duration;
        public float Radius => _radius;
        public StatusEffectKind Effect => _effect;
        public DamageType DamageType => _damageType;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileGravity => _projectileGravity;
        public int ProjectileCount => _projectileCount;
        public float ProjectileSpread => _projectileSpread;
        public ProjectileKind ProjectileKind => _projectileKind;
        public float EffectMagnitude => _effectMagnitude;
        public float EffectDuration => _effectDuration;
        public Color Color => _color;
        public string Glyph => _glyph;
        public Sprite Icon => _icon;
        public int HealthCost => _healthCost;
        public float LifeSteal => _lifeSteal;
        public Fusion.NetworkObject SpawnPrefab => _spawnPrefab;
        public float StaggerDuration => _staggerDuration;
        public bool IsCooldownBased => _charges >= 99;
        /// Subclass the ability belongs to, -1 for the whole class.
        public int Subclass => _subclass;
        /// Stacks of the subclass resource needed to use it.
        public int ResourceCost => _resourceCost;
        /// Magnitude added per spent stack.
        public float StackBonus => _stackBonus;
        /// Uses up every stack of the subclass resource.
        public bool SpendsResource => _resourceCost > 0 || _stackBonus > 0f;
        /// A skill that goes to the ally under the crosshair like a support spell.
        public bool IsOnAlly => _isOnAlly;
        /// Area damage: half-angle of the cone in front of the user, 0 for a full circle.
        public float ConeAngle => _coneAngle;
        /// Area damage: speed the struck bodies are pushed away with.
        public float Push => _push;
        /// Hitscan reach, ground aim limit and projectile flight distance.
        public float Range => _range;
        /// Seconds the area effect around the caster keeps pulsing; 0 for a single burst.
        public float AuraTime => _auraTime;
        public AbilityTargeting Targeting => _isGround ? AbilityTargeting.Ground : _kind switch
        {
            AbilityKind.Projectile => AbilityTargeting.Projectile,
            AbilityKind.ChainLightning or AbilityKind.LightningStrike => AbilityTargeting.Hitscan,
            AbilityKind.Heal or AbilityKind.Buff or AbilityKind.Shield or AbilityKind.WeaponEnchant => _isSpell || _isOnAlly ? AbilityTargeting.Hitscan : AbilityTargeting.Self,
            AbilityKind.AreaDamage or AbilityKind.AreaHeal or AbilityKind.AreaBuff or AbilityKind.Zone => AbilityTargeting.Aura,
            AbilityKind.TargetEffect => AbilityTargeting.Hitscan,
            AbilityKind.Throw => AbilityTargeting.Projectile,
            _ => AbilityTargeting.Self
        };
        public StatusEffectKind SecondEffect => _secondEffect;
        public float SecondMagnitude => _secondMagnitude;
        public float SecondDuration => _secondDuration;
        /// Heals and drains: the magnitude is a share of the target's maximum health in percent.
        public bool IsPercentHealth => _isPercentHealth;
        public AbilityFlags Flags => _flags;
        public ZoneKind Zone => _zone;
        public float ZoneRadius => _zoneRadius;
        public float ZoneDuration => _zoneDuration;
        /// Seconds before the zone strikes.
        public float ZoneDelay => _zoneDelay;
        /// Damage or heal per second of the zone, percent of maximum health for heals.
        public float ZoneTick => _zoneTick;
        /// Seconds the user stands still before it takes effect.
        public float Channel => _channel;
        /// Share of the user's maximum health in percent it costs.
        public float HealthCostPercent => _healthCostPercent;
        public ItemConfig Item => _item;
        public AudioClip CastSound => _castSound;
        public AudioClip ImpactSound => _impactSound;
        public VfxStyle Vfx => _vfx;

        public bool Has(AbilityFlags flag)
        {
            return (_flags & flag) != 0;
        }

        /// Spell memory: the spell wheel (0 or 1) this skill opens.
        public int Wheel => Mathf.RoundToInt(_magnitude);

        [SerializeField]
        private string _displayName;

        [SerializeField, TextArea]
        private string _description;

        [SerializeField]
        private AbilityKind _kind;

        [SerializeField]
        private bool _isSpell;

        [SerializeField]
        private int _charges = 1;

        [SerializeField]
        private float _cooldown = 20f;

        [SerializeField]
        private float _castTime;

        [SerializeField]
        private float _magnitude = 20f;

        [SerializeField]
        private float _duration = 8f;

        [SerializeField]
        private float _radius = 2f;

        [SerializeField]
        private StatusEffectKind _effect;

        [SerializeField]
        private DamageType _damageType = DamageType.Magical;

        [SerializeField]
        private float _projectileSpeed = 24f;

        [SerializeField]
        private float _projectileGravity;

        [SerializeField]
        private int _projectileCount = 1;

        [SerializeField]
        private float _projectileSpread;

        [SerializeField]
        private ProjectileKind _projectileKind = ProjectileKind.Magic;

        [SerializeField]
        private float _effectMagnitude;

        [SerializeField]
        private float _effectDuration;

        [SerializeField]
        private Color _color = Color.white;

        [SerializeField]
        private string _glyph = "*";

        [SerializeField]
        private Sprite _icon;

        [SerializeField]
        private int _healthCost;

        [SerializeField]
        private float _lifeSteal;

        [SerializeField]
        private Fusion.NetworkObject _spawnPrefab;

        [SerializeField]
        private float _staggerDuration;

        [SerializeField]
        private int _subclass = -1;

        [SerializeField]
        private int _resourceCost;

        [SerializeField]
        private float _stackBonus;

        [SerializeField]
        private bool _isOnAlly;

        [SerializeField]
        private float _coneAngle;

        [SerializeField]
        private float _push;

        [SerializeField]
        private float _range = 20f;

        [SerializeField]
        private float _auraTime;

        [SerializeField]
        private bool _isGround;

        [SerializeField]
        private StatusEffectKind _secondEffect;

        [SerializeField]
        private float _secondMagnitude;

        [SerializeField]
        private float _secondDuration;

        [SerializeField]
        private bool _isPercentHealth;

        [SerializeField]
        private AbilityFlags _flags;

        [SerializeField]
        private ZoneKind _zone;

        [SerializeField]
        private float _zoneRadius;

        [SerializeField]
        private float _zoneDuration;

        [SerializeField]
        private float _zoneDelay;

        [SerializeField]
        private float _zoneTick;

        [SerializeField]
        private float _channel;

        [SerializeField]
        private float _healthCostPercent;

        [SerializeField]
        private ItemConfig _item;

        [SerializeField]
        private AudioClip _castSound;

        [SerializeField]
        private AudioClip _impactSound;

        [SerializeField]
        private VfxStyle _vfx;
    }
}
