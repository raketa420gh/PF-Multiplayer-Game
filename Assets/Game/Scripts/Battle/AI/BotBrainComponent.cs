using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public enum BotMode : byte
    {
        Passive,
        Block,
        Attack,
        Spar
    }

    public sealed class BotBrainComponent : NetworkBehaviour, FighterComponent.IInputSource
    {
        [Networked]
        public BotMode Mode { get; private set; }

        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField]
        private float _turnSpeed = 300f;

        [SerializeField]
        private float _meleeLeash = 14f;

        [SerializeField]
        private float _rangedLeash = 26f;

        [SerializeField]
        private int _team = 1;

        [SerializeField]
        private Vector2 _attackPause = new(0.5f, 1.4f);

        [SerializeField, Range(0f, 1f)]
        private float _blockChance = 0.7f;

        [SerializeField]
        private Vector2 _rangedDistance = new(7f, 14f);

        [SerializeField]
        private Vector2 _aimSpread = new(2.5f, 1.5f);

        private const int ModeCount = 4;
        private const float HomeRadius = 1f;

        private Vector3 _home;
        private Vector2 _look;
        private float _nextAttackTime;
        private float _strafe;
        private float _nextStrafeTime;
        private float _releaseTime;
        private int _plannedChain;
        private Vector2 _aimError;
        private int _reactedAttackTick;
        private bool _isReactionBlock;
        private bool _wasAttackDown;

        public override void Spawned()
        {
            _fighter.SetInputSource(this, _team);
            _home = transform.position;
            _look = new Vector2(0f, transform.eulerAngles.y);
        }

        public static void CycleModes()
        {
            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if (fighter.TryGetComponent(out BotBrainComponent brain))
                    brain.Mode = (BotMode)(((int)brain.Mode + 1) % ModeCount);
            }
        }

        public void Setup(int weaponSlot, BotMode mode)
        {
            Mode = mode;
            _fighter.Combat.SetInitialSlot(weaponSlot);
        }

        public PlayerInputData GetInput()
        {
            PlayerInputData input = default;
            CombatComponent combat = _fighter.Combat;
            WeaponConfig weapon = combat.Weapon;
            FighterComponent target = FindTarget(weapon.IsRanged ? _rangedLeash : _meleeLeash);

            if (target == null)
            {
                ReturnHome(ref input);
            }
            else
            {
                Vector3 toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;

                bool isAttackDown = false;
                bool isBlockDown = false;

                if (weapon.IsRanged)
                    ThinkRanged(target, combat, weapon.Ranged, distance, ref input, ref isAttackDown);
                else
                    ThinkMelee(target, combat, weapon, distance, ref input, ref isAttackDown, ref isBlockDown);

                input.Buttons.Set(weapon.AttackButton, isAttackDown);
                input.Buttons.Set(weapon.BlockButton, isBlockDown);
                _wasAttackDown = isAttackDown;
            }

            input.LookRotation = _look;

            return input;
        }

        private FighterComponent FindTarget(float leash)
        {
            FighterComponent best = null;
            float bestDistance = leash * leash;

            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if (fighter.IsBot || fighter.Health.IsDead)
                    continue;

                float distance = (fighter.transform.position - _home).sqrMagnitude;

                if (distance < bestDistance)
                {
                    best = fighter;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void ReturnHome(ref PlayerInputData input)
        {
            _fighter.Health.Restore(1);

            Vector3 toHome = _home - transform.position;
            toHome.y = 0f;

            if (toHome.magnitude < HomeRadius)
                return;

            AimAt(_fighter.Body.EyePosition + toHome);
            input.MoveDirection = Vector2.up;
        }

        private void ThinkMelee(FighterComponent target, CombatComponent combat, WeaponConfig weapon, float distance,
            ref PlayerInputData input, ref bool isAttackDown, ref bool isBlockDown)
        {
            AimAt(target.Body.ChestPosition);

            if (Mode == BotMode.Passive)
                return;

            if (Mode == BotMode.Block)
            {
                isBlockDown = true;

                return;
            }

            float time = Runner.SimulationTime;
            float reach = weapon.Reach;
            UpdateStrafe(time);

            if (distance > reach * 0.8f)
            {
                input.MoveDirection = new Vector2(_strafe * 0.3f, 1f);
                input.Buttons.Set(PlayerInputButtons.Sprint, distance > 6f);
            }
            else if (distance < reach * 0.45f)
            {
                input.MoveDirection = new Vector2(_strafe, -1f);
            }
            else
            {
                input.MoveDirection = new Vector2(_strafe, 0f);
            }

            if (Mode == BotMode.Spar && ShouldBlock(target, combat, distance))
            {
                isBlockDown = true;

                return;
            }

            if (combat.State == CombatState.Attack)
            {
                bool wantsCombo = combat.AttackIndex + 1 < _plannedChain && combat.IsComboWindowOpen && !combat.IsComboQueued;
                isAttackDown = wantsCombo && !_wasAttackDown;
                _nextAttackTime = time + Random.Range(_attackPause.x, _attackPause.y);
            }
            else if (distance <= reach && time >= _nextAttackTime && !_wasAttackDown)
            {
                isAttackDown = true;
                _plannedChain = Random.Range(1, weapon.Attacks.Length + 1);
            }
        }

        private bool ShouldBlock(FighterComponent target, CombatComponent combat, float distance)
        {
            CombatComponent targetCombat = target.Combat;
            bool isThreat = targetCombat.State == CombatState.Attack &&
                            targetCombat.Phase != AttackPhase.Recovery &&
                            distance <= targetCombat.Weapon.Reach + 1f;

            if (!isThreat || !combat.Weapon.Block.CanBlock || combat.State == CombatState.Attack)
                return false;

            if (_reactedAttackTick != targetCombat.StateTick)
            {
                _reactedAttackTick = targetCombat.StateTick;
                _isReactionBlock = Random.value < _blockChance;
            }

            return _isReactionBlock;
        }

        private void ThinkRanged(FighterComponent target, CombatComponent combat, RangedConfig ranged, float distance,
            ref PlayerInputData input, ref bool isAttackDown)
        {
            float flightTime = distance / ranged.MaxSpeed;
            Vector3 aimPoint = target.Body.ChestPosition;
            aimPoint.y += 0.5f * -ranged.Gravity * flightTime * flightTime;
            AimAt(aimPoint, _aimError);

            if (Mode == BotMode.Passive || Mode == BotMode.Block)
                return;

            float time = Runner.SimulationTime;
            UpdateStrafe(time);

            float forward = distance < _rangedDistance.x ? -1f : distance > _rangedDistance.y ? 1f : 0f;
            input.MoveDirection = new Vector2(_strafe, forward);

            if (combat.State == CombatState.Draw)
            {
                if (combat.DrawPower < 1f)
                    _releaseTime = time + Random.Range(0.1f, 0.5f);

                isAttackDown = time < _releaseTime;
                _nextAttackTime = time + Random.Range(_attackPause.x, _attackPause.y);
            }
            else if (combat.State == CombatState.Idle && time >= _nextAttackTime)
            {
                isAttackDown = true;
                _releaseTime = float.MaxValue;
                _aimError = new Vector2(Random.Range(-_aimSpread.y, _aimSpread.y), Random.Range(-_aimSpread.x, _aimSpread.x));
            }
        }

        private void UpdateStrafe(float time)
        {
            if (time < _nextStrafeTime)
                return;

            _strafe = Random.Range(-1, 2);
            _nextStrafeTime = time + Random.Range(0.8f, 2f);
        }

        private void AimAt(Vector3 point, Vector2 error = default)
        {
            Vector3 direction = point - _fighter.Body.EyePosition;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + error.y;
            float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg + error.x;
            float step = _turnSpeed * Runner.DeltaTime;

            _look.y = Mathf.MoveTowardsAngle(_look.y, yaw, step);
            _look.x = Mathf.MoveTowards(_look.x, pitch, step);
        }
    }
}
