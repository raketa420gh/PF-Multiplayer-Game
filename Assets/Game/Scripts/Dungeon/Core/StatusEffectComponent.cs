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
        Strength,
        Fortify,
        Rupture,
        Taunt,
        ArmorPenetration,
        /// Weapon enchants: every weapon hit adds magical damage and burns or chills the victim.
        FireWeapon,
        FrostWeapon,
        /// The next spell is cast instantly.
        QuickCast,
        /// Weapon enchant of light: every weapon hit adds magical damage, nothing else.
        HolyWeapon,
        /// Percent less damage taken from every hit.
        Guard,
        /// Percent of weapon damage dealt comes back as health.
        Siphon,
        /// No spells can be cast.
        Silence,
        /// The next weapon hit on a body adds the magnitude as physical damage.
        Empower,
        /// The next weapon hit, a blocked one too, adds the magnitude as physical damage past the block and staggers.
        Sunder,
        /// The next weapon hit on a body adds the magnitude as physical damage and silences the victim.
        Verdict,
        /// The next shot of a bow or a crossbow looses this many projectiles in a fan.
        Volley,
        /// Percent faster reloads of bows and crossbows.
        QuickReload,
        /// The next shot flies straight and fast and adds the magnitude as physical damage.
        AimedShot,
        /// Damage over time from venom: the magnitude over the whole duration.
        Poison,
        /// Cannot move a step.
        Root,
        /// Percent more damage taken from every hit.
        Vulnerable,
        /// Percent less damage dealt.
        Weakened,
        /// Every attribute lowered by the magnitude.
        Cursed,
        /// Marked by a tracker: percent more damage taken from every hit.
        Marked,
        Agility,
        Dexterity,
        Vitality,
        Spirit,
        Knowledge,
        /// Every attribute raised by the magnitude.
        AllAttributes,
        /// Flat armor rating.
        Armor,
        /// Percent more armor rating.
        ArmorPercent,
        /// Percent faster spell and prayer casting.
        CastSpeed,
        /// Percent more damage dealt.
        DamageBonus,
        /// Percent faster drinking and bandaging.
        UseSpeed,
        /// A small light floats over the head.
        Lightbearer,
        /// Enemy silhouettes are seen through walls within the magnitude in metres.
        Tracking,
        /// No damage from anything in front.
        Bulwark,
        /// Strength raised by the magnitude; when it runs out the body pays with Sluggish.
        BullBlood,
        /// Agility lowered by the magnitude.
        Sluggish,
        /// Percent faster actions, bought with health.
        Mercury,
        /// Loses the magnitude in health every second, never below one.
        Drain,
        /// Every weapon hit poisons the victim for the magnitude in seconds.
        VenomBlade,
        /// The next weapon hit deals the magnitude in percent more damage.
        HeavyBlow,
        /// The next weapon hit ignores armor and deals the magnitude in percent more damage.
        Expose,
        /// The next weapon hit on a body below 40% health deals the magnitude times the damage.
        Execute,
        /// The next weapon hit poisons hard: the magnitude over 6 s and a slow.
        DeadRoot,
        /// The next weapon hit stuns for the magnitude in seconds.
        Judgement,
        /// The next shot deals the magnitude in percent more damage.
        Exhale,
        /// The next shot ignores armor.
        PiercingShot,
        /// The next shot slows by the magnitude in percent.
        PinningShot,
        /// The next shot burns where it lands and lights the place up.
        FlareShot,
        /// The next thrown knife flies as a fan of this many.
        KnifeFan,
        /// The attacks after a blocked hit are the magnitude in percent faster.
        Retaliation,
        /// Hits in a row on one body: percent faster actions.
        Onslaught,
        /// The next weapon hit deals the magnitude in percent more damage (righteous wrath).
        Wrath,
        /// Percent less healing received.
        Festering
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
        /// Scales the duration of an incoming effect; 0 makes the owner immune to it.
        public interface IResistance
        {
            float GetDurationScale(StatusEffectKind kind);
            /// Scales the damage of a damage-over-time effect.
            float GetDamageScale(StatusEffectKind kind);
        }

        public const int Capacity = 16;
        /// A drain never takes the last point of health.
        private const int DrainFloor = 1;

        public NetworkArray<StatusEffect> Effects => _effects;

        [SerializeField]
        private HealthComponent _health;

        [Networked, Capacity(Capacity)]
        private NetworkArray<StatusEffect> _effects => default;

        private IResistance _resistance;
        private float _healAccumulator;
        private float _burnAccumulator;
        private float _drainAccumulator;

        /// State authority only: an effect ran out on its own (not removed, not replaced).
        public event Action<StatusEffectKind, float> OnExpired;

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

                if (effect.Remaining <= 0f)
                    OnExpired?.Invoke(effect.Kind, effect.Magnitude);
            }

            Flush();
        }

        public void SetResistance(IResistance resistance)
        {
            _resistance = resistance;
        }

        public void Add(StatusEffectKind kind, float magnitude, float duration)
        {
            float scale = _resistance?.GetDurationScale(kind) ?? 1f;

            if (scale <= 0f)
                return;

            duration *= scale;

            // Burn and poison deal their magnitude over the whole duration: a shorter one is a weaker one.
            if (kind is StatusEffectKind.Burn or StatusEffectKind.Poison)
                magnitude *= scale * (_resistance?.GetDamageScale(kind) ?? 1f);

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

        public void Remove(StatusEffectKind kind)
        {
            int index = Find(kind);

            if (index >= 0)
                _effects.Set(index, default);
        }

        public void ClearAll()
        {
            for (int i = 0; i < Capacity; i++)
                _effects.Set(i, default);
        }

        /// Changes when an effect starts, ends or changes magnitude; remaining time is ignored so stats are not rebuilt every tick.
        public int GetSignature()
        {
            int hash = 17;

            for (int i = 0; i < Capacity; i++)
            {
                StatusEffect effect = _effects[i];

                if (effect.IsActive)
                    hash = hash * 31 + ((int)effect.Kind ^ effect.Magnitude.GetHashCode());
            }

            return hash;
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

        /// Festering wounds take a share of every heal.
        public int ScaleHealing(int heal)
        {
            return Mathf.RoundToInt(heal * Mathf.Clamp01(1f - GetMagnitude(StatusEffectKind.Festering) / 100f));
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
                case StatusEffectKind.Burn or StatusEffectKind.Poison:
                    _burnAccumulator += effect.Magnitude / effect.Duration * deltaTime;
                    break;
                case StatusEffectKind.Drain:
                    _drainAccumulator += effect.Magnitude * deltaTime;
                    break;
            }
        }

        private void Flush()
        {
            if (_healAccumulator >= 1f)
            {
                int heal = Mathf.FloorToInt(_healAccumulator);
                _healAccumulator -= heal;
                _health.Restore(ScaleHealing(heal));
            }

            if (_burnAccumulator >= 1f)
            {
                int burn = Mathf.FloorToInt(_burnAccumulator);
                _burnAccumulator -= burn;
                _health.TakeDamage(burn);
            }

            if (_drainAccumulator >= 1f)
            {
                int drain = Mathf.Min(Mathf.FloorToInt(_drainAccumulator), _health.CurrentHealth - DrainFloor);
                _drainAccumulator -= Mathf.FloorToInt(_drainAccumulator);

                if (drain > 0)
                    _health.TakeDamage(drain);
            }
        }
    }
}
