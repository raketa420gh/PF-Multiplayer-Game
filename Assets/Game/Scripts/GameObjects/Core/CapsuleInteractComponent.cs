using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class CapsuleInteractComponent : NetworkBehaviour
    {
        private static readonly Collider[] s_colliders = new Collider[32];
        
        [SerializeField]
        private CapsuleCollider _collider;
        
        [SerializeField]
        private LayerMask _layerMask;

        public override void FixedUpdateNetwork()
        {
            _collider.GetPointsAndRadius(out Vector3 point0, out Vector3 point1, out float radius);

            PhysicsScene scene = Runner.GetPhysicsScene();

            int count = scene.OverlapCapsule(
                point0,
                point1,
                radius,
                s_colliders,
                _layerMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider collider = s_colliders[i];
                IInteractableComponent interactable = collider.GetComponentInParent<IInteractableComponent>();
                interactable?.Interact(gameObject);
            }
        }
    }
}