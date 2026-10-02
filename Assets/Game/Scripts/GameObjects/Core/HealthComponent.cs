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
            if (HasStateAuthority)
                CurrentHealth = MaxHealth;
        }

        public void Restore(int heal)
        {
            if (heal <= 0 || IsDead)
                return;

            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + heal);
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || IsDead)
                return;

            CurrentHealth = Math.Max(0, CurrentHealth - damage);
        }

        /// Changes the cap keeping the current health ratio (used when gear or buffs change).
        public void SetMaxHealth(int maxHealth, bool fill)
        {
            maxHealth = Math.Max(1, maxHealth);
            float ratio = MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 1f;
            MaxHealth = maxHealth;
            CurrentHealth = fill ? maxHealth : Math.Max(IsAlive ? 1 : 0, (int)Math.Round(ratio * maxHealth));
        }

        public void Kill()
        {
            CurrentHealth = 0;
        }

        private void InvokeHealthChanged(NetworkBehaviourBuffer previous)
        {
            int prevValue = s_propertyReader.Read(previous);
            OnHealthChanged?.Invoke(prevValue, CurrentHealth);
        }
    }
}
