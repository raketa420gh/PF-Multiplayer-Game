using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Pull once to unlock a door or disarm a trap.
    public sealed class LeverComponent : InteractableComponent
    {
        public override string Prompt => IsPulled ? "Lever (pulled)" : "Pull lever";
        public override bool IsAvailable => !IsPulled;

        [Networked]
        public NetworkBool IsPulled { get; private set; }

        [SerializeField]
        private DoorComponent _door;

        [SerializeField]
        private TrapComponent _trap;

        [SerializeField]
        private Transform _handle;

        private float _blend;

        public override void Render()
        {
            if (_handle == null)
                return;

            _blend = Mathf.MoveTowards(_blend, IsPulled ? 1f : 0f, Time.deltaTime * 3f);
            _handle.localRotation = Quaternion.Euler(Mathf.Lerp(-40f, 40f, _blend), 0f, 0f);
        }

        public void Setup(DoorComponent door, TrapComponent trap)
        {
            _door = door;
            _trap = trap;
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            IsPulled = true;

            if (_door != null)
                _door.Unlock();

            if (_trap != null)
                _trap.Disarm();
        }
    }
}
