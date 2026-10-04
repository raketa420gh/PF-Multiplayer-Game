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

        [Networked]
        public NetworkBool IsOpen { get; private set; }

        [Networked]
        public NetworkBool IsLocked { get; private set; }

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

            _angle = IsOpen ? _openAngle : 0f;
            ApplyAngle();
        }

        public override void Render()
        {
            float target = IsOpen ? _openAngle : 0f;

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

        public override void Complete(AdventurerComponent adventurer)
        {
            if (IsLocked)
            {
                if (!adventurer.Stats.HasThreshold(StatType.Craft) && !adventurer.Inventory.TryConsumeUtility(UtilityKind.Lockpick))
                    return;

                IsLocked = false;
            }

            IsOpen = !IsOpen;
        }

        public void Unlock()
        {
            IsLocked = false;
        }

        /// Monsters shove doors open on their way to a target.
        public void ForceOpen()
        {
            IsLocked = false;
            IsOpen = true;
        }

        private void ApplyAngle()
        {
            if (_leaf != null)
                _leaf.localRotation = Quaternion.Euler(0f, _angle, 0f);
        }
    }
}
