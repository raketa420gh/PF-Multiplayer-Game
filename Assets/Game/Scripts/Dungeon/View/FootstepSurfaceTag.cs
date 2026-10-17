using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Marks the colliders of this object and its children as the surface footsteps sound on; untagged ground is stone.
    public class FootstepSurfaceTag : MonoBehaviour
    {
        [SerializeField]
        private FootstepSurface _surface;

        public virtual FootstepSurface GetSurface(Vector3 point)
        {
            return _surface;
        }
    }
}
