using System;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum StatusEffectKind : byte
    {
        None,
        HealOverTime,
        Protection,
        Power,
        Haste,
        ActionSpeed,
        Slow,
        Burn,
        Invisible,
        Rage,
        Strength
    }

    public struct StatusEffect : INetworkStruct
    {
        public StatusEffectKind Kind;
        public float Magnitude;
        public float Remaining;
        public float Duration;

        public bool IsActive => Kind != StatusEffectKind.None && Remaining > 0f;
    }

    /// Timed buffs and debuffs. Heal-over-time and burn tick every simulation step on the state authority.
    public sealed class StatusEffectComponent : NetworkBehaviour
    {
        public const int Capacity = 8;

        public NetworkArray<StatusEffect> Effects => _effects;

        [SerializeField]
        private HealthComponent _health;

        [Networked, Capacity(Capacity)]
        private NetworkArray<StatusEffect> _effects => default;

        private float _healAccumulator;
        private float _burnAccumulator;

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            float deltaTime = Runner.DeltaTime;

            for (int i = 0; i < Capacity; i++)
            {
                StatusEffect effect = _effects[i];

                if (!effect.IsActive)
                    continue;

                Tick(effect, deltaTime);
                effect.Remaining -= deltaTime;
                _effects.Set(i, effect.Remaining > 0f ? effect : default);
            }

            Flush();
        }

        public void Add(StatusEffectKind kind, float magnitude, float duration)
        {
            int slot = -1;

            for (int i = 0; i < Capacity; i++)
            {
                StatusEffect effect = _effects[i];

                if (effect.Kind == kind && effect.IsActive)
                {
                    slot = i;

                    break;
                }

                if (slot < 0 && !effect.IsActive)
                    slot = i;
            }

            if (slot < 0)
                slot = 0;

            _effects.Set(slot, new StatusEffect { Kind = kind, Magnitude = magnitude, Remaining = duration, Duration = duration });
        }

        public void ClearAll()
        {
            for (int i = 0; i < Capacity; i++)
                _effects.Set(i, default);
        }

        public bool Has(StatusEffectKind kind)
        {
            return GetMagnitude(kind) != 0f || Find(kind) >= 0;
        }

        public float GetMagnitude(StatusEffectKind kind)
        {
            int index = Find(kind);

            return index >= 0 ? _effects[index].Magnitude : 0f;
        }

        public float GetRemaining(StatusEffectKind kind)
        {
            int index = Find(kind);

            return index >= 0 ? _effects[index].Remaining : 0f;
        }

        /// Protection absorbs incoming damage; returns the damage left after the shield.
        public int Absorb(int damage)
        {
            int index = Find(StatusEffectKind.Protection);

            if (index < 0)
                return damage;

            StatusEffect shield = _effects[index];
            int absorbed = Mathf.Min(damage, Mathf.RoundToInt(shield.Magnitude));
            shield.Magnitude -= absorbed;
            _effects.Set(index, shield.Magnitude > 0.5f ? shield : default);

            return damage - absorbed;
        }

        private int Find(StatusEffectKind kind)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (_effects[i].Kind == kind && _effects[i].IsActive)
                    return i;
            }

            return -1;
        }

        private void Tick(in StatusEffect effect, float deltaTime)
        {
            switch (effect.Kind)
            {
                case StatusEffectKind.HealOverTime:
                    _healAccumulator += effect.Magnitude / effect.Duration * deltaTime;
                    break;
                case StatusEffectKind.Burn:
                    _burnAccumulator += effect.Magnitude / effect.Duration * deltaTime;
                    break;
            }
        }

        private void Flush()
        {
            if (_healAccumulator >= 1f)
            {
                int heal = Mathf.FloorToInt(_healAccumulator);
                _healAccumulator -= heal;
                _health.Restore(heal);
            }

            if (_burnAccumulator >= 1f)
            {
                int burn = Mathf.FloorToInt(_burnAccumulator);
                _burnAccumulator -= burn;
                _health.TakeDamage(burn);
            }
        }
    }
}
