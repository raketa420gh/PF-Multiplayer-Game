using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Placed campfire: adventurers nearby who rest (hold F) regain health over time, spells come back to those who sit by it
    /// (rest key); burns out after a while.
    public sealed class CampfireComponent : InteractableComponent
    {
        public const float WarmRadius = 3.5f;

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

        private static readonly List<CampfireComponent> s_all = new();
        private float _flicker;

        public override void Spawned()
        {
            s_all.Add(this);

            if (HasStateAuthority)
                _lifetime = TickTimer.CreateFromSeconds(Runner, _burnTime);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            s_all.Remove(this);
        }

        /// A lit campfire burns within reach of the point.
        public static bool IsWarming(Vector3 position)
        {
            return s_all.Exists(fire => fire.IsAvailable && (fire.transform.position - position).sqrMagnitude < WarmRadius * WarmRadius);
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
            float amount = adventurer.Fighter.Health.MaxHealth * _healPercent / 100f * adventurer.Stats.PhysicalHealing;
            adventurer.Effects.Add(StatusEffectKind.HealOverTime, amount, _healDuration);
        }
    }
}
