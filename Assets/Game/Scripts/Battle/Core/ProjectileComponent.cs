using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public enum ProjectileKind : byte
    {
        Arrow,
        Magic,
        Fire,
        Ice,
        Dark,
        Holy,
        Thrown
    }

    public struct ProjectileData : INetworkStruct
    {
        public int FireTick;
        public int FinishTick;
        public Vector3 Origin;
        public Vector3 Velocity;
        public Vector3 HitPoint;
        public float Gravity;
        public float StaggerDuration;
        public float Radius;
        public float EffectMagnitude;
        public float EffectDuration;
        public float LifeSteal;
        public short Damage;
        public byte Kind;
        public byte DamageType;
        public byte Effect;
        public byte Impact;
        public NetworkBool IsHidden;

        public bool IsFlying => FireTick > 0 && FinishTick == 0;
        public ProjectileKind KindValue => (ProjectileKind)Kind;

        public Vector3 GetPosition(float time)
        {
            return Origin + Velocity * time + new Vector3(0f, 0.5f * Gravity * time * time, 0f);
        }

        public Vector3 GetVelocity(float time)
        {
            return Velocity + new Vector3(0f, Gravity * time, 0f);
        }
    }

    public sealed class ProjectileComponent : NetworkBehaviour
    {
        public const int Capacity = 8;

        /// Raised on the state authority when a projectile lands on a receiver (used for spell side effects).
        public delegate void ProjectileHitHandler(in ProjectileData data, DamageReceiverComponent receiver, HitZone zone, HitResult result);

        public event ProjectileHitHandler OnReceiverHit;

        public NetworkArray<ProjectileData> Projectiles => _projectiles;
        public LayerMask HitMask => _hitMask;

        [SerializeField]
        private DamageReceiverComponent _ownReceiver;

        [SerializeField]
        private LayerMask _hitMask;

        [SerializeField]
        private float _lifetime = 5f;

        [Networked, Capacity(Capacity)]
        private NetworkArray<ProjectileData> _projectiles => default;

        [Networked]
        private int _fireCount { get; set; }

        private static readonly List<LagCompensatedHit> s_hits = new(16);
        private static readonly List<LagCompensatedHit> s_overlaps = new(16);

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            for (int i = 0; i < Capacity; i++)
            {
                ProjectileData data = _projectiles[i];

                if (!data.IsFlying || data.FireTick >= Runner.Tick)
                    continue;

                Simulate(ref data);
                _projectiles.Set(i, data);
            }
        }

        public void Fire(Vector3 origin, Vector3 velocity, float gravity, int damage, float staggerDuration,
            DamageType damageType = DamageType.Physical, ProjectileKind kind = ProjectileKind.Arrow,
            float radius = 0f, byte effect = 0, float effectMagnitude = 0f, float effectDuration = 0f, float lifeSteal = 0f, int impact = 3)
        {
            _projectiles.Set(_fireCount % Capacity, new ProjectileData
            {
                FireTick = Runner.Tick,
                Origin = origin,
                Velocity = velocity,
                Gravity = gravity,
                Damage = (short)damage,
                StaggerDuration = staggerDuration,
                DamageType = (byte)damageType,
                Kind = (byte)kind,
                Radius = radius,
                Effect = effect,
                EffectMagnitude = effectMagnitude,
                EffectDuration = effectDuration,
                LifeSteal = lifeSteal,
                Impact = (byte)impact
            });
            _fireCount++;
        }

        private void Simulate(ref ProjectileData data)
        {
            float time = Runner.SecondsSince(data.FireTick);
            Vector3 from = data.GetPosition(time - Runner.DeltaTime);
            Vector3 to = data.GetPosition(time);

            Runner.RaycastAllSorted(from, to, Object.InputAuthority, s_hits, _hitMask,
                HitOptions.IncludePhysX | HitOptions.SubtickAccuracy);

            foreach (LagCompensatedHit hit in s_hits)
            {
                if (hit.Hitbox != null && (hit.Hitbox.Root == _ownReceiver.HitboxRoot || !ZoneHitbox.IsInsideShape(Runner, hit, Object.InputAuthority)))
                    continue;

                data.FinishTick = Runner.Tick;
                data.HitPoint = hit.Point;
                data.IsHidden = hit.Hitbox != null || data.KindValue != ProjectileKind.Arrow;

                if (data.Radius > 0f)
                    Explode(data, hit.Point);
                else if (hit.Hitbox != null)
                    ApplyHit(data, hit, from);

                return;
            }

            if (time >= _lifetime)
            {
                data.FinishTick = Runner.Tick;
                data.IsHidden = true;
            }
        }

        private void ApplyHit(in ProjectileData data, in LagCompensatedHit hit, Vector3 from)
        {
            if (!hit.Hitbox.Root.TryGetComponent(out DamageReceiverComponent receiver) || !receiver.CanBeHitBy(_ownReceiver))
                return;

            HitZone zone = hit.Hitbox is ZoneHitbox zoneHitbox ? zoneHitbox.Zone : HitZone.Torso;
            bool isBlock = zone == HitZone.Block;

            HitResult result = receiver.ApplyHit(new HitRequest
            {
                BaseDamage = data.Damage,
                BodyRays = isBlock ? 0 : 1,
                BlockRays = isBlock ? 1 : 0,
                Zone = isBlock ? HitZone.Torso : zone,
                Point = hit.Point,
                Normal = hit.Normal,
                AttackerPosition = from,
                StaggerDuration = data.StaggerDuration,
                Impact = data.Impact,
                DamageType = (DamageType)data.DamageType,
                Attacker = _ownReceiver
            });

            OnReceiverHit?.Invoke(data, receiver, isBlock ? HitZone.Block : zone, result);
        }

        private void Explode(in ProjectileData data, Vector3 point)
        {
            Runner.LagCompensation.OverlapSphere(point, data.Radius, Object.InputAuthority, s_overlaps, _hitMask, HitOptions.None);

            for (int i = 0; i < s_overlaps.Count; i++)
            {
                LagCompensatedHit hit = s_overlaps[i];

                if (hit.Hitbox == null || !hit.Hitbox.Root.TryGetComponent(out DamageReceiverComponent receiver))
                    continue;

                if (!receiver.CanBeHitBy(_ownReceiver) || WasAlreadyHit(receiver, i))
                    continue;

                float distance = Vector3.Distance(point, hit.Hitbox.Root.transform.position + Vector3.up);
                float falloff = Mathf.Clamp01(1f - distance / (data.Radius * 1.5f));

                HitResult result = receiver.ApplyHit(new HitRequest
                {
                    BaseDamage = Mathf.RoundToInt(data.Damage * Mathf.Lerp(0.4f, 1f, falloff)),
                    BodyRays = 1,
                    Zone = HitZone.Torso,
                    Point = hit.Point,
                    Normal = (hit.Point - point).normalized,
                    AttackerPosition = point,
                    StaggerDuration = data.StaggerDuration,
                    Impact = data.Impact,
                    DamageType = (DamageType)data.DamageType,
                    Attacker = _ownReceiver
                });

                OnReceiverHit?.Invoke(data, receiver, HitZone.Torso, result);
            }
        }

        private static bool WasAlreadyHit(DamageReceiverComponent receiver, int index)
        {
            for (int i = 0; i < index; i++)
            {
                if (s_overlaps[i].Hitbox != null && s_overlaps[i].Hitbox.Root == receiver.HitboxRoot)
                    return true;
            }

            return false;
        }
    }
}
