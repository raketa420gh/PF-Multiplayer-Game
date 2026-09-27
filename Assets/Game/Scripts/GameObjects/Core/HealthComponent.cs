using System;
using Fusion;

namespace Game.Scripts
{
    public sealed class HealthComponent : NetworkBehaviour
    {
        public delegate void HealthChangedHandler(int previous, int current);
        
        private static PropertyReader<int> s_propertyReader =
                GetPropertyReader<int>(typeof(HealthComponent), nameof(CurrentHealth));
        public event HealthChangedHandler OnHealthChanged;

        [Networked, OnChangedRender(nameof(InvokeHealthChanged))] 
        public int CurrentHealth { get; set; } = 100;

        [Networked]
        public int MaxHealth { get; set; } = 100;

        public bool IsDead => CurrentHealth <= 0;
        public bool IsAlive => CurrentHealth > 0;
        public bool IsNotFull => CurrentHealth < MaxHealth && CurrentHealth > 0;
        public float Progress => (float) CurrentHealth / MaxHealth;

        public override void Spawned()
        {
            CurrentHealth = MaxHealth;
        }

        public void Restore(int heal)
        {
            if (heal <= 0)
                return;
            
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + heal);
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || IsDead)
                return;

            CurrentHealth = Math.Max(0, CurrentHealth - damage);
        }

        private void InvokeHealthChanged(NetworkBehaviourBuffer previous)
        {
            int prevValue = s_propertyReader.Read(previous);
            OnHealthChanged?.Invoke(prevValue, CurrentHealth);
        }
    }
}