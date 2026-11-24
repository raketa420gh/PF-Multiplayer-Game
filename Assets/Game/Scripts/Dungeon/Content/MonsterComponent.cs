using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A dungeon monster on top of the fighter stack: config-driven stats, experience reward and a lootable corpse.
    public sealed class MonsterComponent : NetworkBehaviour, ICombatStats, DamageReceiverComponent.IHitModifier
    {
        public MonsterConfig Config => _config;
        public FighterComponent Fighter => _fighter;
        public ContainerComponent Corpse => _corpse;

        [Networked]
        public byte Floor { get; private set; }

        [Networked]
        public NetworkBool IsRewarded { get; private set; }

        /// The brain has a target: clients voice the alert on its rise and keep the idle mutter for calm times.
        [Networked]
        public NetworkBool IsAlerted { get; private set; }

        /// Lies folded on the floor, out of the fight, until an adventurer comes near or something hurts it.
        [Networked]
        public NetworkBool IsDormant { get; private set; }

        /// 0 while it lies folded, 0..1 while it gets up, 1 once it stands.
        public float Rise => IsDormant ? 0f : 1f - (_riseTimer.RemainingTime(Runner) ?? 0f) / RiseTime;
        public bool IsRisen => !IsDormant && _riseTimer.ExpiredOrNotRunning(Runner);

        /// Seconds a woken monster takes to get up; it can be hit meanwhile, but neither moves nor strikes.
        public const float RiseTime = 2f;

        [SerializeField]
        private MonsterConfig _config;

        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField]
        private ContainerComponent _corpse;

        [SerializeField]
        private int _team = 2;

        private const int MaxSlowStacks = 5;
        private const float SlowDuration = 7f;

        [SerializeField]
        private StatusEffectComponent _effects;

        [SerializeField, Tooltip("Seconds the body stays when it carries loot / when it is empty")]
        private Vector2 _corpseLifetime = new(300f, 30f);

        [Networked]
        private TickTimer _despawnTimer { get; set; }

        [Networked]
        private TickTimer _riseTimer { get; set; }

        public float ActionSpeed => _config.ActionSpeed;
        public float HandlingSpeed => 1f;
        public StatusEffectComponent Effects => _effects;
        /// Monsters plant their feet while swinging so players can read and dodge the attack, as in Dark and Darker. Slows, curses and roots hold them too.
        public float MoveSpeedMultiplier => (_fighter.Combat.State == CombatState.Attack ? AttackMoveMultiplier : _config.MoveSpeed / DungeonFormulas.BaseMoveSpeed) * EffectMoveScale;

        private float EffectMoveScale => _effects == null ? 1f : _effects.Has(StatusEffectKind.Root) ? 0f
            : Mathf.Max(0.2f, 1f - (_effects.GetMagnitude(StatusEffectKind.Slow) + _effects.GetMagnitude(StatusEffectKind.Cursed) * 3f) / 100f);
        public bool IsBoss => _config.IsBoss;
        public static System.Collections.Generic.IReadOnlyList<MonsterComponent> All => s_all;

        private static readonly System.Collections.Generic.List<MonsterComponent> s_all = new();

        /// A lunging boss keeps momentum through the windup, a charger rams through the active phase; everyone else stands still.
        private float AttackMoveMultiplier => _fighter.Combat.Phase switch
        {
            AttackPhase.Windup when _config.LungeImpulse > 0f => 1f,
            AttackPhase.Active when _config.IsCharger => _config.ChargeSpeed / DungeonFormulas.BaseMoveSpeed,
            _ => 0f
        };

        public override void Spawned()
        {
            s_all.Add(this);
            _fighter.SetStats(this);
            _fighter.Receiver.SetModifier(this);
            _fighter.Receiver.SetTeam(_team);

            if (!HasStateAuthority)
                return;

            if (_config.HitSlow > 0f)
                _fighter.Receiver.OnHitDealt += OnHitDealt;

            _fighter.Health.SetMaxHealth(_config.MaxHealth, true);
            _fighter.Combat.SetSlotWeapon(0, _config.WeaponIndex);
            _fighter.Combat.SetInitialSlot(0);

            // A heap of bones on the floor is nothing to strike at.
            IsDormant = _config.IsDormant;
            _fighter.Receiver.HitboxRoot.HitboxRootActive = !IsDormant;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            s_all.Remove(this);
            _fighter.Receiver.OnHitDealt -= OnHitDealt;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (_fighter.Health.IsDead && !IsRewarded)
                Reward();

            // A spell or a trap that reaches the heap wakes it.
            if (IsDormant && _fighter.Health.IsAlive && _fighter.Health.CurrentHealth < _fighter.Health.MaxHealth)
                Wake();

            if (_despawnTimer.Expired(Runner))
                Runner.Despawn(Object);
        }

        public void Setup(byte floor)
        {
            Floor = floor;
        }

        public void SetAlerted(bool isAlerted) => IsAlerted = isAlerted;

        /// The folded monster starts getting up. State authority only.
        public void Wake()
        {
            if (!IsDormant)
                return;

            IsDormant = false;
            _riseTimer = TickTimer.CreateFromSeconds(Runner, RiseTime);
            _fighter.Receiver.HitboxRoot.HitboxRootActive = true;
        }

        public float GetDamageMultiplier(DamageType type)
        {
            return _config.DamageMultiplier * StatusDamage.Dealt(_effects, true, type == DamageType.Physical);
        }




        void DamageReceiverComponent.IHitModifier.ModifyOutgoingHit(DamageReceiverComponent victim, ref HitRequest request)
        {
        }

        int DamageReceiverComponent.IHitModifier.ModifyIncomingDamage(int damage, in HitRequest request)
        {
            float rot = _effects == null ? 0f : _effects.GetMagnitude(StatusEffectKind.Rotting) / 100f;
            float reduction = request.IgnoreArmor ? 0f : request.DamageType == DamageType.Physical ? _config.ArmorReduction - rot : request.DamageType == DamageType.Magical ? _config.MagicReduction - rot : 0f;

            return Mathf.RoundToInt(damage * (1f - reduction) * StatusDamage.Taken(_effects));
        }

        /// The loot stays on the body: the corpse is a container the adventurers search.
        private void Reward()
        {
            IsRewarded = true;

            DamageReceiverComponent killer = _fighter.Receiver.LastAttacker;

            if (killer != null && killer.TryGetComponent(out AdventurerComponent adventurer))
            {
                adventurer.AddExperience(_config.Experience);
                adventurer.AddKill();
            }

            // A monster without a corpse container (a spider) leaves nothing to search.
            if (_corpse != null)
                _corpse.Fill(_config.LootTable, Runner.Tick + (int)Object.Id.Raw * 7919);

            _despawnTimer = TickTimer.CreateFromSeconds(Runner, _corpse != null && _corpse.Inventory.CountItems() > 0 ? _corpseLifetime.x : _corpseLifetime.y);
        }

        /// Each landed blow adds a stack of slow and restarts it, up to MaxSlowStacks.
        private void OnHitDealt(DamageReceiverComponent victim, HitResult result, int damage, HitRequest request)
        {
            if (result != HitResult.Hit || !victim.TryGetComponent(out AdventurerComponent adventurer) || adventurer.Effects == null)
                return;

            float stacked = Mathf.Min(adventurer.Effects.GetMagnitude(StatusEffectKind.Slow) + _config.HitSlow, _config.HitSlow * MaxSlowStacks);
            adventurer.Effects.Add(StatusEffectKind.Slow, stacked, SlowDuration);
        }
    }
}
