using System;
using Fusion;

namespace Game.Scripts
{
    public sealed class HealthComponent : NetworkBehaviour
    {
        [Networked] 
        public int CurrentHealth { get; set; } = 100;

        [Networked]
        public int MaxHealth { get; set; } = 100;
        
        public bool IsDead => CurrentHealth <= 0;
        public bool IsAlive => CurrentHealth > 0;
        public bool IsNotFull => CurrentHealth < MaxHealth && CurrentHealth > 0;

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
    }
}