using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public enum HitResult : byte
    {
        Hit,
        Blocked,
        PartialBlock
    }

    /// What dealt a hit: class perks and status effects tell sources apart by it.
    [Flags]
    public enum DamageTag : ushort
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Thrown = 4,
        Spell = 8,
        Fire = 16,
        Frost = 32,
        Lightning = 64,
        Holy = 128,
        Poison = 256,
        Flask = 512,
        Trap = 1024,
        Dark = 2048
    }

    public struct HitRequest
    {
        public int BaseDamage;
        public int BodyRays;
        public int BlockRays;
        public HitZone Zone;
        public Vector3 Point;
        public Vector3 Normal;
        public Vector3 AttackerPosition;
        public float StaggerDuration;
        public int Impact;
        public DamageType DamageType;
        public DamageReceiverComponent Attacker;

        /// Sound set of the striking weapon, see WeaponSoundConfig.Id.
        public byte Sound;
        public DamageTag Tag;
        /// The victim's armor and resistances do not reduce the hit.
        public bool IgnoreArmor;
    }

    public struct HitEventData : INetworkStruct
    {
        public Vector3 Point;
        public Vector3 Normal;
        public HitResult Result;
        public HitZone Zone;
        public short Damage;
        public byte Sound;
    }

    public sealed class DamageReceiverComponent : NetworkBehaviour
    {
        public interface IOwner
        {
            BlockConfig ActiveBlock { get; }
            Vector3 BlockDirection { get; }
            void OnHitReceived(HitResult result, float staggerDuration, int impact);
        }

        /// Optional second interface: outgoing hit bonuses and incoming armor and resistances of the receiver.
        public interface IHitModifier
        {
            /// Attacker side, before the victim's reductions: the striker's bonuses may change the damage and the request.
            void ModifyOutgoingHit(DamageReceiverComponent victim, ref HitRequest request);
            /// Victim side: share of the hit the raised block stops.
            float ModifyBlockMitigation(float mitigation);
            int ModifyIncomingDamage(int damage, in HitRequest request);
        }

        /// Everything that can be hit: fighters, monsters and dummies.
        public static IReadOnlyList<DamageReceiverComponent> All => s_all;

        public event Action<HitEventData> OnHitEvent;

        /// State authority only: this receiver's owner landed a hit on another receiver.
        public event Action<DamageReceiverComponent, HitResult, int, HitRequest> OnHitDealt;

        /// State authority only: this receiver was hit; the damage is what went through.
        public event Action<HitResult, int, HitRequest> OnHitTaken;

        public HitboxRoot HitboxRoot => _hitboxRoot;
        public bool IsAlive => _health.IsAlive;
        public int Team => _team;
        public DamageReceiverComponent LastAttacker => _lastAttacker;
        public HealthComponent Health => _health;

        [SerializeField]
        private HealthComponent _health;

        [SerializeField]
        private HitboxRoot _hitboxRoot;

        [SerializeField]
        private HitZoneConfig _zoneConfig;

        private const int EventCapacity = 8;

        [Networked, Capacity(EventCapacity)]
        private NetworkArray<HitEventData> _events => default;

        [Networked]
        private int _eventCount { get; set; }

        private const int NoTeam = 0;

        private static readonly List<DamageReceiverComponent> s_all = new();
        private IOwner _owner;
        private IHitModifier _modifier;
        private DamageReceiverComponent _lastAttacker;
        private int _renderedEvents;
        private int _team = NoTeam;

        public override void Spawned()
        {
            s_all.Add(this);
            _renderedEvents = _eventCount;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            s_all.Remove(this);
        }

        public override void Render()
        {
            for (int i = Mathf.Max(_renderedEvents, _eventCount - EventCapacity); i < _eventCount; i++)
                OnHitEvent?.Invoke(_events[i % EventCapacity]);

            _renderedEvents = _eventCount;
        }

        public void SetOwner(IOwner owner)
        {
            _owner = owner;
        }

        public void SetModifier(IHitModifier modifier)
        {
            _modifier = modifier;
        }

        public void SetTeam(int team)
        {
            _team = team;
        }

        public bool CanBeHitBy(DamageReceiverComponent attacker)
        {
            return IsAlive && (attacker == null || _team == NoTeam || _team != attacker.Team);
        }

        public HitResult ApplyHit(in HitRequest request)
        {
            HitRequest hit = request;
            BlockConfig block = _owner?.ActiveBlock;
            int blockRays = hit.BlockRays;
            int bodyRays = hit.BodyRays;

            if (blockRays > 0 && !IsBlockValid(block, hit.AttackerPosition))
            {
                bodyRays += blockRays;
                blockRays = 0;
            }

            HitResult result = blockRays == 0 ? HitResult.Hit : bodyRays == 0 ? HitResult.Blocked : HitResult.PartialBlock;
            float blockedShare = blockRays / (float)(blockRays + bodyRays);

            if (hit.Attacker != null && hit.Attacker._modifier != null)
                hit.Attacker._modifier.ModifyOutgoingHit(this, ref hit);

            float zoneMultiplier = result == HitResult.Blocked ? 1f : _zoneConfig.GetMultiplier(hit.Zone);
            float mitigation = block != null ? block.Mitigation * blockedShare : 0f;

            if (_modifier != null && mitigation > 0f)
                mitigation = Mathf.Clamp01(_modifier.ModifyBlockMitigation(mitigation));

            int damage = Mathf.RoundToInt(hit.BaseDamage * zoneMultiplier * (1f - mitigation));

            if (_modifier != null && damage > 0)
                damage = Mathf.Max(0, _modifier.ModifyIncomingDamage(damage, hit));

            if (hit.Attacker != null)
                _lastAttacker = hit.Attacker;

            _health.TakeDamage(damage);

            _events.Set(_eventCount % EventCapacity, new HitEventData
            {
                Point = hit.Point,
                Normal = hit.Normal,
                Result = result,
                Zone = result == HitResult.Blocked ? HitZone.Block : hit.Zone,
                Damage = (short)damage,
                Sound = hit.Sound
            });
            _eventCount++;

            _owner?.OnHitReceived(result, hit.StaggerDuration, hit.Impact);
            OnHitTaken?.Invoke(result, damage, hit);

            if (hit.Attacker != null)
                hit.Attacker.OnHitDealt?.Invoke(this, result, damage, hit);

            return result;
        }

        private bool IsBlockValid(BlockConfig block, Vector3 attackerPosition)
        {
            if (block == null)
                return false;

            Vector3 toAttacker = attackerPosition - transform.position;
            toAttacker.y = 0f;
            Vector3 blockDirection = _owner.BlockDirection;
            blockDirection.y = 0f;

            return Vector3.Angle(blockDirection, toAttacker) <= block.AngleTolerance;
        }
    }
}
