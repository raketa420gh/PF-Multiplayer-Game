using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A resizable piece of the level kit: a box or a cylinder standing on its pivot. Scale it with the Scale tool or set the size:
    /// the editor turns the scale into the size and rebuilds the mesh and the box collider, so the texture keeps its scale.
    /// Name it Wall… to be ink on the map and no floor for the NavMesh, Ceiling… to be hidden on the map.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class KitBlockComponent : MonoBehaviour
    {
        public Vector3 Size => _size;
        public bool IsRound => _isRound;

        [SerializeField]
        private Vector3 _size = new(4f, 6f, 0.6f);

        [SerializeField]
        private bool _isRound;

        public void SetSize(Vector3 size)
        {
            _size = size;
        }

        public void SetRound(bool isRound)
        {
            _isRound = isRound;
        }
    }
}
