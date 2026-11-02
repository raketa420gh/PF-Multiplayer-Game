using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A floor plate of a room's sequence: an adventurer stepping on it takes the step.
    public sealed class RoomPlateComponent : NetworkBehaviour
    {
        [SerializeField]
        private RoomMechanismComponent _mechanism;

        [SerializeField]
        private int _step;

        [SerializeField]
        private Transform _top;

        [SerializeField]
        private Vector3 _halfExtents = new(0.8f, 0.6f, 0.8f);

        [SerializeField]
        private float _sink = 0.06f;

        [SerializeField]
        private LayerMask _bodyMask;

        [Networked]
        private NetworkBool _isDown { get; set; }

        private static readonly Collider[] s_colliders = new Collider[8];
        private float _rest;

        public override void Spawned()
        {
            _rest = _top.localPosition.y;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            bool isDown = IsStoodOn();

            if (isDown && !_isDown)
                _mechanism.Press(_step);

            _isDown = isDown;
        }

        public override void Render()
        {
            Vector3 position = _top.localPosition;
            _top.localPosition = new Vector3(position.x, _rest - (_isDown ? _sink : 0f), position.z);
        }

        private bool IsStoodOn()
        {
            int count = Runner.GetPhysicsScene().OverlapBox(transform.position + Vector3.up * _halfExtents.y, _halfExtents, s_colliders, transform.rotation, _bodyMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (s_colliders[i].GetComponentInParent<AdventurerComponent>() != null)
                    return true;
            }

            return false;
        }
    }
}
