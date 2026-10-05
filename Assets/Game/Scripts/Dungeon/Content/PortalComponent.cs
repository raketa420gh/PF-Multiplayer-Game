using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum PortalKind : byte
    {
        Escape,
        Descend
    }

    /// Blue portal escapes the dungeon; red portal teleports to the lower floor.
    public sealed class PortalComponent : InteractableComponent
    {
        public override string Prompt => Kind == PortalKind.Escape ? "Escape the dungeon" : "Descend deeper";
        public override float HoldTime => _activationTime;
        public override bool IsAvailable => IsActive && !IsUsed;
        /// Opened like a door (standing still, hands busy), but at Magical Interaction speed like an altar.
        public override bool IsMagical => true;
        public override bool IsRooting => true;
        public override bool IsHandsOccupied => true;
        public PortalKind Kind => _kind;

        [Networked]
        public NetworkBool IsActive { get; private set; }

        [Networked]
        public NetworkBool IsUsed { get; private set; }

        [SerializeField]
        private PortalKind _kind;

        [SerializeField]
        private float _activationTime = 3f;

        [SerializeField]
        private bool _isSingleUse = true;

        [SerializeField]
        private GameObject _visual;

        [SerializeField]
        private Transform _destination;

        [SerializeField]
        private float _spinSpeed = 40f;

        public override void Spawned()
        {
            UpdateVisual();
        }

        public override void Render()
        {
            UpdateVisual();

            if (_visual != null && IsAvailable)
                _visual.transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime, Space.Self);
        }

        public void Activate(Transform destination)
        {
            _destination = destination;
            IsActive = true;
            IsUsed = false;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            if (!IsAvailable)
                return;

            if (_kind == PortalKind.Escape)
                adventurer.Extract();
            else if (_destination != null)
                adventurer.Descend(_destination.position, _destination.eulerAngles.y);

            IsUsed = _isSingleUse;
        }

        private void UpdateVisual()
        {
            if (_visual != null && _visual.activeSelf != IsAvailable)
                _visual.SetActive(IsAvailable);
        }
    }
}
