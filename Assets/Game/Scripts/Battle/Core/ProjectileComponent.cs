using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public struct ProjectileData : INetworkStruct
    {
        public int FireTick;
        public int FinishTick;
        public Vector3 Origin;
        public Vector3 Velocity;
        public Vector3 HitPoint;
        public float Gravity;
        public float StaggerDuration;
        public short Damage;
        public NetworkBool IsHidden;

        public bool IsFlying => FireTick > 0 && FinishTick == 0;

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

        public NetworkArray<ProjectileData> Projectiles => _projectiles;

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

        public void Fire(Vector3 origin, Vector3 velocity, float gravity, int damage, float staggerDuration)
        {
            _projectiles.Set(_fireCount % Capacity, new ProjectileData
            {
                FireTick = Runner.Tick,
                Origin = origin,
                Velocity = velocity,
                Gravity = gravity,
                Damage = (short)damage,
                StaggerDuration = staggerDuration
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
                if (hit.Hitbox != null && hit.Hitbox.Root == _ownReceiver.HitboxRoot)
                    continue;

                data.FinishTick = Runner.Tick;
                data.HitPoint = hit.Point;
                data.IsHidden = hit.Hitbox != null;

                if (hit.Hitbox != null)
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

            receiver.ApplyHit(new HitRequest
            {
                BaseDamage = data.Damage,
                BodyRays = isBlock ? 0 : 1,
                BlockRays = isBlock ? 1 : 0,
                Zone = isBlock ? HitZone.Torso : zone,
                Point = hit.Point,
                Normal = hit.Normal,
                AttackerPosition = from,
                StaggerDuration = data.StaggerDuration
            });
        }
    }
}
