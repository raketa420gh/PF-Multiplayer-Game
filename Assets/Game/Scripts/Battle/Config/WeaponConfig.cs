using System;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public enum WeaponKind
    {
        OneHanded,
        TwoHanded,
        Ranged
    }

    public enum HandSide
    {
        Right,
        Left
    }

    public enum WeaponSocket
    {
        RightHand,
        LeftHand,
        RightShield,
        LeftShield
    }

    [Serializable]
    public sealed class WeaponAttachment
    {
        public GameObject Prefab => _prefab;
        public WeaponSocket Socket => _socket;

        [SerializeField]
        private GameObject _prefab;

        [SerializeField]
        private WeaponSocket _socket;
    }

    [Serializable]
    public sealed class MeleeAttackConfig
    {
        public float ActiveStart => _windupTime;
        public float ActiveEnd => _windupTime + _activeTime;
        public float Duration => _windupTime + _activeTime + _recoveryTime;
        public float ComboWindowStart => _comboWindowStart;
        public float ComboWindowEnd => _comboWindowEnd;
        public int Damage => _damage;
        public float MoveMultiplier => _moveMultiplier;
        public float StaggerDuration => _staggerDuration;
        public float RecoveryTime => _recoveryTime;

        [SerializeField]
        private float _windupTime = 0.3f;

        [SerializeField]
        private float _activeTime = 0.2f;

        [SerializeField]
        private float _recoveryTime = 0.4f;

        [SerializeField]
        private float _comboWindowStart = 0.4f;

        [SerializeField]
        private float _comboWindowEnd = 0.75f;

        [SerializeField]
        private int _damage = 20;

        [SerializeField]
        private float _moveMultiplier = 0.6f;

        [SerializeField]
        private float _staggerDuration;

        [Header("Baked from animation clip (root space, standing, zero pitch)")]
        [SerializeField]
        private float _traceSampleRate = 60f;

        [SerializeField]
        private Vector3[] _traceBase = Array.Empty<Vector3>();

        [SerializeField]
        private Vector3[] _traceTip = Array.Empty<Vector3>();

        public bool IsComboWindow(float time)
        {
            return time >= _comboWindowStart && time <= _comboWindowEnd;
        }

        /// Screen-plane travel of the blade tip during the active phase (x right, y up), scaled by its share of the full
        /// motion: a short vector means the swing is mostly a thrust.
        public Vector2 GetSwingDirection(bool mirror)
        {
            if (!EvaluateTrace(ActiveStart, mirror, out _, out Vector3 from) || !EvaluateTrace(ActiveEnd, mirror, out _, out Vector3 to))
                return Vector2.zero;

            Vector3 delta = to - from;

            return delta.sqrMagnitude > 1e-6f ? new Vector2(delta.x, delta.y) / delta.magnitude : Vector2.zero;
        }

        public bool EvaluateTrace(float time, bool mirror, out Vector3 basePoint, out Vector3 tipPoint)
        {
            int count = Mathf.Min(_traceBase.Length, _traceTip.Length);

            if (count == 0)
            {
                basePoint = tipPoint = default;

                return false;
            }

            float sample = Mathf.Clamp(time * _traceSampleRate, 0f, count - 1);
            int index = Mathf.Min((int)sample, Mathf.Max(count - 2, 0));
            int next = Mathf.Min(index + 1, count - 1);
            float alpha = sample - index;

            basePoint = Vector3.LerpUnclamped(_traceBase[index], _traceBase[next], alpha);
            tipPoint = Vector3.LerpUnclamped(_traceTip[index], _traceTip[next], alpha);

            if (mirror)
            {
                basePoint.x = -basePoint.x;
                tipPoint.x = -tipPoint.x;
            }

            return true;
        }
    }

    [Serializable]
    public sealed class BlockConfig
    {
        public bool CanBlock => _canBlock;
        public float RaiseTime => _raiseTime;
        public float Mitigation => _mitigation;
        public float ImpactDuration => _impactDuration;
        public float RecoveryDuration => _recoveryDuration;
        public int Stability => _stability;
        public float BreakDuration => _breakDuration;
        public float AngleTolerance => _angleTolerance;
        public float MoveMultiplier => _moveMultiplier;

        [SerializeField]
        private bool _canBlock = true;

        [SerializeField]
        private float _raiseTime = 0.2f;

        [SerializeField, Range(0f, 1f)]
        private float _mitigation = 1f;

        [SerializeField]
        private float _impactDuration = 0.25f;

        [SerializeField]
        private float _recoveryDuration = 0.35f;

        /// A hit whose Impact is higher than this breaks the block: the defender reels for BreakDuration.
        [SerializeField, Range(1, 10)]
        private int _stability = 3;

        [SerializeField]
        private float _breakDuration = 0.8f;

        [SerializeField, Range(0f, 180f)]
        private float _angleTolerance = 100f;

        [SerializeField]
        private float _moveMultiplier = 0.55f;
    }

    [Serializable]
    public sealed class RangedConfig
    {
        public float MinDrawTime => _minDrawTime;
        public float FullDrawTime => _fullDrawTime;
        public float ReloadTime => _reloadTime;
        public float MinSpeed => _minSpeed;
        public float MaxSpeed => _maxSpeed;
        public int MinDamage => _minDamage;
        public int MaxDamage => _maxDamage;
        public float Gravity => _gravity;
        public float Lifetime => _lifetime;
        public float DrawMoveMultiplier => _drawMoveMultiplier;
        public float StaggerDuration => _staggerDuration;

        [SerializeField]
        private float _minDrawTime = 0.35f;

        [SerializeField]
        private float _fullDrawTime = 0.9f;

        [SerializeField]
        private float _reloadTime = 0.6f;

        [SerializeField]
        private float _minSpeed = 14f;

        [SerializeField]
        private float _maxSpeed = 30f;

        [SerializeField]
        private int _minDamage = 10;

        [SerializeField]
        private int _maxDamage = 35;

        [SerializeField]
        private float _gravity = -9.81f;

        [SerializeField]
        private float _lifetime = 5f;

        [SerializeField]
        private float _drawMoveMultiplier = 0.5f;

        [SerializeField]
        private float _staggerDuration = 0.15f;

        public float GetPower(float drawTime) => Mathf.Clamp01(drawTime / _fullDrawTime);
        public float GetSpeed(float power) => Mathf.Lerp(_minSpeed, _maxSpeed, power);
        public int GetDamage(float power) => Mathf.RoundToInt(Mathf.Lerp(_minDamage, _maxDamage, power));
    }

    [CreateAssetMenu(menuName = "Game/Battle/Weapon Config")]
    public sealed class WeaponConfig : ScriptableObject
    {
        public string DisplayName => _displayName;
        public WeaponKind Kind => _kind;
        public HandSide MainHand => _mainHand;
        public bool IsMirrored => _mainHand == HandSide.Left;
        public bool IsRanged => _kind == WeaponKind.Ranged;
        public string AnimationPrefix => _animationPrefix;
        public WeaponAttachment[] Attachments => _attachments;
        public float DeflectDuration => _deflectDuration;
        public float Reach => _reach;
        public MeleeAttackConfig[] Attacks => _attacks;
        public bool HasRiposte => _hasRiposte;
        public MeleeAttackConfig Riposte => _riposte;
        public BlockConfig Block => _block;
        public RangedConfig Ranged => _ranged;
        public DamageType DamageType => _damageType;
        public int Impact => _impact;

        public PlayerInputButtons AttackButton =>
            _mainHand == HandSide.Right ? PlayerInputButtons.Primary : PlayerInputButtons.Secondary;

        public PlayerInputButtons BlockButton =>
            _mainHand == HandSide.Right ? PlayerInputButtons.Secondary : PlayerInputButtons.Primary;

        [SerializeField]
        private string _displayName;

        [SerializeField]
        private WeaponKind _kind;

        [SerializeField]
        private HandSide _mainHand;

        [SerializeField]
        private string _animationPrefix;

        [SerializeField]
        private WeaponAttachment[] _attachments = Array.Empty<WeaponAttachment>();

        [SerializeField]
        private float _deflectDuration = 0.6f;

        [SerializeField]
        private float _reach = 1.6f;

        [SerializeField]
        private MeleeAttackConfig[] _attacks = Array.Empty<MeleeAttackConfig>();

        [SerializeField]
        private bool _hasRiposte;

        /// The swing that answers a blocked hit; it is not a part of the series.
        [SerializeField]
        private MeleeAttackConfig _riposte = new();

        [SerializeField]
        private BlockConfig _block = new();

        [SerializeField]
        private RangedConfig _ranged = new();

        [SerializeField]
        private DamageType _damageType = DamageType.Physical;

        /// How hard the weapon knocks a block, compared with BlockConfig.Stability of the defender.
        [SerializeField, Range(1, 10)]
        private int _impact = 3;
    }
}
