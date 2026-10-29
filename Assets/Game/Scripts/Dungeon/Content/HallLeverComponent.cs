using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum HallLeverAction : byte
    {
        OreCageUp,
        OreCageDown,
        Ride,
        Seal
    }

    /// A lever of the Great Hall's machinery: calls the cages, sends the one it stands in, or breaks a seal.
    public sealed class HallLeverComponent : InteractableComponent
    {
        public override string Prompt => _action == HallLeverAction.Seal ? IsAvailable ? "Break the seal" : "Seal broken"
            : IsAvailable ? _action == HallLeverAction.Ride ? "Send the cage" : "Call the cage" : "The cage is here";

        public override bool IsAvailable => _action switch
        {
            HallLeverAction.Seal => !_machinery.IsSealBroken(_seal),
            HallLeverAction.Ride => !_machinery.IsMoving,
            _ => !_machinery.IsMoving && _machinery.IsOreCageUp != (_action == HallLeverAction.OreCageUp)
        };

        [SerializeField]
        private GreatHallMachineryComponent _machinery;

        [SerializeField]
        private HallLeverAction _action;

        [SerializeField]
        private int _seal;

        [SerializeField]
        private Transform _handle;

        private float _blend;

        public override void Render()
        {
            if (_handle == null)
                return;

            bool isPulled = _action == HallLeverAction.Seal ? _machinery.IsSealBroken(_seal) : _machinery.IsMoving;
            _blend = Mathf.MoveTowards(_blend, isPulled ? 1f : 0f, Time.deltaTime * 3f);
            _handle.localRotation = Quaternion.Euler(Mathf.Lerp(-40f, 40f, _blend), 0f, 0f);
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            if (!IsAvailable)
                return;

            if (_action == HallLeverAction.Seal)
                _machinery.BreakSeal(_seal);
            else
                _machinery.SetOreCageUp(_action == HallLeverAction.Ride ? !_machinery.IsOreCageUp : _action == HallLeverAction.OreCageUp);
        }
    }
}
