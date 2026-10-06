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
                    Stats = new ClassStats(21, 22, 8, 9, 15, 15), Color = new Color(0.85f, 0.35f, 0.2f), Body = Color.white,
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
                    Kit = new[] { ("Battle Axe", EquipSlot.Weapon1Main, 1, true), ("Zweihander", EquipSlot.Weapon2Main, 1, true), ("Peasant Trousers", EquipSlot.Legs, 1, true), ("Peasant Boots", EquipSlot.Feet, 1, true), ("Francisca Axe", EquipSlot.Utility1, 2, true), ("Bandage", EquipSlot.Utility2, 3, true) },
                    Weapons = new[] { WeaponClass.Axe, WeaponClass.Sword, WeaponClass.Mace, WeaponClass.Spear, WeaponClass.Dagger, WeaponClass.Torch }
                },
                CreateWizard(),
                CreateSellsword(),
                CreateChaplain()
            };
        }

        /// Magical damage from two spell wheels (or one wheel and an active skill), cast with a staff, a spellbook or a crystal
        /// ball. Spells have charges that come back only by a campfire. Paper-thin against weapons: little health.
        private static ClassDef CreateWizard()
        {
            Color frost = new Color(0.55f, 0.85f, 1f);
            Color fire = new Color(1f, 0.45f, 0.12f);
            Color storm = new Color(0.7f, 0.75f, 1f);
            Color arcane = new Color(0.65f, 0.45f, 1f);

            return new ClassDef
            {
                Id = 1, Name = "Wizard", Description = "Master of spells: frost, fire and lightning from two spell wheels. Spells run out and come back only by a campfire. Fragile in melee.",
                Stats = new ClassStats(8, 10, 23, 21, 15, 13), Color = arcane, Body = Color.white,
                Skills = new[]
                {
                    Memory("Spell Memory I", "Hold to open the first spell wheel and ready a spell; hold RMB with a magical focus in hand and release to cast. The centre of the wheel returns RMB to the weapon.", "M1", arcane, 0),
                    Memory("Spell Memory II", "Hold to open the second spell wheel. Take both memories for ten spells, or one and an active skill.", "M2", arcane, 1),
                    Skill("Arcane Shield", "A barrier absorbs 35 damage for 6s.", AbilityKind.Shield, 35f, 6f, 30f, "AS", arcane, castTime: 0.3f, icon: "Shield"),
                    Skill("Quick Chant", "The next spell within 15s is cast instantly.", AbilityKind.QuickCast, 0f, 15f, 35f, "QC", arcane, castTime: 0.2f, icon: "Hourglass")
                },
                Spells = new[]
                {
                    Projectile("Ice Bolt", "Shard of ice: 20 magical damage and 30% slow for 2s.", true, 6, 0.8f, 0.6f, 20f, 32f, 0f, ProjectileKind.Ice, DamageType.Magical,
                        1, 0f, "IB", frost, StatusEffectKind.Slow, 30f, 2f, icon: "Snowflake"),
                    Projectile("Fireball", "Explodes for up to 30 magical damage around the impact and sets the struck ablaze.", true, 3, 1f, 1.1f, 30f, 22f, 0f,
                        ProjectileKind.Fire, DamageType.Magical, 1, 0f, "FB", fire, StatusEffectKind.Burn, 12f, 4f, 2.5f, stagger: 0.3f, icon: "Flame"),
                    Enchant("Ignite Weapon", "Sets the weapon of the ally you aim at on fire for 30s (your own when you miss): +5 magical damage per hit and a burn.",
                        StatusEffectKind.FireWeapon, "IW", fire, "Flame"),
                    Enchant("Frost Weapon", "Chills the weapon of the ally you aim at for 30s (your own when you miss): +5 magical damage per hit and a slow.",
                        StatusEffectKind.FrostWeapon, "FW", frost, "Snowflake"),
                    new AbilityDef
                    {
                        Name = "Chain Lightning", Description = "Hitscan bolt: 18 magical damage, then jumps to two more bodies within 6m, weaker each time. Careful, it hits allies too.",
                        Kind = AbilityKind.ChainLightning, IsSpell = true, Charges = 4, Cooldown = 1f, CastTime = 0.9f, Magnitude = 18f, Radius = 6f, Count = 2,
                        Stagger = 0.15f, Glyph = "CL", Color = storm, Icon = "Lightning"
                    },
                    new AbilityDef
                    {
                        Name = "Lightning Strike", Description = "Hitscan: the body under the crosshair is struck by lightning from the sky for 40 magical damage and a stagger.",
                        Kind = AbilityKind.LightningStrike, IsSpell = true, Charges = 2, Cooldown = 1f, CastTime = 1.4f, Magnitude = 40f, Stagger = 0.6f, Glyph = "LS",
                        Color = storm, Icon = "Lightning"
                    },
                    new AbilityDef
                    {
                        Name = "Blink", Description = "Rush 7m along your view in a blur; the first wall or body stops you.", Kind = AbilityKind.Blink, IsSpell = true,
                        Charges = 3, Cooldown = 1f, CastTime = 0.25f, Magnitude = 7f, Duration = 0.18f, Glyph = "Bl", Color = arcane, Icon = "Dash"
                    },
                    Spell("Haste", "+20% move speed for 20s for the ally you aim at (for you when you miss).", AbilityKind.Buff, 2, 0.8f, 20f, 20f, "Hs", new Color(0.6f, 1f, 0.6f), StatusEffectKind.Haste, icon: "Chevrons"),
                    Projectile("Magic Missile", "Five arcane darts in a fan, 7 magical damage each.", true, 5, 1f, 0.7f, 7f, 34f, 0f, ProjectileKind.Magic, DamageType.Magical,
                        5, 5f, "MM", arcane, stagger: 0.05f, icon: "Bolt"),
                    Spell("Invisibility", "Fade from sight for 8s.", AbilityKind.Invisibility, 1, 1.2f, 0f, 8f, "In", new Color(0.75f, 0.75f, 0.85f), icon: "Eye")
                },
                Perks = new[]
                {
                    new PerkDef("Arcane Mastery", "+6 magical power and +10% magical damage.", new StatModifier(StatType.MagicalPower, 6f), new StatModifier(StatType.MagicalDamageBonus, 0.1f)) { Icon = "Burst" },
                    new PerkDef("Sage", "+10 Knowledge: more charges for every spell, faster casting.", new StatModifier(StatType.Knowledge, 10f)) { Icon = "Book" },
                    new PerkDef("Spell Ward", "+30 magic resistance.", new StatModifier(StatType.MagicResistance, 30f)) { Icon = "Shield" },
                    new PerkDef("Focused Mind", "+8 Spirit: stronger spells and healing.", new StatModifier(StatType.Spirit, 8f)) { Icon = "Eye" }
                },
                Kit = new[] { ("Spellbook", EquipSlot.Weapon1Main, 1, true), ("Magic Staff", EquipSlot.Weapon2Main, 1, true), ("Peasant Hood", EquipSlot.Head, 1, true),
                    ("Peasant Shirt", EquipSlot.Chest, 1, true), ("Peasant Trousers", EquipSlot.Legs, 1, true), ("Peasant Boots", EquipSlot.Feet, 1, true), ("Campfire Kit", EquipSlot.Utility1, 1, true), ("Bandage", EquipSlot.Utility2, 2, true) },
                Weapons = new[] { WeaponClass.Staff, WeaponClass.Spellbook, WeaponClass.CrystalBall, WeaponClass.Dagger, WeaponClass.Sword },
                Focus = CastFocus.Magic
            };
        }

        /// Weapon generalist with no magic: every weapon but the magical foci, shields and the heaviest armour. Perks and gear
        /// make a wall, a hard hitter or quick hands of him.
        private static ClassDef CreateSellsword()
        {
            Color steel = new Color(0.62f, 0.72f, 0.86f);

            return new ClassDef
            {
                Id = 2, Name = "Sellsword", Description = "Hired blade at home with any weapon but a magical one. Plate and a shield make a wall of him, perks a hard hitter or quick hands. No magic at all.",
                Stats = new ClassStats(18, 18, 9, 10, 17, 18), Color = steel, Body = Color.white,
                Skills = new[]
                {
                    new AbilityDef
                    {
                        Name = "Rally", Description = "Catch your breath: restore 40 health over 8s.", Kind = AbilityKind.Heal, Magnitude = 40f, Duration = 8f,
                        Cooldown = 45f, CastTime = 0.3f, DamageType = DamageType.Physical, Glyph = "Ra", Color = new Color(0.9f, 0.3f, 0.3f), Icon = "Heart"
                    },
                    Skill("Onslaught", "+20% action speed for 8s.", AbilityKind.Buff, 20f, 8f, 35f, "On", steel, StatusEffectKind.ActionSpeed, 0.2f, icon: "Sword")
                },
                Perks = new[]
                {
                    new PerkDef("Bulwark", "+35 armor rating and +10% max health.", new StatModifier(StatType.ArmorRating, 35f), new StatModifier(StatType.MaxHealth, 10f)) { Icon = "Shield" },
                    new PerkDef("Weapon Drill", "+6 physical power and +8% physical damage.", new StatModifier(StatType.PhysicalPower, 6f), new StatModifier(StatType.PhysicalDamageBonus, 0.08f)) { Icon = "Sword", Color = new Color(0.9f, 0.6f, 0.35f) },
                    new PerkDef("Fleet Footwork", "+14 move speed: armour weighs less on the feet.", new StatModifier(StatType.MoveSpeed, 14f)) { Icon = "Dash", Color = new Color(0.6f, 0.9f, 0.6f) },
                    new PerkDef("Quick Hands", "+10% action speed.", new StatModifier(StatType.ActionSpeed, 10f)) { Icon = "Bolt", Color = new Color(0.95f, 0.85f, 0.4f) }
                },
                Kit = new[] { ("Arming Sword", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Longsword", EquipSlot.Weapon2Main, 1, true),
                    ("Peasant Shirt", EquipSlot.Chest, 1, true), ("Peasant Trousers", EquipSlot.Legs, 1, true), ("Peasant Boots", EquipSlot.Feet, 1, true), ("Bandage", EquipSlot.Utility1, 3, true) },
                Weapons = new[] { WeaponClass.Sword, WeaponClass.Axe, WeaponClass.Mace, WeaponClass.Dagger, WeaponClass.Spear, WeaponClass.Bow, WeaponClass.Crossbow,
                    WeaponClass.Staff, WeaponClass.Shield, WeaponClass.Torch }
            };
        }

        /// War priest: blunt weapons, a shield and one wheel of prayers of light cast with a spellbook or a magic staff. Spirit
        /// and cloth make a healer; a mace, some plate and the two weapon skills a slow front-liner who needs no prayer memory.
        private static ClassDef CreateChaplain()
        {
            Color light = new Color(1f, 0.9f, 0.55f);
            Color dawn = new Color(1f, 0.7f, 0.3f);
            Color mending = new Color(0.6f, 1f, 0.65f);

            return new ClassDef
            {
                Id = 3, Name = "Chaplain", Description = "War priest of the dawn: maces, a shield and prayers of light that heal, ward and smite. A frail healer in cloth or a slow wall in plate, as the build goes.",
                Stats = new ClassStats(15, 14, 21, 16, 12, 12), Color = light, Body = Color.white,
                Skills = new[]
                {
                    Memory("Prayer Memory", "Hold to open the prayer wheel and ready a prayer; hold RMB with a spellbook or a magic staff in hand and release to cast. The centre of the wheel returns RMB to the weapon.", "PM", light),
                    new AbilityDef
                    {
                        Name = "Hallow Weapon", Description = "Light on your weapon, or on the weapon of the ally you aim at, for 12s: +8 magical damage per hit.",
                        Kind = AbilityKind.WeaponEnchant, Cooldown = 35f, CastTime = 0.4f, Magnitude = 8f, Duration = 12f, Effect = StatusEffectKind.HolyWeapon,
                        Glyph = "HW", Color = light, Icon = "Mace"
                    },
                    new AbilityDef
                    {
                        Name = "Rebuke", Description = "Burst of light: 25 magical damage and a stagger to everyone within 4.5m, allies too.", Kind = AbilityKind.AreaDamage,
                        Cooldown = 30f, CastTime = 0.5f, Magnitude = 25f, Radius = 4.5f, Stagger = 0.5f, Glyph = "Rb", Color = dawn, Icon = "Burst"
                    }
                },
                Spells = new[]
                {
                    Spell("Mending Prayer", "Heals the ally you aim at (yourself when you miss) for 22.", AbilityKind.Heal, 4, 1.2f, 22f, 0f, "MP", mending, icon: "Cross"),
                    Spell("Aegis", "Shield of light on the ally you aim at (on you when you miss): absorbs 25 damage for 15s.", AbilityKind.Shield, 3, 0.8f, 25f, 15f, "Ae", light, icon: "Shield"),
                    Spell("Benediction", "Blesses the ally you aim at (yourself when you miss): +5 Strength for 30s.", AbilityKind.Buff, 3, 0.8f, 5f, 30f, "Be", light,
                        StatusEffectKind.Strength, icon: "Chevrons"),
                    Projectile("Sunlance", "Lance of light: 22 magical damage and a stagger.", true, 4, 1f, 0.9f, 22f, 30f, 0f, ProjectileKind.Holy, DamageType.Magical,
                        1, 0f, "SL", dawn, stagger: 0.25f, icon: "Bolt"),
                    new AbilityDef
                    {
                        Name = "Circle of Dawn", Description = "Everyone within 5m, you included, heals 30 over 6s.", Kind = AbilityKind.AreaHeal, IsSpell = true,
                        Charges = 2, Cooldown = 1f, CastTime = 1.5f, Magnitude = 30f, Duration = 6f, Radius = 5f, Glyph = "CD", Color = mending, Icon = "Halo"
                    }
                },
                Perks = new[]
                {
                    new PerkDef("Devotion", "+6 Spirit: stronger prayers and healing.", new StatModifier(StatType.Spirit, 6f)) { Icon = "Halo" },
                    new PerkDef("Litany", "+6 Knowledge: one more charge of every prayer, faster casting.", new StatModifier(StatType.Knowledge, 6f)) { Icon = "Book" },
                    new PerkDef("Zealot", "+6 physical power and +8% physical damage.", new StatModifier(StatType.PhysicalPower, 6f), new StatModifier(StatType.PhysicalDamageBonus, 0.08f)) { Icon = "Mace", Color = new Color(0.9f, 0.6f, 0.35f) },
                    new PerkDef("Iron Vow", "+20 armor rating and +15 magic resistance.", new StatModifier(StatType.ArmorRating, 20f), new StatModifier(StatType.MagicResistance, 15f)) { Icon = "Armor", Color = new Color(0.75f, 0.8f, 0.9f) }
                },
                Kit = new[] { ("Flanged Mace", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Spellbook", EquipSlot.Weapon2Main, 1, true),
                    ("Peasant Shirt", EquipSlot.Chest, 1, true), ("Peasant Trousers", EquipSlot.Legs, 1, true), ("Peasant Boots", EquipSlot.Feet, 1, true), ("Campfire Kit", EquipSlot.Utility1, 1, true), ("Bandage", EquipSlot.Utility2, 2, true) },
                Weapons = new[] { WeaponClass.Mace, WeaponClass.Staff, WeaponClass.Spellbook, WeaponClass.Shield, WeaponClass.Torch },
                Focus = CastFocus.Magic
            };
        }

        /// Spell memory skills store the wheel they open in the magnitude.
        private static AbilityDef Memory(string name, string description, string glyph, Color color, int wheel = 0)
        {
            return new AbilityDef { Name = name, Description = description, Kind = AbilityKind.SpellMemory, Cooldown = 0f, Glyph = glyph, Color = color, Magnitude = wheel };
        }

        /// Range of the ally search is fixed in AdventurerComponent; magnitude is the bonus damage per hit before magical power.
        private static AbilityDef Enchant(string name, string description, StatusEffectKind effect, string glyph, Color color, string icon)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = AbilityKind.WeaponEnchant, IsSpell = true, Charges = 2, Cooldown = 1f, CastTime = 1f, Magnitude = 5f,
                Duration = 30f, Effect = effect, Glyph = glyph, Color = color, Icon = icon
            };
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
            string glyph, Color color, StatusEffectKind effect = StatusEffectKind.None, float cooldown = 1f, int healthCost = 0, string icon = null)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = kind, IsSpell = true, Charges = charges, CastTime = castTime, Magnitude = magnitude,
                Duration = duration, Glyph = glyph, Color = color, Effect = effect, Cooldown = cooldown, HealthCost = healthCost, Icon = icon
            };
        }

        private static AbilityDef Projectile(string name, string description, bool isSpell, int charges, float cooldown, float castTime, float damage,
            float speed, float gravity, ProjectileKind kind, DamageType damageType, int count, float spread, string glyph, Color color,
            StatusEffectKind hitEffect = StatusEffectKind.None, float hitMagnitude = 0f, float hitDuration = 0f, float radius = 0f,
            float lifeSteal = 0f, int healthCost = 0, float stagger = 0.1f, string icon = null)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = AbilityKind.Projectile, IsSpell = isSpell, Charges = charges, Cooldown = cooldown,
                CastTime = castTime, Magnitude = damage, Speed = speed, Gravity = gravity, Projectile = kind, DamageType = damageType, Count = count,
                Spread = spread, Glyph = glyph, Color = color, HitEffect = hitEffect, HitMagnitude = hitMagnitude, HitDuration = hitDuration, Radius = radius,
                LifeSteal = lifeSteal, HealthCost = healthCost, Stagger = stagger, Icon = icon
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
