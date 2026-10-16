using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A flight of the level kit climbing Size.y along local +Z over Size.z, Size.x wide, from its pivot at the foot. Scale it
    /// or set the size: the editor rebuilds the steps, the hidden walkable ramp and the fill under it (NavMesh and character
    /// controllers need both).
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class KitStairsComponent : MonoBehaviour
    {
        public Vector3 Size => _size;

        [SerializeField]
        private Vector3 _size = new(2f, 1.5f, 3f);

        public void SetSize(Vector3 size)
        {
            _size = size;
        }
    }
}
