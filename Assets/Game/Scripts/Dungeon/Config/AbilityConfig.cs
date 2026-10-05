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
        QuickCast
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
    }
}
