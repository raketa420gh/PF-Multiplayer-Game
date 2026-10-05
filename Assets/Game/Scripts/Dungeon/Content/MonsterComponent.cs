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

        [SerializeField]
        private MonsterConfig _config;

        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField]
        private ContainerComponent _corpse;

        [SerializeField]
        private int _team = 2;

        [SerializeField, Tooltip("Seconds the body stays when it carries loot / when it is empty")]
        private Vector2 _corpseLifetime = new(300f, 30f);

        [Networked]
        private TickTimer _despawnTimer { get; set; }

        public float ActionSpeed => _config.ActionSpeed;
        public float HandlingSpeed => 1f;
        /// Monsters plant their feet while swinging so players can read and dodge the attack, as in Dark and Darker.
        public float MoveSpeedMultiplier => _fighter.Combat.State == CombatState.Attack ? AttackMoveMultiplier : _config.MoveSpeed / DungeonFormulas.BaseMoveSpeed;
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

            _fighter.Health.SetMaxHealth(_config.MaxHealth, true);
            _fighter.Combat.SetSlotWeapon(0, _config.WeaponIndex);
            _fighter.Combat.SetInitialSlot(0);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            s_all.Remove(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (_fighter.Health.IsDead && !IsRewarded)
                Reward();

            if (_despawnTimer.Expired(Runner))
                Runner.Despawn(Object);
        }

        public void Setup(byte floor)
        {
            Floor = floor;
        }

        public float GetDamageMultiplier(DamageType type)
        {
            return _config.DamageMultiplier;
        }




        int DamageReceiverComponent.IHitModifier.ModifyIncomingDamage(int damage, DamageType type, HitZone zone)
        {
            float reduction = type == DamageType.Physical ? _config.ArmorReduction : type == DamageType.Magical ? _config.MagicReduction : 0f;

            return Mathf.RoundToInt(damage * (1f - reduction));
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

            _corpse.Fill(_config.LootTable, Runner.Tick + (int)Object.Id.Raw * 7919);
            _despawnTimer = TickTimer.CreateFromSeconds(Runner, _corpse.Inventory.CountItems() > 0 ? _corpseLifetime.x : _corpseLifetime.y);
        }
    }
}
