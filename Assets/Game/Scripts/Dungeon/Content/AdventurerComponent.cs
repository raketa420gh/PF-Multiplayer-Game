using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum AdventurerState : byte
    {
        Alive,
        Dead,
        Extracted
    }

    public enum PendingAction : byte
    {
        None,
        Ability,
        Consumable,
        Interact,
        Utility,
        Shapeshift
    }

    /// The player's body inside the dungeon: class, inventory, interaction, abilities, death and extraction on top of the fighter.
    public sealed class AdventurerComponent : NetworkBehaviour, InventoryActionsComponent.IOwner, DamageReceiverComponent.IHitModifier
    {
        public const int AbilityCapacity = 16;
        public const float InteractRange = 2.6f;
        public const byte BusyCast = 0;
        public const byte BusyUse = 1;
        public const byte BusyInteract = 2;
        public const byte BusyThrow = 3;
        public const byte BusyOpen = 4;
        public const byte BusyPickUp = 5;
        public const byte BusyBandage = 6;
        public const int BeltGroupSize = 3;
        public const byte NoBelt = 255;
        public const byte NoSearch = 255;
        private const float ContainerRange = 5f;
        private const float CastPadding = 0.15f;
        private const float RootTurnLimit = 90f;

        public FighterComponent Fighter => _fighter;
        public InventoryComponent Inventory => _inventory;
        public InventoryActionsComponent Actions => _actions;
        public StatusEffectComponent Effects => _effects;
        public AdventurerStats Stats => _stats;
        public ClassConfig Class => _class;
        public PlayerSessionComponent Session => _session;
        public IReadOnlyList<AbilityConfig> Abilities => _abilities;
        public InteractableComponent LookTarget => _lookTarget;
        public bool IsInvisible => _effects.Has(StatusEffectKind.Invisible);
        public ContainerComponent OpenedContainer => Runner != null && Runner.TryFindBehaviour(OpenContainerId, out NetworkBehaviour b) ? b as ContainerComponent : null;

        [Networked]
        public byte ClassId { get; private set; }

        [Networked]
        public AdventurerState State { get; private set; }

        [Networked]
        public byte Floor { get; private set; }

        [Networked]
        public int Kills { get; private set; }

        [Networked]
        public int RunExperience { get; private set; }

        [Networked]
        public NetworkBehaviourId OpenContainerId { get; private set; }

        [Networked]
        public PendingAction Pending { get; private set; }

        [Networked]
        public NetworkBool IsResting { get; private set; }

        [Networked]
        public NetworkBool IsInSwarm { get; private set; }

        [Networked]
        public byte ReadiedSpell { get; private set; } = NoSpell;

        [Networked]
        public ShapeshiftForm Form { get; private set; }

        [Networked]
        public byte SkillA { get; private set; }

        [Networked]
        public byte SkillB { get; private set; } = 1;

        [Networked]
        public int PerkMask { get; private set; }

        [Networked]
        public int SpellMask { get; private set; }

        /// Equipment slot of the belt item held in hand instead of the weapon (keys 3 / 4), NoBelt when the weapon is out.
        [Networked]
        public byte BeltSlot { get; private set; } = NoBelt;

        public bool HasBeltItemInHand => BeltSlot != NoBelt;

        /// The weapon set is put away (X): bare hands until a set is taken out again.
        [Networked]
        public NetworkBool IsHolstered { get; private set; }

        public bool HasWeaponInHand => !HasBeltItemInHand && !IsHolstered && Form == ShapeshiftForm.None && !IsHandsOccupied;

        /// Doors, shrines and bandages take both hands: whatever was held is put away until the action ends.
        public bool IsHandsOccupied => Pending switch
        {
            PendingAction.Interact => Runner.TryFindBehaviour(_pendingTarget, out NetworkBehaviour behaviour) && behaviour is InteractableComponent { IsHandsOccupied: true },
            PendingAction.Consumable => _fighter.Combat.BusyKind == BusyBandage,
            _ => false
        };

        /// A potion is being drunk: the animation says it all, no progress bar; bandages and surgical kits fill one.
        public bool IsDrinking => Pending == PendingAction.Consumable && _fighter.Combat.BusyKind == BusyUse;

        /// Bag index of the item being discovered in the opened container, NoSearch when nothing is left to find.
        [Networked]
        public byte SearchIndex { get; private set; } = NoSearch;

        public float SearchProgress => SearchIndex == NoSearch || _searchEndTick <= _searchStartTick
            ? 0f
            : Mathf.Clamp01((Runner.Tick - _searchStartTick) / (float)(_searchEndTick - _searchStartTick));

        /// Charge progress of the spell being held (0 when not charging).
        public float CastCharge => Pending == PendingAction.Ability && _isHoldingCast ? _fighter.Combat.BusyProgress : 0f;
        public bool IsHoldingCast => Pending == PendingAction.Ability && _isHoldingCast;

        public const byte NoSpell = 255;
        public AbilityConfig ReadiedSpellConfig => _class != null && ReadiedSpell < _class.Spells.Length ? _class.Spells[ReadiedSpell] : null;
        public bool HasFocus => _class.Focus switch
        {
            CastFocus.BareHands => true,
            CastFocus.Instrument => HoldsWeaponClass(WeaponClass.Instrument),
            _ => HoldsFocus()
        };

        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField]
        private InventoryComponent _inventory;

        [SerializeField]
        private InventoryActionsComponent _actions;

        [SerializeField]
        private StatusEffectComponent _effects;

        [SerializeField]
        private ItemDatabase _database;

        [SerializeField]
        private ClassConfig[] _classes;

        [SerializeField]
        private DungeonConfig _config;

        [SerializeField]
        private LayerMask _interactMask;

        [SerializeField]
        private NetworkObject _worldItemPrefab;

        [SerializeField]
        private NetworkObject _corpsePrefab;

        [SerializeField]
        private NetworkObject _campfirePrefab;

        [SerializeField]
        private WeaponConfig _fistsWeapon;

        [SerializeField]
        private WeaponConfig[] _formWeapons;

        [SerializeField]
        private float[] _formScales = { 1f, 1.45f, 1f, 0.4f };

        [SerializeField]
        private float _restHealInterval = 2f;

        [Networked, Capacity(AbilityCapacity)]
        private NetworkArray<TickTimer> _cooldowns => default;

        [Networked, Capacity(AbilityCapacity)]
        private NetworkArray<byte> _charges => default;

        [Networked]
        private byte _pendingIndex { get; set; }

        [Networked]
        private NetworkBehaviourId _pendingTarget { get; set; }

        [Networked]
        private int _pendingCompleteTick { get; set; }

        [Networked]
        private int _inventoryVersion { get; set; }

        [Networked]
        private TickTimer _restTimer { get; set; }

        [Networked]
        private TickTimer _removeTimer { get; set; }

        [Networked]
        private NetworkBool _isHoldingCast { get; set; }

        [Networked]
        private int _seenStaggerTick { get; set; }

        [Networked]
        private NetworkBool _isAttuned { get; set; }

        [Networked]
        private NetworkBool _isRooted { get; set; }

        [Networked]
        private float _rootYaw { get; set; }

        [Networked]
        private int _searchStartTick { get; set; }

        [Networked]
        private int _searchEndTick { get; set; }

        private readonly List<AbilityConfig> _abilities = new();
        private readonly AbilityConfig[] _skills = new AbilityConfig[2];
        private int _skillCount;
        private readonly AdventurerStats _stats = new();
        private ClassConfig _class;
        private PlayerSessionComponent _session;
        private InteractableComponent _lookTarget;
        private float _swarmAccumulator;
        private int _appliedVersion = -1;
        private int _appliedEffects;
        private ShapeshiftForm _appliedForm;
        private byte _appliedBelt = NoBelt;
        private int _appliedWeaponSet = -1;
        private bool _wasAlive = true;

        public override void Spawned()
        {
            _class = FindClass(ClassId);
            BuildAbilityList();

            _fighter.SetStats(_stats);
            _fighter.Receiver.SetModifier(this);
            _effects.SetResistance(_stats);
            _fighter.OnSimulateInput += OnSimulateInput;
            _actions.SetOwner(this);
            _fighter.Combat.Projectiles.OnReceiverHit += OnProjectileHit;
            _fighter.Receiver.OnHitDealt += OnHitDealt;
            ResolveSession();

            if (HasInputAuthority && DungeonContext.Instance != null)
                DungeonContext.Instance.SetLocalAdventurer(this);

            RefreshStats(true);

            if (HasStateAuthority)
            {
                for (int i = 0; i < _abilities.Count; i++)
                    _charges.Set(i, (byte)GetMaxCharges(i));

                _isAttuned = true;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _fighter.OnSimulateInput -= OnSimulateInput;
            _fighter.Combat.Projectiles.OnReceiverHit -= OnProjectileHit;
            _fighter.Receiver.OnHitDealt -= OnHitDealt;

            if (DungeonContext.Instance != null && DungeonContext.Instance.LocalAdventurer == this)
                DungeonContext.Instance.SetLocalAdventurer(null);
        }

        public override void FixedUpdateNetwork()
        {
            if (_session == null)
                ResolveSession();

            if (_appliedVersion != _inventory.Version || _appliedForm != Form || _appliedBelt != BeltSlot || _appliedWeaponSet != HeldWeaponSet || _appliedEffects != _effects.GetSignature())
                RefreshStats(false);

            _fighter.Combat.SetBlockSuppressed((ReadiedSpell != NoSpell && HasFocus && State == AdventurerState.Alive) || HasBeltItemInHand);
            _fighter.Combat.SetAttackSuppressed(HasBeltItemInHand);

            if (!HasStateAuthority)
                return;

            if (State == AdventurerState.Alive && _fighter.Health.IsDead)
                Die();

            if (State != AdventurerState.Alive)
            {
                if (_removeTimer.Expired(Runner))
                    Runner.Despawn(Object);

                return;
            }

            if (HasBeltItemInHand && _inventory.GetEquipped((EquipSlot)BeltSlot).IsEmpty)
                BeltSlot = NoBelt;

            SimulateRest();
            SimulateSwarm();
            SimulateSearch();
        }

        public override void Render()
        {
            if (_appliedVersion != _inventory.Version || _appliedBelt != BeltSlot || _appliedWeaponSet != HeldWeaponSet || _appliedEffects != _effects.GetSignature())
                RefreshStats(false);

            if (HasInputAuthority)
                _lookTarget = FindInteractable();
        }

        public void Setup(byte classId, PlayerSessionComponent session)
        {
            ClassId = classId;
            _session = session;
            SkillA = session.SkillA;
            SkillB = session.SkillB;
            PerkMask = session.PerkMask;
            SpellMask = session.SpellMask;
            Floor = 1;
            State = AdventurerState.Alive;
        }

        /// Two chosen skills first (Q, E), then every spell of the class.
        private void BuildAbilityList()
        {
            _abilities.Clear();
            _skills[0] = _class.Skills.Length > 0 ? _class.Skills[Mathf.Clamp(SkillA, 0, _class.Skills.Length - 1)] : null;
            _skills[1] = _class.Skills.Length > 1 ? _class.Skills[Mathf.Clamp(SkillB, 0, _class.Skills.Length - 1)] : null;

            foreach (AbilityConfig skill in _skills)
            {
                if (skill != null)
                    _abilities.Add(skill);
            }

            _skillCount = _abilities.Count;
            _abilities.AddRange(_class.Spells);
        }

        public AbilityConfig GetSkill(int slot)
        {
            return slot < _skills.Length ? _skills[slot] : null;
        }

        public int SkillCount => _skillCount;
        public int PerkCount => ClassConfig.PerkCountForLevel(_session != null ? _session.Level : 1);

        public float GetCooldownLeft(int ability)
        {
            return _cooldowns[ability].RemainingTime(Runner) ?? 0f;
        }

        public int GetCharges(int ability)
        {
            return _charges[ability];
        }

        /// Resonance adds charges to every charge-based spell.
        public int GetMaxCharges(int ability)
        {
            AbilityConfig config = _abilities[ability];

            return config.IsSpell && !config.IsCooldownBased ? config.Charges + _stats.BonusCharges : config.Charges;
        }

        public float GetCooldownDuration(int ability)
        {
            return _abilities[ability].Cooldown / _stats.CooldownSpeed;
        }

        public float InteractProgress => Pending == PendingAction.Interact ? _fighter.Combat.BusyProgress : 0f;

        public void OpenContainer(ContainerComponent container)
        {
            OpenContainerId = container.Id;
        }

        public void CloseContainer()
        {
            OpenContainerId = default;
            SearchIndex = NoSearch;
        }

        public void Extract()
        {
            if (State != AdventurerState.Alive)
                return;

            State = AdventurerState.Extracted;
            _fighter.SetInputBlocked(true);
            _session?.OnExtracted(this);
            _removeTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);
        }

        public void Descend(Vector3 position, float yaw)
        {
            Floor = 2;
            _fighter.Move.Teleport(position, yaw);
            _fighter.SetLook(new Vector2(0f, yaw));
            CloseContainer();
            RpcTeleported(yaw);
        }

        public void AddExperience(int amount)
        {
            RunExperience += amount;
            _session?.AddExperience(amount);
        }

        public void AddKill()
        {
            Kills++;
        }

        public void ApplyDamageOverTime(float amount)
        {
            _fighter.Health.TakeDamage(Mathf.RoundToInt(amount));
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RpcTeleported(float yaw)
        {
            if (DungeonContext.Instance != null)
                DungeonContext.Instance.Battle.Input.SetLook(new Vector2(0f, yaw));
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcCloseContainer()
        {
            CloseContainer();
        }

        /// Spell wheel selection: readies a spell; it is cast with the secondary button once a focus is taken in hand.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcReadySpell(byte spellIndex)
        {
            ReadiedSpell = spellIndex < _class.Spells.Length && IsSpellInWheel(spellIndex) ? spellIndex : NoSpell;
        }

        public bool IsSpellInWheel(int spellIndex)
        {
            return (SpellMask & (1 << spellIndex)) != 0;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcShapeshift(ShapeshiftForm form)
        {
            if (State != AdventurerState.Alive || Pending != PendingAction.None)
                return;

            // Druid forms take a second to change into, as in the original; the body is vulnerable meanwhile.
            float duration = 1f / Mathf.Max(0.3f, _stats.ActionSpeed);

            if (!_fighter.Combat.StartBusy(duration, BusyCast))
                return;

            Pending = PendingAction.Shapeshift;
            _pendingIndex = (byte)form;
            _pendingCompleteTick = Runner.Tick + Mathf.CeilToInt(duration / Runner.DeltaTime);
        }

        /// Resting at a campfire brings every spell back to full charges and re-attunes the first free cast.
        public void RestoreCharges()
        {
            for (int i = 0; i < _abilities.Count; i++)
            {
                if (_abilities[i].IsSpell)
                    _charges.Set(i, (byte)GetMaxCharges(i));
            }

            _isAttuned = true;
        }

        private bool HoldsFocus()
        {
            return HoldsWeaponClass(WeaponClass.Staff) || HoldsWeaponClass(WeaponClass.Spellbook) || HoldsWeaponClass(WeaponClass.CrystalBall);
        }

        private bool HoldsWeaponClass(WeaponClass weaponClass)
        {
            if (!HasWeaponInHand)
                return false;

            int slot = _fighter.Combat.WeaponSlot;
            WeaponItemConfig main = _inventory.GetEquippedConfig<WeaponItemConfig>(slot == 0 ? EquipSlot.Weapon1Main : EquipSlot.Weapon2Main);
            WeaponItemConfig off = _inventory.GetEquippedConfig<WeaponItemConfig>(slot == 0 ? EquipSlot.Weapon1Off : EquipSlot.Weapon2Off);

            return (main != null && main.WeaponClass == weaponClass) || (off != null && off.WeaponClass == weaponClass);
        }

        private void ResolveSession()
        {
            if (_session != null || Runner == null)
                return;

            if (Runner.TryGetPlayerObject(Object.InputAuthority, out NetworkObject playerObject))
                _session = playerObject.GetComponent<PlayerSessionComponent>();
        }

        private ClassConfig FindClass(int id)
        {
            foreach (ClassConfig config in _classes)
            {
                if (config.Id == id)
                    return config;
            }

            return _classes[0];
        }

        private void RefreshStats(bool fillHealth)
        {
            _appliedVersion = _inventory.Version;
            _appliedEffects = _effects.GetSignature();
            int level = _session != null ? _session.Level : 1;
            _appliedForm = Form;
            _appliedBelt = BeltSlot;
            _appliedWeaponSet = HeldWeaponSet;
            _stats.Recalculate(_class, _inventory, _effects, _appliedWeaponSet, ClassConfig.PerkCountForLevel(level), Form, PerkMask, HasBeltItemInHand ? BeltSlot : -1);

            if (!HasStateAuthority)
                return;

            _fighter.Health.SetMaxHealth(_stats.MaxHealth, fillHealth);
            ApplyWeaponSlots();
        }

        /// Weapon set whose stats count: none while the hands hold a belt item, nothing or claws.
        private int HeldWeaponSet => HasWeaponInHand ? _fighter.Combat.WeaponSlot : -1;

        private void ApplyWeaponSlots()
        {
            CombatComponent combat = _fighter.Combat;
            combat.SetSlotWeapon(0, ResolveWeaponIndex(0, EquipSlot.Weapon1Main, EquipSlot.Weapon1Off));
            combat.SetSlotWeapon(1, ResolveWeaponIndex(1, EquipSlot.Weapon2Main, EquipSlot.Weapon2Off));
        }

        private int ResolveWeaponIndex(int set, EquipSlot mainSlot, EquipSlot offSlot)
        {
            if (Form != ShapeshiftForm.None && _formWeapons != null && (int)Form - 1 < _formWeapons.Length)
                return Mathf.Max(0, _fighter.Combat.FindCatalogIndex(_formWeapons[(int)Form - 1]));

            // A belt item in hand or the holster key puts the active set away: empty hands.
            if (!HasWeaponInHand && set == _fighter.Combat.WeaponSlot)
                return Mathf.Max(0, _fighter.Combat.FindCatalogIndex(_fistsWeapon));

            WeaponItemConfig main = _inventory.GetEquippedConfig<WeaponItemConfig>(mainSlot);
            WeaponItemConfig off = _inventory.GetEquippedConfig<WeaponItemConfig>(offSlot);
            CombatComponent combat = _fighter.Combat;

            if (main == null)
                return Mathf.Max(0, combat.FindCatalogIndex(off != null && off.Weapon != null ? off.Weapon : _fistsWeapon));

            WeaponConfig config = main.Weapon;

            if (off != null && off.WeaponClass == WeaponClass.Shield && main.WeaponWithShield != null)
                config = main.WeaponWithShield;

            return Mathf.Max(0, combat.FindCatalogIndex(config));
        }

        private void OnSimulateInput(NetworkButtons buttons, NetworkButtons previous)
        {
            if (!HasStateAuthority || State != AdventurerState.Alive)
                return;

            CombatComponent combat = _fighter.Combat;

            if (combat.State == CombatState.Stagger && combat.StateTick != _seenStaggerTick)
            {
                _seenStaggerTick = combat.StateTick;

                if (_stats.HasThreshold(StatType.Reflex))
                    _effects.Add(StatusEffectKind.ActionSpeed, 20f, 2f);
            }

            if (_isHoldingCast && Pending == PendingAction.Ability)
            {
                if (Runner.Tick >= _pendingCompleteTick)
                    FinishCast();
                else if (!buttons.IsSet(PlayerInputButtons.Secondary) || buttons.WasPressed(previous, PlayerInputButtons.Interact))
                    CancelPending();
            }
            else
            {
                SimulatePending();
            }

            if (Pending == PendingAction.Interact && (_isRooted ? Mathf.Abs(Mathf.DeltaAngle(_rootYaw, _fighter.Look.y)) > RootTurnLimit : !buttons.IsSet(PlayerInputButtons.Interact)))
                CancelPending();

            if (Pending != PendingAction.None && combat.State != CombatState.Busy)
                CancelPending();

            // Resting is done on one knee: the body goes down as in a crouch.
            IsResting = Pending == PendingAction.None && buttons.IsSet(PlayerInputButtons.Rest) && combat.State == CombatState.Idle;
            _fighter.SetCrouchForced(IsResting);
            _fighter.SetRooted(Pending == PendingAction.Interact && _isRooted);

            if (Pending != PendingAction.None)
                return;

            if (buttons.WasPressed(previous, PlayerInputButtons.Interact))
                TryInteract();

            bool isBeltInHand = SimulateBelt(buttons, previous);

            for (int i = 0; i < _skillCount; i++)
            {
                if (buttons.WasPressed(previous, PlayerInputButtons.Skill1 + i) && _abilities[i].Kind is not (AbilityKind.SpellMemory or AbilityKind.Shapeshift))
                    TryUseAbility(i);
            }

            if (!isBeltInHand && ReadiedSpell != NoSpell && buttons.WasPressed(previous, PlayerInputButtons.Secondary) && Form == ShapeshiftForm.None)
                TryCastReadiedSpell();
        }

        /// 1 / 2 take a weapon set out, X puts it away, 3 / 4 cycle the three belt slots of their group; LMB uses the belt item, RMB puts it away.
        private bool SimulateBelt(NetworkButtons buttons, NetworkButtons previous)
        {
            if (buttons.WasPressed(previous, PlayerInputButtons.Weapon1) || buttons.WasPressed(previous, PlayerInputButtons.Weapon2))
            {
                BeltSlot = NoBelt;
                IsHolstered = false;
            }

            if (buttons.WasPressed(previous, PlayerInputButtons.Holster) && CanChangeHands())
            {
                IsHolstered = HasBeltItemInHand || !IsHolstered;
                BeltSlot = NoBelt;
            }

            for (int group = 0; group < 2; group++)
            {
                if (buttons.WasPressed(previous, PlayerInputButtons.Weapon3 + group))
                    SelectBelt(group);
            }

            if (!HasBeltItemInHand)
                return false;

            if (buttons.WasPressed(previous, PlayerInputButtons.Primary))
                OnUseItem(_inventory, -1, (EquipSlot)BeltSlot);
            else if (buttons.WasPressed(previous, PlayerInputButtons.Secondary))
                BeltSlot = NoBelt;

            return true;
        }

        private bool CanChangeHands()
        {
            return Form == ShapeshiftForm.None && _fighter.Combat.State is CombatState.Idle or CombatState.Equip or CombatState.BlockRaise or CombatState.Block;
        }

        private void SelectBelt(int group)
        {
            if (!CanChangeHands())
                return;

            int first = (int)EquipSlot.Utility1 + group * BeltGroupSize;
            int start = BeltSlot >= first && BeltSlot < first + BeltGroupSize ? BeltSlot - first + 1 : 0;

            for (int i = 0; i < BeltGroupSize; i++)
            {
                int slot = first + (start + i) % BeltGroupSize;

                if (_inventory.GetEquipped((EquipSlot)slot).IsEmpty)
                    continue;

                BeltSlot = (byte)slot;

                return;
            }
        }

        private void TryInteract()
        {
            InteractableComponent target = FindInteractable();

            if (target == null || !target.IsAvailable)
                return;

            if (target.HoldTime <= 0f)
            {
                target.Complete(this);

                return;
            }

            float duration = target.HoldTime / _stats.InteractionSpeed;

            if (!_fighter.Combat.StartBusy(duration, target.BusyKind))
                return;

            Pending = PendingAction.Interact;
            _pendingTarget = target.Id;
            _pendingCompleteTick = Runner.Tick + Mathf.CeilToInt(duration / Runner.DeltaTime);
            _isRooted = target.IsRooting;
            _rootYaw = _fighter.Look.y;
        }

        private void TryCastReadiedSpell()
        {
            AbilityConfig spell = ReadiedSpellConfig;

            if (spell == null)
                return;

            if (!HasFocus || _fighter.Health.CurrentHealth <= spell.HealthCost)
                return;

            int index = _skillCount + ReadiedSpell;

            if (!_cooldowns[index].ExpiredOrNotRunning(Runner) || (!spell.IsCooldownBased && _charges[index] == 0))
                return;

            float duration = Mathf.Max(0.1f, spell.CastTime / _stats.CastSpeed);

            if (!_fighter.Combat.StartBusy(duration + CastPadding, BusyCast))
                return;

            Pending = PendingAction.Ability;
            _pendingIndex = (byte)index;
            _pendingCompleteTick = Runner.Tick + Mathf.CeilToInt(duration / Runner.DeltaTime);
            _isHoldingCast = true;
        }

        /// The spell takes effect when the cast completes; letting go of the button earlier cancels it for free.
        private void FinishCast()
        {
            int index = _pendingIndex;
            Pending = PendingAction.None;
            _isHoldingCast = false;
            _fighter.Combat.CancelBusy();
            AbilityConfig ability = _abilities[index];

            if (ability.HealthCost > 0)
                _fighter.Health.TakeDamage(ability.HealthCost);

            ApplyAbility(index);
        }

        private void TryUseAbility(int index)
        {
            if (index >= _abilities.Count)
                return;

            AbilityConfig ability = _abilities[index];

            if (!_cooldowns[index].ExpiredOrNotRunning(Runner) || (ability.IsSpell && !ability.IsCooldownBased && _charges[index] == 0))
                return;

            if (ability.HealthCost > 0)
                _fighter.Health.TakeDamage(ability.HealthCost);

            float duration = Mathf.Max(0.05f, ability.CastTime / (ability.IsSpell ? _stats.CastSpeed : _stats.ActionSpeed));

            if (!_fighter.Combat.StartBusy(duration, BusyCast))
                return;

            Pending = PendingAction.Ability;
            _pendingIndex = (byte)index;
            _pendingCompleteTick = Runner.Tick + Mathf.CeilToInt(duration / Runner.DeltaTime);
        }

        private void SimulatePending()
        {
            if (Pending == PendingAction.None || Runner.Tick < _pendingCompleteTick)
                return;

            PendingAction action = Pending;
            Pending = PendingAction.None;

            switch (action)
            {
                case PendingAction.Interact:
                    if (Runner.TryFindBehaviour(_pendingTarget, out NetworkBehaviour behaviour) && behaviour is InteractableComponent target && target.IsAvailable)
                        target.Complete(this);
                    break;
                case PendingAction.Ability:
                    ApplyAbility(_pendingIndex);
                    break;
                case PendingAction.Consumable:
                    ApplyConsumable((EquipSlot)_pendingIndex);
                    break;
                case PendingAction.Utility:
                    ApplyUtility((EquipSlot)_pendingIndex);
                    break;
                case PendingAction.Shapeshift:
                    Form = Form == (ShapeshiftForm)_pendingIndex ? ShapeshiftForm.None : (ShapeshiftForm)_pendingIndex;
                    _fighter.Combat.CancelBusy();
                    break;
            }
        }

        private void CancelPending()
        {
            _isHoldingCast = false;

            if (Pending == PendingAction.Interact && Runner.TryFindBehaviour(_pendingTarget, out NetworkBehaviour behaviour) && behaviour is InteractableComponent target)
                target.Cancel(this);

            Pending = PendingAction.None;
            _fighter.Combat.CancelBusy();
        }

        private void ApplyAbility(int index)
        {
            AbilityConfig ability = _abilities[index];
            CombatComponent combat = _fighter.Combat;
            float buffDuration = ability.Duration;

            switch (ability.Kind)
            {
                case AbilityKind.Heal:
                    if (ability.Duration > 0f)
                        _effects.Add(StatusEffectKind.HealOverTime, ability.Magnitude * HealScale() * _stats.Mending, ability.Duration);
                    else
                        _fighter.Health.Restore(Mathf.RoundToInt(ability.Magnitude * HealScale() * _stats.Mending));
                    break;
                case AbilityKind.Buff:
                    _effects.Add(ability.Effect, ability.Magnitude, buffDuration);
                    break;
                case AbilityKind.Shield:
                    _effects.Add(StatusEffectKind.Protection, ability.Magnitude, buffDuration);
                    break;
                case AbilityKind.Invisibility:
                    _effects.Add(StatusEffectKind.Invisible, 1f, buffDuration);
                    break;
                case AbilityKind.Dash:
                    _fighter.Move.AddImpulse(-transform.forward * ability.Magnitude + Vector3.up * 2f);
                    break;
                case AbilityKind.Projectile:
                    FireSpell(ability, combat);
                    break;
                case AbilityKind.AreaDamage:
                    AreaDamage(ability, combat);
                    break;
                case AbilityKind.Taunt:
                    _effects.Add(StatusEffectKind.Taunt, ability.Magnitude, buffDuration);
                    break;
                case AbilityKind.Spawn:
                    if (ability.SpawnPrefab != null)
                        Runner.Spawn(ability.SpawnPrefab, transform.position + transform.forward * ability.Radius + Vector3.up * 0.2f, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
                    break;
                case AbilityKind.RestoreCharges:
                    RestoreCharges();
                    break;
            }

            if (ability.IsSpell && !ability.IsCooldownBased)
            {
                if (_isAttuned && _stats.HasThreshold(StatType.Resonance))
                    _isAttuned = false;
                else
                    _charges.Set(index, (byte)Mathf.Max(0, _charges[index] - 1));
            }

            _cooldowns.Set(index, TickTimer.CreateFromSeconds(Runner, GetCooldownDuration(index)));
        }

        private float HealScale()
        {
            return 1f + DungeonFormulas.PowerBonus(_stats.MagicalPower) * 0.5f;
        }

        private void FireSpell(AbilityConfig ability, CombatComponent combat)
        {
            int damage = combat.ScaleDamage(Mathf.RoundToInt(ability.Magnitude), ability.DamageType);
            Vector3 origin = combat.Body.EyePosition + combat.Body.AimDirection * 0.4f;
            Quaternion aim = combat.Body.AimRotation;

            for (int i = 0; i < ability.ProjectileCount; i++)
            {
                float spread = ability.ProjectileCount > 1 ? Mathf.Lerp(-ability.ProjectileSpread, ability.ProjectileSpread, i / (ability.ProjectileCount - 1f)) : 0f;
                Vector3 direction = aim * Quaternion.Euler(0f, spread, 0f) * Vector3.forward;
                combat.Projectiles.Fire(origin, direction * ability.ProjectileSpeed, ability.ProjectileGravity, damage, ability.StaggerDuration,
                    ability.DamageType, ability.ProjectileKind, ability.Radius, (byte)ability.Effect, ability.EffectMagnitude, ability.EffectDuration, ability.LifeSteal);
            }
        }

        private void AreaDamage(AbilityConfig ability, CombatComponent combat)
        {
            int damage = combat.ScaleDamage(Mathf.RoundToInt(ability.Magnitude), ability.DamageType);
            Vector3 center = transform.position + Vector3.up;

            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if (fighter == _fighter || !fighter.Receiver.CanBeHitBy(_fighter.Receiver))
                    continue;

                Vector3 point = fighter.Body.ChestPosition;

                if ((point - center).sqrMagnitude > ability.Radius * ability.Radius)
                    continue;

                if (ability.Effect == StatusEffectKind.HealOverTime)
                    continue;

                fighter.Receiver.ApplyHit(new HitRequest
                {
                    BaseDamage = damage,
                    BodyRays = 1,
                    Zone = HitZone.Torso,
                    Point = point,
                    Normal = (point - center).normalized,
                    AttackerPosition = center,
                    StaggerDuration = Mathf.Max(0.25f, ability.StaggerDuration),
                    DamageType = ability.DamageType,
                    Attacker = _fighter.Receiver
                });
            }
        }

        private void OnProjectileHit(in ProjectileData data, DamageReceiverComponent receiver)
        {
            if (data.LifeSteal > 0f)
                _fighter.Health.Restore(Mathf.RoundToInt(data.Damage * data.LifeSteal));

            if (data.Effect == 0 || !receiver.TryGetComponent(out StatusEffectComponent effects))
                return;

            effects.Add((StatusEffectKind)data.Effect, data.EffectMagnitude, data.EffectDuration);
        }

        /// Rupture-style buffs: every melee hit while active opens a bleed on the victim.
        private void OnHitDealt(DamageReceiverComponent victim, HitResult result, int damage)
        {
            if (result != HitResult.Hit || !HasStateAuthority)
                return;

            float rupture = _effects.GetMagnitude(StatusEffectKind.Rupture);

            if (rupture > 0f && victim.TryGetComponent(out StatusEffectComponent effects))
                effects.Add(StatusEffectKind.Burn, rupture, 5f);
        }

        private void ApplyConsumable(EquipSlot slot)
        {
            ItemStack stack = _inventory.GetEquipped(slot);
            ConsumableItemConfig item = _database.Get<ConsumableItemConfig>(stack.ItemId);

            if (item == null)
                return;

            float tier = Mathf.Max(0, stack.Rarity - (int)ItemRarity.Common);

            switch (item.Effect)
            {
                case ConsumableEffect.HealInstant:
                    _fighter.Health.Restore(Mathf.RoundToInt((item.Magnitude + tier * 4f) * _stats.Mending));
                    break;
                case ConsumableEffect.HealOverTime:
                    _effects.Add(StatusEffectKind.HealOverTime, item.Magnitude * _stats.Mending, Mathf.Max(1f, item.Duration - tier * 2.5f));
                    break;
                case ConsumableEffect.Protection:
                    _effects.Add(StatusEffectKind.Protection, item.Magnitude + tier * 5f, item.Duration);
                    break;
                case ConsumableEffect.Haste:
                    _effects.Add(StatusEffectKind.Haste, item.Magnitude, item.Duration);
                    break;
                case ConsumableEffect.Invisibility:
                    _effects.Add(StatusEffectKind.Invisible, 1f, item.Duration + tier * 2f);
                    break;
            }

            Consume(slot, stack);
        }

        private void ApplyUtility(EquipSlot slot)
        {
            ItemStack stack = _inventory.GetEquipped(slot);
            UtilityItemConfig item = _database.Get<UtilityItemConfig>(stack.ItemId);

            if (item == null)
                return;

            switch (item.UtilityKind)
            {
                case UtilityKind.Campfire:
                    if (_campfirePrefab != null)
                        Runner.Spawn(_campfirePrefab, transform.position + transform.forward * 1.2f, Quaternion.identity);
                    break;
                case UtilityKind.ThrowingWeapon:
                    CombatComponent combat = _fighter.Combat;
                    combat.Projectiles.Fire(combat.Body.EyePosition, combat.Body.AimDirection * 16f, -9.81f,
                        combat.ScaleDamage(item.Damage, DamageType.Physical), 0.2f, DamageType.Physical, ProjectileKind.Thrown);
                    break;
                default:
                    return;
            }

            Consume(slot, stack);
        }

        private void Consume(EquipSlot slot, ItemStack stack)
        {
            _inventory.SetEquipment(slot, stack.Count > 1 ? stack.WithCount(stack.Count - 1) : default);
        }

        private void SimulateRest()
        {
            if (!IsResting || _fighter.Move.Velocity.sqrMagnitude > 0.05f)
            {
                _restTimer = TickTimer.None;

                return;
            }

            if (!_restTimer.IsRunning)
                _restTimer = TickTimer.CreateFromSeconds(Runner, RestHealInterval());

            if (_restTimer.Expired(Runner))
            {
                _fighter.Health.Restore(1);
                _restTimer = TickTimer.CreateFromSeconds(Runner, RestHealInterval());
            }
        }

        /// Flesh 30 rests twice as fast.
        private float RestHealInterval()
        {
            return _restHealInterval / _stats.Mending / (_stats.HasThreshold(StatType.Flesh) ? 2f : 1f);
        }

        /// Unsearched loot of the opened container is discovered one item at a time; Perception sets the pace.
        private void SimulateSearch()
        {
            ContainerComponent container = OpenedContainer;

            if (container != null && (container.transform.position - transform.position).sqrMagnitude > ContainerRange * ContainerRange)
            {
                CloseContainer();
                container = null;
            }

            int index = container != null ? container.Inventory.FindHidden() : -1;

            if (index < 0)
            {
                SearchIndex = NoSearch;

                return;
            }

            if (SearchIndex != index)
            {
                float duration = DungeonFormulas.SearchTime(container.Inventory.Bag[index].RarityValue) / _stats.Perception;
                SearchIndex = (byte)index;
                _searchStartTick = Runner.Tick;
                _searchEndTick = Runner.Tick + Mathf.CeilToInt(duration / Runner.DeltaTime);

                return;
            }

            if (Runner.Tick < _searchEndTick)
                return;

            container.Inventory.Reveal(index);
            SearchIndex = NoSearch;
        }

        private void SimulateSwarm()
        {
            MatchComponent match = DungeonContext.Instance != null ? DungeonContext.Instance.Match : null;

            if (match == null)
            {
                IsInSwarm = false;

                return;
            }

            float damage = match.GetSwarmDamage(Floor, transform.position);
            IsInSwarm = damage > 0f;

            if (match.IsTimeUp(Floor))
            {
                _fighter.Health.Kill();

                return;
            }

            _swarmAccumulator += damage * Runner.DeltaTime;

            if (_swarmAccumulator >= 1f)
            {
                int tick = Mathf.FloorToInt(_swarmAccumulator);
                _swarmAccumulator -= tick;
                _fighter.Health.TakeDamage(tick);
            }
        }

        private void Die()
        {
            State = AdventurerState.Dead;
            Pending = PendingAction.None;
            _fighter.SetInputBlocked(true);
            CloseContainer();
            SpawnCorpse();
            _session?.OnDied(this);
            _removeTimer = TickTimer.CreateFromSeconds(Runner, 2.5f);
        }

        private void SpawnCorpse()
        {
            if (_corpsePrefab == null)
                return;

            Vector3 position = transform.position;
            Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            Runner.Spawn(_corpsePrefab, position, rotation, PlayerRef.None, (_, corpse) =>
            {
                CorpseComponent body = corpse.GetComponent<CorpseComponent>();
                body.Setup(_inventory, _class, _session != null ? _session.DisplayName : "Adventurer");
            });

            _inventory.Clear();
        }

        private InteractableComponent FindInteractable()
        {
            FighterBodyComponent body = _fighter.Body;
            Vector3 origin = body.EyePosition;
            Vector3 direction = HasInputAuthority && DungeonContext.Instance != null
                ? Quaternion.Euler(DungeonContext.Instance.Battle.Input.LookRotation.x, DungeonContext.Instance.Battle.Input.LookRotation.y, 0f) * Vector3.forward
                : body.AimDirection;

            PhysicsScene scene = Runner.GetPhysicsScene();

            if (!scene.Raycast(origin, direction, out RaycastHit hit, InteractRange, _interactMask, QueryTriggerInteraction.Collide))
                return null;

            return hit.collider.GetComponentInParent<InteractableComponent>();
        }

        bool InventoryActionsComponent.IOwner.CanEquip(ItemConfig item, EquipSlot slot)
        {
            return item switch
            {
                WeaponItemConfig weapon => _class.CanUseWeapon(weapon.WeaponClass),
                ArmorItemConfig armor => _class.CanWearArmor(armor.ArmorType),
                _ => true
            };
        }

        bool InventoryActionsComponent.IOwner.CanAccess(InventoryComponent other)
        {
            ContainerComponent container = OpenedContainer;

            if (container == null || container.Inventory != other)
                return false;

            return (other.transform.position - transform.position).sqrMagnitude < ContainerRange * ContainerRange;
        }

        void InventoryActionsComponent.IOwner.OnUseItem(InventoryComponent source, int bagIndex, EquipSlot slot)
        {
            OnUseItem(source, bagIndex, slot);
        }

        private void OnUseItem(InventoryComponent source, int bagIndex, EquipSlot slot)
        {
            if (State != AdventurerState.Alive || Pending != PendingAction.None)
                return;

            if (bagIndex >= 0)
            {
                ItemStack stack = source.Bag[bagIndex];
                ItemConfig config = source.GetConfig(stack);

                if (config == null || config.Kind is not (ItemKind.Consumable or ItemKind.Utility))
                    return;

                slot = FindFreeUtilitySlot();

                if (slot == EquipSlot.Count)
                    return;

                source.RemoveAt(bagIndex);
                _inventory.SetEquipment(slot, stack.At(0, 0));
            }

            ItemStack equipped = _inventory.GetEquipped(slot);
            ItemConfig item = _database.Get(equipped.ItemId);
            float useTime;
            float animSpeed = 1f;
            PendingAction action;
            byte kind = BusyUse;

            switch (item)
            {
                // Potions are drunk at Action Speed, bandages are wound at Handling Speed.
                case ConsumableItemConfig consumable when consumable.Effect == ConsumableEffect.HealInstant:
                    useTime = consumable.UseTime / _stats.HandlingSpeed;
                    action = PendingAction.Consumable;
                    kind = BusyBandage;
                    break;
                case ConsumableItemConfig consumable:
                    animSpeed = _stats.ActionSpeed;
                    useTime = consumable.UseTime / animSpeed;
                    action = PendingAction.Consumable;
                    break;
                case UtilityItemConfig utility when utility.UtilityKind != UtilityKind.Lockpick:
                    useTime = utility.UseTime / _stats.HandlingSpeed;
                    action = PendingAction.Utility;
                    kind = utility.UtilityKind == UtilityKind.ThrowingWeapon ? BusyThrow : BusyInteract;
                    break;
                default:
                    return;
            }

            if (!_fighter.Combat.StartBusy(useTime, kind, animSpeed))
                return;

            Pending = action;
            _pendingIndex = (byte)slot;
            _pendingCompleteTick = Runner.Tick + Mathf.CeilToInt(useTime / Runner.DeltaTime);
        }

        private EquipSlot FindFreeUtilitySlot()
        {
            for (EquipSlot slot = EquipSlot.Utility1; slot <= EquipSlot.Utility6; slot++)
            {
                if (_inventory.GetEquipped(slot).IsEmpty)
                    return slot;
            }

            return EquipSlot.Count;
        }

        void InventoryActionsComponent.IOwner.OnDropItem(ItemStack stack)
        {
            if (_worldItemPrefab == null)
                return;

            Vector3 position = transform.position + transform.forward * 0.8f + Vector3.up * 0.3f;
            Runner.Spawn(_worldItemPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), PlayerRef.None,
                (_, item) => item.GetComponent<WorldItemComponent>().Setup(stack));
        }

        void InventoryActionsComponent.IOwner.OnLoadChunk(byte kind, byte chunk, byte chunkCount, byte[] data)
        {
        }

        float DamageReceiverComponent.IHitModifier.WeakpointMultiplier => _stats.Weakpoint;

        int DamageReceiverComponent.IHitModifier.ModifyIncomingDamage(int damage, DamageType type, HitZone zone)
        {
            int reduced = _stats.ModifyIncomingDamage(damage, type, zone);

            return _effects.Absorb(reduced);
        }
    }
}
