using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Floor that moves on its own (a lift cage): a body standing on it is carried along by the platform's step of each tick.
    public interface IMovingPlatform
    {
        /// Displacement of the floor from the previous tick to this one; a pure function of the tick, so prediction agrees.
        Vector3 GetStep(int tick);
    }
}
