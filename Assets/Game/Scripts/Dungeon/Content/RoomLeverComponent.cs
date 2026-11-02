using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A lever of a room's mechanism: opens its secret, takes a step of its sequence or sends the lift.
    public sealed class RoomLeverComponent : InteractableComponent
    {
        public override string Prompt => _isLift ? IsAvailable ? "Send the lift" : "The lift is moving" : IsAvailable ? "Pull lever" : "Lever (pulled)";
        public override bool IsAvailable => _isLift ? !_mechanism.IsLiftMoving : !_mechanism.IsOpen;

        [SerializeField]
        private RoomMechanismComponent _mechanism;

        [SerializeField, Tooltip("Place in the room's sequence, from 1; 0 outside one")]
        private int _step;

        [SerializeField]
        private bool _isLift;

        [SerializeField]
        private Transform _handle;

        private float _blend;

        public override void Render()
        {
            if (_handle == null)
                return;

            bool isPulled = _isLift ? _mechanism.IsLiftMoving : _mechanism.IsOpen || _step > 0 && _mechanism.Progress >= _step;
            _blend = Mathf.MoveTowards(_blend, isPulled ? 1f : 0f, Time.deltaTime * 3f);
            _handle.localRotation = Quaternion.Euler(Mathf.Lerp(-40f, 40f, _blend), 0f, 0f);
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            if (!IsAvailable)
                return;

            if (_isLift)
                _mechanism.SendLift();
            else
                _mechanism.Press(_step);
        }
    }
}
