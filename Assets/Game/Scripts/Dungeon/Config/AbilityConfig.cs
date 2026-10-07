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
        AreaBuff
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
            AbilityKind.AreaDamage or AbilityKind.AreaHeal or AbilityKind.AreaBuff => AbilityTargeting.Aura,
            _ => AbilityTargeting.Self
        };
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
    }
}
