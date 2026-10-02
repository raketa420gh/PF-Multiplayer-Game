using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Placed campfire: adventurers nearby who rest (hold F) regain health over time; burns out after a while.
    public sealed class CampfireComponent : InteractableComponent
    {
        public override string Prompt => "Rest at campfire";
        public override float HoldTime => _restTime;
        public override bool IsAvailable => _lifetime.ExpiredOrNotRunning(Runner) == false;

        [SerializeField]
        private float _restTime = 6f;

        [SerializeField]
        private float _healPercent = 60f;

        [SerializeField]
        private float _healDuration = 8f;

        [SerializeField]
        private float _burnTime = 60f;

        [SerializeField]
        private Light _light;

        [Networked]
        private TickTimer _lifetime { get; set; }

        private float _flicker;

        public override void Spawned()
        {
            if (HasStateAuthority)
                _lifetime = TickTimer.CreateFromSeconds(Runner, _burnTime);
        }

        public override void Render()
        {
            if (_light == null)
                return;

            _flicker += Time.deltaTime * 11f;
            _light.intensity = 2.2f + Mathf.PerlinNoise(_flicker, 0.3f) * 1.2f;
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && _lifetime.Expired(Runner))
                Runner.Despawn(Object);
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            float amount = adventurer.Fighter.Health.MaxHealth * _healPercent / 100f;
            adventurer.Effects.Add(StatusEffectKind.HealOverTime, amount, _healDuration);
        }
    }
}
