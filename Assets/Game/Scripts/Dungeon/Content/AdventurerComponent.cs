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
        Extracted,
        Descended
    }

    public enum PendingAction : byte
    {
        None,
        Ability,
        Consumable,
        Interact,
        Utility,
        Shapeshift,
        /// A torch (RMB) or a throwing weapon (LMB) drawn back while the button is held, thrown when it is let go.
        Throw
    }

    /// The player's body inside the dungeon: class, inventory, interaction, abilities, death and extraction on top of the fighter.
    public sealed partial class AdventurerComponent : NetworkBehaviour, InventoryActionsComponent.IOwner, DamageReceiverComponent.IHitModifier, CombatComponent.IShotModifier, HitFeedbackComponent.IBodySource
    {
        public const int AbilityCapacity = 16;
        public const float InteractRange = 2.6f;
        public const byte BusyCast = 0;
        public const byte BusyUse = 1;
        public const byte BusyInteract = 2;
        public const byte BusyOpen = 4;
        public const byte BusyPickUp = 5;
        public const byte BusyBandage = 6;
        public const byte BusyRelease = 7;
        /// A skill: it takes effect at the top of the activation clip, the clip plays out after.
        public const byte BusyAbility = 8;
        /// A torch raised over the head while RMB is held, and its throw when RMB is let go.
        public const byte BusyTorchAim = 9;
        public const byte BusyTorchThrow = 10;
        /// A knife or a francisca drawn back while LMB is held, and its throw when LMB is let go.
        public const byte BusyThrowAim = 11;
        public const byte BusyThrowRelease = 12;
        public const int BeltGroupSize = 3;
        public const byte NoBelt = 255;
        public const byte NoSearch = 255;
        /// Setup without a known party: the adventurer is a team of one.
        public const int NoTeam = int.MinValue;
        private const float ContainerRange = 5f;
        private const float KillingBlowTime = 0.5f;
        private const float CastReleaseTime = 0.35f;
        private const float RootTurnLimit = 90f;
        private const float QuickCastTime = 0.05f;
        /// Where the ground marker may stand above or below the aimed point, and how far a ray looks for the floor.
        private const float GroundProbe = 3f;
        private const int WorldMask = 1;
        private const float AuraPulse = 0.5f;
        private const float ChainFalloff = 0.7f;
        private const float StrikeHeight = 14f;
        private const float EnchantBurnTime = 3f;
        private const float EnchantSlow = 25f;
        private const float EnchantSlowTime = 1.5f;
        private const float SunderStagger = 0.7f;
        private const float VerdictSilence = 3f;
        private const int LethalDamage = 100000;
        /// Half-angle of a volley fan and the share of the shot's damage every projectile of it carries.
        private const float VolleySpread = 6f;
        private const float VolleyDamage = 0.7f;
        /// An aimed shot flies this much faster and drops this much less.
        private const float AimedSpeed = 1.8f;
        private const float AimedGravity = 0.25f;
        private const float TorchThrowSpeed = 14f;
        private const float PinTime = 3f;
        private const float ThrowSpeed = 22f;
        /// A throw let go at once flies this share of the full speed, high as a candle; held FullDrawTime it flies almost
        /// straight at full speed and power.
        private const float LobSpeed = 0.25f;
        private const float LobLift = 6.5f;
        private const float StraightLift = 0.6f;
        private const float LobDamage = 0.5f;
        private const float FullDrawTime = 1f;
        /// A weapon that stops in a surface sinks this deep into it.
        private const float StickDepth = 0.06f;
        /// A lit campfire heals the seated this many times faster; without one spells come back this many times slower.
        private const float FireRestBoost = 4f;
        private const float FanSpread = 7f;

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
        public SubclassDefinition SubclassConfig => _class != null ? _class.GetSubclass(Subclass) : null;
        /// Sitting down (G) or getting up takes FighterMoveComponent.SitTime; nothing else is done meanwhile.
        public bool IsInSeat => IsSeated || Runner.Tick - _seatTick < SeatTicks;
        /// Seated on the ground: health and spell charges come back, much faster by a lit campfire.
        public bool IsResting => IsSeated && Runner.Tick - _seatTick >= SeatTicks;
        public bool IsByFire => CampfireComponent.IsWarming(transform.position);
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
        public NetworkBool IsSeated { get; private set; }

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

        /// Just spawned and not acted yet: the client may still be loading in, so monsters leave the adventurer alone.
        [Networked]
        public NetworkBool IsLoadingIn { get; private set; }

        [Networked]
        public byte Subclass { get; private set; }

        /// Party of the adventurer: allies share it, everybody else has a team of one.
        [Networked]
        public int Team { get; private set; }

        /// Stacks of the subclass resource.
        [Networked]
        public byte Resource { get; private set; }

        [Networked]
        public long SpellLayout { get; private set; }

        /// Equipment slot of the belt item held in hand instead of the weapon (keys 3 / 4), NoBelt when the weapon is out.
        [Networked]
        public byte BeltSlot { get; private set; } = NoBelt;

        public bool HasBeltItemInHand => BeltSlot != NoBelt;
        public bool IsTorchInHand => HasBeltItemInHand && _torchWeapon != null && _inventory.GetEquippedConfig<ConsumableItemConfig>((EquipSlot)BeltSlot) is { IsLight: true };

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

        /// Charge progress of the spell being held: 0 when not charging, 1 once it is kept ready until the button is let go.
        /// Where a ground area lands: under the crosshair, held at the spell's range when the aim goes farther.
        public Vector3 GetGroundPoint(float range)
        {
            FighterBodyComponent body = _fighter.Body;
            Vector3 origin = body.EyePosition;
            Vector3 point = Physics.Raycast(origin, body.AimDirection, out RaycastHit hit, range * 3f, WorldMask, QueryTriggerInteraction.Ignore) ? hit.point : origin + body.AimDirection * range * 3f;
            Vector3 flat = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);

            if (flat.magnitude > range)
                point = transform.position + flat.normalized * range;

            return Physics.Raycast(point + Vector3.up * GroundProbe, Vector3.down, out hit, GroundProbe * 4f, WorldMask, QueryTriggerInteraction.Ignore) ? hit.point : point;
        }

        public float CastCharge => IsHoldingCast ? Mathf.InverseLerp(_fighter.Combat.StateTick, _pendingCompleteTick, Runner.Tick) : 0f;
        public bool IsHoldingCast => Pending == PendingAction.Ability && _isHoldingCast;
        public bool IsAimingThrow => Pending == PendingAction.Throw && _fighter.Combat.BusyKind is BusyTorchAim or BusyThrowAim;
        /// How far the throw being held is drawn, 0..1.
        public float ThrowDraw => IsAimingThrow ? Mathf.Clamp01((Runner.Tick - _fighter.Combat.StateTick) * Runner.DeltaTime * (1f + Perk(PerkEffect.QuickThrow)) / FullDrawTime) : 0f;
        public float ReloadSpeed => _stats.ReloadSpeed * (1f + _effects.GetMagnitude(StatusEffectKind.QuickReload) / 100f);

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
        private AbilityZoneComponent _zonePrefab;

        [SerializeField]
        private WeaponConfig _fistsWeapon;

        [SerializeField, Tooltip("Strikes of a burning torch held from the belt")]
        private WeaponConfig _torchWeapon;

        [SerializeField]
        private WeaponConfig[] _formWeapons;

        [SerializeField]
        private float[] _formScales = { 1f, 1.45f, 1f, 0.4f };

        [SerializeField]
        private float _restHealInterval = 2f;

        [SerializeField, Tooltip("Seconds per spell charge regained by the campfire, at neutral Knowledge")]
        private float _spellRecoverInterval = 2f;

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
        private int _seatTick { get; set; }

        [Networked]
        private float _seatYaw { get; set; }

        /// Share of the next charge each spell has regained by resting.
        [Networked, Capacity(AbilityCapacity)]
        private NetworkArray<float> _recovery => default;

        [Networked]
        private TickTimer _removeTimer { get; set; }

        [Networked]
        private TickTimer _loadingTimer { get; set; }

        [Networked]
        private NetworkBool _isHoldingCast { get; set; }

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

        [Networked]
        private TickTimer _resourceTimer { get; set; }

        [Networked]
        private TickTimer _burnTimer { get; set; }

        [Networked]
        private float _throwPower { get; set; }

        private static readonly List<LagCompensatedHit> s_hits = new(16);
        /// A thrown torch leaves the hand above the head and ahead of the eyes, along the aim and a little up.
        private static readonly Vector3 s_torchRelease = new(-0.1f, 0.15f, 0.4f);
        private static readonly List<DamageReceiverComponent> s_struck = new();

        private AbilityConfig _aura;
        private int _auraEndTick;
        private int _auraNextTick;
        private readonly List<AbilityConfig> _abilities = new();
        private readonly List<NetworkObject> _traps = new();
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
        private int _appliedResource;
        private bool _wasAlive = true;
        /// Stacks the ability being applied has spent; its magnitude grows with them.
        private int _spentStacks;
        /// A follow-up hit of an enchant or an empowered strike is under way: it triggers nothing itself.
        private bool _isBonusHit;
        private bool _isChanneling;
        private bool _isPaidInBlood;
        /// Health and spell charges brought down from the floor above, restored on the first server tick.
        private FloorTransfer.Carry _arrival;

        public override void Spawned()
        {
            _class = FindClass(ClassId);
            BuildAbilityList();

            _fighter.SetStats(_stats);
            _fighter.Receiver.SetModifier(this);
            _fighter.Combat.SetShotModifier(this);
            _effects.SetResistance(_stats);
            _fighter.OnSimulateInput += OnSimulateInput;
            _actions.SetOwner(this);
            _fighter.Combat.Projectiles.OnReceiverHit += OnProjectileHit;
            _fighter.Receiver.OnHitDealt += OnHitDealt;
            _fighter.Receiver.OnHitTaken += OnHitTaken;
            _fighter.Combat.Projectiles.OnImpact += OnProjectileImpact;
            _effects.OnExpired += OnEffectExpired;
            _inventory.SetStackBonus(StackBonus);
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
            _fighter.Receiver.OnHitTaken -= OnHitTaken;
            _fighter.Combat.Projectiles.OnImpact -= OnProjectileImpact;
            _effects.OnExpired -= OnEffectExpired;

            if (DungeonContext.Instance != null && DungeonContext.Instance.LocalAdventurer == this)
                DungeonContext.Instance.SetLocalAdventurer(null);
        }

        public override void FixedUpdateNetwork()
        {
            if (_session == null)
                ResolveSession();

            if (IsStatsChanged || _appliedForm != Form)
                RefreshStats(false);

            _fighter.Combat.SetBlockSuppressed((ReadiedSpell != NoSpell && HasFocus && State == AdventurerState.Alive) || HasBeltItemInHand || IsInSeat);
            _fighter.Combat.SetAttackSuppressed((HasBeltItemInHand && !IsTorchInHand) || IsInSeat);
            _fighter.Combat.SetBlockSlowIgnored(Perk(PerkEffect.SteadyBlock) > 0f);

            if (!HasStateAuthority)
                return;

            if (_arrival != null)
                RestoreArrival();

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

            BurnTorch();

            if (Resource > 0 && _resourceTimer.Expired(Runner))
                Resource = 0;

            if (IsLoadingIn)
                SimulateLoadingIn();

            SimulateAura();
            SimulatePerkAuras();
            SimulateDrain();
            SimulateRest();
            SimulateSwarm();
            SimulateSearch();
            SimulateFall();
        }

        public override void Render()
        {
            if (IsStatsChanged)
                RefreshStats(false);

            if (!HasInputAuthority)
                return;

            _lookTarget = FindInteractable();

            // Seated, the head turns as far as at a door.
            if (DungeonContext.Instance != null)
                DungeonContext.Instance.Battle.Input.SetYawLimit(_seatYaw, IsInSeat ? RootTurnLimit : -1f);
        }

        public void Setup(byte classId, PlayerSessionComponent session, byte floor = 1, int team = NoTeam)
        {
            ClassId = classId;
            Team = team != NoTeam ? team : -1 - session.Object.InputAuthority.PlayerId;
            _session = session;
            SkillA = session.SkillA;
            SkillB = session.SkillB;
            PerkMask = session.PerkMask;
            SpellLayout = session.SpellLayout;
            Subclass = session.Subclass;
            Floor = floor;
            State = AdventurerState.Alive;
            IsLoadingIn = true;
        }

        /// Spawn callback of an adventurer coming down from the floor above.
        public void Arrive(FloorTransfer.Carry carry)
        {
            _arrival = carry;
            Floor = carry.Floor;
            Kills = carry.Kills;
            RunExperience = carry.Experience;
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

        /// Ranged perks speed up the reload and add damage; a volley splits the shot into a fan, an aimed shot flies straight and bites deeper.
        public void ModifyShot(ref ShotRequest shot)
        {
            float volley = _effects.GetMagnitude(StatusEffectKind.Volley);
            float aimed = _effects.GetMagnitude(StatusEffectKind.AimedShot);

            if (volley > 0f)
            {
                shot.Count = Mathf.RoundToInt(volley);
                shot.Spread = VolleySpread;
                shot.Damage = Mathf.RoundToInt(shot.Damage * VolleyDamage);
            }

            if (aimed > 0f)
            {
                shot.Damage += _fighter.Combat.ScaleDamage(Mathf.RoundToInt(aimed), DamageType.Physical);
                shot.Speed *= AimedSpeed;
                shot.Gravity *= AimedGravity;
            }

            float exhale = _effects.GetMagnitude(StatusEffectKind.Exhale);
            float pinning = _effects.GetMagnitude(StatusEffectKind.PinningShot);
            shot.Damage = Mathf.Max(1, Mathf.RoundToInt(shot.Damage * _stats.RangedDamageMultiplier * (1f + exhale / 100f)));

            if (_effects.Has(StatusEffectKind.PiercingShot))
                shot.Flags |= ProjectileFlags.IgnoreArmor;

            if (_effects.Has(StatusEffectKind.FlareShot))
                shot.Flags |= ProjectileFlags.Flare;

            if (pinning > 0f)
            {
                shot.Effect = (byte)StatusEffectKind.Slow;
                shot.EffectMagnitude = pinning;
                shot.EffectDuration = PinTime;
            }

            if (!HasStateAuthority)
                return;

            _effects.Remove(StatusEffectKind.Volley);
            _effects.Remove(StatusEffectKind.AimedShot);
            _effects.Remove(StatusEffectKind.Exhale);
            _effects.Remove(StatusEffectKind.PiercingShot);
            _effects.Remove(StatusEffectKind.PinningShot);
            _effects.Remove(StatusEffectKind.FlareShot);
        }

        /// Bolts lie in the bag; a weapon without ammunition set never runs out.
        public int CountAmmo(int slot)
        {
            ItemConfig ammo = GetAmmo(slot);

            return ammo != null ? _inventory.CountOf(ammo.Id) : int.MaxValue;
        }

        public void ConsumeAmmo(int slot)
        {
            ItemConfig ammo = GetAmmo(slot);

            if (ammo != null)
                _inventory.Remove(ammo.Id, 1);
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

        /// Knowledge adds charges to every charge-based spell.
        public int GetMaxCharges(int ability)
        {
            AbilityConfig config = _abilities[ability];

            if (!config.IsSpell || config.IsCooldownBased)
                return config.Charges;

            float perks = (config.Has(AbilityFlags.Frost) ? Perk(PerkEffect.WinterMemory) : 0f) + (config.Has(AbilityFlags.Healing) ? Perk(PerkEffect.Patience) : 0f);

            return config.Charges + _stats.BonusCharges + Mathf.RoundToInt(perks);
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

        /// The next floor is another session: the adventurer leaves this one and its owner carries it down.
        public void Descend()
        {
            if (State != AdventurerState.Alive)
                return;

            State = AdventurerState.Descended;
            Pending = PendingAction.None;
            _fighter.SetInputBlocked(true);
            CloseContainer();
            _session?.OnDescended(this);
            _removeTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);
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

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcCloseContainer()
        {
            CloseContainer();
        }

        /// Spell wheel selection: readies a spell; it is cast with the secondary button once a focus is taken in hand.
        /// The centre of the wheel sends NoSpell: the secondary button goes back to blocking with the weapon.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcReadySpell(byte spellIndex)
        {
            byte spell = spellIndex < _class.Spells.Length && IsSpellMemorized(spellIndex) ? spellIndex : NoSpell;

            if (spell != ReadiedSpell && IsHoldingCast)
                CancelPending();

            ReadiedSpell = spell;
        }

        /// Spell in the wheel slot, -1 for a free one.
        public int GetSlotSpell(int slot)
        {
            return ClassConfig.GetSlotSpell(SpellLayout, slot);
        }

        /// A spell can be readied when it sits in the wheel of an equipped spell memory: the memory in skill slot i opens wheel i.
        public bool IsSpellMemorized(int spellIndex)
        {
            for (int wheel = 0; wheel < _skills.Length; wheel++)
            {
                if (_skills[wheel] != null && _skills[wheel].Kind == AbilityKind.SpellMemory && ClassConfig.IsInWheel(SpellLayout, wheel, spellIndex))
                    return true;
            }

            return false;
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

        /// Rereading the book or the rosary: every spell gets this many charges back.
        public void RestoreCharges(int amount)
        {
            if (amount <= 0)
            {
                RestoreCharges();

                return;
            }

            for (int i = 0; i < _abilities.Count; i++)
            {
                if (_abilities[i].IsSpell && !_abilities[i].IsCooldownBased)
                    _charges.Set(i, (byte)Mathf.Min(GetMaxCharges(i), _charges[i] + amount));
            }
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
            if (!HasWeaponInHand)
                return false;

            int slot = _fighter.Combat.WeaponSlot;
            WeaponItemConfig main = _inventory.GetEquippedConfig<WeaponItemConfig>(slot == 0 ? EquipSlot.Weapon1Main : EquipSlot.Weapon2Main);
            WeaponItemConfig off = _inventory.GetEquippedConfig<WeaponItemConfig>(slot == 0 ? EquipSlot.Weapon1Off : EquipSlot.Weapon2Off);

            return (main != null && main.IsFocus) || (off != null && off.IsFocus);
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

        private bool IsStatsChanged => _appliedVersion != _inventory.Version || _appliedBelt != BeltSlot || _appliedWeaponSet != HeldWeaponSet
            || _appliedEffects != _effects.GetSignature() || _appliedResource != Resource;

        private void RefreshStats(bool fillHealth)
        {
            _appliedVersion = _inventory.Version;
            _appliedResource = Resource;
            _appliedEffects = _effects.GetSignature();
            int level = _session != null ? _session.Level : 1;
            _appliedForm = Form;
            _appliedBelt = BeltSlot;
            _appliedWeaponSet = HeldWeaponSet;
            _class.SumPerkEffects(PerkMask, ClassConfig.PerkCountForLevel(level), _perks);
            _stats.Recalculate(_class, _inventory, _effects, _appliedWeaponSet, ClassConfig.PerkCountForLevel(level), Form, PerkMask, HasBeltItemInHand ? BeltSlot : -1, Resource, _perks);

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

        private ItemConfig GetAmmo(int slot)
        {
            WeaponItemConfig weapon = _inventory.GetEquippedConfig<WeaponItemConfig>(slot == 0 ? EquipSlot.Weapon1Main : EquipSlot.Weapon2Main);

            return weapon != null ? weapon.Ammo : null;
        }

        private int ResolveWeaponIndex(int set, EquipSlot mainSlot, EquipSlot offSlot)
        {
            if (Form != ShapeshiftForm.None && _formWeapons != null && (int)Form - 1 < _formWeapons.Length)
                return Mathf.Max(0, _fighter.Combat.FindCatalogIndex(_formWeapons[(int)Form - 1]));

            // A torch in hand strikes with LMB.
            if (IsTorchInHand && set == _fighter.Combat.WeaponSlot)
                return Mathf.Max(0, _fighter.Combat.FindCatalogIndex(_torchWeapon));

            // A belt item in hand or the holster key puts the active set away: empty hands.
            if (!HasWeaponInHand && set == _fighter.Combat.WeaponSlot)
                return Mathf.Max(0, _fighter.Combat.FindCatalogIndex(_fistsWeapon));

            WeaponItemConfig main = _inventory.GetEquippedConfig<WeaponItemConfig>(mainSlot);
            WeaponItemConfig off = _inventory.GetEquippedConfig<WeaponItemConfig>(offSlot);
            CombatComponent combat = _fighter.Combat;

            if (main == null)
                return Mathf.Max(0, combat.FindCatalogIndex(off != null && off.Weapon != null ? off.Weapon : _fistsWeapon));

            return Mathf.Max(0, combat.FindCatalogIndex(main.GetWeapon(off)));
        }

        private void OnSimulateInput(NetworkButtons buttons, NetworkButtons previous)
        {
            if (!HasStateAuthority || State != AdventurerState.Alive)
                return;

            CombatComponent combat = _fighter.Combat;

            if (IsHoldingCast && _effects.Has(StatusEffectKind.Silence))
                CancelPending();

            if (IsHoldingCast)
            {
                bool isReleased = !buttons.IsSet(PlayerInputButtons.Secondary);

                if (isReleased && Runner.Tick >= _pendingCompleteTick)
                    FinishCast();
                else if (isReleased || buttons.WasPressed(previous, PlayerInputButtons.Interact))
                    CancelPending();
            }
            else
            {
                if (IsAimingThrow && !buttons.IsSet(_fighter.Combat.BusyKind == BusyTorchAim ? PlayerInputButtons.Secondary : PlayerInputButtons.Primary))
                    ReleaseThrow();

                SimulatePending();
            }

            if (Pending == PendingAction.Interact && (_isRooted ? Mathf.Abs(Mathf.DeltaAngle(_rootYaw, _fighter.Look.y)) > RootTurnLimit : !buttons.IsSet(PlayerInputButtons.Interact)))
                CancelPending();

            if (Pending != PendingAction.None && combat.State != CombatState.Busy)
                CancelPending();

            if (SimulateSeat(buttons, previous))
                return;

            _fighter.SetRooted((Pending == PendingAction.Interact && _isRooted) || (Pending == PendingAction.Ability && _isChanneling));
            _fighter.SetCrouchForced(Pending == PendingAction.Ability && _isChanneling);

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

        /// G sits down on the ground with empty hands or gets up again; in the seat the body stays put and only the head turns.
        private bool SimulateSeat(NetworkButtons buttons, NetworkButtons previous)
        {
            bool isSettled = Runner.Tick - _seatTick >= SeatTicks;
            bool canSit = Pending == PendingAction.None && _fighter.Combat.State == CombatState.Idle && Form == ShapeshiftForm.None && _fighter.Move.IsGrounded;

            if (buttons.WasPressed(previous, PlayerInputButtons.Rest) && isSettled && (IsSeated || canSit))
            {
                IsSeated = !IsSeated;
                _seatTick = Runner.Tick;
                _seatYaw = _fighter.Look.y;
                BeltSlot = NoBelt;
                IsHolstered |= IsSeated;
            }

            bool isInSeat = IsInSeat;
            _fighter.SetSeat(IsSeated, isInSeat);
            _fighter.SetRooted(isInSeat);
            _fighter.SetCrouchForced(false);

            return isInSeat;
        }

        private int SeatTicks => Mathf.CeilToInt(FighterMoveComponent.SitTime / Runner.DeltaTime);

        /// 1 / 2 take a weapon set out, X puts it away, 3 / 4 cycle the three belt slots of their group; LMB uses the belt item, RMB puts it away.
        /// A torch strikes with LMB and is thrown with RMB; a throwing weapon is thrown with LMB and has nothing on RMB.
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

            ItemConfig held = _inventory.GetEquippedConfig<ItemConfig>((EquipSlot)BeltSlot);
            bool isTorch = held is ConsumableItemConfig { IsLight: true };
            bool isThrowing = held is UtilityItemConfig { UtilityKind: UtilityKind.ThrowingWeapon };

            if (buttons.WasPressed(previous, PlayerInputButtons.Primary) && isThrowing)
                TryAimThrow(BusyThrowAim);
            else if (buttons.WasPressed(previous, PlayerInputButtons.Primary))
                OnUseItem((EquipSlot)BeltSlot);
            else if (buttons.WasPressed(previous, PlayerInputButtons.Secondary) && isTorch)
                TryAimThrow(BusyTorchAim);
            else if (buttons.WasPressed(previous, PlayerInputButtons.Secondary) && !isThrowing)
                BeltSlot = NoBelt;

            return true;
        }

        /// The held torch or throwing weapon is drawn back; it flies when the button is let go, the harder the longer it was held.
        private void TryAimThrow(byte kind)
        {
            if (!_fighter.Combat.StartBusy(float.PositiveInfinity, kind))
                return;

            Pending = PendingAction.Throw;
            _pendingIndex = BeltSlot;
            _pendingCompleteTick = int.MaxValue;
        }

        /// The throw plays out; the item leaves the hand at its release moment (SimulatePending).
        private void ReleaseThrow()
        {
            byte kind = _fighter.Combat.BusyKind == BusyTorchAim ? BusyTorchThrow : BusyThrowRelease;
            float speed = kind == BusyThrowRelease ? _stats.HandlingSpeed * (1f + Perk(PerkEffect.QuickThrow)) : 1f;
            _throwPower = ThrowDraw;
            _fighter.Combat.CancelBusy();
            _fighter.Combat.StartBusy(FighterAnimComponent.TorchThrowTime / speed, kind, speed);
            _pendingCompleteTick = Runner.Tick + Mathf.CeilToInt(FighterAnimComponent.TorchRelease / speed / Runner.DeltaTime);
        }

        /// Let go at once the throw goes up as a candle and lands near; fully drawn it flies almost straight, slightly dropping.
        private Vector3 ThrowVelocity(float fullSpeed)
        {
            return _fighter.Body.AimDirection * (fullSpeed * Mathf.Lerp(LobSpeed, 1f, _throwPower)) + Vector3.up * Mathf.Lerp(LobLift, StraightLift, _throwPower);
        }

        private void Throw(EquipSlot slot)
        {
            ItemStack stack = _inventory.GetEquipped(slot);

            if (_database.Get(stack.ItemId) is not UtilityItemConfig { UtilityKind: UtilityKind.ThrowingWeapon } item)
            {
                ThrowTorch(slot);

                return;
            }

            ThrowWeapon(item, stack);
            Consume(slot, stack);
        }

        /// The torch flies from over the head along the aim and burns on where it lands.
        private void ThrowTorch(EquipSlot slot)
        {
            ItemStack torch = _inventory.GetEquipped(slot);

            if (_worldItemPrefab == null || _database.Get(torch.ItemId) is not ConsumableItemConfig { IsLight: true })
                return;

            _inventory.SetEquipment(slot, default);
            FighterBodyComponent body = _fighter.Body;
            Vector3 velocity = ThrowVelocity(TorchThrowSpeed);
            Runner.Spawn(_worldItemPrefab, body.EyePosition + body.AimRotation * s_torchRelease, transform.rotation, PlayerRef.None, (_, item) =>
            {
                WorldItemComponent thrown = item.GetComponent<WorldItemComponent>();
                thrown.Setup(torch.WithLit(true));
                thrown.Launch(velocity);
            });
        }

        /// The torch in hand burns a second of its fuel each second and is gone when the fuel runs out.
        private void BurnTorch()
        {
            if (!HasBeltItemInHand || IsHandsOccupied || !_burnTimer.ExpiredOrNotRunning(Runner))
                return;

            EquipSlot slot = (EquipSlot)BeltSlot;
            ItemStack torch = _inventory.GetEquipped(slot);

            if (_database.Get(torch.ItemId) is not ConsumableItemConfig { IsLight: true } config)
                return;

            _burnTimer = TickTimer.CreateFromSeconds(Runner, 1f);
            _inventory.SetEquipment(slot, torch.Burnt + 1 < config.Duration ? torch.WithBurnt(torch.Burnt + 1) : default);
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

            float duration = target.HoldTime / _stats.GetInteractionSpeed(target.IsMagical);

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

            if (!HasFocus || !CanAfford(spell))
                return;

            int index = _skillCount + ReadiedSpell;

            if (!_cooldowns[index].ExpiredOrNotRunning(Runner) || (!spell.IsCooldownBased && _charges[index] == 0 && !CanPayInBlood()))
                return;

            bool isQuick = _effects.Has(StatusEffectKind.QuickCast);
            float duration = isQuick ? QuickCastTime : Mathf.Max(0.1f, spell.CastTime / SpellCastSpeed(spell));

            // The charged spell stays in the hands for as long as the button is held.
            if (!_fighter.Combat.StartBusy(float.PositiveInfinity, BusyCast))
                return;

            if (isQuick)
                _effects.Remove(StatusEffectKind.QuickCast);

            Pending = PendingAction.Ability;
            _pendingIndex = (byte)index;
            _pendingCompleteTick = Runner.Tick + Mathf.CeilToInt(duration / Runner.DeltaTime);
            _isHoldingCast = true;
        }

        /// The charged spell takes effect when the button is let go; letting go before the cast completes cancels it for free.
        private void FinishCast()
        {
            int index = _pendingIndex;
            Pending = PendingAction.None;
            _isHoldingCast = false;
            _fighter.Combat.CancelBusy();
            _fighter.Combat.StartBusy(CastReleaseTime, BusyRelease);
            AbilityConfig ability = _abilities[index];

            if (ability.HealthCost > 0)
                _fighter.Health.TakeDamage(ability.HealthCost);

            if (!ability.IsCooldownBased && _charges[index] == 0)
            {
                if (!CanPayInBlood())
                    return;

                _fighter.Health.TakeDamage(BloodPrice);
                _isPaidInBlood = true;
            }

            ApplyAbility(index);
            _isPaidInBlood = false;
        }

        /// Blood prayer: a prayer with no charges left is read for a share of health.
        private int BloodPrice => Mathf.CeilToInt(_fighter.Health.MaxHealth * Perk(PerkEffect.BloodPrayer) / 100f);

        private bool CanPayInBlood()
        {
            return Perk(PerkEffect.BloodPrayer) > 0f && _fighter.Health.CurrentHealth > BloodPrice;
        }

        /// Knowledge, a concentration buff, the altar boy's habit and the quick word of the stormcaller.
        private float SpellCastSpeed(AbilityConfig spell)
        {
            return _stats.CastSpeed * (1f + Perk(PerkEffect.Acolyte) + (spell.Has(AbilityFlags.Lightning) ? Perk(PerkEffect.QuickWord) : 0f));
        }

        private void TryUseAbility(int index)
        {
            if (index >= _abilities.Count)
                return;

            AbilityConfig ability = _abilities[index];

            if (!_cooldowns[index].ExpiredOrNotRunning(Runner) || (ability.IsSpell && !ability.IsCooldownBased && _charges[index] == 0) || !CanAfford(ability))
                return;

            if (ability.Has(AbilityFlags.RequiresShield) && !HoldsWeaponClass(WeaponClass.Shield))
                return;

            if (ability.HealthCost > 0)
                _fighter.Health.TakeDamage(ability.HealthCost);

            float speed = ability.IsSpell ? _stats.CastSpeed : _stats.ActionSpeed;

            // Reading the book or praying on the knees: the hands are busy and the feet stay put for the whole channel.
            if (ability.Channel > 0f)
            {
                if (!_fighter.Combat.StartBusy(ability.Channel, BusyCast))
                    return;

                Pending = PendingAction.Ability;
                _pendingIndex = (byte)index;
                _pendingCompleteTick = Runner.Tick + Mathf.CeilToInt(ability.Channel / Runner.DeltaTime);
                _isChanneling = true;
                RpcCast((byte)index);

                return;
            }

            if (!_fighter.Combat.StartBusy(FighterAnimComponent.AbilityTime / speed, BusyAbility, speed))
                return;

            Pending = PendingAction.Ability;
            _pendingIndex = (byte)index;
            _pendingCompleteTick = Runner.Tick + Mathf.CeilToInt(FighterAnimComponent.AbilityPeak / speed / Runner.DeltaTime);
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
                    _isChanneling = false;
                    ApplyAbility(_pendingIndex);
                    break;
                case PendingAction.Consumable:
                    ApplyConsumable((EquipSlot)_pendingIndex);
                    break;
                case PendingAction.Utility:
                    ApplyUtility((EquipSlot)_pendingIndex);
                    break;
                case PendingAction.Throw:
                    Throw((EquipSlot)_pendingIndex);
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
            _isChanneling = false;

            if (Pending == PendingAction.Interact && Runner.TryFindBehaviour(_pendingTarget, out NetworkBehaviour behaviour) && behaviour is InteractableComponent target)
                target.Cancel(this);

            Pending = PendingAction.None;
            _fighter.Combat.CancelBusy();
        }

        /// Silence stops spells; health and subclass stacks pay for the abilities that cost them.
        private bool CanAfford(AbilityConfig ability)
        {
            return (!ability.IsSpell || !_effects.Has(StatusEffectKind.Silence)) && _fighter.Health.CurrentHealth > ability.HealthCost + HealthPercentCost(ability)
                && Resource >= ability.ResourceCost;
        }

        private int HealthPercentCost(AbilityConfig ability)
        {
            return Mathf.CeilToInt(_fighter.Health.MaxHealth * ability.HealthCostPercent / 100f);
        }

        /// Magnitude of the ability being applied, grown by the stacks it spent.
        private float Power(AbilityConfig ability)
        {
            return ability.Magnitude + _spentStacks * ability.StackBonus;
        }

        /// A stack of the subclass resource when the subclass earns it from this kind of event; every new stack restarts the decay.
        private void GainResource(ResourceSource source)
        {
            SubclassDefinition subclass = SubclassConfig;

            if (subclass == null || (subclass.ResourceSources & source) == 0 || State != AdventurerState.Alive)
                return;

            Resource = (byte)Mathf.Min(Resource + 1, subclass.ResourceMax);
            _resourceTimer = TickTimer.CreateFromSeconds(Runner, subclass.ResourceDecay);
        }

        private void ApplyAbility(int index)
        {
            AbilityConfig ability = _abilities[index];
            CombatComponent combat = _fighter.Combat;
            float buffDuration = ability.Duration * (ability.Has(AbilityFlags.Mixture) ? 1f + Perk(PerkEffect.DoubleDose) : 1f);
            _spentStacks = ability.SpendsResource ? Resource : 0;

            if (ability.SpendsResource)
                Resource = 0;
            else if (ability.Kind is AbilityKind.Heal or AbilityKind.AreaHeal or AbilityKind.Shield || (ability.Kind == AbilityKind.Buff && (ability.IsSpell || ability.IsOnAlly)))
                GainResource(ResourceSource.Support);

            if (ability.HealthCostPercent > 0f)
                _fighter.Health.TakeDamage(HealthPercentCost(ability));

            if (ability.Channel <= 0f)
                RpcCast((byte)index);

            switch (ability.Kind)
            {
                case AbilityKind.Heal:
                    AdventurerComponent healed = SupportTarget(ability);
                    Heal(healed, ability);
                    ApplySecond(healed, ability);
                    break;
                case AbilityKind.AreaHeal:
                    AreaHeal(ability, AreaCenter(ability));
                    break;
                case AbilityKind.Buff:
                    AdventurerComponent buffed = SupportTarget(ability);
                    buffed._effects.Add(ability.Effect, Power(ability), buffDuration);
                    ApplySecond(buffed, ability);

                    if (buffed != this)
                        RpcFlash(BodyPoint(buffed._fighter.Receiver), ability.Color, 4f);
                    break;
                case AbilityKind.AreaBuff:
                    StartAura(ability);
                    break;
                case AbilityKind.Shield:
                    SupportTarget(ability)._effects.Add(StatusEffectKind.Protection, Power(ability), buffDuration);
                    break;
                case AbilityKind.Invisibility:
                    _effects.Add(StatusEffectKind.Invisible, 1f, buffDuration);
                    break;
                case AbilityKind.Dash:
                    Blink(ability, -1f);
                    break;
                case AbilityKind.Projectile or AbilityKind.Throw:
                    FireSpell(ability, combat, index);
                    break;
                case AbilityKind.AreaDamage:
                    AreaDamage(ability, combat, AreaCenter(ability));
                    RpcImpact((byte)index, AreaCenter(ability) + Vector3.up, true);
                    break;
                case AbilityKind.Taunt:
                    _effects.Add(StatusEffectKind.Taunt, Power(ability), buffDuration);
                    break;
                case AbilityKind.Spawn:
                    SpawnObject(ability, index);
                    break;
                case AbilityKind.RestoreCharges:
                    RestoreCharges(Mathf.RoundToInt(ability.Magnitude));
                    break;
                case AbilityKind.WeaponEnchant:
                    SupportTarget(ability).Effects.Add(ability.Effect, combat.ScaleDamage(Mathf.RoundToInt(Power(ability)), DamageType.Magical), buffDuration);
                    break;
                case AbilityKind.ChainLightning:
                    ChainLightning(ability, combat);
                    break;
                case AbilityKind.LightningStrike:
                    LightningStrike(ability, combat);
                    break;
                case AbilityKind.Blink:
                    Blink(ability);
                    break;
                case AbilityKind.QuickCast:
                    _effects.Add(StatusEffectKind.QuickCast, 1f, buffDuration);
                    break;
                case AbilityKind.Zone:
                    Vector3 spot = ability.Targeting == AbilityTargeting.Ground ? GetGroundPoint(ability.Range) : ZoneOrigin(ability);
                    SpawnZone(ability, spot);
                    RpcImpact((byte)index, spot, false);
                    break;
                case AbilityKind.TargetEffect:
                    TargetEffect(ability, combat, index);
                    break;
                case AbilityKind.GiveItem:
                    GiveItem(ability);
                    break;
            }

            if (ability.IsSpell && !ability.IsCooldownBased)
            {
                if (_isPaidInBlood)
                    return;

                if (_isAttuned && _stats.HasThreshold(StatType.Knowledge))
                    _isAttuned = false;
                else if (Random.value >= Perk(PerkEffect.FrugalWord))
                    _charges.Set(index, (byte)Mathf.Max(0, _charges[index] - 1));

                return;
            }

            _cooldowns.Set(index, TickTimer.CreateFromSeconds(Runner, GetCooldownDuration(index)));
        }

        /// The second effect of a buff or a brew; a mixture's side effect is shortened by habit, the bull's blood bill waits for the buff to run out.
        private void ApplySecond(AdventurerComponent target, AbilityConfig ability)
        {
            if (ability.SecondEffect == StatusEffectKind.None)
                return;

            bool isMixture = ability.Has(AbilityFlags.Mixture);
            bool isSide = isMixture && ability.SecondEffect is StatusEffectKind.Sluggish or StatusEffectKind.Drain;
            float duration = ability.SecondDuration * (isSide ? 1f - Perk(PerkEffect.Accustomed) : isMixture ? 1f + Perk(PerkEffect.DoubleDose) : 1f);

            if (ability.Effect == StatusEffectKind.BullBlood)
            {
                _sideEffect = ability.SecondEffect;
                _sideMagnitude = ability.SecondMagnitude;
                _sideDuration = duration;

                return;
            }

            target._effects.Add(ability.SecondEffect, ability.SecondMagnitude, duration);
        }

        /// The banner is planted a step ahead, every other zone stands around the user.
        private Vector3 ZoneOrigin(AbilityConfig ability)
        {
            Vector3 origin = ability.Zone == ZoneKind.Banner ? transform.position + transform.forward : transform.position;

            return Physics.Raycast(origin + Vector3.up, Vector3.down, out RaycastHit hit, GroundProbe, WorldMask, QueryTriggerInteraction.Ignore) ? hit.point : origin;
        }

        /// A wall of fire stands across the aim; flasks spread wider with the demolitionist's glass.
        private void SpawnZone(AbilityConfig ability, Vector3 point)
        {
            if (_zonePrefab == null)
                return;

            float radius = ability.ZoneRadius * (ability.Has(AbilityFlags.Flask) ? 1f + Perk(PerkEffect.WideSpread) : 1f);
            float magnitude = Power(ability);
            Quaternion rotation = Quaternion.Euler(0f, _fighter.Look.y, 0f);
            Runner.Spawn(_zonePrefab, point, rotation, PlayerRef.None,
                (_, zone) => zone.GetComponent<AbilityZoneComponent>().Setup(this, ability, radius, ability.ZoneDuration, ability.ZoneTick, magnitude));
        }

        /// Traps: never more on the ground than the trapper may keep, the oldest one goes first.
        private void SpawnObject(AbilityConfig ability, int index)
        {
            if (ability.SpawnPrefab == null)
                return;

            _traps.RemoveAll(trap => trap == null || !trap.IsValid);
            int limit = 2 + Mathf.RoundToInt(Perk(PerkEffect.IronStock));

            while (_traps.Count >= limit)
            {
                Runner.Despawn(_traps[0]);
                _traps.RemoveAt(0);
            }

            NetworkObject spawned = Runner.Spawn(ability.SpawnPrefab, transform.position + transform.forward * ability.Radius + Vector3.up * 0.2f, Quaternion.Euler(0f, transform.eulerAngles.y, 0f),
                Object.InputAuthority, (_, obj) =>
                {
                    if (obj.TryGetComponent(out TrapComponent trap))
                        trap.SetOwner(this);
                });

            if (spawned != null)
                _traps.Add(spawned);

            RpcImpact((byte)index, transform.position, false);
        }

        /// A hitscan on the one under the crosshair: a strike of light, a drain over time or a binding effect.
        private void TargetEffect(AbilityConfig ability, CombatComponent combat, int index)
        {
            DamageReceiverComponent target = AimReceiver(ability.Range, out Vector3 point);
            Vector3 end = target != null ? BodyPoint(target) : point;
            RpcBeam(ability.Vfx, HandPoint, end, ability.Color);
            RpcImpact((byte)index, end, true);

            if (target == null || !target.CanBeHitBy(_fighter.Receiver))
                return;

            target.TryGetComponent(out StatusEffectComponent effects);
            float damage = combat.ScaleDamage(Mathf.RoundToInt(Power(ability)), ability.DamageType);

            if (ability.Has(AbilityFlags.HalfVsLiving) && !IsUndead(target))
                damage *= 0.5f;

            if (ability.Magnitude > 0f && ability.Duration > 0f)
                StartDrain(target, ability, damage);
            else if (ability.Magnitude > 0f)
                SpellHit(target, Mathf.RoundToInt(damage), ability, HandPoint);
            else if (effects != null && target.IsAlive)
                ApplyEffectTo(effects, ability.Effect, ability.EffectMagnitude, ability.EffectDuration, ability.Flags);
        }

        /// A spare knife from the boot: onto a matching belt stack or into a free belt slot, never over the stack.
        private void GiveItem(AbilityConfig ability)
        {
            if (ability.Item == null)
                return;

            ItemStack stack = ItemStack.Create(ability.Item, Mathf.RoundToInt(ability.Magnitude), ability.Item.BaseRarity);
            _inventory.TryAddToBelt(ref stack);
        }

        /// Cast sound and flourish of an ability on every client.
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcCast(byte index)
        {
            if (index >= _abilities.Count)
                return;

            AbilityConfig ability = _abilities[index];
            DungeonAudioComponent.Play(ability.CastSound, transform.position + Vector3.up, 1f, Random.Range(0.96f, 1.04f));
            AbilityVfx.Cast(ability.Vfx, transform, ability.Color);
        }

        /// Impact sound of an ability where it struck, and its burst when the place has no visual of its own.
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcImpact(byte index, Vector3 point, NetworkBool isBurst)
        {
            if (index >= _abilities.Count)
                return;

            AbilityConfig ability = _abilities[index];
            DungeonAudioComponent.Play(ability.ImpactSound, point, 1f, Random.Range(0.95f, 1.05f));

            if (isBurst)
                AbilityVfx.Burst(ability.Vfx, point, Mathf.Max(1f, ability.Radius), ability.Color);
        }

        private float HealScale()
        {
            return 1f + DungeonFormulas.PowerBonus(_stats.MagicalPower) * 0.5f;
        }

        /// A caught breath grows with physical healing, a prayer with magical power and magical healing; both are the healer's.
        private float HealAmount(AbilityConfig ability)
        {
            return Power(ability) * (ability.DamageType == DamageType.Physical ? _stats.PhysicalHealing : HealScale() * _stats.MagicalHealing);
        }

        /// Healing, shielding and blessing spells go to the adventurer under the crosshair, or to the caster when the aim misses; skills stay on their user.
        private AdventurerComponent SupportTarget(AbilityConfig ability)
        {
            return (ability.IsSpell || ability.IsOnAlly) && ability.Range > 0f ? FindAlly(ability) : this;
        }

        private void Heal(AdventurerComponent target, AbilityConfig ability)
        {
            float scale = ability.DamageType == DamageType.Physical ? _stats.PhysicalHealing : HealScale() * _stats.MagicalHealing;
            HealTarget(target, Power(ability), ability.IsPercentHealth, ability.Duration > 0f, ability.Duration, scale);

            if (ability.IsSpell)
                RpcFlash(BodyPoint(target._fighter.Receiver), ability.Color, 5f);
        }

        /// The caster for a burst or an aura, the marked spot for a ground area.
        private Vector3 AreaCenter(AbilityConfig ability)
        {
            return ability.Targeting == AbilityTargeting.Ground ? GetGroundPoint(ability.Range) : transform.position;
        }

        private void AreaHeal(AbilityConfig ability, Vector3 center)
        {
            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if ((fighter.transform.position - center).sqrMagnitude > ability.Radius * ability.Radius
                    || !fighter.TryGetComponent(out AdventurerComponent ally) || ally.State != AdventurerState.Alive)
                    continue;

                HealTarget(ally, Power(ability), ability.IsPercentHealth, true, ability.Duration, HealScale() * _stats.MagicalHealing);
            }

            RpcFlash(center + Vector3.up, ability.Color, ability.Radius * 2f);
        }

        /// A burst buffs once; with an aura time the buff keeps being renewed on whoever stands in it, the caster moving it along.
        private void StartAura(AbilityConfig ability)
        {
            AreaBuff(ability, ability.Duration);

            if (ability.AuraTime <= 0f)
                return;

            _aura = ability;
            _auraEndTick = Runner.Tick + Mathf.CeilToInt(ability.AuraTime / Runner.DeltaTime);
            _auraNextTick = Runner.Tick + Mathf.CeilToInt(AuraPulse / Runner.DeltaTime);
        }

        private void SimulateAura()
        {
            if (_aura == null)
                return;

            if (State != AdventurerState.Alive || Runner.Tick >= _auraEndTick)
            {
                _aura = null;

                return;
            }

            if (Runner.Tick < _auraNextTick)
                return;

            _auraNextTick = Runner.Tick + Mathf.CeilToInt(AuraPulse / Runner.DeltaTime);
            AreaBuff(_aura, AuraPulse * 2f, false);
        }

        /// The user and every adventurer of the user's party within the radius.
        private void AreaBuff(AbilityConfig ability, float duration, bool isFlash = true)
        {
            float radius = ability.Radius * (ability.Has(AbilityFlags.Shout) ? 1f + Perk(PerkEffect.Voice) : 1f);

            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if ((fighter.transform.position - transform.position).sqrMagnitude > radius * radius
                    || !fighter.TryGetComponent(out AdventurerComponent ally) || ally.State != AdventurerState.Alive || !IsAllyOf(ally))
                    continue;

                ally._effects.Add(ability.Effect, Power(ability), duration);
            }

            if (isFlash)
                RpcFlash(transform.position + Vector3.up, ability.Color, radius * 2f);
        }

        private void FireSpell(AbilityConfig ability, CombatComponent combat, int index)
        {
            int damage = ability.Magnitude > 0f ? combat.ScaleDamage(Mathf.RoundToInt(Power(ability)), ability.DamageType) : 0;
            float radius = ability.Kind == AbilityKind.Throw ? Mathf.Max(0.05f, ability.Radius * (ability.Has(AbilityFlags.Flask) ? 1f + Perk(PerkEffect.WideSpread) : 1f)) : ability.Radius;
            Vector3 origin = combat.Body.EyePosition + combat.Body.AimDirection * 0.4f;
            Quaternion aim = combat.Body.AimRotation;

            for (int i = 0; i < ability.ProjectileCount; i++)
            {
                float spread = ability.ProjectileCount > 1 ? Mathf.Lerp(-ability.ProjectileSpread, ability.ProjectileSpread, i / (ability.ProjectileCount - 1f)) : 0f;
                Vector3 direction = aim * Quaternion.Euler(0f, spread, 0f) * Vector3.forward;
                combat.Projectiles.Fire(origin, direction * ability.ProjectileSpeed, ability.ProjectileGravity, damage, ability.StaggerDuration,
                    ability.DamageType, ability.ProjectileKind, radius, (byte)ability.Effect, ability.EffectMagnitude, ability.EffectDuration, ability.LifeSteal,
                    maxDistance: ability.Kind == AbilityKind.Throw ? 0f : ability.Range, ability: (byte)index);
            }
        }

        /// The adventurer under the crosshair, or the caster when the aim misses.
        private AdventurerComponent FindAlly(AbilityConfig ability)
        {
            DamageReceiverComponent receiver = AimReceiver(ability.Range, out _);

            return receiver != null && receiver.TryGetComponent(out AdventurerComponent ally) && ally.State == AdventurerState.Alive ? ally : this;
        }

        /// First body on the crosshair ray, null when a wall or nothing comes first; the point is where the ray stops.
        private DamageReceiverComponent AimReceiver(float range, out Vector3 point)
        {
            FighterBodyComponent body = _fighter.Body;
            Vector3 origin = body.EyePosition;
            point = origin + body.AimDirection * range;
            Runner.RaycastAllSorted(origin, point, Object.InputAuthority, s_hits, _fighter.Combat.Projectiles.HitMask, HitOptions.IncludePhysX | HitOptions.SubtickAccuracy);

            foreach (LagCompensatedHit hit in s_hits)
            {
                if (hit.Hitbox != null && (hit.Hitbox.Root == _fighter.Receiver.HitboxRoot || !ZoneHitbox.IsInsideShape(Runner, hit, Object.InputAuthority)))
                    continue;

                point = hit.Point;

                return hit.Hitbox != null && hit.Hitbox.Root.TryGetComponent(out DamageReceiverComponent receiver) ? receiver : null;
            }

            return null;
        }

        /// The bolt leaves the casting hand, a little below and in front of the eyes.
        private Vector3 HandPoint => _fighter.Body.EyePosition + _fighter.Body.AimDirection * 0.5f - Vector3.up * 0.25f;

        private static Vector3 BodyPoint(DamageReceiverComponent receiver)
        {
            return receiver.transform.position + Vector3.up * 1.2f;
        }

        private void ChainLightning(AbilityConfig ability, CombatComponent combat)
        {
            DamageReceiverComponent target = AimReceiver(ability.Range, out Vector3 point);
            float damage = combat.ScaleDamage(Mathf.RoundToInt(Power(ability)), ability.DamageType);
            Vector3 from = HandPoint;
            RpcBolt(from, target != null ? BodyPoint(target) : point, false);
            s_struck.Clear();

            int jumps = ability.ProjectileCount + (ability.ProjectileCount > 0 ? Mathf.RoundToInt(Perk(PerkEffect.LongChain)) : 0);

            for (int jump = 0; target != null && jump <= jumps; jump++)
            {
                if (target.CanBeHitBy(_fighter.Receiver))
                    SpellHit(target, Mathf.RoundToInt(damage), ability, from);

                s_struck.Add(target);
                from = BodyPoint(target);
                target = FindChainTarget(from, ability.Radius);
                damage *= ChainFalloff;

                if (target != null)
                    RpcBolt(from, BodyPoint(target), false);
            }
        }

        private DamageReceiverComponent FindChainTarget(Vector3 from, float radius)
        {
            DamageReceiverComponent best = null;
            float bestDistance = radius * radius;

            foreach (DamageReceiverComponent receiver in DamageReceiverComponent.All)
            {
                float distance = (BodyPoint(receiver) - from).sqrMagnitude;

                if (receiver == _fighter.Receiver || s_struck.Contains(receiver) || !receiver.CanBeHitBy(_fighter.Receiver) || distance > bestDistance)
                    continue;

                best = receiver;
                bestDistance = distance;
            }

            return best;
        }

        /// Lightning from the sky on the marked spot: every body within the radius is struck.
        private void LightningStrike(AbilityConfig ability, CombatComponent combat)
        {
            Vector3 ground = GetGroundPoint(ability.Range);
            Vector3 sky = ground + Vector3.up * StrikeHeight;
            RpcBolt(HandPoint, sky, false);
            RpcBolt(sky, ground, true);
            GatherBodies(ground, ability.Radius);
            int damage = combat.ScaleDamage(Mathf.RoundToInt(Power(ability)), ability.DamageType);

            foreach (DamageReceiverComponent receiver in s_struck)
                SpellHit(receiver, damage, ability, sky);
        }

        /// Every body in reach of the point except the caster's own, training dummies too; gathered first because a hit may take a body off the list.
        private void GatherBodies(Vector3 center, float radius)
        {
            s_struck.Clear();

            foreach (DamageReceiverComponent receiver in DamageReceiverComponent.All)
            {
                if (receiver != _fighter.Receiver && receiver.CanBeHitBy(_fighter.Receiver) && (BodyPoint(receiver) - center).sqrMagnitude <= radius * radius)
                    s_struck.Add(receiver);
            }
        }

        private void SpellHit(DamageReceiverComponent target, int damage, AbilityConfig ability, Vector3 from)
        {
            Vector3 point = BodyPoint(target);
            target.ApplyHit(new HitRequest
            {
                BaseDamage = damage,
                BodyRays = 1,
                Zone = HitZone.Torso,
                Point = point,
                Normal = (from - point).normalized,
                AttackerPosition = from,
                StaggerDuration = ability.StaggerDuration,
                DamageType = ability.DamageType,
                Attacker = _fighter.Receiver,
                Tag = TagOf(ability.Flags)
            });

            if (ability.Effect != StatusEffectKind.None && target.IsAlive && target.TryGetComponent(out StatusEffectComponent effects))
                ApplyEffectTo(effects, ability.Effect, ability.EffectMagnitude, ability.EffectDuration, ability.Flags);
        }

        /// Along the view (way 1) or away from it (way -1), flattened: Magnitude metres in Duration seconds.
        private void Blink(AbilityConfig ability, float way = 1f)
        {
            Vector3 direction = Vector3.ProjectOnPlane(_fighter.Body.AimDirection, Vector3.up);

            if (direction.sqrMagnitude < 0.01f)
                direction = transform.forward;

            _fighter.Move.Dash(direction.normalized * (way * ability.Magnitude / Mathf.Max(0.05f, ability.Duration)), ability.Duration);

            if (ability.Effect != StatusEffectKind.None)
                _effects.Add(ability.Effect, ability.EffectMagnitude + _spentStacks * ability.StackBonus, ability.EffectDuration);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcBolt(Vector3 from, Vector3 to, NetworkBool isStrike)
        {
            LightningBoltEffect.Play(from, to, isStrike);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcFlash(Vector3 position, Color color, float range)
        {
            LightFlashEffect.Play(position, color, range);
        }

        /// Everyone in reach, or in the cone in front of the user; a push throws the struck away from the user and a hit effect stays on them.
        private void AreaDamage(AbilityConfig ability, CombatComponent combat, Vector3 origin)
        {
            int damage = combat.ScaleDamage(Mathf.RoundToInt(Power(ability)), ability.DamageType);
            Vector3 center = origin + Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(_fighter.Body.AimDirection, Vector3.up).normalized;
            RpcFlash(ability.ConeAngle > 0f ? center + forward * ability.Radius * 0.5f : center, ability.Color, ability.Radius * 2f);
            GatherBodies(center, ability.Radius);

            if (ability.ConeAngle > 0f)
                s_struck.RemoveAll(receiver => Vector3.Angle(forward, Vector3.ProjectOnPlane(BodyPoint(receiver) - center, Vector3.up)) > ability.ConeAngle);

            if (ability.Has(AbilityFlags.UndeadOnly))
                s_struck.RemoveAll(receiver => !IsUndead(receiver));

            foreach (DamageReceiverComponent receiver in s_struck)
            {
                Vector3 point = BodyPoint(receiver);
                receiver.ApplyHit(new HitRequest
                {
                    BaseDamage = damage,
                    BodyRays = 1,
                    Zone = HitZone.Torso,
                    Point = point,
                    Normal = (point - center).normalized,
                    AttackerPosition = center,
                    StaggerDuration = Mathf.Max(0.25f, ability.StaggerDuration),
                    DamageType = ability.DamageType,
                    Attacker = _fighter.Receiver,
                    Tag = ability.IsSpell ? TagOf(ability.Flags) : DamageTag.Melee | TagOf(ability.Flags) & ~DamageTag.Spell
                });

                if (ability.Effect != StatusEffectKind.None && receiver.IsAlive && receiver.TryGetComponent(out StatusEffectComponent effects))
                    ApplyEffectTo(effects, ability.Effect, ability.EffectMagnitude, ability.EffectDuration, ability.Flags);

                Push(receiver, center, ability.Push);
            }
        }

        /// Throws a body away from a point, unless heavy bones keep it standing.
        private static void Push(DamageReceiverComponent receiver, Vector3 center, float push)
        {
            if (push <= 0f || !receiver.IsAlive || !receiver.TryGetComponent(out FighterComponent fighter)
                || (receiver.TryGetComponent(out AdventurerComponent adventurer) && adventurer.Perk(PerkEffect.KnockbackImmune) > 0f))
                return;

            fighter.Move.AddImpulse(Vector3.ProjectOnPlane(receiver.transform.position - center, Vector3.up).normalized * push + Vector3.up * 1.5f);
        }

        private static bool IsUndead(DamageReceiverComponent receiver)
        {
            return receiver.TryGetComponent(out MonsterComponent monster) && monster.Config.IsUndead;
        }

        /// A projectile stopped somewhere: a thrown flask leaves its zone, a flare arrow lights the place.
        private void OnProjectileImpact(ProjectileData data, Vector3 point, Vector3 normal)
        {
            DropThrown(data, point);

            if ((data.Flags & ProjectileFlags.Flare) != 0 && _zonePrefab != null)
                Runner.Spawn(_zonePrefab, point + normal * 0.1f, Quaternion.identity, PlayerRef.None, (_, zone) => zone.GetComponent<AbilityZoneComponent>().SetupFlare(this, new Color(1f, 0.6f, 0.25f)));

            if (data.Ability >= _abilities.Count)
                return;

            AbilityConfig ability = _abilities[data.Ability];
            RpcImpact(data.Ability, point, false);

            if (ability.Zone != ZoneKind.None)
                SpawnZone(ability, point);
        }

        /// Arrows, bolts and thrown weapons that land on a body earn ranged stacks; a headshot earns one more and the headshot bonus.
        private void OnProjectileHit(in ProjectileData data, DamageReceiverComponent receiver, HitZone zone, HitResult result)
        {
            if (data.KindValue is ProjectileKind.Arrow or ProjectileKind.Thrown && result == HitResult.Hit)
            {
                GainResource(ResourceSource.RangedHit);

                if (zone == HitZone.Head)
                {
                    GainResource(ResourceSource.Headshot);

                    if (_stats.HeadshotBonus > 0f)
                        BonusHit(receiver, data.Damage * _stats.HeadshotBonus, DamageType.Physical, 0f);
                }
            }

            if (data.LifeSteal > 0f)
                _fighter.Health.Restore(Mathf.RoundToInt(data.Damage * data.LifeSteal));

            AbilityConfig ability = data.Ability < _abilities.Count ? _abilities[data.Ability] : null;

            if (ability != null)
                Push(receiver, data.HitPoint, ability.Push);

            if (data.Effect == 0 || !receiver.IsAlive || !receiver.TryGetComponent(out StatusEffectComponent effects))
                return;

            ApplyEffectTo(effects, (StatusEffectKind)data.Effect, data.EffectMagnitude, data.EffectDuration, ability != null ? ability.Flags : AbilityFlags.None);
        }

        /// Rupture-style buffs: every melee hit while active opens a bleed on the victim. Weapon enchants add magical damage to
        /// every weapon hit and burn or chill the victim; spells and the enchant's own damage do not trigger them. Hits earn
        /// stacks of the subclass resource; empowered strikes go off on the next weapon hit.
        private void OnHitDealt(DamageReceiverComponent victim, HitResult result, int damage, HitRequest request)
        {
            if (!HasStateAuthority || _isBonusHit)
                return;

            bool isMelee = (request.Tag & DamageTag.Melee) != 0 && (request.Tag & DamageTag.Spell) == 0;
            victim.TryGetComponent(out StatusEffectComponent victimEffects);

            if (isMelee)
                OnWeaponHit(victim, result, damage);
            else if (result == HitResult.Hit && damage > 0)
                GainResource(ResourceSource.SpellHit);

            if (result == HitResult.Hit && damage > 0)
                OnDamageDealt(victim, request, victimEffects, isMelee);
        }

        private void OnWeaponHit(DamageReceiverComponent victim, HitResult result, int damage)
        {
            float sunder = _effects.GetMagnitude(StatusEffectKind.Sunder);

            if (sunder > 0f)
            {
                _effects.Remove(StatusEffectKind.Sunder);
                BonusHit(victim, _fighter.Combat.ScaleDamage(Mathf.RoundToInt(sunder), DamageType.Physical), DamageType.Physical, SunderStagger);
            }

            if (result != HitResult.Hit)
            {
                GainResource(ResourceSource.HitOnBlock);

                return;
            }

            GainResource(ResourceSource.WeaponHit);

            if (_fighter.Combat.IsRiposte)
                GainResource(ResourceSource.Riposte);

            float empower = _effects.GetMagnitude(StatusEffectKind.Empower);
            float verdict = _effects.GetMagnitude(StatusEffectKind.Verdict);
            float siphon = _effects.GetMagnitude(StatusEffectKind.Siphon);

            float rupture = _effects.GetMagnitude(StatusEffectKind.Rupture);
            float fire = _effects.GetMagnitude(StatusEffectKind.FireWeapon);
            float frost = _effects.GetMagnitude(StatusEffectKind.FrostWeapon);
            float holy = _effects.GetMagnitude(StatusEffectKind.HolyWeapon);
            victim.TryGetComponent(out StatusEffectComponent effects);

            if (rupture > 0f && effects != null)
                effects.Add(StatusEffectKind.Burn, rupture, 5f);

            if (fire > 0f)
                Enchant(victim, fire, effects, StatusEffectKind.Burn, fire, EnchantBurnTime);

            if (frost > 0f)
                Enchant(victim, frost, effects, StatusEffectKind.Slow, EnchantSlow, EnchantSlowTime);


            if (holy > 0f)
                Enchant(victim, IsUndead(victim) ? holy * 2f : holy, effects, StatusEffectKind.None, 0f, 0f);

            if (siphon > 0f)
                _fighter.Health.Restore(Mathf.Max(1, Mathf.RoundToInt(damage * siphon / 100f)));

            if (empower > 0f)
            {
                _effects.Remove(StatusEffectKind.Empower);
                BonusHit(victim, _fighter.Combat.ScaleDamage(Mathf.RoundToInt(empower), DamageType.Physical), DamageType.Physical, 0f);
            }

            if (verdict > 0f)
            {
                _effects.Remove(StatusEffectKind.Verdict);
                BonusHit(victim, _fighter.Combat.ScaleDamage(Mathf.RoundToInt(verdict), DamageType.Physical), DamageType.Physical, 0f);

                if (effects != null && victim.IsAlive)
                    effects.Add(StatusEffectKind.Silence, 1f, VerdictSilence);
            }
        }

        private void Enchant(DamageReceiverComponent victim, float damage, StatusEffectComponent effects, StatusEffectKind effect, float magnitude, float duration)
        {
            BonusHit(victim, damage, DamageType.Magical, 0f);

            if (effect != StatusEffectKind.None && effects != null && victim.IsAlive)
                ApplyEffectTo(effects, effect, magnitude, duration);
        }

        /// Extra damage, already scaled, on top of a weapon hit; it lands on the body even when the weapon struck a block.
        private void BonusHit(DamageReceiverComponent victim, float damage, DamageType type, float stagger)
        {
            if (!victim.IsAlive)
                return;

            Vector3 point = BodyPoint(victim);
            _isBonusHit = true;
            victim.ApplyHit(new HitRequest
            {
                BaseDamage = Mathf.RoundToInt(damage),
                BodyRays = 1,
                Zone = HitZone.Torso,
                Point = point,
                Normal = (transform.position - victim.transform.position).normalized,
                AttackerPosition = transform.position,
                StaggerDuration = stagger,
                DamageType = type,
                Attacker = _fighter.Receiver
            });
            _isBonusHit = false;
        }

        private void ApplyConsumable(EquipSlot slot)
        {
            ItemStack stack = _inventory.GetEquipped(slot);
            ConsumableItemConfig item = _database.Get<ConsumableItemConfig>(stack.ItemId);

            if (item == null)
                return;

            float tier = Mathf.Max(0, stack.Rarity - (int)ItemRarity.Common);
            // Potion lore stretches what a drink does; the heal of a healing potion keeps its total.
            float lore = 1f + Perk(PerkEffect.PotionDuration);

            switch (item.Effect)
            {
                case ConsumableEffect.HealInstant:
                    HealSelf(Mathf.RoundToInt((item.Magnitude + tier * 4f) * _stats.PhysicalHealing * (1f + Perk(PerkEffect.BandageHealing))));
                    break;
                case ConsumableEffect.HealOverTime:
                    float heal = item.Magnitude * _stats.MagicalHealing;
                    float duration = Mathf.Max(1f, item.Duration - tier * 2.5f) * lore;
                    _effects.Add(StatusEffectKind.HealOverTime, heal, duration);
                    ShareHealingPotion(heal, duration);

                    if (Perk(PerkEffect.PureBlood) > 0f)
                        _effects.Remove(StatusEffectKind.Poison);
                    break;
                case ConsumableEffect.Protection:
                    _effects.Add(StatusEffectKind.Protection, item.Magnitude + tier * 5f, item.Duration * lore);
                    break;
                case ConsumableEffect.Haste:
                    _effects.Add(StatusEffectKind.Haste, item.Magnitude, item.Duration * lore);
                    break;
                case ConsumableEffect.Invisibility:
                    _effects.Add(StatusEffectKind.Invisible, 1f, (item.Duration + tier * 2f) * lore);
                    break;
            }

            Consume(slot, stack);
        }

        private void HealSelf(int heal)
        {
            _fighter.Health.Restore(_effects.ScaleHealing(heal));

            if (Perk(PerkEffect.PureBlood) > 0f)
                _effects.Remove(StatusEffectKind.Poison);
        }

        /// Generous flask: a healing potion drunk heals the allies around for a share of it.
        private void ShareHealingPotion(float heal, float duration)
        {
            float share = Perk(PerkEffect.GenerousFlask);

            if (share <= 0f)
                return;

            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if ((fighter.transform.position - transform.position).sqrMagnitude > GenerousRadius * GenerousRadius || !fighter.TryGetComponent(out AdventurerComponent ally)
                    || ally == this || !IsAllyOf(ally) || ally.State != AdventurerState.Alive)
                    continue;

                HealTarget(ally, heal * share, false, true, duration);
            }

            RpcBurst(VfxStyle.Heal, transform.position + Vector3.up, GenerousRadius, new Color(0.55f, 0.95f, 0.55f));
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
                        Runner.Spawn(_campfirePrefab, FindGround(transform.position + transform.forward * 1.2f), Quaternion.identity);
                    break;
                default:
                    return;
            }

            Consume(slot, stack);
        }

        /// A knife or an axe from the belt, as hard as it was drawn; a knife fan buff turns the next knife into a fan of them, only
        /// one spent. The one paid for stops where it lands, to be picked up again; the fan's extra ones are conjured.
        private void ThrowWeapon(UtilityItemConfig item, ItemStack stack)
        {
            CombatComponent combat = _fighter.Combat;
            float bonus = (1f + Perk(PerkEffect.TrueThrow) + (item.IsBlade ? Perk(PerkEffect.ShortBlade) : 0f)) * Mathf.Lerp(LobDamage, 1f, _throwPower);
            int damage = Mathf.RoundToInt(combat.ScaleDamage(item.Damage, DamageType.Physical) * _stats.RangedDamageMultiplier * bonus);
            int count = item.IsBlade && Take(StatusEffectKind.KnifeFan, out float fan) ? Mathf.RoundToInt(fan) : 1;
            Vector3 velocity = ThrowVelocity(ThrowSpeed);

            for (int i = 0; i < count; i++)
            {
                float yaw = count > 1 ? Mathf.Lerp(-FanSpread, FanSpread, i / (count - 1f)) : 0f;
                combat.Projectiles.Fire(combat.Body.EyePosition, Quaternion.Euler(0f, yaw, 0f) * velocity, -9.81f, damage, 0.2f, DamageType.Physical,
                    ProjectileKind.Thrown, item: i == count / 2 ? stack.ItemId : (short)0, itemRarity: stack.Rarity);
            }
        }

        /// A thrown weapon stops in the surface it hit, along its flight; one that struck a body drops to the floor below.
        private void DropThrown(in ProjectileData data, Vector3 point)
        {
            if (data.Item <= 0 || _worldItemPrefab == null || _database.Get(data.Item) is not { } config)
                return;

            ItemStack stack = ItemStack.Create(config, 1, (ItemRarity)data.ItemRarity);
            Vector3 direction = data.GetVelocity(Runner.SecondsSince(data.FireTick)).normalized;

            if ((data.Flags & ProjectileFlags.HitBody) == 0)
            {
                Runner.Spawn(_worldItemPrefab, point + direction * StickDepth, WorldItemComponent.StuckRotation(direction), PlayerRef.None,
                    (_, item) => item.GetComponent<WorldItemComponent>().Setup(stack));

                return;
            }

            Runner.Spawn(_worldItemPrefab, point - direction * 0.2f, Quaternion.identity, PlayerRef.None, (_, item) =>
            {
                WorldItemComponent dropped = item.GetComponent<WorldItemComponent>();
                dropped.Setup(stack);
                dropped.Launch(Vector3.up - direction);
            });
        }

        /// The floor under a point (a campfire is set on it), or the point itself over a drop.
        private static Vector3 FindGround(Vector3 point)
        {
            return Physics.Raycast(point + Vector3.up, Vector3.down, out RaycastHit hit, GroundProbe, WorldMask, QueryTriggerInteraction.Ignore) ? hit.point : point;
        }

        private void Consume(EquipSlot slot, ItemStack stack)
        {
            _inventory.SetEquipment(slot, stack.Count > 1 ? stack.WithCount(stack.Count - 1) : default);
        }

        /// Seated, health comes back a point at a time and every spent spell fills toward its next charge, much faster by a lit fire.
        private void SimulateRest()
        {
            float pace = IsByFire ? FireRestBoost : 1f;

            if (!IsResting)
            {
                _restTimer = TickTimer.None;

                return;
            }

            SimulateSpellRecovery(pace / FireRestBoost);

            if (!_restTimer.IsRunning)
                _restTimer = TickTimer.CreateFromSeconds(Runner, RestHealInterval() / pace);

            if (_restTimer.Expired(Runner))
            {
                _fighter.Health.Restore(1);
                _restTimer = TickTimer.CreateFromSeconds(Runner, RestHealInterval() / pace);
            }
        }

        /// Every spell short of charges fills at once, a heavy one (few charges) slower; Cooldown Recovery sets the pace. The free
        /// cast of Knowledge 30 returns with the last charge.
        private void SimulateSpellRecovery(float pace)
        {
            bool isSpent = false;

            for (int i = _skillCount; i < _abilities.Count; i++)
            {
                if (_abilities[i].IsCooldownBased || _charges[i] >= GetMaxCharges(i))
                {
                    _recovery.Set(i, 0f);

                    continue;
                }

                float recovery = _recovery[i] + Runner.DeltaTime * pace * _stats.CooldownSpeed / (_spellRecoverInterval * SpellWeight(_abilities[i]));

                if (recovery >= 1f)
                {
                    _charges.Set(i, (byte)(_charges[i] + 1));
                    recovery = 0f;
                }

                _recovery.Set(i, recovery);
                isSpent |= _charges[i] < GetMaxCharges(i);
            }

            _isAttuned |= !isSpent;
        }

        /// A spell of six charges comes back three times faster than one of a single charge.
        private static float SpellWeight(AbilityConfig spell)
        {
            return Mathf.Lerp(0.6f, 2f, Mathf.InverseLerp(6f, 1f, spell.Charges));
        }

        /// Charges of the ability with the share of the next one regained by resting, over its maximum: the bar under a wheel icon.
        public float GetChargeFill(int ability)
        {
            int max = GetMaxCharges(ability);

            return max > 0 ? Mathf.Clamp01((_charges[ability] + _recovery[ability]) / max) : 1f;
        }

        /// Vitality 30 rests twice as fast.
        private float RestHealInterval()
        {
            return _restHealInterval / _stats.PhysicalHealing / (_stats.HasThreshold(StatType.Vitality) ? 2f : 1f);
        }

        /// Protection ends with the first move or button press of the player, or after DungeonConfig.LoadInProtection at the latest.
        private void SimulateLoadingIn()
        {
            if (!_loadingTimer.IsRunning)
                _loadingTimer = TickTimer.CreateFromSeconds(Runner, _config.LoadInProtection);

            if (_loadingTimer.Expired(Runner) || GetInput(out PlayerInputData input) && (input.MoveDirection != Vector2.zero || input.Buttons.Bits != 0))
                IsLoadingIn = false;
        }

        /// A drop deeper than the threshold hurts by every metre beyond it; one into a chasm kills.
        private void SimulateFall()
        {
            float fall = _fighter.Move.TakeFall();
            float excess = fall - _config.FallDamageThreshold;

            if (excess <= 0f)
                return;

            _fighter.Receiver.ApplyHit(new HitRequest
            {
                BaseDamage = fall >= _config.LethalFall ? LethalDamage : Mathf.RoundToInt(excess * _config.FallDamagePerMeter),
                BodyRays = 1,
                Zone = HitZone.Legs,
                Point = transform.position,
                Normal = Vector3.up,
                AttackerPosition = transform.position + Vector3.down,
                DamageType = DamageType.True
            });
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
                bool isBody = container.GetComponent<CorpseComponent>() != null || container.GetComponent<MonsterComponent>() != null;
                float knack = 1f + Perk(PerkEffect.VeinExpert) + (isBody ? Perk(PerkEffect.GraveSense) : 0f);
                float duration = DungeonFormulas.SearchTime(container.Inventory.Bag[index].RarityValue) / (_stats.Perception * knack);
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

        private void RestoreArrival()
        {
            FloorTransfer.Carry carry = _arrival;
            _arrival = null;
            _fighter.Health.CurrentHealth = Mathf.Clamp(carry.Health, 1, _fighter.Health.MaxHealth);

            for (int i = 0; i < _abilities.Count && i < carry.Charges.Length; i++)
                _charges.Set(i, (byte)Mathf.Min(carry.Charges[i], GetMaxCharges(i)));
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

            // The ragdoll is pushed only by the blow that killed, not by an earlier one before bleeding out.
            DamageReceiverComponent receiver = _fighter.Receiver;
            bool isStruck = (Runner.Tick - receiver.LastHitTick) * Runner.DeltaTime <= KillingBlowTime;

            Runner.Spawn(_corpsePrefab, position, rotation, PlayerRef.None, (_, corpse) =>
            {
                CorpseComponent body = corpse.GetComponent<CorpseComponent>();
                body.Setup(_inventory, _class, _session != null ? _session.DisplayName : "Adventurer", Object.Id, _fighter.Move.Velocity,
                    receiver.LastHitPoint, isStruck ? receiver.LastHitPush : Vector3.zero);
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

            InteractableComponent target = scene.Raycast(origin, direction, out RaycastHit hit, InteractRange, _interactMask, QueryTriggerInteraction.Collide)
                ? hit.collider.GetComponentInParent<InteractableComponent>()
                : null;

            // Bodies come to rest differently on every client: for another player's look the server takes any body by the line of sight.
            if (target != null || HasInputAuthority)
                return target;

            CorpseComponent corpse = CorpseComponent.FindAlong(origin, direction, InteractRange);

            return corpse != null ? corpse.Container : null;
        }

        bool InventoryActionsComponent.IOwner.CanEquip(ItemConfig item, EquipSlot slot)
        {
            return item switch
            {
                WeaponItemConfig weapon => weapon.Fits(_class) || Perk(PerkEffect.AnyWeapon) > 0f,
                ArmorItemConfig armor => armor.Fits(_class),
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

        void InventoryActionsComponent.IOwner.OnUseItem(InventoryComponent source, int bagIndex)
        {
            if (State != AdventurerState.Alive)
                return;

            // Loot is never used straight from a bag: it lands on the belt (from a container: or in the bag) and is used from the hand.
            ItemStack stack = source.Bag[bagIndex];
            int count = stack.Count;
            _ = source == _inventory ? _inventory.TryAddToBelt(ref stack) : _inventory.TryLoot(ref stack);

            if (stack.Count < count)
                source.RemoveAt(bagIndex, count - stack.Count);
        }

        private void OnUseItem(EquipSlot slot)
        {
            if (State != AdventurerState.Alive || Pending != PendingAction.None)
                return;

            ItemStack equipped = _inventory.GetEquipped(slot);
            ItemConfig item = _database.Get(equipped.ItemId);
            float useTime;
            float animSpeed = 1f;
            PendingAction action;
            byte kind = BusyUse;

            switch (item)
            {
                // A torch is thrown with RMB (TryAimTorch); one dropped from the inventory lies unlit.
                case ConsumableItemConfig { IsLight: true }:
                    return;
                // Potions are drunk at Action Speed, bandages are wound at Handling Speed.
                case ConsumableItemConfig consumable when consumable.Effect == ConsumableEffect.HealInstant:
                    useTime = consumable.UseTime / _stats.HandlingSpeed / UseSpeed;
                    action = PendingAction.Consumable;
                    kind = BusyBandage;
                    break;
                case ConsumableItemConfig consumable:
                    animSpeed = _stats.ActionSpeed * UseSpeed;
                    useTime = consumable.UseTime / animSpeed;
                    action = PendingAction.Consumable;
                    break;
                // Throwing weapons are drawn and thrown in SimulateBelt.
                case UtilityItemConfig utility when utility.UtilityKind is not (UtilityKind.Lockpick or UtilityKind.ThrowingWeapon):
                    useTime = utility.UseTime / _stats.HandlingSpeed;
                    action = PendingAction.Utility;
                    kind = BusyInteract;
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

        /// Calm hands of a physician nearby.
        private float UseSpeed => 1f + _effects.GetMagnitude(StatusEffectKind.UseSpeed) / 100f;

        void InventoryActionsComponent.IOwner.OnDropItem(ItemStack stack)
        {
            DropItem(stack);
        }

        private void DropItem(ItemStack stack)
        {
            if (_worldItemPrefab == null)
                return;

            Vector3 position = transform.position + transform.forward * 0.8f + Vector3.up * 0.3f;

            // Down onto the floor, so a dropped item lies on it instead of hanging in the air.
            if (Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position = hit.point;

            Runner.Spawn(_worldItemPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), PlayerRef.None,
                (_, item) => item.GetComponent<WorldItemComponent>().Setup(stack));
        }

        void InventoryActionsComponent.IOwner.OnLoadChunk(byte kind, byte chunk, byte chunkCount, byte[] data)
        {
        }


        /// Plate on the struck part rings like armor; anything softer is a body hit.
        ImpactSurface HitFeedbackComponent.IBodySource.GetBodySurface(HitZone zone)
        {
            EquipSlot slot = zone switch
            {
                HitZone.Head => EquipSlot.Head,
                HitZone.Legs => EquipSlot.Legs,
                _ => EquipSlot.Chest
            };

            return _inventory.GetEquippedConfig<ArmorItemConfig>(slot) is { ArmorType: ArmorType.Plate } ? ImpactSurface.Plate : ImpactSurface.None;
        }

    }
}
