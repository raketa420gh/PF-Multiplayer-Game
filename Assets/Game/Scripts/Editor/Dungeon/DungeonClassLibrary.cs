using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Classes of Dark and Darker: base attributes, skill pools, spell lists, perk pools and squire kits.
    internal static class DungeonClassLibrary
    {
        private const string Caltrops = "Caltrops";
        private const string Smoke = "SmokePot";

        public static ClassDef[] CreateClasses()
        {
            return new[]
            {
                new ClassDef
                {
                    Id = 0, Name = "Fighter", Description = "Versatile frontliner. Any weapon, any armor.",
                    Stats = new ClassStats(15, 15, 15, 15, 15, 15, 15), Color = new Color(0.8f, 0.7f, 0.5f), Body = new Color(0.62f, 0.66f, 0.72f),
                    Skills = new[]
                    {
                        Skill("Second Wind", "Recover 40% of max health over 12s.", AbilityKind.Heal, 50f, 12f, 60f, "SW", new Color(0.3f, 1f, 0.4f)),
                        Skill("Sprint", "+39 move speed for 6s.", AbilityKind.Buff, 39f, 6f, 28f, "Sp", new Color(1f, 0.9f, 0.3f), StatusEffectKind.Haste),
                        Skill("Adrenaline Rush", "+15% action speed and +15 move speed for 8s.", AbilityKind.Buff, 15f, 8f, 28f, "AR", new Color(1f, 0.6f, 0.3f), StatusEffectKind.ActionSpeed),
                        Area("Shield Slam", "Bash everything in front of you for 30 and stagger it.", false, 1, 6f, 0.25f, 30f, 1.8f, DamageType.Physical, "SS", new Color(0.8f, 0.8f, 0.9f), stagger: 0.7f),
                        Skill("Perfect Block", "Absorb 25 damage for 4s.", AbilityKind.Shield, 25f, 4f, 8f, "PB", new Color(0.7f, 0.8f, 1f)),
                        Skill("Taunt", "Monsters nearby focus you for 12s.", AbilityKind.Taunt, 1f, 12f, 18f, "Ta", new Color(1f, 0.5f, 0.5f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Defense Mastery", "+15 armor rating.", new StatModifier(StatType.ArmorRating, 15f)),
                        new PerkDef("Swift", "+10 move speed.", new StatModifier(StatType.MoveSpeed, 10f)),
                        new PerkDef("Combo Attack", "+5% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.05f)),
                        new PerkDef("Barricade", "+25 magic resistance.", new StatModifier(StatType.MagicResistance, 25f)),
                        new PerkDef("Dual Wield", "+10% action speed.", new StatModifier(StatType.ActionSpeed, 10f)),
                        new PerkDef("Slayer", "+5 physical power.", new StatModifier(StatType.PhysicalPower, 5f)),
                        new PerkDef("Projectile Resistance", "+10 armor rating.", new StatModifier(StatType.ArmorRating, 10f)),
                        new PerkDef("Counterattack", "+5 move speed, +5% action speed.", new StatModifier(StatType.MoveSpeed, 5f), new StatModifier(StatType.ActionSpeed, 5f))
                    },
                    Kit = new[] { ("Arming Sword", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Torch", EquipSlot.Weapon2Main, 1, true), ("Adventurer Tunic", EquipSlot.Chest, 1, true), ("Leather Cap", EquipSlot.Head, 1, true), ("Adventurer Boots", EquipSlot.Feet, 1, true), ("Bandage", EquipSlot.Utility1, 3, true), ("Potion of Healing", EquipSlot.Utility2, 1, true) },
                    Weapons = new[] { WeaponClass.Sword, WeaponClass.Axe, WeaponClass.Mace, WeaponClass.Spear, WeaponClass.Bow, WeaponClass.Crossbow, WeaponClass.Shield, WeaponClass.Torch, WeaponClass.Dagger },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather, ArmorType.Chain, ArmorType.Plate }
                },
                new ClassDef
                {
                    Id = 1, Name = "Barbarian", Description = "Huge health pool and two-handed brutality.",
                    Stats = new ClassStats(20, 25, 13, 12, 18, 5, 12), Color = new Color(0.8f, 0.35f, 0.25f), Body = new Color(0.72f, 0.55f, 0.45f),
                    Skills = new[]
                    {
                        Skill("Rage", "+10 strength and +7% move speed for 8s, less armor.", AbilityKind.Buff, 10f, 8f, 28f, "Rg", new Color(1f, 0.3f, 0.2f), StatusEffectKind.Rage),
                        Skill("Reckless Attack", "+15 physical power for 8s.", AbilityKind.Buff, 15f, 8f, 24f, "RA", new Color(1f, 0.5f, 0.2f), StatusEffectKind.Power),
                        Skill("War Cry", "+15% max health for 12s.", AbilityKind.Buff, 15f, 12f, 28f, "WC", new Color(1f, 0.8f, 0.4f), StatusEffectKind.Fortify),
                        Projectile("Hurl Weapon", "Throw your weapon for heavy damage.", false, 1, 8f, 0.4f, 60f, 22f, -6f, ProjectileKind.Thrown, DamageType.Physical, 1, 0f, "HW", new Color(0.8f, 0.8f, 0.8f), stagger: 0.4f),
                        Area("Whirlwind", "Spin, hitting everything around you for 45.", false, 1, 32f, 0.5f, 45f, 2.3f, DamageType.Physical, "Ww", new Color(1f, 0.7f, 0.4f), stagger: 0.4f),
                        Skill("Grappling Hook", "Lunge forward.", AbilityKind.Dash, -9f, 0f, 16f, "GH", new Color(0.7f, 0.7f, 0.6f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Two-Hander", "+5% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.05f)),
                        new PerkDef("Iron Will", "+75 magic resistance.", new StatModifier(StatType.MagicResistance, 75f)),
                        new PerkDef("Robust", "+10% max health.", new StatModifier(StatType.MaxHealth, 10f)),
                        new PerkDef("Savage", "+10 physical power.", new StatModifier(StatType.PhysicalPower, 10f)),
                        new PerkDef("Executioner", "+10% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.1f)),
                        new PerkDef("Berserker", "+10 physical power.", new StatModifier(StatType.PhysicalPower, 10f)),
                        new PerkDef("Carnage", "+10 move speed.", new StatModifier(StatType.MoveSpeed, 10f)),
                        new PerkDef("Smash", "+5% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.05f))
                    },
                    Kit = new[] { ("Battle Axe", EquipSlot.Weapon1Main, 1, true), ("Torch", EquipSlot.Weapon2Main, 1, true), ("Doublet", EquipSlot.Chest, 1, true), ("Leather Leggings", EquipSlot.Legs, 1, true), ("Adventurer Boots", EquipSlot.Feet, 1, true), ("Bandage", EquipSlot.Utility1, 3, true), ("Francisca Axe", EquipSlot.Utility2, 2, true) },
                    Weapons = new[] { WeaponClass.Axe, WeaponClass.Sword, WeaponClass.Mace, WeaponClass.Shield, WeaponClass.Torch },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather, ArmorType.Plate }
                },
                new ClassDef
                {
                    Id = 2, Name = "Rogue", Description = "Fast, quiet, fragile. Hides in the dark and strikes first.",
                    Stats = new ClassStats(9, 6, 25, 20, 10, 10, 25), Color = new Color(0.4f, 0.4f, 0.5f), Body = new Color(0.3f, 0.32f, 0.36f),
                    Skills = new[]
                    {
                        Skill("Hide", "Become invisible for 8s. Monsters lose you.", AbilityKind.Invisibility, 1f, 8f, 32f, "Hi", new Color(0.6f, 0.6f, 0.9f)),
                        Skill("Rupture", "For 6s your hits open bleeding wounds (15 over 5s).", AbilityKind.Buff, 15f, 6f, 24f, "Ru", new Color(0.9f, 0.2f, 0.2f), StatusEffectKind.Rupture),
                        Skill("Weakpoint Attack", "+30% armor penetration for 3s.", AbilityKind.Buff, 30f, 3f, 24f, "WA", new Color(1f, 0.8f, 0.6f), StatusEffectKind.ArmorPenetration),
                        Spawn("Caltrops", "Drop spikes that slow and hurt whoever steps in.", 28f, Caltrops, 1.2f, "Ca", new Color(0.7f, 0.7f, 0.7f)),
                        Spawn("Smoke Pot", "A cloud that hides everyone inside.", 60f, Smoke, 1.5f, "SP", new Color(0.6f, 0.6f, 0.6f)),
                        Skill("Tumbling", "Backflip away from danger.", AbilityKind.Dash, 7f, 0f, 3f, "Tu", new Color(0.8f, 0.9f, 1f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Back Attack", "+10% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.1f)),
                        new PerkDef("Dagger Mastery", "+5 physical power.", new StatModifier(StatType.PhysicalPower, 5f)),
                        new PerkDef("Creep", "+10 move speed.", new StatModifier(StatType.MoveSpeed, 10f)),
                        new PerkDef("Poisoned Weapon", "+5% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.05f)),
                        new PerkDef("Thrust", "+15% physical damage with daggers.", new StatModifier(StatType.PhysicalDamageBonus, 0.15f)),
                        new PerkDef("Double Jump", "+2 agility.", new StatModifier(StatType.Agility, 2f)),
                        new PerkDef("Stealth", "+1 agility.", new StatModifier(StatType.Agility, 1f)),
                        new PerkDef("Jokester", "+3 resourcefulness.", new StatModifier(StatType.Resourcefulness, 3f))
                    },
                    Kit = new[] { ("Rondel Dagger", EquipSlot.Weapon1Main, 1, true), ("Torch", EquipSlot.Weapon2Main, 1, true), ("Rogue Cowl", EquipSlot.Head, 1, true), ("Doublet", EquipSlot.Chest, 1, true), ("Adventurer Boots", EquipSlot.Feet, 1, true), ("Throwing Knife", EquipSlot.Utility1, 2, true), ("Lockpick", EquipSlot.Utility2, 3, true), ("Bandage", EquipSlot.Utility3, 3, true) },
                    Weapons = new[] { WeaponClass.Dagger, WeaponClass.Sword, WeaponClass.Torch, WeaponClass.Crossbow },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather }
                },
                new ClassDef
                {
                    Id = 3, Name = "Ranger", Description = "Bows and traps. Deadly at range, nimble up close.",
                    Stats = new ClassStats(12, 10, 20, 18, 10, 12, 23), Color = new Color(0.35f, 0.65f, 0.35f), Body = new Color(0.4f, 0.5f, 0.35f),
                    Skills = new[]
                    {
                        Skill("Quick Fire", "+50% action speed for 8s.", AbilityKind.Buff, 50f, 8f, 18f, "QF", new Color(0.6f, 1f, 0.5f), StatusEffectKind.ActionSpeed),
                        Projectile("Multishot", "Fire five arrows in a cone.", false, 1, 22f, 0.3f, 20f, 30f, -9.81f, ProjectileKind.Arrow, DamageType.Physical, 5, 12f, "MS", new Color(0.8f, 0.9f, 0.5f)),
                        Projectile("Quickshot", "Three arrows in a burst.", false, 1, 22f, 0.3f, 22f, 32f, -9.81f, ProjectileKind.Arrow, DamageType.Physical, 3, 2f, "QS", new Color(0.7f, 0.9f, 0.6f)),
                        Skill("Penetrating Shot", "+50% armor penetration for 5s.", AbilityKind.Buff, 50f, 5f, 18f, "PS", new Color(1f, 0.9f, 0.6f), StatusEffectKind.ArmorPenetration),
                        Skill("Back Step", "Leap backwards.", AbilityKind.Dash, 6f, 0f, 12f, "BS", new Color(0.8f, 0.9f, 1f)),
                        Skill("Field Ration", "Eat for 40 health.", AbilityKind.Heal, 40f, 0f, 24f, "FR", new Color(0.9f, 0.7f, 0.4f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Sharpshooter", "+10% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.1f)),
                        new PerkDef("Nimble Hands", "+15% action speed.", new StatModifier(StatType.ActionSpeed, 15f)),
                        new PerkDef("Ranged Weapons Mastery", "+5 physical power.", new StatModifier(StatType.PhysicalPower, 5f)),
                        new PerkDef("Windfletch", "+5% action speed.", new StatModifier(StatType.ActionSpeed, 5f)),
                        new PerkDef("Crossbow Mastery", "+10% action speed.", new StatModifier(StatType.ActionSpeed, 10f)),
                        new PerkDef("Longshot Expert", "+10% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.1f)),
                        new PerkDef("Point-Blank Expert", "+5 physical power.", new StatModifier(StatType.PhysicalPower, 5f)),
                        new PerkDef("Crippling Shot", "+5% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.05f))
                    },
                    Kit = new[] { ("Recurve Bow", EquipSlot.Weapon1Main, 1, true), ("Arming Sword", EquipSlot.Weapon2Main, 1, true), ("Leather Cap", EquipSlot.Head, 1, true), ("Doublet", EquipSlot.Chest, 1, true), ("Leather Leggings", EquipSlot.Legs, 1, true), ("Bandage", EquipSlot.Utility1, 3, true), ("Campfire Kit", EquipSlot.Utility2, 1, true) },
                    Weapons = new[] { WeaponClass.Bow, WeaponClass.Crossbow, WeaponClass.Sword, WeaponClass.Spear, WeaponClass.Torch, WeaponClass.Dagger },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather }
                },
                new ClassDef
                {
                    Id = 4, Name = "Wizard", Description = "Glass cannon of arcane fire and frost. Needs a staff or spellbook to cast.",
                    Stats = new ClassStats(6, 7, 15, 17, 20, 25, 15), Color = new Color(0.45f, 0.5f, 0.95f), Body = new Color(0.35f, 0.35f, 0.55f),
                    Skills = new[]
                    {
                        Memory("Spell Memory", "Hold to open the spell wheel, release to ready a spell. Cast with RMB.", "SM", new Color(0.6f, 0.6f, 1f)),
                        Skill("Meditation", "Restore all spell charges.", AbilityKind.RestoreCharges, 0f, 0f, 45f, "Me", new Color(0.7f, 0.8f, 1f), castTime: 2f),
                        Skill("Arcane Shield", "Absorb 15 damage for 12s.", AbilityKind.Shield, 15f, 12f, 25f, "AS", new Color(0.5f, 0.7f, 1f)),
                        Skill("Arcane Surge", "+30% casting and action speed for 12s.", AbilityKind.Buff, 30f, 12f, 18f, "Su", new Color(0.7f, 0.5f, 1f), StatusEffectKind.ActionSpeed),
                        Skill("Intense Focus", "+100% action speed for 3s.", AbilityKind.Buff, 100f, 3f, 18f, "IF", new Color(0.9f, 0.8f, 1f), StatusEffectKind.ActionSpeed)
                    },
                    Spells = new[]
                    {
                        Projectile("Zap", "15 lightning + burn.", true, 5, 1f, 1.25f, 15f, 40f, 0f, ProjectileKind.Magic, DamageType.Magical, 1, 0f, "Zp", new Color(0.8f, 0.8f, 1f), StatusEffectKind.Burn, 3f, 3f),
                        Projectile("Ice Bolt", "20 ice, slows the target.", true, 5, 1f, 1.25f, 20f, 28f, 0f, ProjectileKind.Ice, DamageType.Magical, 1, 0f, "Ic", new Color(0.6f, 0.9f, 1f), StatusEffectKind.Slow, 20f, 1.5f),
                        Projectile("Slow", "-40% move speed for 2s.", true, 3, 1f, 1.25f, 1f, 30f, 0f, ProjectileKind.Dark, DamageType.Magical, 1, 0f, "Sl", new Color(0.5f, 0.4f, 0.7f), StatusEffectKind.Slow, 40f, 2f),
                        Projectile("Magic Missile", "Three arcane darts.", true, 10, 1f, 1.25f, 10f, 30f, 0f, ProjectileKind.Magic, DamageType.Magical, 3, 5f, "MM", new Color(0.7f, 0.6f, 1f)),
                        Spell("Haste", "+15 move speed and action speed for 6s.", AbilityKind.Buff, 4, 0.75f, 15f, 6f, "Ha", new Color(1f, 0.9f, 0.4f), StatusEffectKind.Haste),
                        Projectile("Fireball", "30 fire with splash and burn.", true, 4, 1f, 2f, 30f, 20f, -2f, ProjectileKind.Fire, DamageType.Magical, 1, 0f, "Fb", new Color(1f, 0.5f, 0.1f), StatusEffectKind.Burn, 6f, 2f, 1.6f),
                        Projectile("Lightning Strike", "25 lightning in a small area.", true, 5, 1f, 2f, 25f, 45f, 0f, ProjectileKind.Magic, DamageType.Magical, 1, 0f, "LS", new Color(0.9f, 0.9f, 1f), StatusEffectKind.None, 0f, 0f, 1.2f),
                        Spell("Invisibility", "Vanish for 4s.", AbilityKind.Invisibility, 4, 0.75f, 1f, 4f, "In", new Color(0.7f, 0.7f, 0.9f)),
                        Area("Explosion", "20 fire to everything within 3m.", true, 4, 1f, 1.75f, 20f, 3f, DamageType.Magical, "Ex", new Color(1f, 0.6f, 0.2f)),
                        Projectile("Chain Lightning", "Three bolts of 30 lightning.", true, 2, 1f, 2.5f, 30f, 50f, 0f, ProjectileKind.Magic, DamageType.Magical, 3, 8f, "CL", new Color(0.8f, 0.9f, 1f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Quick Chant", "+15% action speed.", new StatModifier(StatType.ActionSpeed, 15f)),
                        new PerkDef("Mana Surge", "+10% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.1f)),
                        new PerkDef("Glass Cannon", "-10% health, +20 magical power.", new StatModifier(StatType.MaxHealth, -10f), new StatModifier(StatType.MagicalPower, 20f)),
                        new PerkDef("Sage", "+3 knowledge.", new StatModifier(StatType.Knowledge, 3f)),
                        new PerkDef("Fire Mastery", "+5% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.05f)),
                        new PerkDef("Ice Mastery", "+5% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.05f)),
                        new PerkDef("Reactive Shield", "+30 magic resistance.", new StatModifier(StatType.MagicResistance, 30f)),
                        new PerkDef("Arcane Mastery", "+10 magical power.", new StatModifier(StatType.MagicalPower, 10f))
                    },
                    Kit = new[] { ("Magic Staff", EquipSlot.Weapon1Main, 1, true), ("Spellbook", EquipSlot.Weapon2Main, 1, true), ("Wizard Hat", EquipSlot.Head, 1, true), ("Frock", EquipSlot.Chest, 1, true), ("Cloth Pants", EquipSlot.Legs, 1, true), ("Potion of Protection", EquipSlot.Utility1, 1, true), ("Bandage", EquipSlot.Utility2, 3, true) },
                    Weapons = new[] { WeaponClass.Staff, WeaponClass.Dagger, WeaponClass.Crossbow, WeaponClass.Torch, WeaponClass.Spellbook, WeaponClass.CrystalBall },
                    Armor = new[] { ArmorType.Cloth }
                },
                new ClassDef
                {
                    Id = 5, Name = "Cleric", Description = "Holy healer in heavy armor. Bane of the undead.",
                    Stats = new ClassStats(11, 13, 12, 14, 23, 20, 12), Color = new Color(0.95f, 0.9f, 0.6f), Body = new Color(0.75f, 0.72f, 0.65f),
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
                        Spell("Bless", "+2 strength for 30s.", AbilityKind.Buff, 5, 0.75f, 2f, 30f, "Bl", new Color(1f, 1f, 0.7f), StatusEffectKind.Strength),
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
                        new PerkDef("Requiem", "+2 will.", new StatModifier(StatType.Will, 2f)),
                        new PerkDef("Protection from Evil", "+30 magic resistance.", new StatModifier(StatType.MagicResistance, 30f))
                    },
                    Kit = new[] { ("Flanged Mace", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Spellbook", EquipSlot.Weapon2Main, 1, true), ("Kettle Hat", EquipSlot.Head, 1, true), ("Adventurer Tunic", EquipSlot.Chest, 1, true), ("Plate Boots", EquipSlot.Feet, 1, true), ("Bandage", EquipSlot.Utility1, 3, true) },
                    Weapons = new[] { WeaponClass.Mace, WeaponClass.Staff, WeaponClass.Shield, WeaponClass.Torch, WeaponClass.Spellbook, WeaponClass.CrystalBall },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather, ArmorType.Chain, ArmorType.Plate }
                },
                new ClassDef
                {
                    Id = 6, Name = "Warlock", Description = "Dark magic paid for in blood. Spells cost health instead of charges.",
                    Stats = new ClassStats(11, 14, 14, 15, 22, 15, 14), Color = new Color(0.6f, 0.2f, 0.7f), Body = new Color(0.4f, 0.25f, 0.4f),
                    Skills = new[]
                    {
                        Memory("Spell Memory", "Hold to open the spell wheel, release to ready a spell. Cast with RMB.", "SM", new Color(0.8f, 0.5f, 0.9f)),
                        Skill("Phantomize", "Fade from sight for 4s.", AbilityKind.Invisibility, 1f, 4f, 28f, "Ph", new Color(0.7f, 0.4f, 0.9f)),
                        Skill("Blood Pact", "Demon form: +25% max health for 12s.", AbilityKind.Buff, 25f, 12f, 45f, "BP", new Color(0.8f, 0.2f, 0.3f), StatusEffectKind.Fortify),
                        Skill("Blow of Corruption", "+12 physical power for 8s.", AbilityKind.Buff, 12f, 8f, 24f, "BC", new Color(0.8f, 0.3f, 0.6f), StatusEffectKind.Power),
                        Skill("Dark Offering", "Sacrifice 20 health to restore all charges.", AbilityKind.RestoreCharges, 0f, 0f, 24f, "DO", new Color(0.6f, 0.1f, 0.3f), healthCost: 20)
                    },
                    Spells = new[]
                    {
                        Projectile("Bolt of Darkness", "20 dark damage. Costs 4 health.", true, 99, 1f, 1f, 20f, 30f, 0f, ProjectileKind.Dark, DamageType.Magical, 1, 0f, "BD", new Color(0.5f, 0.2f, 0.7f), healthCost: 4),
                        Projectile("Curse of Pain", "15 damage over 8s. Costs 4 health.", true, 99, 1f, 1f, 5f, 34f, 0f, ProjectileKind.Dark, DamageType.Magical, 1, 0f, "CP", new Color(0.7f, 0.2f, 0.5f), StatusEffectKind.Burn, 15f, 8f, healthCost: 4),
                        Spell("Power of Sacrifice", "+10 physical power for 12s. Costs 4 health.", AbilityKind.Buff, 99, 1f, 10f, 12f, "PS", new Color(0.9f, 0.3f, 0.4f), StatusEffectKind.Power, healthCost: 4),
                        Projectile("Curse of Weakness", "Slows the target for 10s. Costs 4 health.", true, 99, 1f, 1f, 3f, 34f, 0f, ProjectileKind.Dark, DamageType.Magical, 1, 0f, "CW", new Color(0.6f, 0.3f, 0.6f), StatusEffectKind.Slow, 20f, 10f, healthCost: 4),
                        Projectile("Ray of Darkness", "A burst of three dark rays. Costs 5 health.", true, 99, 1f, 1f, 12f, 60f, 0f, ProjectileKind.Dark, DamageType.Magical, 3, 1f, "RD", new Color(0.45f, 0.1f, 0.6f), healthCost: 5),
                        Projectile("Life Drain", "25 dark damage, heals you for the damage dealt. Costs 5 health.", true, 99, 1.5f, 1.5f, 25f, 34f, 0f, ProjectileKind.Dark, DamageType.Magical, 1, 0f, "LD", new Color(0.9f, 0.2f, 0.3f), lifeSteal: 1f, healthCost: 5),
                        Area("Hellfire", "60 fire to everything within 4m. Costs 6 health.", true, 99, 1f, 2f, 60f, 4f, DamageType.Magical, "Hf", new Color(1f, 0.4f, 0.1f), healthCost: 6),
                        Spell("Eldritch Shield", "Absorb 25 damage for 10s. Costs 6 health.", AbilityKind.Shield, 99, 0.75f, 25f, 10f, "ES", new Color(0.6f, 0.3f, 0.8f), healthCost: 6)
                    },
                    Perks = new[]
                    {
                        new PerkDef("Antimagic", "+60 magic resistance.", new StatModifier(StatType.MagicResistance, 60f)),
                        new PerkDef("Dark Enhancement", "+20% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.2f)),
                        new PerkDef("Malice", "+3 will.", new StatModifier(StatType.Will, 3f)),
                        new PerkDef("Shadow Touch", "+5% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.05f)),
                        new PerkDef("Curse Mastery", "+5 magical power.", new StatModifier(StatType.MagicalPower, 5f)),
                        new PerkDef("Demon Armor", "+20 armor rating.", new StatModifier(StatType.ArmorRating, 20f)),
                        new PerkDef("Vampirism", "+5% max health.", new StatModifier(StatType.MaxHealth, 5f)),
                        new PerkDef("Soul Collector", "+2 will.", new StatModifier(StatType.Will, 2f))
                    },
                    Kit = new[] { ("Falchion", EquipSlot.Weapon1Main, 1, true), ("Crystal Ball", EquipSlot.Weapon1Off, 1, true), ("Magic Staff", EquipSlot.Weapon2Main, 1, true), ("Frock", EquipSlot.Chest, 1, true), ("Cloth Pants", EquipSlot.Legs, 1, true), ("Adventurer Boots", EquipSlot.Feet, 1, true), ("Bandage", EquipSlot.Utility1, 3, true) },
                    Weapons = new[] { WeaponClass.Sword, WeaponClass.Staff, WeaponClass.Dagger, WeaponClass.Shield, WeaponClass.Torch, WeaponClass.Spellbook, WeaponClass.CrystalBall },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather }
                },
                new ClassDef
                {
                    Id = 7, Name = "Bard", Description = "Songs that bolster allies and shatter enemies. Needs an instrument in hand.",
                    Stats = new ClassStats(13, 13, 13, 20, 11, 20, 15), Color = new Color(0.9f, 0.6f, 0.8f), Body = new Color(0.6f, 0.45f, 0.5f),
                    Focus = CastFocus.Instrument,
                    Skills = new[]
                    {
                        Memory("Music Memory", "Hold to open the song wheel, release to ready a song. Play with RMB while holding an instrument.", "MM", new Color(1f, 0.8f, 0.9f)),
                        Area("Dissonance", "10 damage to all enemies within 6m.", false, 1, 24f, 0.4f, 10f, 6f, DamageType.Magical, "Di", new Color(1f, 0.7f, 0.9f)),
                        Skill("Encore", "Restore all song charges.", AbilityKind.RestoreCharges, 0f, 0f, 40f, "En", new Color(1f, 0.85f, 0.6f), castTime: 1f),
                        Skill("Party Maker", "+10% max health for 20s.", AbilityKind.Buff, 10f, 20f, 40f, "PM", new Color(1f, 0.8f, 0.5f), StatusEffectKind.Fortify)
                    },
                    Spells = new[]
                    {
                        Spell("Rousing Rhythms", "+2 strength for 60s.", AbilityKind.Buff, 4, 1.5f, 2f, 60f, "RR", new Color(1f, 0.85f, 0.6f), StatusEffectKind.Strength),
                        Spell("Beats of Alacrity", "+6 move speed for 20s.", AbilityKind.Buff, 4, 1f, 6f, 20f, "BA", new Color(1f, 0.9f, 0.5f), StatusEffectKind.Haste),
                        Spell("Accelerando", "+12 move speed for 10s.", AbilityKind.Buff, 4, 1f, 12f, 10f, "Ac", new Color(1f, 0.95f, 0.6f), StatusEffectKind.Haste),
                        Spell("Allegro", "+5% action speed for 20s.", AbilityKind.Buff, 4, 1f, 5f, 20f, "Al", new Color(0.9f, 0.9f, 1f), StatusEffectKind.ActionSpeed),
                        Spell("Harmonic Shield", "Absorb 15 damage for 20s.", AbilityKind.Shield, 4, 1f, 15f, 20f, "HS", new Color(0.8f, 0.8f, 1f)),
                        Area("Piercing Shrill", "20 damage within 5m.", true, 4, 1f, 1f, 20f, 5f, DamageType.Physical, "PS", new Color(1f, 0.6f, 0.7f)),
                        Area("Shriek of Weakness", "5 damage within 5m.", true, 4, 1f, 1f, 5f, 5f, DamageType.Magical, "SW", new Color(0.9f, 0.5f, 0.7f)),
                        Spell("Song of Shadow", "Invisible for 10s.", AbilityKind.Invisibility, 4, 1.5f, 1f, 10f, "SS", new Color(0.6f, 0.5f, 0.8f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Rapier Mastery", "+3 physical power, +5% action speed.", new StatModifier(StatType.PhysicalPower, 3f), new StatModifier(StatType.ActionSpeed, 5f)),
                        new PerkDef("Dancing Feet", "+10 move speed.", new StatModifier(StatType.MoveSpeed, 10f)),
                        new PerkDef("Melodic Protection", "+20 armor rating.", new StatModifier(StatType.ArmorRating, 20f)),
                        new PerkDef("Lore Mastery", "+5 resourcefulness.", new StatModifier(StatType.Resourcefulness, 5f)),
                        new PerkDef("War Song", "+3 physical power.", new StatModifier(StatType.PhysicalPower, 3f)),
                        new PerkDef("Story Teller", "+5 will, +3 knowledge.", new StatModifier(StatType.Will, 5f), new StatModifier(StatType.Knowledge, 3f)),
                        new PerkDef("Superior Dexterity", "+10% action speed.", new StatModifier(StatType.ActionSpeed, 10f)),
                        new PerkDef("Wanderer's Luck", "+3 resourcefulness.", new StatModifier(StatType.Resourcefulness, 3f))
                    },
                    Kit = new[] { ("Lute", EquipSlot.Weapon1Main, 1, true), ("Arming Sword", EquipSlot.Weapon2Main, 1, true), ("Round Shield", EquipSlot.Weapon2Off, 1, true), ("Woolen Cap", EquipSlot.Head, 1, true), ("Doublet", EquipSlot.Chest, 1, true), ("Bandage", EquipSlot.Utility1, 3, true), ("Ale", EquipSlot.Utility2, 2, true) },
                    Weapons = new[] { WeaponClass.Sword, WeaponClass.Dagger, WeaponClass.Crossbow, WeaponClass.Bow, WeaponClass.Shield, WeaponClass.Torch, WeaponClass.Instrument },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather }
                },
                new ClassDef
                {
                    Id = 8, Name = "Druid", Description = "Nature's wrath and nature's mercy. Shapeshifts into beasts.",
                    Stats = new ClassStats(12, 13, 12, 12, 18, 20, 18), Color = new Color(0.5f, 0.8f, 0.4f), Body = new Color(0.45f, 0.55f, 0.4f),
                    Skills = new[]
                    {
                        Shapeshift("Shapeshift Memory", "Hold to open the form wheel: bear, panther or rat.", "SF", new Color(0.6f, 0.9f, 0.5f)),
                        Memory("Spell Memory", "Hold to open the spell wheel, release to ready a spell. Cast with RMB.", "SM", new Color(0.7f, 1f, 0.6f)),
                        Skill("Wild Fury", "+15 physical power for 10s.", AbilityKind.Buff, 15f, 10f, 32f, "WF", new Color(0.7f, 1f, 0.5f), StatusEffectKind.Power),
                        Skill("Rush", "Lunge forward.", AbilityKind.Dash, -7f, 0f, 24f, "Ru", new Color(0.6f, 0.9f, 0.5f)),
                        Skill("Survival Instinct", "Heal 20 instantly.", AbilityKind.Heal, 20f, 0f, 28f, "SI", new Color(0.8f, 1f, 0.7f))
                    },
                    Spells = new[]
                    {
                        Spell("Nature's Touch", "Heal 30 over 12s.", AbilityKind.Heal, 4, 0.75f, 30f, 12f, "NT", new Color(0.6f, 1f, 0.6f)),
                        Spell("Barkskin", "Absorb 20 damage for 10s.", AbilityKind.Shield, 4, 0.75f, 20f, 10f, "Bk", new Color(0.6f, 0.45f, 0.3f)),
                        Projectile("Dreamfire", "15 spirit damage with splash.", true, 4, 1f, 1f, 15f, 26f, 0f, ProjectileKind.Magic, DamageType.Magical, 1, 0f, "Df", new Color(0.5f, 1f, 0.7f), StatusEffectKind.None, 0f, 0f, 1f),
                        Projectile("Entangling Vines", "Roots the target briefly.", true, 2, 1f, 1.25f, 5f, 24f, 0f, ProjectileKind.Magic, DamageType.Magical, 1, 0f, "EV", new Color(0.4f, 0.7f, 0.3f), StatusEffectKind.Slow, 90f, 1.5f),
                        Spell("Restore", "Heal 20 over 12s.", AbilityKind.Heal, 3, 1.5f, 20f, 12f, "Re", new Color(0.7f, 1f, 0.7f)),
                        Spell("Tree of Life", "Heal 25 and +3 strength for 12s.", AbilityKind.Heal, 2, 1f, 25f, 0f, "TL", new Color(0.5f, 0.9f, 0.5f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Enhanced Wildness", "+5 physical power, +20 armor.", new StatModifier(StatType.PhysicalPower, 5f), new StatModifier(StatType.ArmorRating, 20f)),
                        new PerkDef("Thorn Coat", "+10 armor rating.", new StatModifier(StatType.ArmorRating, 10f)),
                        new PerkDef("Natural Healing", "+2 vigor.", new StatModifier(StatType.Vigor, 2f)),
                        new PerkDef("Sun and Moon", "+3 vigor, +5 magical power.", new StatModifier(StatType.Vigor, 3f), new StatModifier(StatType.MagicalPower, 5f)),
                        new PerkDef("Shapeshift Mastery", "+10% action speed.", new StatModifier(StatType.ActionSpeed, 10f)),
                        new PerkDef("Torpor", "+2 vigor.", new StatModifier(StatType.Vigor, 2f)),
                        new PerkDef("Nature's Blessing", "+5 magical power.", new StatModifier(StatType.MagicalPower, 5f)),
                        new PerkDef("Wild Instinct", "+5 move speed.", new StatModifier(StatType.MoveSpeed, 5f))
                    },
                    Kit = new[] { ("Spear", EquipSlot.Weapon1Main, 1, true), ("Spellbook", EquipSlot.Weapon2Main, 1, true), ("Adventurer Tunic", EquipSlot.Chest, 1, true), ("Leather Leggings", EquipSlot.Legs, 1, true), ("Bandage", EquipSlot.Utility1, 3, true), ("Campfire Kit", EquipSlot.Utility2, 1, true) },
                    Weapons = new[] { WeaponClass.Staff, WeaponClass.Spear, WeaponClass.Mace, WeaponClass.Dagger, WeaponClass.Torch, WeaponClass.Spellbook, WeaponClass.CrystalBall },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather }
                },
                new ClassDef
                {
                    Id = 9, Name = "Sorcerer", Description = "Elemental caster on cooldowns. Casts even with bare hands.",
                    Stats = new ClassStats(10, 10, 10, 18, 25, 20, 12), Color = new Color(0.3f, 0.8f, 0.9f), Body = new Color(0.3f, 0.45f, 0.55f),
                    Focus = CastFocus.BareHands,
                    Skills = new[]
                    {
                        Memory("Sorcery Memory", "Hold to open the spell wheel, release to ready a spell. Cast with RMB.", "SM", new Color(0.6f, 0.9f, 1f)),
                        Skill("Sorcery Combat", "+25% action speed for 8s.", AbilityKind.Buff, 25f, 8f, 30f, "SC", new Color(0.5f, 0.9f, 1f), StatusEffectKind.ActionSpeed),
                        Skill("Mana Fold", "+10 move speed for 8s.", AbilityKind.Buff, 10f, 8f, 24f, "MF", new Color(0.7f, 0.9f, 1f), StatusEffectKind.Haste),
                        Skill("Elemental Fury", "+15 physical power for 8s.", AbilityKind.Buff, 15f, 8f, 30f, "EF", new Color(1f, 0.6f, 0.4f), StatusEffectKind.Power)
                    },
                    Spells = new[]
                    {
                        Projectile("Fire Arrow", "10 fire. 12s cooldown.", true, 99, 12f, 0.75f, 10f, 35f, 0f, ProjectileKind.Fire, DamageType.Magical, 1, 0f, "FA", new Color(1f, 0.5f, 0.2f), StatusEffectKind.Burn, 3f, 2f),
                        Projectile("Water Bolt", "15 water, slows. 12s cooldown.", true, 99, 12f, 0.75f, 15f, 30f, 0f, ProjectileKind.Ice, DamageType.Magical, 1, 0f, "WB", new Color(0.4f, 0.6f, 1f), StatusEffectKind.Slow, 20f, 1f),
                        Projectile("Ice Spear", "30 ice. 15s cooldown.", true, 99, 15f, 0.75f, 30f, 34f, 0f, ProjectileKind.Ice, DamageType.Magical, 1, 0f, "IS", new Color(0.7f, 0.9f, 1f)),
                        Area("Eruption", "20 earth damage within 3m. 15s cooldown.", true, 99, 15f, 1f, 20f, 3f, DamageType.Magical, "Er", new Color(0.8f, 0.5f, 0.3f)),
                        Area("Flamestrike", "30 fire within 2.5m. 15s cooldown.", true, 99, 15f, 1f, 30f, 2.5f, DamageType.Magical, "Fs", new Color(1f, 0.45f, 0.15f)),
                        Projectile("Fire Orb", "45 fire with splash. 18s cooldown.", true, 99, 18f, 0.75f, 45f, 12f, 0f, ProjectileKind.Fire, DamageType.Magical, 1, 0f, "FO", new Color(1f, 0.35f, 0.1f), StatusEffectKind.Burn, 5f, 3f, 1.2f),
                        Projectile("Lightning Bolt", "25 lightning. 18s cooldown.", true, 99, 18f, 0.75f, 25f, 60f, 0f, ProjectileKind.Magic, DamageType.Magical, 1, 0f, "LB", new Color(0.9f, 0.9f, 1f)),
                        Area("Vortex", "15 damage and a slow within 3m. 21s cooldown.", true, 99, 21f, 0.75f, 15f, 3f, DamageType.Magical, "Vx", new Color(0.5f, 0.7f, 1f)),
                        Spell("Stone Skin", "Absorb 30 damage for 12s. 20s cooldown.", AbilityKind.Shield, 99, 0.75f, 30f, 12f, "SS", new Color(0.6f, 0.6f, 0.55f), cooldown: 20f)
                    },
                    Perks = new[]
                    {
                        new PerkDef("Apex of Sorcery", "+10 magical power.", new StatModifier(StatType.MagicalPower, 10f)),
                        new PerkDef("Time Distortion", "+10% action speed.", new StatModifier(StatType.ActionSpeed, 10f)),
                        new PerkDef("Mana Flow", "+10% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.1f)),
                        new PerkDef("Lightning Mastery", "+5% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.05f)),
                        new PerkDef("Elemental Fury", "+10% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.1f)),
                        new PerkDef("Fire Mastery", "+5% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.05f)),
                        new PerkDef("Frost Mastery", "+5% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.05f)),
                        new PerkDef("Arcane Focus", "+3 will.", new StatModifier(StatType.Will, 3f))
                    },
                    Kit = new[] { ("Magic Staff", EquipSlot.Weapon1Main, 1, true), ("Falchion", EquipSlot.Weapon2Main, 1, true), ("Wizard Hat", EquipSlot.Head, 1, true), ("Frock", EquipSlot.Chest, 1, true), ("Cloth Pants", EquipSlot.Legs, 1, true), ("Bandage", EquipSlot.Utility1, 3, true) },
                    Weapons = new[] { WeaponClass.Staff, WeaponClass.Sword, WeaponClass.Torch, WeaponClass.Spellbook, WeaponClass.CrystalBall },
                    Armor = new[] { ArmorType.Cloth }
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
