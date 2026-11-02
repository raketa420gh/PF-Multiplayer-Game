using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A stone slab of a room's secret: slides from its closed place by the offset as the mechanism opens.
    public sealed class RoomSlabComponent : MonoBehaviour
    {
        [SerializeField]
        private Vector3 _closed;

        [SerializeField]
        private Vector3 _offset;

        public void Set(float open)
        {
            transform.localPosition = _closed + _offset * open;
        }
    }
}
