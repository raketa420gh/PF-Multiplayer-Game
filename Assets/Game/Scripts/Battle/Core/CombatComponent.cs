using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public enum CombatState : byte
    {
        Idle,
        Equip,
        Attack,
        Deflected,
        BlockRaise,
        Block,
        BlockImpact,
        Draw,
        Reload,
        Stagger
    }

    public enum AttackPhase : byte
    {
        None,
        Windup,
        Active,
        Recovery
    }

    public sealed class CombatComponent : NetworkBehaviour, DamageReceiverComponent.IOwner
    {
        public WeaponConfig[] Loadout => _loadout;
        public WeaponConfig Weapon => _loadout[WeaponSlot];
        public MeleeAttackConfig Attack => Weapon.Attacks[AttackIndex];
        public float StateTime => Runner.SecondsSince(StateTick);
        public float EquipTime => _equipTime;
        public bool IsComboWindowOpen => State == CombatState.Attack && Attack.IsComboWindow(StateTime);
        public bool IsComboQueued => _comboQueued;
        public float DrawPower => State == CombatState.Draw ? Weapon.Ranged.GetPower(StateTime) : 0f;

        public AttackPhase Phase
        {
            get
            {
                if (State != CombatState.Attack)
                    return AttackPhase.None;

                float time = StateTime;
                MeleeAttackConfig attack = Attack;

                return time < attack.ActiveStart ? AttackPhase.Windup
                    : time < attack.ActiveEnd ? AttackPhase.Active
                    : AttackPhase.Recovery;
            }
        }

        public float MoveMultiplier
        {
            get
            {
                return State switch
                {
                    CombatState.Attack => Attack.MoveMultiplier,
                    CombatState.BlockRaise or CombatState.Block or CombatState.BlockImpact => Weapon.Block.MoveMultiplier,
                    CombatState.Draw => Weapon.Ranged.DrawMoveMultiplier,
                    CombatState.Deflected => _deflectedMoveMultiplier,
                    CombatState.Stagger => _staggerMoveMultiplier,
                    _ => 1f
                };
            }
        }

        [Networked]
        public CombatState State { get; private set; }

        [Networked]
        public int StateTick { get; private set; }

        [Networked]
        public byte AttackIndex { get; private set; }

        [Networked]
        public byte WeaponSlot { get; private set; }

        [SerializeField]
        private FighterBodyComponent _body;

        [SerializeField]
        private ProjectileComponent _projectiles;

        [SerializeField]
        private DamageReceiverComponent _receiver;

        [SerializeField]
        private HitboxRoot _hitboxRoot;

        [SerializeField]
        private WeaponConfig[] _loadout;

        [SerializeField]
        private Hitbox[] _blockHitboxes;

        [SerializeField]
        private LayerMask _hitMask;

        [SerializeField]
        private float _equipTime = 0.5f;

        [SerializeField]
        private float _deflectedMoveMultiplier = 0.5f;

        [SerializeField]
        private float _staggerMoveMultiplier = 0.3f;

        [SerializeField]
        private bool _drawTraces;

        private const int TracePoints = 5;

        [Networked]
        private byte _pendingSlot { get; set; }

        [Networked]
        private NetworkBool _comboQueued { get; set; }

        [Networked]
        private float _stateDuration { get; set; }

        private struct Tally
        {
            public HitboxRoot Root;
            public int BodyRays;
            public int BlockRays;
            public HitZone Zone;
            public Vector3 Point;
            public Vector3 Normal;
        }

        private static readonly List<LagCompensatedHit> s_hits = new(32);
        private static readonly List<HitboxRoot> s_rayRoots = new(8);
        private static readonly List<Tally> s_tallies = new(8);
        private readonly List<HitboxRoot> _hitRoots = new(8);

        public override void Spawned()
        {
            _receiver.SetOwner(this);
        }

        public void SetInitialSlot(int slot)
        {
            WeaponSlot = (byte)Mathf.Clamp(slot, 0, _loadout.Length - 1);
            _pendingSlot = WeaponSlot;
        }

        public void ResetState()
        {
            SetState(CombatState.Idle);
            UpdateBlockHitboxes();
        }

        public void Simulate(NetworkButtons buttons, NetworkButtons previous)
        {
            WeaponConfig weapon = Weapon;
            bool isAttackHeld = buttons.IsSet(weapon.AttackButton);
            bool isAttackPressed = buttons.WasPressed(previous, weapon.AttackButton);
            bool isBlockHeld = buttons.IsSet(weapon.BlockButton) && weapon.Block.CanBlock;
            bool isBlockPressed = buttons.WasPressed(previous, weapon.BlockButton);
            float time = StateTime;

            switch (State)
            {
                case CombatState.Idle:
                    SimulateIdle(weapon, buttons, previous, isAttackHeld, isAttackPressed, isBlockHeld);
                    break;

                case CombatState.Equip:
                    if (time >= _equipTime * 0.5f)
                        WeaponSlot = _pendingSlot;

                    if (time >= _equipTime)
                        SetState(CombatState.Idle);
                    break;

                case CombatState.Attack:
                    SimulateAttack(weapon, time, isAttackPressed);
                    break;

                case CombatState.Deflected:
                case CombatState.Reload:
                case CombatState.Stagger:
                    if (time >= _stateDuration)
                        SetState(CombatState.Idle);
                    break;

                case CombatState.BlockRaise:
                case CombatState.Block:
                    if (isAttackPressed)
                        StartAttack(0);
                    else if (!isBlockHeld)
                        SetState(CombatState.Idle);
                    else if (State == CombatState.BlockRaise && time >= weapon.Block.RaiseTime)
                        SetState(CombatState.Block);
                    break;

                case CombatState.BlockImpact:
                    if (time >= _stateDuration)
                        SetState(isBlockHeld ? CombatState.Block : CombatState.Idle);
                    break;

                case CombatState.Draw:
                    if (isBlockPressed)
                        SetState(CombatState.Idle);
                    else if (!isAttackHeld)
                        ReleaseDraw(weapon.Ranged, time);
                    break;
            }

            UpdateBlockHitboxes();
        }

        private void SimulateIdle(WeaponConfig weapon, NetworkButtons buttons, NetworkButtons previous,
            bool isAttackHeld, bool isAttackPressed, bool isBlockHeld)
        {
            int slot = GetRequestedSlot(buttons, previous);

            if (slot >= 0 && slot != WeaponSlot)
            {
                _pendingSlot = (byte)slot;
                SetState(CombatState.Equip);
            }
            else if (weapon.IsRanged)
            {
                if (isAttackHeld)
                    SetState(CombatState.Draw);
            }
            else if (isAttackPressed)
            {
                StartAttack(0);
            }
            else if (isBlockHeld)
            {
                SetState(CombatState.BlockRaise);
            }
        }

        private void SimulateAttack(WeaponConfig weapon, float time, bool isAttackPressed)
        {
            MeleeAttackConfig attack = weapon.Attacks[AttackIndex];

            if (isAttackPressed && attack.IsComboWindow(time))
                _comboQueued = true;

            if (HasStateAuthority)
                Trace(weapon, attack, time);

            if (State != CombatState.Attack)
                return;

            if (_comboQueued && time >= attack.ActiveEnd)
                StartAttack((AttackIndex + 1) % weapon.Attacks.Length);
            else if (time >= attack.Duration)
                SetState(CombatState.Idle);
        }

        private void StartAttack(int index)
        {
            if (Weapon.Attacks.Length == 0)
                return;

            _hitRoots.Clear();
            SetState(CombatState.Attack);
            AttackIndex = (byte)index;
        }

        private void ReleaseDraw(RangedConfig ranged, float time)
        {
            if (time < ranged.MinDrawTime)
            {
                SetState(CombatState.Idle);

                return;
            }

            float power = ranged.GetPower(time);
            Vector3 direction = _body.AimDirection;
            _projectiles.Fire(_body.EyePosition, direction * ranged.GetSpeed(power), ranged.Gravity,
                ranged.GetDamage(power), ranged.StaggerDuration);

            SetState(CombatState.Reload, ranged.ReloadTime);
        }

        private void SetState(CombatState state, float duration = 0f)
        {
            State = state;
            StateTick = Runner.Tick;
            AttackIndex = 0;
            _comboQueued = false;
            _stateDuration = duration;
        }

        private int GetRequestedSlot(NetworkButtons buttons, NetworkButtons previous)
        {
            for (int i = 0; i < _loadout.Length; i++)
            {
                if (buttons.WasPressed(previous, PlayerInputButtons.Weapon1 + i))
                    return i;
            }

            return -1;
        }

        private void UpdateBlockHitboxes()
        {
            for (int i = 0; i < _blockHitboxes.Length; i++)
            {
                if (_blockHitboxes[i] != null)
                    _hitboxRoot.SetHitboxActive(_blockHitboxes[i], State == CombatState.Block && i == WeaponSlot);
            }
        }

        private void Trace(WeaponConfig weapon, MeleeAttackConfig attack, float time)
        {
            float from = Mathf.Max(time - Runner.DeltaTime, attack.ActiveStart);
            float to = Mathf.Min(time, attack.ActiveEnd);
            bool isMirrored = weapon.IsMirrored;

            if (to <= from ||
                !attack.EvaluateTrace(from, isMirrored, out Vector3 baseFrom, out Vector3 tipFrom) ||
                !attack.EvaluateTrace(to, isMirrored, out Vector3 baseTo, out Vector3 tipTo))
                return;

            s_tallies.Clear();

            for (int i = 0; i < TracePoints; i++)
            {
                float alpha = i / (TracePoints - 1f);
                CastRay(Vector3.Lerp(baseFrom, tipFrom, alpha), Vector3.Lerp(baseTo, tipTo, alpha));
            }

            CastRay(baseTo, tipTo);
            ResolveTallies(weapon, attack);
        }

        private void CastRay(Vector3 bindFrom, Vector3 bindTo)
        {
            Vector3 from = _body.UpperToWorld(bindFrom);
            Vector3 to = _body.UpperToWorld(bindTo);

            if (_drawTraces)
                Debug.DrawLine(from, to, Color.red, 1f);

            Runner.RaycastAllSorted(from, to, Object.InputAuthority, s_hits, _hitMask, HitOptions.SubtickAccuracy);
            s_rayRoots.Clear();

            foreach (LagCompensatedHit hit in s_hits)
            {
                if (hit.Hitbox == null)
                    continue;

                HitboxRoot root = hit.Hitbox.Root;

                if (root == _hitboxRoot || _hitRoots.Contains(root) || s_rayRoots.Contains(root))
                    continue;

                s_rayRoots.Add(root);
                AddToTally(root, hit);
            }
        }

        private static void AddToTally(HitboxRoot root, in LagCompensatedHit hit)
        {
            int index = FindTally(root);
            Tally tally = index >= 0 ? s_tallies[index] : new Tally { Root = root, Zone = HitZone.Legs };
            HitZone zone = hit.Hitbox is ZoneHitbox zoneHitbox ? zoneHitbox.Zone : HitZone.Torso;

            if (zone == HitZone.Block)
            {
                tally.BlockRays++;

                if (tally.BodyRays == 0)
                    SetContact(ref tally, hit);
            }
            else
            {
                if (tally.BodyRays == 0 || GetPriority(zone) > GetPriority(tally.Zone))
                {
                    tally.Zone = zone;
                    SetContact(ref tally, hit);
                }

                tally.BodyRays++;
            }

            if (index >= 0)
                s_tallies[index] = tally;
            else
                s_tallies.Add(tally);
        }

        private static int FindTally(HitboxRoot root)
        {
            for (int i = 0; i < s_tallies.Count; i++)
            {
                if (s_tallies[i].Root == root)
                    return i;
            }

            return -1;
        }

        private static void SetContact(ref Tally tally, in LagCompensatedHit hit)
        {
            tally.Point = hit.Point;
            tally.Normal = hit.Normal;
        }

        private static int GetPriority(HitZone zone)
        {
            return zone switch
            {
                HitZone.Head => 2,
                HitZone.Torso => 1,
                _ => 0
            };
        }

        private void ResolveTallies(WeaponConfig weapon, MeleeAttackConfig attack)
        {
            bool isDeflected = false;

            foreach (Tally tally in s_tallies)
            {
                _hitRoots.Add(tally.Root);

                if (!tally.Root.TryGetComponent(out DamageReceiverComponent receiver) || !receiver.CanBeHitBy(_receiver))
                    continue;

                HitResult result = receiver.ApplyHit(new HitRequest
                {
                    BaseDamage = attack.Damage,
                    BodyRays = tally.BodyRays,
                    BlockRays = tally.BlockRays,
                    Zone = tally.Zone,
                    Point = tally.Point,
                    Normal = tally.Normal,
                    AttackerPosition = transform.position,
                    StaggerDuration = attack.StaggerDuration
                });

                isDeflected |= result == HitResult.Blocked;
            }

            if (isDeflected)
                SetState(CombatState.Deflected, weapon.DeflectDuration);
        }

        BlockConfig DamageReceiverComponent.IOwner.ActiveBlock => State == CombatState.Block ? Weapon.Block : null;

        Vector3 DamageReceiverComponent.IOwner.BlockDirection => transform.forward;

        void DamageReceiverComponent.IOwner.OnHitReceived(HitResult result, float staggerDuration)
        {
            if (result != HitResult.Hit)
            {
                BlockConfig block = Weapon.Block;
                SetState(CombatState.BlockImpact, block.ImpactDuration + block.RecoveryDuration);
            }
            else if (staggerDuration > 0f)
            {
                SetState(CombatState.Stagger, staggerDuration);
            }

            UpdateBlockHitboxes();
        }
    }
}
