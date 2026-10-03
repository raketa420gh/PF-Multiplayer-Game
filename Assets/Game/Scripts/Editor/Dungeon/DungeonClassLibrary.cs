using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Playable classes: base attributes, skill pools, spell lists, perk pools and squire kits.
    internal static class DungeonClassLibrary
    {
        public static ClassDef[] CreateClasses()
        {
            return new[]
            {
                new ClassDef
                {
                    Id = 0, Name = "Barbarian", Description = "Tough two-handed axe fighter. Shouts turn the tide, but he is slow and has only throwing axes at range.",
                    Stats = new ClassStats(22, 21, 9, 13, 11, 10), Color = new Color(0.85f, 0.35f, 0.2f), Body = new Color(0.8f, 0.58f, 0.45f),
                    Skills = new[]
                    {
                        Skill("Battle Roar", "Berserk fury: +10 rage (power, speed, but less armor) for 10s.", AbilityKind.Buff, 10f, 10f, 35f, "BR", new Color(1f, 0.4f, 0.2f), StatusEffectKind.Rage, 0.4f, icon: "Maw"),
                        Skill("Unyielding Shout", "Absorb 50 damage for 8s.", AbilityKind.Shield, 50f, 8f, 40f, "US", new Color(0.9f, 0.75f, 0.4f), StatusEffectKind.None, 0.4f, icon: "Shield")
                    },
                    Perks = new[]
                    {
                        new PerkDef("Thick Hide", "+20 armor rating and +10 magic resistance.", new StatModifier(StatType.ArmorRating, 20f), new StatModifier(StatType.MagicResistance, 10f)) { Icon = "Armor", Color = new Color(0.75f, 0.62f, 0.45f) },
                        new PerkDef("Giant's Constitution", "+20% max health.", new StatModifier(StatType.MaxHealth, 20f)) { Icon = "Heart", Color = new Color(0.9f, 0.27f, 0.27f) },
                        new PerkDef("Axe Mastery", "+8 physical power and +10% physical damage.", new StatModifier(StatType.PhysicalPower, 8f), new StatModifier(StatType.PhysicalDamageBonus, 0.1f)) { Icon = "Axe", Color = new Color(0.78f, 0.8f, 0.86f) },
                        new PerkDef("Bloodlust", "+12% action speed.", new StatModifier(StatType.ActionSpeed, 12f)) { Icon = "Blood", Color = new Color(0.86f, 0.14f, 0.17f) }
                    },
                    Kit = new[] { ("Battle Axe", EquipSlot.Weapon1Main, 1, true), ("Zweihander", EquipSlot.Weapon2Main, 1, true), ("Adventurer Tunic", EquipSlot.Chest, 1, true), ("Adventurer Boots", EquipSlot.Feet, 1, true), ("Francisca Axe", EquipSlot.Utility1, 2, true), ("Bandage", EquipSlot.Utility2, 3, true) },
                    Weapons = new[] { WeaponClass.Axe, WeaponClass.Sword, WeaponClass.Mace, WeaponClass.Spear, WeaponClass.Dagger, WeaponClass.Torch },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather, ArmorType.Chain }
                }
            };
        }

        private static AbilityDef Memory(string name, string description, string glyph, Color color)
        {
            return new AbilityDef { Name = name, Description = description, Kind = AbilityKind.SpellMemory, Cooldown = 0f, Glyph = glyph, Color = color };
        }

        private static AbilityDef Shapeshift(string name, string description, string glyph, Color color)
        {
            return new AbilityDef { Name = name, Description = description, Kind = AbilityKind.Shapeshift, Cooldown = 0f, Glyph = glyph, Color = color };
        }

        private static AbilityDef Spawn(string name, string description, float cooldown, string prefab, float distance, string glyph, Color color)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = AbilityKind.Spawn, Cooldown = cooldown, SpawnPrefab = prefab, Radius = distance,
                CastTime = 0.5f, Glyph = glyph, Color = color
            };
        }

        private static AbilityDef Skill(string name, string description, AbilityKind kind, float magnitude, float duration, float cooldown, string glyph,
            Color color, StatusEffectKind effect = StatusEffectKind.None, float castTime = 0.3f, int healthCost = 0, string icon = null)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = kind, Magnitude = magnitude, Duration = duration, Cooldown = cooldown, Glyph = glyph,
                Color = color, Effect = effect, CastTime = castTime, HealthCost = healthCost, Icon = icon
            };
        }

        private static AbilityDef Spell(string name, string description, AbilityKind kind, int charges, float castTime, float magnitude, float duration,
            string glyph, Color color, StatusEffectKind effect = StatusEffectKind.None, float cooldown = 1f, int healthCost = 0)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = kind, IsSpell = true, Charges = charges, CastTime = castTime, Magnitude = magnitude,
                Duration = duration, Glyph = glyph, Color = color, Effect = effect, Cooldown = cooldown, HealthCost = healthCost
            };
        }

        private static AbilityDef Projectile(string name, string description, bool isSpell, int charges, float cooldown, float castTime, float damage,
            float speed, float gravity, ProjectileKind kind, DamageType damageType, int count, float spread, string glyph, Color color,
            StatusEffectKind hitEffect = StatusEffectKind.None, float hitMagnitude = 0f, float hitDuration = 0f, float radius = 0f,
            float lifeSteal = 0f, int healthCost = 0, float stagger = 0.1f)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = AbilityKind.Projectile, IsSpell = isSpell, Charges = charges, Cooldown = cooldown,
                CastTime = castTime, Magnitude = damage, Speed = speed, Gravity = gravity, Projectile = kind, DamageType = damageType, Count = count,
                Spread = spread, Glyph = glyph, Color = color, HitEffect = hitEffect, HitMagnitude = hitMagnitude, HitDuration = hitDuration, Radius = radius,
                LifeSteal = lifeSteal, HealthCost = healthCost, Stagger = stagger
            };
        }

        private static AbilityDef Area(string name, string description, bool isSpell, int charges, float cooldown, float castTime, float damage, float radius,
            DamageType damageType, string glyph, Color color, int healthCost = 0, float stagger = 0.25f)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = AbilityKind.AreaDamage, IsSpell = isSpell, Charges = charges, Cooldown = cooldown,
                CastTime = castTime, Magnitude = damage, Radius = radius, DamageType = damageType, Glyph = glyph, Color = color, HealthCost = healthCost,
                Stagger = stagger
            };
        }
    }
}
