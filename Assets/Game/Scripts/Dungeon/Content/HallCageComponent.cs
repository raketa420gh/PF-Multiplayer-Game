using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A cage of the Great Hall's lift: carries the bodies standing in it.
    public sealed class HallCageComponent : MonoBehaviour, IMovingPlatform
    {
        [SerializeField]
        private GreatHallMachineryComponent _machinery;

        [SerializeField]
        private int _index;

        public Vector3 GetStep(int tick)
        {
            return _machinery.GetCageStep(_index, tick);
        }
    }
}
