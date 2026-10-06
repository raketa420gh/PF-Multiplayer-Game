using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Playable classes: base attributes, subclasses, skill pools, spell lists, perk pools and squire kits. Shared skills come
    /// first in every pool, so the default build (skills 0 and 1) fits any subclass.
    internal static class DungeonClassLibrary
    {
        public static ClassDef[] CreateClasses()
        {
            return new[]
            {
                CreateBarbarian(),
                CreateWizard(),
                CreateWarrior(),
                CreateConfessor()
            };
        }

        /// Two-handed axes and shouts. Berserker feeds on the fight, Juggernaut soaks it, Warchief leads the party through it.
        private static ClassDef CreateBarbarian()
        {
            Color fury = new Color(0.9f, 0.25f, 0.2f);
            Color grit = new Color(0.8f, 0.68f, 0.45f);
            Color command = new Color(1f, 0.6f, 0.25f);

            return new ClassDef
            {
                Id = 0, Name = "Barbarian", Description = "Tough two-handed axe fighter. Shouts turn the tide, but he is slow and has only throwing axes at range.",
                Stats = new ClassStats(21, 22, 8, 9, 15, 15), Color = new Color(0.85f, 0.35f, 0.2f), Body = Color.white,
                Subclasses = new[]
                {
                    Subclass("Berserker", "Reckless fury: every blow dealt or taken stokes Fury that quickens the arms; spend it to drink the foe's blood.",
                        fury, "Fury", 5, ResourceSource.WeaponHit | ResourceSource.DamageTaken, 6f),
                    Subclass("Juggernaut", "Wall of muscle: hits taken and blocked build Grit that hardens the skin; spend it on a shout that soaks a beating.",
                        grit, "Grit", 5, ResourceSource.DamageTaken | ResourceSource.BlockedHit, 10f),
                    Subclass("Warchief", "Leader of the charge: landed blows build Command, a war cry spends it to make the whole party hit harder.",
                        command, "Command", 4, ResourceSource.WeaponHit, 10f)
                },
                Skills = new[]
                {
                    Skill("Battle Roar", "Berserk fury: +10 rage (power, speed, but less armor) for 10s.", AbilityKind.Buff, 10f, 10f, 35f, "BR", new Color(1f, 0.4f, 0.2f), StatusEffectKind.Rage, 0.4f, icon: "Maw"),
                    new AbilityDef
                    {
                        Name = "Reckless Charge", Description = "Charge 6m along your view, stopped by the first body or wall: +5 physical power for 4s.", Kind = AbilityKind.Blink,
                        Cooldown = 18f, CastTime = 0.2f, Magnitude = 6f, Duration = 0.35f, HitEffect = StatusEffectKind.Power, HitMagnitude = 5f, HitDuration = 4f,
                        Glyph = "RC", Color = command, Icon = "Dash"
                    },
                    new AbilityDef
                    {
                        Name = "Blood Frenzy", Description = "Needs 2 Fury, spends all: for 8s every weapon hit heals you for 8% of its damage, +4% per Fury.", Kind = AbilityKind.Buff,
                        Cooldown = 30f, CastTime = 0.3f, Magnitude = 8f, Duration = 8f, Effect = StatusEffectKind.Siphon, ResourceCost = 2, StackBonus = 4f,
                        Glyph = "BF", Color = fury, Icon = "Blood", Subclass = 0
                    },
                    new AbilityDef
                    {
                        Name = "Savage Blow", Description = "Spends all Fury: your next weapon hit within 6s deals +10 physical damage, +6 per Fury.", Kind = AbilityKind.Buff,
                        Cooldown = 12f, CastTime = 0.2f, Magnitude = 10f, Duration = 6f, Effect = StatusEffectKind.Empower, StackBonus = 6f,
                        Glyph = "SB", Color = fury, Icon = "Axe", Subclass = 0
                    },
                    new AbilityDef
                    {
                        Name = "Unyielding Shout", Description = "Spends all Grit: absorb 30 damage, +8 per Grit, for 8s.", Kind = AbilityKind.Shield,
                        Cooldown = 40f, CastTime = 0.4f, Magnitude = 30f, Duration = 8f, StackBonus = 8f, Glyph = "US", Color = grit, Icon = "Shield", Subclass = 1
                    },
                    new AbilityDef
                    {
                        Name = "Iron Hide", Description = "Take 25% less damage from every hit for 6s.", Kind = AbilityKind.Buff, Cooldown = 35f, CastTime = 0.3f,
                        Magnitude = 25f, Duration = 6f, Effect = StatusEffectKind.Guard, Glyph = "IH", Color = grit, Icon = "Armor", Subclass = 1
                    },
                    new AbilityDef
                    {
                        Name = "War Cry", Description = "Spends all Command: you and your party within 8m get +4 physical power, +2 per Command, for 10s.", Kind = AbilityKind.AreaBuff,
                        Cooldown = 40f, CastTime = 0.4f, Magnitude = 4f, Duration = 10f, Radius = 8f, Effect = StatusEffectKind.Power, StackBonus = 2f,
                        Glyph = "WC", Color = command, Icon = "Maw", Subclass = 2
                    },
                    new AbilityDef
                    {
                        Name = "Intimidating Roar", Description = "Everyone within 5m takes 5 physical damage, flinches and is slowed by 30% for 4s.", Kind = AbilityKind.AreaDamage,
                        Cooldown = 25f, CastTime = 0.4f, Magnitude = 5f, Radius = 5f, DamageType = DamageType.Physical, Stagger = 0.3f,
                        HitEffect = StatusEffectKind.Slow, HitMagnitude = 30f, HitDuration = 4f, Glyph = "IR", Color = command, Icon = "Burst", Subclass = 2
                    }
                },
                Perks = new[]
                {
                    new PerkDef("Axe Mastery", "+8 physical power and +10% physical damage.", new StatModifier(StatType.PhysicalPower, 8f), new StatModifier(StatType.PhysicalDamageBonus, 0.1f)) { Icon = "Axe", Color = new Color(0.78f, 0.8f, 0.86f) },
                    new PerkDef("Giant's Constitution", "+20% max health.", new StatModifier(StatType.MaxHealth, 20f)) { Icon = "Heart", Color = new Color(0.9f, 0.27f, 0.27f) },
                    new PerkDef("Thick Hide", "+20 armor rating and +10 magic resistance.", new StatModifier(StatType.ArmorRating, 20f), new StatModifier(StatType.MagicResistance, 10f)) { Icon = "Armor", Color = new Color(0.75f, 0.62f, 0.45f) },
                    new PerkDef("Bloodlust", "+3% action speed per Fury.", new StatModifier(StatType.ActionSpeed, 3f)) { Icon = "Blood", Color = fury, Subclass = 0, PerStack = true },
                    new PerkDef("Rage Within", "+12% physical damage, but -15 armor rating.", new StatModifier(StatType.PhysicalDamageBonus, 0.12f), new StatModifier(StatType.ArmorRating, -15f)) { Icon = "Maw", Color = fury, Subclass = 0 },
                    new PerkDef("Iron Skin", "+6 armor rating per Grit.", new StatModifier(StatType.ArmorRating, 6f)) { Icon = "Armor", Color = grit, Subclass = 1, PerStack = true },
                    new PerkDef("Colossus", "+15% max health, but -8 move speed.", new StatModifier(StatType.MaxHealth, 15f), new StatModifier(StatType.MoveSpeed, -8f)) { Icon = "Heart", Color = grit, Subclass = 1 },
                    new PerkDef("Commanding Presence", "+2 physical power per Command.", new StatModifier(StatType.PhysicalPower, 2f)) { Icon = "Chevrons", Color = command, Subclass = 2, PerStack = true },
                    new PerkDef("Banner Bearer", "+10 armor rating and +15 magic resistance.", new StatModifier(StatType.ArmorRating, 10f), new StatModifier(StatType.MagicResistance, 15f)) { Icon = "Shield", Color = command, Subclass = 2 }
                },
                Kit = new[] { ("Battle Axe", EquipSlot.Weapon1Main, 1, true), ("Zweihander", EquipSlot.Weapon2Main, 1, true), ("Peasant Trousers", EquipSlot.Legs, 1, true), ("Peasant Boots", EquipSlot.Feet, 1, true), ("Francisca Axe", EquipSlot.Utility1, 2, true), ("Bandage", EquipSlot.Utility2, 3, true) },
                Weapons = new[] { WeaponClass.Axe, WeaponClass.Sword, WeaponClass.Mace, WeaponClass.Spear, WeaponClass.Dagger, WeaponClass.Torch }
            };
        }

        /// Magical damage from two spell wheels (or one wheel and an active skill), cast with a staff, a spellbook or a crystal
        /// ball. Spells have charges that come back only by a campfire. Paper-thin against weapons: little health. The school
        /// picks the spells: fire burns, frost holds and guards, lightning strikes fast.
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
                Subclasses = new[]
                {
                    Subclass("Pyromancer", "School of fire: spell hits build Heat that feeds magical power; Combustion spends it in a blast around you.",
                        fire, "Heat", 5, ResourceSource.SpellHit, 8f),
                    Subclass("Cryomancer", "School of frost: blows taken or blocked leave Rime that thickens your guard; Frost Nova spends it to freeze the crowd.",
                        frost, "Rime", 3, ResourceSource.DamageTaken | ResourceSource.BlockedHit, 12f),
                    Subclass("Stormcaller", "School of lightning: spell hits build Static that quickens your feet; Overload spends it for an instant spell.",
                        storm, "Static", 3, ResourceSource.SpellHit, 6f)
                },
                Skills = new[]
                {
                    Memory("Spell Memory I", "Hold to open the first spell wheel and ready a spell; hold RMB with a magical focus in hand and release to cast. The centre of the wheel returns RMB to the weapon.", "M1", arcane, 0),
                    Memory("Spell Memory II", "Hold to open the second spell wheel. Take both memories for ten spells, or one and an active skill.", "M2", arcane, 1),
                    Skill("Arcane Shield", "A barrier absorbs 35 damage for 6s.", AbilityKind.Shield, 35f, 6f, 30f, "AS", arcane, castTime: 0.3f, icon: "Shield"),
                    new AbilityDef
                    {
                        Name = "Combustion", Description = "Needs 2 Heat, spends all: everyone within 3.5m takes 8 magical damage, +7 per Heat, and burns.", Kind = AbilityKind.AreaDamage,
                        Cooldown = 20f, CastTime = 0.3f, Magnitude = 8f, Radius = 3.5f, Stagger = 0.3f, ResourceCost = 2, StackBonus = 7f,
                        HitEffect = StatusEffectKind.Burn, HitMagnitude = 10f, HitDuration = 4f, Glyph = "Co", Color = fire, Icon = "Flame", Subclass = 0
                    },
                    new AbilityDef
                    {
                        Name = "Frost Nova", Description = "Spends all Rime: everyone within 4.5m takes 10 magical damage, +5 per Rime, and is slowed by 50% for 3s.", Kind = AbilityKind.AreaDamage,
                        Cooldown = 25f, CastTime = 0.3f, Magnitude = 10f, Radius = 4.5f, Stagger = 0.2f, StackBonus = 5f,
                        HitEffect = StatusEffectKind.Slow, HitMagnitude = 50f, HitDuration = 3f, Glyph = "FN", Color = frost, Icon = "Snowflake", Subclass = 1
                    },
                    new AbilityDef
                    {
                        Name = "Overload", Description = "Needs 3 Static, spends all: the next spell within 15s is cast instantly.", Kind = AbilityKind.QuickCast,
                        Cooldown = 10f, CastTime = 0.1f, Duration = 15f, ResourceCost = 3, Glyph = "Ov", Color = storm, Icon = "Hourglass", Subclass = 2
                    }
                },
                Spells = new[]
                {
                    Projectile("Magic Missile", "Five arcane darts in a fan, 7 magical damage each.", true, 5, 1f, 0.7f, 7f, 34f, 0f, ProjectileKind.Magic, DamageType.Magical,
                        5, 5f, "MM", arcane, stagger: 0.05f, icon: "Bolt"),
                    new AbilityDef
                    {
                        Name = "Blink", Description = "Rush 7m along your view in a blur; the first wall or body stops you.", Kind = AbilityKind.Blink, IsSpell = true,
                        Charges = 3, Cooldown = 1f, CastTime = 0.25f, Magnitude = 7f, Duration = 0.18f, Glyph = "Bl", Color = arcane, Icon = "Dash"
                    },
                    Spell("Haste", "+20% move speed for 20s for the ally you aim at (for you when you miss).", AbilityKind.Buff, 2, 0.8f, 20f, 20f, "Hs", new Color(0.6f, 1f, 0.6f), StatusEffectKind.Haste, icon: "Chevrons"),
                    Spell("Invisibility", "Fade from sight for 8s.", AbilityKind.Invisibility, 1, 1.2f, 0f, 8f, "In", new Color(0.75f, 0.75f, 0.85f), icon: "Eye"),
                    For(0, Projectile("Fireball", "Explodes for up to 30 magical damage around the impact and sets the struck ablaze.", true, 3, 1f, 1.1f, 30f, 22f, 0f,
                        ProjectileKind.Fire, DamageType.Magical, 1, 0f, "FB", fire, StatusEffectKind.Burn, 12f, 4f, 2.5f, stagger: 0.3f, icon: "Flame")),
                    For(0, Enchant("Ignite Weapon", "Sets the weapon of the ally you aim at on fire for 30s (your own when you miss): +5 magical damage per hit and a burn.",
                        StatusEffectKind.FireWeapon, "IW", fire, "Flame")),
                    For(1, Projectile("Ice Bolt", "Shard of ice: 20 magical damage and 30% slow for 2s.", true, 6, 0.8f, 0.6f, 20f, 32f, 0f, ProjectileKind.Ice, DamageType.Magical,
                        1, 0f, "IB", frost, StatusEffectKind.Slow, 30f, 2f, icon: "Snowflake")),
                    For(1, Enchant("Frost Weapon", "Chills the weapon of the ally you aim at for 30s (your own when you miss): +5 magical damage per hit and a slow.",
                        StatusEffectKind.FrostWeapon, "FW", frost, "Snowflake")),
                    new AbilityDef
                    {
                        Name = "Chain Lightning", Description = "Hitscan bolt: 18 magical damage, then jumps to two more bodies within 6m, weaker each time. Careful, it hits allies too.",
                        Kind = AbilityKind.ChainLightning, IsSpell = true, Charges = 4, Cooldown = 1f, CastTime = 0.9f, Magnitude = 18f, Radius = 6f, Count = 2,
                        Stagger = 0.15f, Glyph = "CL", Color = storm, Icon = "Lightning", Subclass = 2
                    },
                    new AbilityDef
                    {
                        Name = "Lightning Strike", Description = "Hitscan: the body under the crosshair is struck by lightning from the sky for 40 magical damage and a stagger.",
                        Kind = AbilityKind.LightningStrike, IsSpell = true, Charges = 2, Cooldown = 1f, CastTime = 1.4f, Magnitude = 40f, Stagger = 0.6f, Glyph = "LS",
                        Color = storm, Icon = "Lightning", Subclass = 2
                    }
                },
                Perks = new[]
                {
                    new PerkDef("Arcane Mastery", "+6 magical power and +10% magical damage.", new StatModifier(StatType.MagicalPower, 6f), new StatModifier(StatType.MagicalDamageBonus, 0.1f)) { Icon = "Burst" },
                    new PerkDef("Sage", "+10 Knowledge: more charges for every spell, faster casting.", new StatModifier(StatType.Knowledge, 10f)) { Icon = "Book" },
                    new PerkDef("Spell Ward", "+30 magic resistance.", new StatModifier(StatType.MagicResistance, 30f)) { Icon = "Shield" },
                    new PerkDef("Kindling", "+2 magical power per Heat.", new StatModifier(StatType.MagicalPower, 2f)) { Icon = "Flame", Color = fire, Subclass = 0, PerStack = true },
                    new PerkDef("Pyromania", "+12% magical damage, but -15 magic resistance.", new StatModifier(StatType.MagicalDamageBonus, 0.12f), new StatModifier(StatType.MagicResistance, -15f)) { Icon = "Burst", Color = fire, Subclass = 0 },
                    new PerkDef("Permafrost", "+8 armor rating and +5 magic resistance per Rime.", new StatModifier(StatType.ArmorRating, 8f), new StatModifier(StatType.MagicResistance, 5f)) { Icon = "Snowflake", Color = frost, Subclass = 1, PerStack = true },
                    new PerkDef("Glacial Mind", "+6 Knowledge and +10% max health.", new StatModifier(StatType.Knowledge, 6f), new StatModifier(StatType.MaxHealth, 10f)) { Icon = "Book", Color = frost, Subclass = 1 },
                    new PerkDef("Conductor", "+5 move speed per Static.", new StatModifier(StatType.MoveSpeed, 5f)) { Icon = "Lightning", Color = storm, Subclass = 2, PerStack = true },
                    new PerkDef("Storm Sense", "+4 Knowledge and +4 Agility.", new StatModifier(StatType.Knowledge, 4f), new StatModifier(StatType.Agility, 4f)) { Icon = "Eye", Color = storm, Subclass = 2 }
                },
                Kit = new[] { ("Spellbook", EquipSlot.Weapon1Main, 1, true), ("Magic Staff", EquipSlot.Weapon2Main, 1, true), ("Peasant Hood", EquipSlot.Head, 1, true),
                    ("Peasant Shirt", EquipSlot.Chest, 1, true), ("Peasant Trousers", EquipSlot.Legs, 1, true), ("Peasant Boots", EquipSlot.Feet, 1, true), ("Campfire Kit", EquipSlot.Utility1, 1, true), ("Bandage", EquipSlot.Utility2, 2, true) },
                Weapons = new[] { WeaponClass.Staff, WeaponClass.Spellbook, WeaponClass.CrystalBall, WeaponClass.Dagger, WeaponClass.Sword },
                Focus = CastFocus.Magic
            };
        }

        /// Weapon generalist with no magic: every weapon but the magical foci, shields and the heaviest armour. Every Warrior
        /// blocks and ripostes the same; the subclass decides what a good block or hit earns: the Duelist wins on the riposte,
        /// the Guardian on the shield, the Ravager by breaking guards.
        private static ClassDef CreateWarrior()
        {
            Color steel = new Color(0.62f, 0.72f, 0.86f);
            Color tempo = new Color(0.55f, 0.9f, 0.85f);
            Color resolve = new Color(0.75f, 0.8f, 0.95f);
            Color momentum = new Color(0.95f, 0.55f, 0.3f);

            return new ClassDef
            {
                Id = 2, Name = "Warrior", Description = "Veteran of a hundred delves at home with any weapon but a magical one. No magic at all: blocks, ripostes and the right moment win his fights.",
                Stats = new ClassStats(18, 18, 9, 10, 17, 18), Color = steel, Body = Color.white,
                Subclasses = new[]
                {
                    Subclass("Duelist", "Fencer of the counter: blocks and ripostes build Tempo for a lunge and a flurry. Rapier, longsword or arming sword, no shield.",
                        tempo, "Tempo", 3, ResourceSource.BlockedHit | ResourceSource.Riposte, 8f),
                    Subclass("Guardian", "Holder of doorways: every blow stopped by the shield builds Resolve for a harder stance. Sword, mace or spear with a shield.",
                        resolve, "Resolve", 5, ResourceSource.BlockedHit, 12f),
                    Subclass("Ravager", "Breaker of guards: hits, blocked ones too, build Momentum for a blow that goes through any block. Two-handed axes and swords.",
                        momentum, "Momentum", 4, ResourceSource.WeaponHit | ResourceSource.HitOnBlock, 6f)
                },
                Skills = new[]
                {
                    new AbilityDef
                    {
                        Name = "Rally", Description = "Catch your breath: restore 40 health over 8s.", Kind = AbilityKind.Heal, Magnitude = 40f, Duration = 8f,
                        Cooldown = 45f, CastTime = 0.3f, DamageType = DamageType.Physical, Glyph = "Ra", Color = new Color(0.9f, 0.3f, 0.3f), Icon = "Heart"
                    },
                    Skill("Sprint", "+25% move speed for 6s.", AbilityKind.Buff, 25f, 6f, 30f, "Sp", steel, StatusEffectKind.Haste, 0.1f, icon: "Chevrons"),
                    new AbilityDef
                    {
                        Name = "Lunge", Description = "Spends all Tempo: dash 4.5m along your view; a weapon hit within 1.5s deals +8 physical damage, +6 per Tempo.",
                        Kind = AbilityKind.Blink, Cooldown = 10f, CastTime = 0.1f, Magnitude = 4.5f, Duration = 0.15f, StackBonus = 6f,
                        HitEffect = StatusEffectKind.Empower, HitMagnitude = 8f, HitDuration = 1.5f, Glyph = "Lu", Color = tempo, Icon = "Dash", Subclass = 0
                    },
                    new AbilityDef
                    {
                        Name = "Flurry", Description = "Needs 1 Tempo, spends all: +15% action speed, +10% per Tempo, for 5s.", Kind = AbilityKind.Buff,
                        Cooldown = 20f, CastTime = 0.1f, Magnitude = 15f, Duration = 5f, Effect = StatusEffectKind.ActionSpeed, ResourceCost = 1, StackBonus = 10f,
                        Glyph = "Fl", Color = tempo, Icon = "Sword", Subclass = 0
                    },
                    new AbilityDef
                    {
                        Name = "Iron Stance", Description = "Spends all Resolve: take 20% less damage, +6% per Resolve, for 6s.", Kind = AbilityKind.Buff,
                        Cooldown = 30f, CastTime = 0.2f, Magnitude = 20f, Duration = 6f, Effect = StatusEffectKind.Guard, StackBonus = 6f,
                        Glyph = "IS", Color = resolve, Icon = "Armor", Subclass = 1
                    },
                    new AbilityDef
                    {
                        Name = "Shield Bash", Description = "Bash everyone in a narrow cone 2.6m ahead: 10 physical damage, a long stagger and a shove back.", Kind = AbilityKind.AreaDamage,
                        Cooldown = 14f, CastTime = 0.2f, Magnitude = 10f, Radius = 2.6f, Cone = 50f, Push = 7f, Stagger = 0.8f, DamageType = DamageType.Physical,
                        Glyph = "SB", Color = resolve, Icon = "Shield", Subclass = 1
                    },
                    new AbilityDef
                    {
                        Name = "Sunder", Description = "Spends all Momentum: your next weapon hit within 6s, even on a block, deals +12 physical damage past it, +6 per Momentum, and staggers.",
                        Kind = AbilityKind.Buff, Cooldown = 16f, CastTime = 0.2f, Magnitude = 12f, Duration = 6f, Effect = StatusEffectKind.Sunder, StackBonus = 6f,
                        Glyph = "Su", Color = momentum, Icon = "Axe", Subclass = 2
                    },
                    new AbilityDef
                    {
                        Name = "Wide Sweep", Description = "Sweep a wide arc 3.2m ahead: 16 physical damage and a stagger to everyone in it.", Kind = AbilityKind.AreaDamage,
                        Cooldown = 20f, CastTime = 0.35f, Magnitude = 16f, Radius = 3.2f, Cone = 100f, Stagger = 0.4f, DamageType = DamageType.Physical,
                        Glyph = "WS", Color = momentum, Icon = "Sword", Subclass = 2
                    }
                },
                Perks = new[]
                {
                    new PerkDef("Weapon Drill", "+6 physical power and +8% physical damage.", new StatModifier(StatType.PhysicalPower, 6f), new StatModifier(StatType.PhysicalDamageBonus, 0.08f)) { Icon = "Sword", Color = new Color(0.9f, 0.6f, 0.35f) },
                    new PerkDef("Fleet Footwork", "+14 move speed: armour weighs less on the feet.", new StatModifier(StatType.MoveSpeed, 14f)) { Icon = "Dash", Color = new Color(0.6f, 0.9f, 0.6f) },
                    new PerkDef("Quick Hands", "+10% action speed.", new StatModifier(StatType.ActionSpeed, 10f)) { Icon = "Bolt", Color = new Color(0.95f, 0.85f, 0.4f) },
                    new PerkDef("Flow", "+5% action speed per Tempo.", new StatModifier(StatType.ActionSpeed, 5f)) { Icon = "Hourglass", Color = tempo, Subclass = 0, PerStack = true },
                    new PerkDef("Light Feet", "+6 Agility and +4 Dexterity.", new StatModifier(StatType.Agility, 6f), new StatModifier(StatType.Dexterity, 4f)) { Icon = "Dash", Color = tempo, Subclass = 0 },
                    new PerkDef("Steadfast", "+5 armor rating and +3 magic resistance per Resolve.", new StatModifier(StatType.ArmorRating, 5f), new StatModifier(StatType.MagicResistance, 3f)) { Icon = "Shield", Color = resolve, Subclass = 1, PerStack = true },
                    new PerkDef("Bulwark", "+35 armor rating and +10% max health.", new StatModifier(StatType.ArmorRating, 35f), new StatModifier(StatType.MaxHealth, 10f)) { Icon = "Armor", Color = resolve, Subclass = 1 },
                    new PerkDef("Rolling Strikes", "+3 physical power per Momentum.", new StatModifier(StatType.PhysicalPower, 3f)) { Icon = "Axe", Color = momentum, Subclass = 2, PerStack = true },
                    new PerkDef("Heavy Hands", "+12% physical damage, but -6% action speed.", new StatModifier(StatType.PhysicalDamageBonus, 0.12f), new StatModifier(StatType.ActionSpeed, -6f)) { Icon = "Mace", Color = momentum, Subclass = 2 }
                },
                Kit = new[] { ("Arming Sword", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Longsword", EquipSlot.Weapon2Main, 1, true),
                    ("Peasant Shirt", EquipSlot.Chest, 1, true), ("Peasant Trousers", EquipSlot.Legs, 1, true), ("Peasant Boots", EquipSlot.Feet, 1, true), ("Bandage", EquipSlot.Utility1, 3, true) },
                Weapons = new[] { WeaponClass.Sword, WeaponClass.Axe, WeaponClass.Mace, WeaponClass.Dagger, WeaponClass.Spear, WeaponClass.Bow, WeaponClass.Crossbow,
                    WeaponClass.Staff, WeaponClass.Shield, WeaponClass.Torch }
            };
        }

        /// Warrior priest of a forgotten creed who moves life between friend and foe: blunt weapons, a shield and one wheel of
        /// prayers cast with a spellbook or a magic staff. The Inquisitor hunts with the mace, the Absolver keeps the party
        /// alive, the Exorcist holds ground with holy blasts.
        private static ClassDef CreateConfessor()
        {
            Color light = new Color(1f, 0.9f, 0.55f);
            Color dawn = new Color(1f, 0.7f, 0.3f);
            Color mending = new Color(0.6f, 1f, 0.65f);
            Color zeal = new Color(0.95f, 0.4f, 0.3f);
            Color seal = new Color(0.7f, 0.8f, 1f);

            return new ClassDef
            {
                Id = 3, Name = "Confessor", Description = "Warrior priest who goes below for the sins of the fallen: a mace, a shield and prayers that move life between friend and foe. Half healer, half fighter.",
                Stats = new ClassStats(15, 14, 21, 16, 12, 12), Color = light, Body = Color.white,
                Subclasses = new[]
                {
                    Subclass("Inquisitor", "Hunter of heretics and mages: mace hits build Conviction for a verdict that silences the struck. Little healing.",
                        zeal, "Conviction", 5, ResourceSource.WeaponHit, 8f),
                    Subclass("Absolver", "Keeper of the flock: every heal, ward and blessing builds Grace for a miracle that pulls an ally back from the brink.",
                        mending, "Grace", 5, ResourceSource.Support, 20f),
                    Subclass("Exorcist", "Warden of the ritual ground: magical hits build Sigils that break into a holy blast; seals slow whoever comes close.",
                        seal, "Sigils", 3, ResourceSource.SpellHit, 12f)
                },
                Skills = new[]
                {
                    Memory("Prayer Memory", "Hold to open the prayer wheel and ready a prayer; hold RMB with a spellbook or a magic staff in hand and release to cast. The centre of the wheel returns RMB to the weapon.", "PM", light),
                    new AbilityDef
                    {
                        Name = "Hallow Weapon", Description = "Light on your weapon, or on the weapon of the ally you aim at, for 12s: +8 magical damage per hit.",
                        Kind = AbilityKind.WeaponEnchant, Cooldown = 35f, CastTime = 0.4f, Magnitude = 8f, Duration = 12f, Effect = StatusEffectKind.HolyWeapon,
                        Glyph = "HW", Color = light, Icon = "Mace", OnAlly = true
                    },
                    new AbilityDef
                    {
                        Name = "Verdict", Description = "Needs 1 Conviction, spends all: your next weapon hit within 6s deals +8 physical damage, +6 per Conviction, and silences for 3s.",
                        Kind = AbilityKind.Buff, Cooldown = 15f, CastTime = 0.2f, Magnitude = 8f, Duration = 6f, Effect = StatusEffectKind.Verdict, ResourceCost = 1, StackBonus = 6f,
                        Glyph = "Ve", Color = zeal, Icon = "Mace", Subclass = 0
                    },
                    new AbilityDef
                    {
                        Name = "Absolution", Description = "For 8s every weapon hit heals you for 15% of its damage.", Kind = AbilityKind.Buff, Cooldown = 30f, CastTime = 0.3f,
                        Magnitude = 15f, Duration = 8f, Effect = StatusEffectKind.Siphon, Glyph = "Ab", Color = zeal, Icon = "Blood", Subclass = 0
                    },
                    new AbilityDef
                    {
                        Name = "Miracle", Description = "Needs 3 Grace, spends all: the ally you aim at (you when you miss) instantly heals 10, +10 per Grace.", Kind = AbilityKind.Heal,
                        Cooldown = 20f, CastTime = 0.4f, Magnitude = 10f, ResourceCost = 3, StackBonus = 10f, OnAlly = true, Glyph = "Mi", Color = mending, Icon = "Halo", Subclass = 1
                    },
                    new AbilityDef
                    {
                        Name = "Vow of Protection", Description = "The ally you aim at (you when you miss) is shielded from 30 damage for 8s.", Kind = AbilityKind.Shield,
                        Cooldown = 25f, CastTime = 0.3f, Magnitude = 30f, Duration = 8f, OnAlly = true, Glyph = "VP", Color = mending, Icon = "Shield", Subclass = 1
                    },
                    new AbilityDef
                    {
                        Name = "Seal of Renunciation", Description = "A seal flares around you: everyone within 5m takes 6 magical damage and is slowed by 40% for 4s.", Kind = AbilityKind.AreaDamage,
                        Cooldown = 22f, CastTime = 0.4f, Magnitude = 6f, Radius = 5f, Stagger = 0.2f, HitEffect = StatusEffectKind.Slow, HitMagnitude = 40f, HitDuration = 4f,
                        Glyph = "SR", Color = seal, Icon = "Halo", Subclass = 2
                    },
                    new AbilityDef
                    {
                        Name = "Break the Seals", Description = "Needs 1 Sigil, spends all: holy blast within 4m, 10 magical damage, +10 per Sigil, and a stagger.", Kind = AbilityKind.AreaDamage,
                        Cooldown = 15f, CastTime = 0.3f, Magnitude = 10f, Radius = 4f, Stagger = 0.6f, ResourceCost = 1, StackBonus = 10f,
                        Glyph = "BS", Color = seal, Icon = "Burst", Subclass = 2
                    }
                },
                Spells = new[]
                {
                    Spell("Mending Prayer", "Heals the ally you aim at (yourself when you miss) for 22.", AbilityKind.Heal, 4, 1.2f, 22f, 0f, "MP", mending, icon: "Cross"),
                    Spell("Aegis", "Shield of light on the ally you aim at (on you when you miss): absorbs 25 damage for 15s.", AbilityKind.Shield, 3, 0.8f, 25f, 15f, "Ae", light, icon: "Shield"),
                    Projectile("Sunlance", "Lance of light: 22 magical damage and a stagger.", true, 4, 1f, 0.9f, 22f, 30f, 0f, ProjectileKind.Holy, DamageType.Magical,
                        1, 0f, "SL", dawn, stagger: 0.25f, icon: "Bolt"),
                    For(0, Spell("Benediction", "Blesses the ally you aim at (yourself when you miss): +5 Strength for 30s.", AbilityKind.Buff, 3, 0.8f, 5f, 30f, "Be", light,
                        StatusEffectKind.Strength, icon: "Chevrons")),
                    For(0, Spell("Penance", "The one you aim at (you when you miss) takes 20% less damage for 10s.", AbilityKind.Buff, 2, 0.8f, 20f, 10f, "Pe", zeal,
                        StatusEffectKind.Guard, icon: "Armor")),
                    new AbilityDef
                    {
                        Name = "Circle of Dawn", Description = "Everyone within 5m, you included, heals 30 over 6s.", Kind = AbilityKind.AreaHeal, IsSpell = true,
                        Charges = 2, Cooldown = 1f, CastTime = 1.5f, Magnitude = 30f, Duration = 6f, Radius = 5f, Glyph = "CD", Color = mending, Icon = "Halo", Subclass = 1
                    },
                    For(1, Spell("Last Blessing", "The ally you aim at (you when you miss) heals 36 over 6s.", AbilityKind.Heal, 3, 1f, 36f, 6f, "LB", mending, icon: "Heart")),
                    new AbilityDef
                    {
                        Name = "Circle of Penance", Description = "You and your party within 5m take 15% less damage for 10s.", Kind = AbilityKind.AreaBuff, IsSpell = true,
                        Charges = 2, Cooldown = 1f, CastTime = 1.2f, Magnitude = 15f, Duration = 10f, Radius = 5f, Effect = StatusEffectKind.Guard,
                        Glyph = "CP", Color = seal, Icon = "Halo", Subclass = 2
                    },
                    For(2, Projectile("Banishing Light", "Bolt of light: 18 magical damage and 30% slow for 2s.", true, 4, 1f, 0.8f, 18f, 30f, 0f, ProjectileKind.Holy, DamageType.Magical,
                        1, 0f, "BL", seal, StatusEffectKind.Slow, 30f, 2f, stagger: 0.2f, icon: "Bolt"))
                },
                Perks = new[]
                {
                    new PerkDef("Devotion", "+6 Spirit: stronger prayers and healing.", new StatModifier(StatType.Spirit, 6f)) { Icon = "Halo" },
                    new PerkDef("Litany", "+6 Knowledge: one more charge of every prayer, faster casting.", new StatModifier(StatType.Knowledge, 6f)) { Icon = "Book" },
                    new PerkDef("Iron Vow", "+20 armor rating and +15 magic resistance.", new StatModifier(StatType.ArmorRating, 20f), new StatModifier(StatType.MagicResistance, 15f)) { Icon = "Armor", Color = new Color(0.75f, 0.8f, 0.9f) },
                    new PerkDef("Zealot", "+6 physical power and +8% physical damage.", new StatModifier(StatType.PhysicalPower, 6f), new StatModifier(StatType.PhysicalDamageBonus, 0.08f)) { Icon = "Mace", Color = zeal, Subclass = 0 },
                    new PerkDef("Righteous Fury", "+2 physical power per Conviction.", new StatModifier(StatType.PhysicalPower, 2f)) { Icon = "Burst", Color = zeal, Subclass = 0, PerStack = true },
                    new PerkDef("State of Grace", "+2 Spirit per Grace.", new StatModifier(StatType.Spirit, 2f)) { Icon = "Cross", Color = mending, Subclass = 1, PerStack = true },
                    new PerkDef("Martyr's Resolve", "+10% max health and +10 magic resistance.", new StatModifier(StatType.MaxHealth, 10f), new StatModifier(StatType.MagicResistance, 10f)) { Icon = "Heart", Color = mending, Subclass = 1 },
                    new PerkDef("Sigil Ward", "+6 magic resistance per Sigil.", new StatModifier(StatType.MagicResistance, 6f)) { Icon = "Shield", Color = seal, Subclass = 2, PerStack = true },
                    new PerkDef("Ritualist", "+6 magical power.", new StatModifier(StatType.MagicalPower, 6f)) { Icon = "Book", Color = seal, Subclass = 2 }
                },
                Kit = new[] { ("Flanged Mace", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Spellbook", EquipSlot.Weapon2Main, 1, true),
                    ("Peasant Shirt", EquipSlot.Chest, 1, true), ("Peasant Trousers", EquipSlot.Legs, 1, true), ("Peasant Boots", EquipSlot.Feet, 1, true), ("Campfire Kit", EquipSlot.Utility1, 1, true), ("Bandage", EquipSlot.Utility2, 2, true) },
                Weapons = new[] { WeaponClass.Mace, WeaponClass.Staff, WeaponClass.Spellbook, WeaponClass.Shield, WeaponClass.Torch },
                Focus = CastFocus.Magic
            };
        }

        private static SubclassDef Subclass(string name, string description, Color color, string resource, int max, ResourceSource sources, float decay)
        {
            return new SubclassDef { Name = name, Description = description, Color = color, Resource = resource, ResourceMax = max, Sources = sources, Decay = decay };
        }

        /// Hands a shared definition over to one subclass.
        private static AbilityDef For(int subclass, AbilityDef ability)
        {
            ability.Subclass = subclass;

            return ability;
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
