using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A cracked floor slab: a body stepping on it starts it shaking, a moment later it breaks off and falls with whoever stood
    /// there, and a while later the vein has grown a new one in its place.
    public sealed class CollapsingFloorComponent : NetworkBehaviour
    {
        [Networked]
        private int _triggerTick { get; set; }

        [SerializeField]
        private Transform _slab;

        [SerializeField]
        private Collider _collider;

        [SerializeField, Tooltip("Bodies on the slab, local, above its top")]
        private Vector3 _center = new(0f, 0.6f, 0f);

        [SerializeField]
        private Vector3 _halfExtents = new(2f, 0.6f, 1f);

        [SerializeField]
        private float _delay = 0.8f;

        [SerializeField]
        private float _gravity = 20f;

        [SerializeField, Tooltip("Seconds until the slab is back")]
        private float _regrow = 60f;

        [SerializeField]
        private LayerMask _victimMask;

        private static readonly Collider[] s_colliders = new Collider[4];
        private Renderer[] _renderers;
        private int _heardTick;

        public override void Spawned()
        {
            _renderers = _slab.GetComponentsInChildren<Renderer>();
            _heardTick = _triggerTick;
            Apply(Runner.Tick);
        }

        public override void FixedUpdateNetwork()
        {
            Apply(Runner.Tick);

            if (HasStateAuthority && Since(Runner.Tick) >= _regrow && Runner.GetPhysicsScene().OverlapBox(transform.TransformPoint(_center), _halfExtents,
                    s_colliders, transform.rotation, _victimMask, QueryTriggerInteraction.Ignore) > 0)
                _triggerTick = Runner.Tick;
        }

        public override void Render()
        {
            Apply(Runner.Tick + Runner.LocalAlpha);

            if (_heardTick == _triggerTick)
                return;

            _heardTick = _triggerTick;
            DungeonAudioComponent.Play(DungeonSound.Rattle, transform.position);
        }

        private float Since(float tick)
        {
            return _triggerTick == 0 ? float.MaxValue : (tick - _triggerTick) * Runner.DeltaTime;
        }

        private void Apply(float tick)
        {
            float since = Since(tick);
            bool isWhole = since < _delay || since >= _regrow;
            float fall = isWhole ? 0f : since - _delay;
            float shake = since < _delay ? Mathf.Sin(since * 90f) * 0.03f : 0f;
            _slab.localPosition = new Vector3(shake, -0.5f * _gravity * fall * fall, 0f);
            _collider.enabled = isWhole;

            foreach (Renderer renderer in _renderers)
                renderer.enabled = isWhole || fall < 2f;
        }
    }
}
