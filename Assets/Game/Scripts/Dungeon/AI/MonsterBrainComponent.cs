using Fusion;
using Game.Scripts.Battle;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Dungeon
{
    /// Dungeon monster AI: idle at post, aggro on nearby adventurers, path with the NavMesh, fight with the weapon it carries.
    public sealed class MonsterBrainComponent : NetworkBehaviour, FighterComponent.IInputSource
    {
        private enum Mode : byte
        {
            Idle,
            Chase,
            Fight,
            Return
        }

        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField]
        private MonsterComponent _monster;

        [SerializeField]
        private float _turnSpeed = 240f;

        [SerializeField]
        private float _repathInterval = 0.4f;

        [SerializeField]
        private float _loseSightTime = 6f;

        [SerializeField]
        private Vector2 _rangedDistance = new(6f, 11f);

        [SerializeField]
        private LayerMask _sightMask = 1;

        [SerializeField]
        private LayerMask _doorMask;

        private const float HomeRadius = 0.8f;
        private const float WaypointRadius = 0.5f;

        private readonly NavMeshPath _path = new();
        private Vector3 _home;
        private float _homeYaw;
        private Vector2 _look;
        private Mode _mode;
        private AdventurerComponent _target;
        private float _nextRepath;
        private float _lastSeen;
        private float _nextAttackTime;
        private float _strafe;
        private float _nextStrafeTime;
        private float _releaseTime;
        private int _plannedChain;
        private int _reactedAttackTick;
        private bool _isReactionBlock;
        private bool _wasAttackDown;
        private int _corner;

        public override void Spawned()
        {
            _fighter.SetInputSource(this, 2);
            _home = transform.position;
            _homeYaw = transform.eulerAngles.y;
            _look = new Vector2(0f, _homeYaw);
        }

        public PlayerInputData GetInput()
        {
            PlayerInputData input = default;

            if (_fighter.Health.IsDead)
            {
                input.LookRotation = _look;

                return input;
            }

            MonsterConfig config = _monster.Config;
            float time = Runner.SimulationTime;
            UpdateTarget(config, time);

            switch (_mode)
            {
                case Mode.Idle:
                    AimYaw(_homeYaw, 0f);
                    break;
                case Mode.Return:
                    FollowPath(_home, ref input, time);

                    if (Flat(_home - transform.position).magnitude < HomeRadius)
                    {
                        _mode = Mode.Idle;
                        _fighter.Health.Restore(_fighter.Health.MaxHealth);
                    }
                    break;
                case Mode.Chase:
                    FollowPath(_target.transform.position, ref input, time);
                    break;
                case Mode.Fight:
                    Fight(config, ref input, time);
                    break;
            }

            input.LookRotation = _look;

            return input;
        }

        private void UpdateTarget(MonsterConfig config, float time)
        {
            if (_target != null && (_target.Object == null || !_target.Object.IsValid || _target.Fighter.Health.IsDead || _target.State != AdventurerState.Alive))
                _target = null;

            float sqrLeash = config.LeashRange * config.LeashRange;

            if (_target == null)
                _target = FindTarget(config.AggroRange);

            if (_target == null)
            {
                if (_mode is Mode.Chase or Mode.Fight)
                    _mode = Mode.Return;

                return;
            }

            Vector3 toTarget = Flat(_target.transform.position - transform.position);
            float distance = toTarget.magnitude;
            bool canSee = CanSee(_target);

            if (canSee)
                _lastSeen = time;

            if ((transform.position - _home).sqrMagnitude > sqrLeash || time - _lastSeen > _loseSightTime)
            {
                _target = null;
                _mode = Mode.Return;

                return;
            }

            float fightRange = config.IsRanged ? _rangedDistance.y + 2f : _fighter.Combat.Weapon.Reach + 1.2f;
            _mode = distance <= fightRange && canSee ? Mode.Fight : Mode.Chase;
        }

        private AdventurerComponent FindTarget(float range)
        {
            AdventurerComponent best = null;
            float bestDistance = range * range;

            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if (fighter.IsBot || fighter.Health.IsDead || !fighter.TryGetComponent(out AdventurerComponent adventurer))
                    continue;

                if (adventurer.State != AdventurerState.Alive || adventurer.IsInvisible)
                    continue;

                float distance = (fighter.transform.position - transform.position).sqrMagnitude;
                float crouchPenalty = fighter.Move.CrouchAmount > 0.5f ? 0.25f : 1f;

                if (distance < bestDistance * crouchPenalty && CanSee(adventurer))
                {
                    best = adventurer;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private bool CanSee(AdventurerComponent target)
        {
            Vector3 from = _fighter.Body.EyePosition;
            Vector3 to = target.Fighter.Body.ChestPosition;
            Vector3 delta = to - from;

            return !Runner.GetPhysicsScene().Raycast(from, delta.normalized, delta.magnitude, _sightMask, QueryTriggerInteraction.Ignore);
        }

        private void FollowPath(Vector3 destination, ref PlayerInputData input, float time)
        {
            if (time >= _nextRepath)
            {
                _nextRepath = time + _repathInterval;
                _corner = 1;

                if (!NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, _path) || _path.corners.Length < 2)
                    _path.ClearCorners();
            }

            Vector3 waypoint = destination;

            if (_path.corners.Length >= 2)
            {
                while (_corner < _path.corners.Length - 1 && Flat(_path.corners[_corner] - transform.position).magnitude < WaypointRadius)
                    _corner++;

                waypoint = _path.corners[Mathf.Min(_corner, _path.corners.Length - 1)];
            }

            Vector3 direction = Flat(waypoint - transform.position);

            if (direction.sqrMagnitude < 0.01f)
                return;

            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            AimYaw(yaw, 0f);
            Vector3 local = Quaternion.Euler(0f, -_look.y, 0f) * direction.normalized;
            input.MoveDirection = new Vector2(local.x, local.z);
            OpenDoorAhead(direction.normalized);
        }

        private void OpenDoorAhead(Vector3 direction)
        {
            if (!Runner.GetPhysicsScene().Raycast(transform.position + Vector3.up, direction, out RaycastHit hit, 1.6f, _doorMask, QueryTriggerInteraction.Collide))
                return;

            DoorComponent door = hit.collider.GetComponentInParent<DoorComponent>();

            if (door != null && !door.IsOpen)
                door.ForceOpen();
        }

        private void Fight(MonsterConfig config, ref PlayerInputData input, float time)
        {
            CombatComponent combat = _fighter.Combat;
            WeaponConfig weapon = combat.Weapon;
            Vector3 toTarget = Flat(_target.transform.position - transform.position);
            float distance = toTarget.magnitude;
            bool isAttackDown = false;
            bool isBlockDown = false;

            if (weapon.IsRanged)
                FightRanged(weapon.Ranged, distance, ref input, ref isAttackDown, time);
            else
                FightMelee(config, combat, weapon, distance, ref input, ref isAttackDown, ref isBlockDown, time);

            input.Buttons.Set(weapon.AttackButton, isAttackDown);
            input.Buttons.Set(weapon.BlockButton, isBlockDown);
            _wasAttackDown = isAttackDown;
        }

        private void FightMelee(MonsterConfig config, CombatComponent combat, WeaponConfig weapon, float distance,
            ref PlayerInputData input, ref bool isAttackDown, ref bool isBlockDown, float time)
        {
            AimAt(_target.Fighter.Body.ChestPosition);
            UpdateStrafe(time);
            float reach = weapon.Reach;

            if (distance > reach * 0.8f)
                input.MoveDirection = new Vector2(_strafe * 0.2f, 1f);
            else if (distance < reach * 0.4f)
                input.MoveDirection = new Vector2(_strafe * 0.5f, -0.6f);
            else
                input.MoveDirection = new Vector2(_strafe * 0.6f, 0f);

            if (config.CanBlock && ShouldBlock(combat, distance))
            {
                isBlockDown = true;

                return;
            }

            if (combat.State == CombatState.Attack)
            {
                bool wantsCombo = combat.AttackIndex + 1 < _plannedChain && combat.IsComboWindowOpen && !combat.IsComboQueued;
                isAttackDown = wantsCombo && !_wasAttackDown;
                _nextAttackTime = time + Random.Range(config.AttackPauseMin, config.AttackPauseMax);
            }
            else if (distance <= reach && time >= _nextAttackTime && !_wasAttackDown && combat.State == CombatState.Idle)
            {
                isAttackDown = true;
                _plannedChain = Random.Range(1, weapon.Attacks.Length + 1);
            }
        }

        private bool ShouldBlock(CombatComponent combat, float distance)
        {
            CombatComponent targetCombat = _target.Fighter.Combat;
            bool isThreat = targetCombat.State == CombatState.Attack && targetCombat.Phase != AttackPhase.Recovery &&
                            distance <= targetCombat.Weapon.Reach + 1f;

            if (!isThreat || !combat.Weapon.Block.CanBlock || combat.State == CombatState.Attack)
                return false;

            if (_reactedAttackTick != targetCombat.StateTick)
            {
                _reactedAttackTick = targetCombat.StateTick;
                _isReactionBlock = Random.value < 0.5f;
            }

            return _isReactionBlock;
        }

        private void FightRanged(RangedConfig ranged, float distance, ref PlayerInputData input, ref bool isAttackDown, float time)
        {
            CombatComponent combat = _fighter.Combat;
            float flightTime = distance / ranged.MaxSpeed;
            Vector3 aimPoint = _target.Fighter.Body.ChestPosition;
            aimPoint.y += 0.5f * -ranged.Gravity * flightTime * flightTime;
            AimAt(aimPoint);
            UpdateStrafe(time);

            float forward = distance < _rangedDistance.x ? -1f : distance > _rangedDistance.y ? 1f : 0f;
            input.MoveDirection = new Vector2(_strafe * 0.5f, forward);

            if (combat.State == CombatState.Draw)
            {
                if (combat.DrawPower < 1f)
                    _releaseTime = time + Random.Range(0.1f, 0.4f);

                isAttackDown = time < _releaseTime;
                _nextAttackTime = time + Random.Range(_monster.Config.AttackPauseMin, _monster.Config.AttackPauseMax);
            }
            else if (combat.State == CombatState.Idle && time >= _nextAttackTime)
            {
                isAttackDown = true;
                _releaseTime = float.MaxValue;
            }
        }

        private void UpdateStrafe(float time)
        {
            if (time < _nextStrafeTime)
                return;

            _strafe = Random.Range(-1, 2);
            _nextStrafeTime = time + Random.Range(0.8f, 2f);
        }

        private void AimAt(Vector3 point)
        {
            Vector3 direction = point - _fighter.Body.EyePosition;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
            AimYaw(yaw, pitch);
        }

        private void AimYaw(float yaw, float pitch)
        {
            float step = _turnSpeed * Runner.DeltaTime;
            _look.y = Mathf.MoveTowardsAngle(_look.y, yaw, step);
            _look.x = Mathf.MoveTowards(_look.x, pitch, step);
        }

        private static Vector3 Flat(Vector3 vector)
        {
            vector.y = 0f;

            return vector;
        }
    }
}
