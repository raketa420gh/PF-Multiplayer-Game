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

        public override void Spawned() => UpdateHealth(_healthComponent.CurrentHealth);
        public override void Render() => UpdateHealth(_healthComponent.CurrentHealth);

        private void UpdateHealth(int health)
        {
            _healthBarView.SetText($"{health}/{_healthComponent.MaxHealth}");
            _healthBarView.SetProgress(_healthComponent.Progress);
        }
    }
}