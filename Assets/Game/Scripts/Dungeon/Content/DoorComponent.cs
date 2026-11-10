using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public sealed class DoorComponent : InteractableComponent
    {
        public override string Prompt => IsOpen ? "Close door" : IsLocked ? "Locked door" : "Open door";
        public override float HoldTime => IsOpen ? 0.15f : _openTime;
        public override bool IsRooting => !IsOpen;
        public override bool IsHandsOccupied => true;
        /// Someone is holding the door open; ends by itself at the tick the hold would have completed.
        public bool IsOpening => !IsOpen && Runner.Tick < _openingEndTick;

        [Networked]
        public NetworkBool IsOpen { get; private set; }

        [Networked]
        public NetworkBool IsLocked { get; private set; }

        [Networked]
        private int _openingEndTick { get; set; }

        /// The opener stood on the leaf's -Z side: it swings to +Z, away from them.
        [Networked]
        private NetworkBool _isOpenBackward { get; set; }

        [SerializeField]
        private Transform _leaf;

        [SerializeField]
        private float _openAngle = 100f;

        [SerializeField]
        private float _swingSpeed = 220f;

        [SerializeField]
        private float _openTime = 3f;

        [SerializeField]
        private Collider _blocker;

        [SerializeField]
        private bool _startsLocked;

        private float _angle;

        public override void Spawned()
        {
            if (HasStateAuthority)
                IsLocked = _startsLocked;

            _angle = IsOpen ? OpenedAngle() : 0f;
            ApplyAngle();
        }

        public override void Render()
        {
            float target = IsOpen ? OpenedAngle() : 0f;

            if (Mathf.Approximately(_angle, target))
                return;

            _angle = Mathf.MoveTowards(_angle, target, _swingSpeed * Time.deltaTime);
            ApplyAngle();
        }

        public override void FixedUpdateNetwork()
        {
            if (_blocker != null)
                _blocker.enabled = !IsOpen;
        }

        public override void Begin(AdventurerComponent adventurer, int completeTick)
        {
            if (!IsOpen)
                _openingEndTick = completeTick;
        }

        public override void Cancel(AdventurerComponent adventurer)
        {
            _openingEndTick = 0;
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            _openingEndTick = 0;

            if (IsLocked)
            {
                if (!adventurer.Stats.HasThreshold(StatType.Dexterity) && !adventurer.Inventory.TryConsumeUtility(UtilityKind.Lockpick))
                    return;

                IsLocked = false;
            }

            if (!IsOpen)
                SwingAwayFrom(adventurer.transform.position);

            IsOpen = !IsOpen;
        }

        public void Unlock()
        {
            IsLocked = false;
        }

        /// Monsters shove doors open on their way to a target.
        public void ForceOpen(Vector3 from)
        {
            SwingAwayFrom(from);
            IsLocked = false;
            IsOpen = true;
        }

        private void SwingAwayFrom(Vector3 position) => _isOpenBackward = Vector3.Dot(position - transform.position, transform.forward) < 0f;

        private float OpenedAngle() => _isOpenBackward ? -_openAngle : _openAngle;

        private void ApplyAngle()
        {
            if (_leaf != null)
                _leaf.localRotation = Quaternion.Euler(0f, _angle, 0f);
        }
    }
}
