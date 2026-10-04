using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    [System.Serializable]
    public struct MonsterAttachment
    {
        public ArmorVisual Visual;
        public Color Color;
    }

    public enum MonsterTier : byte
    {
        Common,
        Elite,
        Nightmare
    }

    [CreateAssetMenu(menuName = "Game/Dungeon/Monster Config")]
    public sealed class MonsterConfig : ScriptableObject
    {
        public string DisplayName => _displayName;
        public MonsterTier Tier => _tier;
        public int MaxHealth => _maxHealth;
        public float DamageMultiplier => _damageMultiplier;
        public float MoveSpeed => _moveSpeed;
        public float ActionSpeed => _actionSpeed;
        public float AggroRange => _aggroRange;
        public float LeashRange => _leashRange;
        public float ArmorReduction => _armorReduction;
        public float MagicReduction => _magicReduction;
        public int WeaponIndex => _weaponIndex;
        public int Experience => _experience;
        public LootTableConfig LootTable => _lootTable;
        public bool IsRanged => _isRanged;
        public bool CanBlock => _canBlock;
        public float AttackPauseMin => _attackPause.x;
        public float AttackPauseMax => _attackPause.y;
        public Material BodyMaterial => _bodyMaterial;
        public float Scale => _scale;
        public bool IsUndead => _isUndead;
        public bool IsBoss => _isBoss;
        public float LungeImpulse => _lungeImpulse;
        public float ChargeSpeed => _chargeSpeed;
        public bool IsCharger => _chargeSpeed > 0f;
        public MonsterAttachment[] Attachments => _attachments;
        public DungeonSound Voice => _voice;

        [SerializeField]
        private string _displayName = "Skeleton";

        [SerializeField]
        private MonsterTier _tier;

        [SerializeField]
        private int _maxHealth = 100;

        [SerializeField]
        private float _damageMultiplier = 1f;

        [SerializeField]
        private float _moveSpeed = 270f;

        [SerializeField]
        private float _actionSpeed = 1f;

        [SerializeField]
        private float _aggroRange = 9f;

        [SerializeField]
        private float _leashRange = 22f;

        [SerializeField, Range(-0.5f, 0.65f)]
        private float _armorReduction;

        [SerializeField, Range(-0.5f, 0.65f)]
        private float _magicReduction;

        [SerializeField]
        private int _weaponIndex;

        [SerializeField]
        private int _experience = 25;

        [SerializeField]
        private LootTableConfig _lootTable;

        [SerializeField]
        private bool _isRanged;

        [SerializeField]
        private bool _canBlock;

        [SerializeField]
        private Vector2 _attackPause = new(0.6f, 1.5f);

        [SerializeField]
        private Material _bodyMaterial;

        [SerializeField]
        private float _scale = 1f;

        [SerializeField]
        private bool _isUndead = true;

        [SerializeField]
        private bool _isBoss;

        [SerializeField]
        private float _lungeImpulse;

        [SerializeField, Tooltip("Move speed rating of the ram through the active phase of the attack; 0 = fights on the spot")]
        private float _chargeSpeed;

        [SerializeField]
        private MonsterAttachment[] _attachments = System.Array.Empty<MonsterAttachment>();

        [SerializeField]
        private DungeonSound _voice = DungeonSound.Rattle;
    }
}
