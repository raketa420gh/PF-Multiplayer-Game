using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A dungeon monster on top of the fighter stack: config-driven stats, loot drop and experience reward.
    public sealed class MonsterComponent : NetworkBehaviour, ICombatStats, DamageReceiverComponent.IHitModifier
    {
        public MonsterConfig Config => _config;
        public FighterComponent Fighter => _fighter;

        [Networked]
        public byte Floor { get; private set; }

        [Networked]
        public NetworkBool IsRewarded { get; private set; }

        [SerializeField]
        private MonsterConfig _config;

        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField]
        private NetworkObject _worldItemPrefab;

        [SerializeField]
        private int _team = 2;

        [Networked]
        private TickTimer _despawnTimer { get; set; }

        public float ActionSpeed => _config.ActionSpeed;
        public float HandlingSpeed => 1f;
        /// Monsters plant their feet while swinging so players can read and dodge the attack, as in Dark and Darker.
        public float MoveSpeedMultiplier => _fighter.Combat.State == CombatState.Attack ? AttackMoveMultiplier : _config.MoveSpeed / DungeonFormulas.BaseMoveSpeed;
        public bool IsBoss => _config.IsBoss;
        public static System.Collections.Generic.IReadOnlyList<MonsterComponent> All => s_all;

        private static readonly System.Collections.Generic.List<MonsterComponent> s_all = new();

        /// A lunging boss keeps momentum through the windup; everyone else stands still.
        private float AttackMoveMultiplier => _config.LungeImpulse > 0f && _fighter.Combat.Phase == AttackPhase.Windup ? 1f : 0f;

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

        float DamageReceiverComponent.IHitModifier.WeakpointMultiplier => 1f;
        float DamageReceiverComponent.IHitModifier.ImpactMultiplier => 1f;

        float DamageReceiverComponent.IHitModifier.ModifyBlockMitigation(float mitigation) => mitigation;

        float DamageReceiverComponent.IHitModifier.ModifyIncomingStagger(int damage, float staggerDuration) => staggerDuration;

        int DamageReceiverComponent.IHitModifier.ModifyIncomingDamage(int damage, DamageType type, HitZone zone)
        {
            float reduction = type == DamageType.Physical ? _config.ArmorReduction : type == DamageType.Magical ? _config.MagicReduction : 0f;

            return Mathf.RoundToInt(damage * (1f - reduction));
        }

        private void Reward()
        {
            IsRewarded = true;
            _despawnTimer = TickTimer.CreateFromSeconds(Runner, 20f);

            DamageReceiverComponent killer = _fighter.Receiver.LastAttacker;

            if (killer != null && killer.TryGetComponent(out AdventurerComponent adventurer))
            {
                adventurer.AddExperience(_config.Experience);
                adventurer.AddKill();
            }

            DropLoot();
        }

        private void DropLoot()
        {
            if (_config.LootTable == null || _worldItemPrefab == null)
                return;

            System.Random random = new System.Random(Runner.Tick);
            LootEntry[] entries = _config.LootTable.Entries;

            if (entries.Length == 0 || random.NextDouble() > 0.6)
                return;

            LootEntry entry = entries[random.Next(entries.Length)];
            int count = random.Next(entry.MinCount, entry.MaxCount + 1);
            ItemStack stack = ItemStack.Create(entry.Item, count, entry.Item.CanRollRarity ? _config.LootTable.RollRarity(random) : entry.Item.BaseRarity);
            Vector3 position = transform.position + Vector3.up * 0.3f + transform.forward * 0.4f;

            Runner.Spawn(_worldItemPrefab, position, Quaternion.identity, PlayerRef.None,
                (_, item) => item.GetComponent<WorldItemComponent>().Setup(stack));
        }
    }
}
