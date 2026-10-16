using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Lever that lowers a drawbridge for a while; the bridge rises again by itself and the lever may be pulled once more.
    /// The pose follows the networked tick of the pull, so every peer holds the bridge collider in the same place each tick.
    public sealed class BridgeLeverComponent : InteractableComponent
    {
        public override string Prompt => IsAvailable ? "Lower bridge" : "Bridge lowered";
        public override bool IsAvailable => _pullTick == 0 || Since(Runner.Tick) >= _openTime + _swingTime * 2f;

        [SerializeField]
        private Transform _bridge;

        [SerializeField]
        private Transform _handle;

        [SerializeField]
        private float _raisedAngle = 65f;

        [SerializeField]
        private float _swingTime = 1.5f;

        [SerializeField]
        private float _openTime = 30f;

        [Networked]
        private int _pullTick { get; set; }

        private int _heardTick;

        public override void Spawned()
        {
            _heardTick = _pullTick;
            Apply(Runner.Tick);
        }

        public override void FixedUpdateNetwork()
        {
            Apply(Runner.Tick);
        }

        public override void Render()
        {
            Apply(Runner.Tick + Runner.LocalAlpha);

            if (_heardTick == _pullTick)
                return;

            _heardTick = _pullTick;
            DungeonAudioComponent.Play(DungeonSound.Lever, transform.position);
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            if (IsAvailable)
                _pullTick = Runner.Tick;
        }

        private float Since(float tick)
        {
            return (tick - _pullTick) * Runner.DeltaTime;
        }

        private void Apply(float tick)
        {
            float seconds = _pullTick == 0 ? 0f : Since(tick);
            float lowered = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(seconds, _openTime + _swingTime * 2f - seconds) / _swingTime));
            _bridge.localRotation = Quaternion.Euler(-_raisedAngle * (1f - lowered), 0f, 0f);

            if (_handle != null)
                _handle.localRotation = Quaternion.Euler(Mathf.Lerp(-40f, 40f, lowered), 0f, 0f);
        }
    }
}
