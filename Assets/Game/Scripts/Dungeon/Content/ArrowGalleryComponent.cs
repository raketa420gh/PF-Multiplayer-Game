using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Pressure plates in a gallery floor and arrow slits in its end walls: a step on any plate looses a volley from both slits
    /// along the gallery a moment later, hitting everyone in its lane. The host does the damage; the arrows fly on every peer.
    public sealed class ArrowGalleryComponent : NetworkBehaviour
    {
        [Networked]
        private int _triggerTick { get; set; }

        [SerializeField]
        private Vector3[] _plates;

        [SerializeField]
        private Vector3 _plateHalfExtents = new(0.6f, 0.5f, 0.6f);

        [SerializeField, Tooltip("The volley's lane, local: along X from slit to slit")]
        private Vector3 _laneCenter = new(0f, 1.1f, 0f);

        [SerializeField]
        private Vector3 _laneHalfExtents = new(16.8f, 0.9f, 0.8f);

        [SerializeField]
        private float _delay = 0.4f;

        [SerializeField]
        private float _flightTime = 0.35f;

        [SerializeField]
        private float _rearm = 3f;

        [SerializeField]
        private int _damage = 25;

        [SerializeField]
        private LayerMask _victimMask;

        [SerializeField, Tooltip("Half fly from the -X slit, half from the +X one")]
        private Transform[] _arrows;

        private static readonly Collider[] s_colliders = new Collider[16];
        private static readonly HashSet<DamageReceiverComponent> s_hit = new();
        private int _heardTick;

        public override void Spawned()
        {
            _heardTick = _triggerTick;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            float since = Since(Runner.Tick);

            if (_triggerTick != 0 && Runner.Tick == _triggerTick + Mathf.CeilToInt(_delay / Runner.DeltaTime))
                Volley();

            if (_triggerTick != 0 && since < _rearm)
                return;

            foreach (Vector3 plate in _plates)
            {
                if (Overlap(plate + Vector3.up * _plateHalfExtents.y, _plateHalfExtents) > 0)
                {
                    _triggerTick = Runner.Tick;

                    return;
                }
            }
        }

        public override void Render()
        {
            float flight = (Since(Runner.Tick + Runner.LocalAlpha) - _delay) / _flightTime;
            bool isFlying = _triggerTick != 0 && flight >= 0f && flight <= 1f;

            for (int i = 0; i < _arrows.Length; i++)
            {
                _arrows[i].gameObject.SetActive(isFlying);
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 position = _arrows[i].localPosition;
                _arrows[i].localPosition = new Vector3(side * _laneHalfExtents.x * (1f - 2f * Mathf.Clamp01(flight)), position.y, position.z);
            }

            if (_heardTick == _triggerTick)
                return;

            _heardTick = _triggerTick;
            DungeonAudioComponent.Play(DungeonSound.Click, transform.TransformPoint(_plates[0]));
        }

        private float Since(float tick)
        {
            return _triggerTick == 0 ? 99f : (tick - _triggerTick) * Runner.DeltaTime;
        }

        private void Volley()
        {
            int count = Overlap(_laneCenter, _laneHalfExtents);
            s_hit.Clear();

            for (int i = 0; i < count; i++)
            {
                DamageReceiverComponent receiver = s_colliders[i].GetComponentInParent<DamageReceiverComponent>();

                if (receiver == null || !receiver.IsAlive || !s_hit.Add(receiver))
                    continue;

                Vector3 point = s_colliders[i].bounds.center;
                Vector3 slit = transform.TransformPoint(new Vector3(Mathf.Sign(transform.InverseTransformPoint(point).x) * _laneHalfExtents.x, _laneCenter.y, 0f));
                receiver.ApplyHit(new HitRequest
                {
                    BaseDamage = _damage,
                    BodyRays = 1,
                    Zone = HitZone.Torso,
                    Point = point,
                    Normal = (slit - point).normalized,
                    AttackerPosition = slit,
                    StaggerDuration = 0.2f,
                    DamageType = DamageType.Physical,
                    Tag = DamageTag.Trap
                });
            }
        }

        private int Overlap(Vector3 localCenter, Vector3 halfExtents)
        {
            return Runner.GetPhysicsScene().OverlapBox(transform.TransformPoint(localCenter), halfExtents, s_colliders, transform.rotation, _victimMask, QueryTriggerInteraction.Ignore);
        }
    }
}
