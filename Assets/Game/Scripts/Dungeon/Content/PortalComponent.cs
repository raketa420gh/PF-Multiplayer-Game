using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum PortalKind : byte
    {
        Escape,
        Descend
    }

    /// Blue portal escapes the dungeon, red portal leads to the lower floor; stepping into an open one takes the adventurer
    /// through at once. A blue portal shows up as a pedestal rising out of the ground (marked on the map) and opens only after
    /// someone holds F on the pedestal. A way down opens by itself: its gate (a cellar grate) lifts and the red glow behind waits.
    public sealed class PortalComponent : InteractableComponent
    {
        public override string Prompt => "Open the portal";
        public override float HoldTime => _activationTime;
        public override bool IsAvailable => IsActive && !IsOpened && _kind == PortalKind.Escape;
        /// Opened like a door (standing still, hands busy), but at Magical Interaction speed like an altar.
        public override bool IsMagical => true;
        public override bool IsRooting => true;
        public override bool IsHandsOccupied => true;
        public PortalKind Kind => _kind;
        /// Open and passable: stepping into the zone goes through.
        public bool IsOpen => IsActive && IsOpened;

        [Networked]
        public NetworkBool IsActive { get; private set; }

        [Networked]
        public NetworkBool IsOpened { get; private set; }

        [SerializeField]
        private PortalKind _kind;

        [SerializeField]
        private float _activationTime = 3f;

        [SerializeField]
        private GameObject _visual;

        [SerializeField]
        private Transform _destination;

        [SerializeField]
        private float _spinSpeed = 40f;

        [SerializeField]
        private Transform _gate;

        [SerializeField]
        private float _gateLift = 2.2f;

        [SerializeField]
        private float _gateSpeed = 1.5f;

        [SerializeField, Tooltip("Rises out of the ground when the portal shows up; holding F on it opens the portal")]
        private Transform _pedestal;

        [SerializeField]
        private float _pedestalDepth = 1.6f;

        [SerializeField]
        private float _pedestalSpeed = 0.8f;

        [SerializeField, Tooltip("Local box an adventurer steps into to go through")]
        private Vector3 _zoneCenter = new(0f, 1.15f, 0f);

        [SerializeField]
        private Vector3 _zoneSize = new(1.8f, 2.4f, 0.8f);

        private float _gateHeight;
        private float _pedestalHeight;

        public override void Spawned()
        {
            _pedestalHeight = IsActive ? 0f : -_pedestalDepth;
            UpdateVisual();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !IsOpen)
                return;

            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if (fighter != null && fighter.TryGetComponent(out AdventurerComponent adventurer) && adventurer.State == AdventurerState.Alive && IsInZone(adventurer.transform.position))
                    Pass(adventurer);
            }
        }

        public override void Render()
        {
            UpdateVisual();

            if (_visual != null && IsOpen)
                _visual.transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime, Space.Self);

            if (_pedestal != null)
            {
                _pedestalHeight = Mathf.MoveTowards(_pedestalHeight, IsActive ? 0f : -_pedestalDepth, _pedestalSpeed * Time.deltaTime);
                _pedestal.localPosition = Vector3.up * _pedestalHeight;
            }

            if (_gate == null)
                return;

            _gateHeight = Mathf.MoveTowards(_gateHeight, IsOpen ? _gateLift : 0f, _gateSpeed * Time.deltaTime);
            _gate.localPosition = Vector3.up * _gateHeight;
        }

        /// A way down opens at once; an escape portal waits for someone at its pedestal.
        public void Activate(Transform destination)
        {
            _destination = destination;
            IsActive = true;
            IsOpened = _kind == PortalKind.Descend;
        }

        public void Deactivate()
        {
            IsActive = false;
            IsOpened = false;
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            if (IsAvailable)
                IsOpened = true;
        }

        private bool IsInZone(Vector3 position)
        {
            Vector3 local = transform.InverseTransformPoint(position + Vector3.up * 0.9f) - _zoneCenter;

            return Mathf.Abs(local.x) <= _zoneSize.x * 0.5f && Mathf.Abs(local.y) <= _zoneSize.y * 0.5f && Mathf.Abs(local.z) <= _zoneSize.z * 0.5f;
        }

        private void Pass(AdventurerComponent adventurer)
        {
            if (_kind == PortalKind.Escape)
                adventurer.Extract();
            else if (_destination != null)
                adventurer.Descend(_destination.position, _destination.eulerAngles.y);
        }

        private void UpdateVisual()
        {
            if (_visual != null && _visual.activeSelf != IsOpen)
                _visual.SetActive(IsOpen);
        }
    }
}
