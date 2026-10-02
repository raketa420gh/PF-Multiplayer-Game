using System;
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
        Stagger,
        Busy
    }

    public enum AttackPhase : byte
    {
        None,
        Windup,
        Active,
        Recovery
    }

    /// Stats that scale combat: implemented by the adventurer (class + gear) and by monsters.
    public interface ICombatStats
    {
        float GetDamageMultiplier(DamageType type);
        float ActionSpeed { get; }
        float MoveSpeedMultiplier { get; }
    }

    public sealed class CombatComponent : NetworkBehaviour, DamageReceiverComponent.IOwner
    {
        public const byte NoWeapon = 255;

        public event Action<Vector3, Vector3> OnWorldHit;

        /// Every weapon the prefab knows about; slots map onto this catalog.
        public WeaponConfig[] Catalog => _loadout;
        public int SlotCount => _slotCount;
        public int WeaponIndex => GetWeaponIndex(WeaponSlot);
        public WeaponConfig Weapon => _loadout[WeaponIndex];
        public MeleeAttackConfig Attack => Weapon.Attacks[AttackIndex];
        public float StateTime => Runner.SecondsSince(StateTick) * TimeScale;
        public float EquipTime => _equipTime;
        public bool IsComboWindowOpen => State == CombatState.Attack && Attack.IsComboWindow(StateTime);
        public bool IsComboQueued => _comboQueued;
        public float DrawPower => State == CombatState.Draw ? Weapon.Ranged.GetPower(StateTime) : 0f;
        public float BusyProgress => State == CombatState.Busy && _stateDuration > 0f ? Mathf.Clamp01(StateTime / _stateDuration) : 0f;
        public byte BusyKind => _busyKind;
        public float TimeScale => State is CombatState.Attack or CombatState.BlockRaise or CombatState.Draw or CombatState.Reload ? ActionSpeed : 1f;
        public float ActionSpeed => _stats?.ActionSpeed ?? 1f;
        public DamageReceiverComponent Receiver => _receiver;
        public ProjectileComponent Projectiles => _projectiles;
        public FighterBodyComponent Body => _body;
        public bool IsBlocking => State == CombatState.Block;

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
                    CombatState.Busy or CombatState.Equip => _busyMoveMultiplier,
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
        private int _slotCount = 4;

        [SerializeField]
        private float _equipTime = 0.5f;

        [SerializeField]
        private float _deflectedMoveMultiplier = 0.5f;

        [SerializeField]
        private float _staggerMoveMultiplier = 0.3f;

        [SerializeField]
        private float _busyMoveMultiplier = 0.65f;

        [SerializeField]
        private bool _drawTraces;

        private const int TracePoints = 5;
        private const int MaxSlots = 4;

        [Networked]
        private byte _pendingSlot { get; set; }

        [Networked]
        private NetworkBool _comboQueued { get; set; }

        [Networked]
        private float _stateDuration { get; set; }

        [Networked]
        private byte _busyKind { get; set; }

        [Networked]
        private int _worldHitCount { get; set; }

        [Networked]
        private Vector3 _worldHitPoint { get; set; }

        [Networked]
        private Vector3 _worldHitNormal { get; set; }

        [Networked, Capacity(MaxSlots)]
        private NetworkArray<byte> _slotWeapons => default;

        [Networked]
        private NetworkBool _slotsInitialized { get; set; }

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
        private ICombatStats _stats;
        private int _renderedWorldHits;
        private bool _hasWorldHit;
        private LagCompensatedHit _worldHit;

        public override void Spawned()
        {
            _receiver.SetOwner(this);
            _renderedWorldHits = _worldHitCount;

            if (HasStateAuthority && !_slotsInitialized)
            {
                for (int i = 0; i < MaxSlots; i++)
                    _slotWeapons.Set(i, (byte)(i < _loadout.Length ? i : 0));

                _slotsInitialized = true;
            }
        }

        public override void Render()
        {
            if (_renderedWorldHits != _worldHitCount)
                OnWorldHit?.Invoke(_worldHitPoint, _worldHitNormal);

            _renderedWorldHits = _worldHitCount;
        }

        public void SetStats(ICombatStats stats)
        {
            _stats = stats;
        }

        public int GetWeaponIndex(int slot)
        {
            int index = _slotWeapons[Mathf.Clamp(slot, 0, MaxSlots - 1)];

            return index < _loadout.Length ? index : 0;
        }

        public int FindCatalogIndex(WeaponConfig config)
        {
            return Array.IndexOf(_loadout, config);
        }

        /// Assigns which catalog weapon a weapon slot holds. State authority only.
        public void SetSlotWeapon(int slot, int catalogIndex)
        {
            _slotWeapons.Set(slot, (byte)Mathf.Clamp(catalogIndex, 0, _loadout.Length - 1));
            _slotsInitialized = true;

            if (slot == WeaponSlot && State != CombatState.Busy)
                SetState(CombatState.Equip);
        }

        public void SetInitialSlot(int slot)
        {
            WeaponSlot = (byte)Mathf.Clamp(slot, 0, _slotCount - 1);
            _pendingSlot = WeaponSlot;
        }

        public void ResetState()
        {
            SetState(CombatState.Idle);
            UpdateBlockHitboxes();
        }

        /// Interrupts combat for a cast, a consumable or an interaction.
        public bool StartBusy(float duration, byte kind)
        {
            if (State is CombatState.Attack or CombatState.Stagger or CombatState.Busy)
                return false;

            SetState(CombatState.Busy, duration);
            _busyKind = kind;
            UpdateBlockHitboxes();

            return true;
        }

        public void CancelBusy()
        {
            if (State == CombatState.Busy)
                SetState(CombatState.Idle);
        }

        public void Stagger(float duration)
        {
            SetState(CombatState.Stagger, duration);
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
                case CombatState.Busy:
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
            int damage = ScaleDamage(ranged.GetDamage(power), Weapon.DamageType);
            _projectiles.Fire(_body.EyePosition, direction * ranged.GetSpeed(power), ranged.Gravity, damage,
                ranged.StaggerDuration, Weapon.DamageType, ProjectileKind.Arrow);

            SetState(CombatState.Reload, ranged.ReloadTime);
        }

        public int ScaleDamage(int baseDamage, DamageType type)
        {
            float multiplier = _stats?.GetDamageMultiplier(type) ?? 1f;

            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
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
            for (int i = 0; i < _slotCount; i++)
            {
                if (buttons.WasPressed(previous, PlayerInputButtons.Weapon1 + i))
                    return i;
            }

            return -1;
        }

        private void UpdateBlockHitboxes()
        {
            int weaponIndex = WeaponIndex;

            for (int i = 0; i < _blockHitboxes.Length; i++)
            {
                Hitbox hitbox = _blockHitboxes[i];

                if (hitbox != null && IsRegistered(hitbox))
                    _hitboxRoot.SetHitboxActive(hitbox, State == CombatState.Block && i == weaponIndex);
            }
        }

        /// The root assigns hitbox indices when it starts; until then activation changes must wait.
        private bool IsRegistered(Hitbox hitbox)
        {
            int index = hitbox.HitboxIndex;

            return index >= 0 && index < _hitboxRoot.Hitboxes.Length && _hitboxRoot.Hitboxes[index] == hitbox;
        }

        private void Trace(WeaponConfig weapon, MeleeAttackConfig attack, float time)
        {
            float from = Mathf.Max(time - Runner.DeltaTime * ActionSpeed, attack.ActiveStart);
            float to = Mathf.Min(time, attack.ActiveEnd);
            bool isMirrored = weapon.IsMirrored;

            if (to <= from ||
                !attack.EvaluateTrace(from, isMirrored, out Vector3 baseFrom, out Vector3 tipFrom) ||
                !attack.EvaluateTrace(to, isMirrored, out Vector3 baseTo, out Vector3 tipTo))
                return;

            s_tallies.Clear();
            _hasWorldHit = false;

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

            Runner.RaycastAllSorted(from, to, Object.InputAuthority, s_hits, _hitMask,
                HitOptions.SubtickAccuracy | HitOptions.IncludePhysX);
            s_rayRoots.Clear();

            foreach (LagCompensatedHit hit in s_hits)
            {
                if (hit.Hitbox == null)
                {
                    _hasWorldHit = true;
                    _worldHit = hit;

                    break;
                }

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
            int damage = ScaleDamage(attack.Damage, weapon.DamageType);

            foreach (Tally tally in s_tallies)
            {
                _hitRoots.Add(tally.Root);

                if (!tally.Root.TryGetComponent(out DamageReceiverComponent receiver) || !receiver.CanBeHitBy(_receiver))
                    continue;

                HitResult result = receiver.ApplyHit(new HitRequest
                {
                    BaseDamage = damage,
                    BodyRays = tally.BodyRays,
                    BlockRays = tally.BlockRays,
                    Zone = tally.Zone,
                    Point = tally.Point,
                    Normal = tally.Normal,
                    AttackerPosition = transform.position,
                    StaggerDuration = attack.StaggerDuration,
                    DamageType = weapon.DamageType,
                    Attacker = _receiver
                });

                isDeflected |= result == HitResult.Blocked;
            }

            if (_hasWorldHit)
            {
                _worldHitPoint = _worldHit.Point;
                _worldHitNormal = _worldHit.Normal;
                _worldHitCount++;
                isDeflected = true;
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
