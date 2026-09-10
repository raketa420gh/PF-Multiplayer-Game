using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class MeleeAttackComponent : NetworkBehaviour
    {
        [SerializeField]
        private Transform _attackPoint;

        [SerializeField]
        private float _attackRadius = 0.5f;

        [SerializeField]
        private int _damage = 1;
        
        [SerializeField]
        private LayerMask _layerMask;
        
        private static readonly Collider[] s_colliders = new Collider[16];
        
        public void Attack()
        {
            int count = Physics.OverlapSphereNonAlloc(_attackPoint.position, _attackRadius, s_colliders, 
                _layerMask, QueryTriggerInteraction.Ignore);

            PlayerRef thisAuthority = Object.InputAuthority;
            
            for (int i = 0; i < count; i++)
            {
                Collider collider = s_colliders[i];
                NetworkObject other = collider.GetComponentInParent<NetworkObject>();

                if (other != null && 
                    other.InputAuthority != thisAuthority && 
                    other.TryGetComponent(out HealthComponent health) &&
                    health.IsAlive)
                {
                    health.TakeDamage(_damage);
                    break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_attackPoint.position, _attackRadius);
            }
        }
    }
}