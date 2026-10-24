using System;
using Fusion;

namespace Game.Scripts
{
    /// Dark and Darker health: taken damage leaves a recoverable part of the lost health that only bandages and rest bring back.
    public sealed class HealthComponent : NetworkBehaviour
    {
        public const float DamageRecoverRatio = 0.65f;
        public const float ZoneRecoverRatio = 1f;

        public delegate void HealthChangedHandler(int previous, int current);

        private static PropertyReader<int> s_propertyReader =
                GetPropertyReader<int>(typeof(HealthComponent), nameof(CurrentHealth));
        public event HealthChangedHandler OnHealthChanged;

        [Networked, OnChangedRender(nameof(InvokeHealthChanged))]
        public int CurrentHealth { get; set; } = 100;

        [Networked]
        public int MaxHealth { get; set; } = 100;

        [Networked]
        public int RecoverableHealth { get; set; }

        public bool IsDead => CurrentHealth <= 0;
        public bool IsAlive => CurrentHealth > 0;
        public bool IsNotFull => CurrentHealth < MaxHealth && CurrentHealth > 0;
        public float Progress => (float) CurrentHealth / MaxHealth;
        public float RecoverableProgress => (float) (CurrentHealth + RecoverableHealth) / MaxHealth;

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
            ClampRecoverable();
        }

        /// Bandage and rest: turns the recoverable part back into health one to one, never past it.
        public void Recover(int heal)
        {
            if (heal <= 0 || IsDead)
                return;

            int recovered = Math.Min(heal, RecoverableHealth);
            CurrentHealth += recovered;
            RecoverableHealth -= recovered;
        }

        public void TakeDamage(int damage, float recoverRatio = DamageRecoverRatio)
        {
            if (damage <= 0 || IsDead)
                return;

            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            RecoverableHealth = IsDead ? 0 : Math.Min(RecoverableHealth + (int)Math.Round(damage * recoverRatio), MaxHealth - CurrentHealth);
        }

        /// Changes the cap keeping the current health ratio (used when gear or buffs change).
        public void SetMaxHealth(int maxHealth, bool fill)
        {
            maxHealth = Math.Max(1, maxHealth);
            float ratio = MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 1f;
            MaxHealth = maxHealth;
            CurrentHealth = fill ? maxHealth : Math.Max(IsAlive ? 1 : 0, (int)Math.Round(ratio * maxHealth));
            ClampRecoverable();
        }

        public void Kill()
        {
            CurrentHealth = 0;
            RecoverableHealth = 0;
        }

        private void ClampRecoverable()
        {
            RecoverableHealth = IsDead ? 0 : Math.Min(RecoverableHealth, MaxHealth - CurrentHealth);
        }

        private void InvokeHealthChanged(NetworkBehaviourBuffer previous)
        {
            int prevValue = s_propertyReader.Read(previous);
            OnHealthChanged?.Invoke(prevValue, CurrentHealth);
        }
    }
}
