using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// The deck of a room's lift: carries the bodies standing on it.
    public sealed class RoomLiftComponent : MonoBehaviour, IMovingPlatform
    {
        [SerializeField]
        private RoomMechanismComponent _mechanism;

        public Vector3 GetStep(int tick)
        {
            return _mechanism.GetLiftStep(tick);
        }
    }
}
