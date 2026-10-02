using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum TrapKind : byte
    {
        Spikes,
        SwingingBlade
    }

    /// Floor spikes trigger on contact with a cooldown; the blade swings on a cycle. Damage is applied by the host.
    public sealed class TrapComponent : NetworkBehaviour
    {
        public TrapKind Kind => _kind;

        [Networked]
        public NetworkBool IsArmed { get; private set; }

        [Networked]
        public int TriggerTick { get; private set; }

        [SerializeField]
        private TrapKind _kind;

        [SerializeField]
        private int _damage = 30;

        [SerializeField]
        private float _period = 3f;

        [SerializeField]
        private float _activeTime = 0.6f;

        [SerializeField]
        private Vector3 _halfExtents = new(0.9f, 0.8f, 0.9f);

        [SerializeField]
        private Vector3 _center = new(0f, 0.8f, 0f);

        [SerializeField]
        private LayerMask _victimMask;

        [SerializeField]
        private Transform _moving;

        [SerializeField]
        private Vector3 _restPosition;

        [SerializeField]
        private Vector3 _activePosition;

        [SerializeField]
        private Vector3 _swingEuler = new(0f, 0f, 70f);

        [Networked]
        private TickTimer _cooldown { get; set; }

        private static readonly Collider[] s_colliders = new Collider[16];
        private readonly HashSet<DamageReceiverComponent> _hitThisCycle = new();

        public override void Spawned()
        {
            if (HasStateAuthority)
                IsArmed = true;
        }

        public override void Render()
        {
            if (_moving == null)
                return;

            if (_kind == TrapKind.SwingingBlade)
            {
                float phase = IsArmed ? Mathf.Sin((Runner.Tick + Runner.LocalAlpha) * Runner.DeltaTime * Mathf.PI * 2f / _period) : 0f;
                _moving.localRotation = Quaternion.Euler(_swingEuler * phase);
            }
            else
            {
                float time = TriggerTick > 0 ? Runner.SecondsSince(TriggerTick) : 99f;
                float up = time < _activeTime ? 1f : Mathf.Clamp01(1f - (time - _activeTime) * 2f);
                _moving.localPosition = Vector3.Lerp(_restPosition, _activePosition, up);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !IsArmed)
                return;

            bool isActive;

            if (_kind == TrapKind.SwingingBlade)
            {
                float phase = Mathf.Sin(Runner.SimulationTime * Mathf.PI * 2f / _period);
                isActive = Mathf.Abs(phase) < 0.45f;

                if (!isActive)
                    _hitThisCycle.Clear();
            }
            else
            {
                isActive = TriggerTick > 0 && Runner.SecondsSince(TriggerTick) < _activeTime;

                if (!isActive && _cooldown.ExpiredOrNotRunning(Runner) && Overlap(out _))
                {
                    TriggerTick = Runner.Tick;
                    _cooldown = TickTimer.CreateFromSeconds(Runner, _period);
                    _hitThisCycle.Clear();
                    isActive = true;
                }
            }

            if (!isActive || !Overlap(out int count))
                return;

            for (int i = 0; i < count; i++)
            {
                DamageReceiverComponent receiver = s_colliders[i].GetComponentInParent<DamageReceiverComponent>();

                if (receiver == null || !receiver.IsAlive || !_hitThisCycle.Add(receiver))
                    continue;

                receiver.ApplyHit(new HitRequest
                {
                    BaseDamage = _damage,
                    BodyRays = 1,
                    Zone = HitZone.Legs,
                    Point = s_colliders[i].bounds.center,
                    Normal = Vector3.up,
                    AttackerPosition = transform.position,
                    StaggerDuration = 0.3f,
                    DamageType = DamageType.True
                });
            }
        }

        public void Disarm()
        {
            IsArmed = false;
        }

        private bool Overlap(out int count)
        {
            count = Runner.GetPhysicsScene().OverlapBox(transform.TransformPoint(_center), _halfExtents, s_colliders,
                transform.rotation, _victimMask, QueryTriggerInteraction.Ignore);

            return count > 0;
        }
    }
}
