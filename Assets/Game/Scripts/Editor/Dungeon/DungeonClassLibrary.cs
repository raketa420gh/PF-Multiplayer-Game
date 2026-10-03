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
                    Id = 0, Name = "Cleric", Description = "Holy healer in heavy armor. Bane of the undead.",
                    Stats = new ClassStats(16, 13, 12, 12, 15, 22), Color = new Color(0.95f, 0.9f, 0.6f), Body = new Color(0.86f, 0.64f, 0.5f),
                    Skills = new[]
                    {
                        Memory("Spell Memory", "Hold to open the spell wheel, release to ready a spell. Cast with RMB.", "SM", new Color(1f, 0.95f, 0.7f)),
                        Skill("Divine Protection", "Absorb 40 damage for 6s.", AbilityKind.Shield, 40f, 6f, 45f, "DP", new Color(1f, 0.95f, 0.6f)),
                        Area("Holy Purification", "100 divine damage to everything within 7.5m.", false, 1, 45f, 0.5f, 100f, 7.5f, DamageType.Magical, "HP", new Color(1f, 0.9f, 0.4f)),
                        Projectile("Judgement", "25 divine damage and a slow.", false, 1, 28f, 1.25f, 25f, 34f, 0f, ProjectileKind.Holy, DamageType.Magical, 1, 0f, "Ju", new Color(1f, 0.85f, 0.5f), StatusEffectKind.Slow, 30f, 2f),
                        Skill("Smite", "+10 physical power for 12s.", AbilityKind.Buff, 10f, 12f, 18f, "Sm", new Color(1f, 0.8f, 0.3f), StatusEffectKind.Power),
                        Skill("Confession", "Heal 10 instantly.", AbilityKind.Heal, 10f, 0f, 12f, "Co", new Color(0.8f, 1f, 0.8f))
                    },
                    Spells = new[]
                    {
                        Spell("Bless", "+2 grip for 30s.", AbilityKind.Buff, 5, 0.75f, 2f, 30f, "Bl", new Color(1f, 1f, 0.7f), StatusEffectKind.Grip),
                        Spell("Protection", "Absorb 20 damage for 8s.", AbilityKind.Shield, 5, 0.75f, 20f, 8f, "Pr", new Color(0.8f, 0.9f, 1f)),
                        Spell("Lesser Heal", "Heal 20.", AbilityKind.Heal, 4, 1.25f, 20f, 0f, "LH", new Color(0.6f, 1f, 0.6f)),
                        Projectile("Holy Strike", "20 divine damage.", true, 4, 1f, 2f, 20f, 30f, 0f, ProjectileKind.Holy, DamageType.Magical, 1, 0f, "HS", new Color(1f, 0.95f, 0.5f)),
                        Spell("Holy Light", "Heal 35.", AbilityKind.Heal, 3, 1.75f, 35f, 0f, "HL", new Color(1f, 1f, 0.8f)),
                        Spell("Sanctuary", "Heal 25 over 5s.", AbilityKind.Heal, 2, 2.25f, 25f, 5f, "Sa", new Color(0.9f, 1f, 0.9f)),
                        Projectile("Bind", "Roots the target for a moment.", true, 4, 1f, 1f, 1f, 36f, 0f, ProjectileKind.Holy, DamageType.Magical, 1, 0f, "Bi", new Color(1f, 0.9f, 0.7f), StatusEffectKind.Slow, 95f, 0.75f),
                        Spell("Divine Strike", "+5 physical power for 12s.", AbilityKind.Buff, 4, 0.75f, 5f, 12f, "DS", new Color(1f, 0.85f, 0.4f), StatusEffectKind.Power)
                    },
                    Perks = new[]
                    {
                        new PerkDef("Advanced Healer", "+4 magical power.", new StatModifier(StatType.MagicalPower, 4f)),
                        new PerkDef("Holy Aura", "+15 armor and magic resistance.", new StatModifier(StatType.ArmorRating, 15f), new StatModifier(StatType.MagicResistance, 15f)),
                        new PerkDef("Blunt Weapon Mastery", "+10 physical power.", new StatModifier(StatType.PhysicalPower, 10f)),
                        new PerkDef("Perseverance", "+10 armor rating.", new StatModifier(StatType.ArmorRating, 10f)),
                        new PerkDef("Kindness", "+5% max health.", new StatModifier(StatType.MaxHealth, 5f)),
                        new PerkDef("Undead Slaying", "+10% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.1f)),
                        new PerkDef("Requiem", "+2 resonance.", new StatModifier(StatType.Resonance, 2f)),
                        new PerkDef("Protection from Evil", "+30 magic resistance.", new StatModifier(StatType.MagicResistance, 30f))
                    },
                    Kit = new[] { ("Flanged Mace", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Spellbook", EquipSlot.Weapon2Main, 1, true), ("Kettle Hat", EquipSlot.Head, 1, true), ("Adventurer Tunic", EquipSlot.Chest, 1, true), ("Plate Boots", EquipSlot.Feet, 1, true), ("Bandage", EquipSlot.Utility1, 3, true) },
                    Weapons = new[] { WeaponClass.Mace, WeaponClass.Staff, WeaponClass.Shield, WeaponClass.Torch, WeaponClass.Spellbook, WeaponClass.CrystalBall },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather, ArmorType.Chain, ArmorType.Plate }
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
            Color color, StatusEffectKind effect = StatusEffectKind.None, float castTime = 0.3f, int healthCost = 0)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = kind, Magnitude = magnitude, Duration = duration, Cooldown = cooldown, Glyph = glyph,
                Color = color, Effect = effect, CastTime = castTime, HealthCost = healthCost
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
