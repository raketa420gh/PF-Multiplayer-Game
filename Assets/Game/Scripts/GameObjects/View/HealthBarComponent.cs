using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class HealthBarComponent : NetworkBehaviour
    {
        [SerializeField]
        private HealthComponent _healthComponent;
        
        [SerializeField]
        private HealthBarView _healthBarView;

        public override void Spawned()
        {
            _healthComponent.OnHealthChanged += OnHealthChanged;
            UpdateHealth(_healthComponent.CurrentHealth);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _healthComponent.OnHealthChanged -= OnHealthChanged;
        }

        private void UpdateHealth(int current)
        {
            _healthBarView.SetText($"{current}/{_healthComponent.MaxHealth}");
            _healthBarView.SetProgress(_healthComponent.Progress);
        }

        private void OnHealthChanged(int previous, int current)
        {
            UpdateHealth(current);
        }
    }
}