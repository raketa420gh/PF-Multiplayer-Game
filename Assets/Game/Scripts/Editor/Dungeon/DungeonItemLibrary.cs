using System.Collections.Generic;
using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    internal sealed class ItemDef
    {
        public string Name;
        public ItemKind Kind;
        public int Width = 1;
        public int Height = 1;
        public int Stack = 1;
        public int Value = 10;
        public Color Color = Color.white;
        public string Glyph = "?";
        public ItemRarity Rarity = ItemRarity.Common;
        public bool RollsRarity;
        public string Description = string.Empty;
        public List<StatModifier> Modifiers = new();

        public string WeaponPrefix;
        public string ShieldPrefix;
        public WeaponClass WeaponClass;
        public bool TwoHanded;
        public bool OffHand;
        public float MovePenalty;
        public float LightRange;

        public EquipSlot Slot;
        public ArmorType ArmorType;
        public float Armor;
        public float MagicResist;
        public ArmorVisual Visual;
        public Color VisualColor = Color.gray;

        public ConsumableEffect Effect;
        public float Magnitude;
        public float Duration;
        public float UseTime = 1f;

        public UtilityKind Utility;
        public int Damage;
    }

    internal sealed class AbilityDef
    {
        public string Name;
        public string Description;
        public AbilityKind Kind;
        public bool IsSpell;
        public int Charges = 1;
        public float Cooldown = 20f;
        public float CastTime;
        public float Magnitude;
        public float Duration;
        public float Radius;
        public StatusEffectKind Effect;
        public DamageType DamageType = DamageType.Magical;
        public float Speed = 26f;
        public float Gravity;
        public int Count = 1;
        public float Spread;
        public ProjectileKind Projectile = ProjectileKind.Magic;
        public StatusEffectKind HitEffect;
        public float HitMagnitude;
        public float HitDuration;
        public Color Color = Color.white;
        public string Glyph = "*";
    }

    internal sealed class PerkDef
    {
        public string Name;
        public string Description;
        public StatModifier[] Modifiers;

        public PerkDef(string name, string description, params StatModifier[] modifiers)
        {
            Name = name;
            Description = description;
            Modifiers = modifiers;
        }
    }

    internal sealed class ClassDef
    {
        public byte Id;
        public string Name;
        public string Description;
        public ClassStats Stats;
        public Color Color;
        public Color Body;
        public AbilityDef[] Skills;
        public AbilityDef[] Spells = System.Array.Empty<AbilityDef>();
        public PerkDef[] Perks;
        public (string item, EquipSlot slot, int count, bool equipped)[] Kit;
        public WeaponClass[] Weapons;
        public ArmorType[] Armor;
    }

    /// Items, classes, abilities and perks of the prototype, numbers taken from the Dark and Darker wiki.
    internal static class DungeonItemLibrary
    {
        private static readonly Color s_steel = new(0.8f, 0.82f, 0.88f);
        private static readonly Color s_wood = new(0.65f, 0.45f, 0.25f);
        private static readonly Color s_leather = new(0.55f, 0.35f, 0.18f);
        private static readonly Color s_cloth = new(0.75f, 0.7f, 0.6f);
        private static readonly Color s_plate = new(0.7f, 0.72f, 0.78f);
        private static readonly Color s_potion = new(0.9f, 0.25f, 0.3f);
        private static readonly Color s_gold = new(1f, 0.82f, 0.3f);

        public static List<ItemDef> CreateItems()
        {
            List<ItemDef> items = new()
            {
                Weapon("Arming Sword", DungeonWeaponLibrary.ArmingSword, WeaponClass.Sword, 1, 3, 20f, "Sw", s_steel, 25, "A reliable one-handed sword. Pairs with a shield.", shield: DungeonWeaponLibrary.SwordShield),
                Weapon("Falchion", DungeonWeaponLibrary.Falchion, WeaponClass.Sword, 1, 3, 25f, "Sw", s_steel, 30, "Heavy curved blade with strong slashes."),
                Weapon("Longsword", DungeonWeaponLibrary.Longsword, WeaponClass.Sword, 1, 4, 30f, "2H", s_steel, 45, "Two-handed sword with a fast three-hit chain.", twoHanded: true),
                Weapon("Zweihander", DungeonWeaponLibrary.Greatsword, WeaponClass.Sword, 1, 4, 40f, "2H", s_steel, 60, "Massive greatsword. Slow, wide, devastating.", twoHanded: true),
                Weapon("Battle Axe", DungeonWeaponLibrary.BattleAxe, WeaponClass.Axe, 2, 4, 30f, "Ax", s_steel, 50, "Two-handed axe. Two heavy chops.", twoHanded: true),
                Weapon("Spear", DungeonWeaponLibrary.Spear, WeaponClass.Spear, 1, 5, 40f, "Sp", s_wood, 40, "Long reach thrusts keep enemies at bay.", twoHanded: true),
                Weapon("Flanged Mace", DungeonWeaponLibrary.Mace, WeaponClass.Mace, 1, 3, 20f, "Mc", s_steel, 28, "Blunt one-hander. Staggers on the third hit.", shield: DungeonWeaponLibrary.MaceShield),
                Weapon("Rondel Dagger", DungeonWeaponLibrary.Dagger, WeaponClass.Dagger, 1, 2, 10f, "Dg", s_steel, 18, "Quick stabs. Weak, but barely slows you down."),
                Weapon("Recurve Bow", DungeonWeaponLibrary.Bow, WeaponClass.Bow, 1, 3, 40f, "Bw", s_wood, 45, "Draw and release. Arrows fall with distance.", twoHanded: true),
                Weapon("Crossbow", DungeonWeaponLibrary.Crossbow, WeaponClass.Crossbow, 2, 3, 50f, "Xb", s_wood, 55, "Hard-hitting bolt, slow reload.", twoHanded: true),
                Weapon("Magic Staff", DungeonWeaponLibrary.Staff, WeaponClass.Staff, 1, 4, 20f, "St", new Color(0.5f, 0.7f, 1f), 50, "Caster focus. Also a decent club.", twoHanded: true, modifiers: new[] { new StatModifier(StatType.MagicalPower, 4f) }),
                Weapon("Torch", DungeonWeaponLibrary.Torch, WeaponClass.Torch, 1, 3, 5f, "Tr", new Color(1f, 0.6f, 0.2f), 2, "Lights the way. Can be swung in a pinch.", light: 9f),
                Weapon("Round Shield", null, WeaponClass.Shield, 2, 3, 13f, "Sh", s_wood, 30, "Blocks with a one-handed weapon in the main hand.", offHand: true, modifiers: new[] { new StatModifier(StatType.ArmorRating, 20f) }),

                Armor("Woolen Cap", EquipSlot.Head, ArmorType.Cloth, 20f, 2f, ArmorVisual.Cap, new Color(0.35f, 0.3f, 0.25f), 2, 2, "Hd", 8),
                Armor("Rogue Cowl", EquipSlot.Head, ArmorType.Cloth, 25f, 2f, ArmorVisual.Hood, new Color(0.2f, 0.2f, 0.22f), 2, 2, "Hd", 14, modifiers: new[] { new StatModifier(StatType.Agility, 1f) }),
                Armor("Wizard Hat", EquipSlot.Head, ArmorType.Cloth, 20f, 2f, ArmorVisual.Hood, new Color(0.25f, 0.2f, 0.45f), 2, 2, "Hd", 14, modifiers: new[] { new StatModifier(StatType.Knowledge, 1f) }),
                Armor("Leather Cap", EquipSlot.Head, ArmorType.Leather, 31f, 3f, ArmorVisual.Cap, s_leather, 2, 2, "Hd", 15),
                Armor("Kettle Hat", EquipSlot.Head, ArmorType.Plate, 30f, 3f, ArmorVisual.Helmet, s_plate, 2, 2, "Hd", 22),
                Armor("Great Helm", EquipSlot.Head, ArmorType.Plate, 49f, 7f, ArmorVisual.GreatHelm, s_plate, 2, 2, "Hd", 40),
                Armor("Adventurer Tunic", EquipSlot.Chest, ArmorType.Cloth, 33f, 3f, ArmorVisual.Tunic, new Color(0.45f, 0.4f, 0.3f), 2, 3, "Ch", 12),
                Armor("Frock", EquipSlot.Chest, ArmorType.Cloth, 42f, 4f, ArmorVisual.Tunic, new Color(0.3f, 0.25f, 0.5f), 2, 3, "Ch", 25, magicResist: 25f),
                Armor("Doublet", EquipSlot.Chest, ArmorType.Leather, 44f, 4f, ArmorVisual.LeatherChest, new Color(0.4f, 0.25f, 0.15f), 2, 3, "Ch", 28, modifiers: new[] { new StatModifier(StatType.Dexterity, 1f) }),
                Armor("Heavy Gambeson", EquipSlot.Chest, ArmorType.Leather, 85f, 8f, ArmorVisual.LeatherChest, new Color(0.5f, 0.42f, 0.3f), 2, 3, "Ch", 45),
                Armor("Templar Armor", EquipSlot.Chest, ArmorType.Plate, 81f, 9f, ArmorVisual.PlateChest, new Color(0.75f, 0.75f, 0.8f), 2, 3, "Ch", 70, magicResist: 20f),
                Armor("Dark Plate Armor", EquipSlot.Chest, ArmorType.Plate, 101f, 14f, ArmorVisual.PlateChest, new Color(0.25f, 0.25f, 0.3f), 2, 3, "Ch", 110),
                Armor("Leather Gloves", EquipSlot.Hands, ArmorType.Leather, 15f, 0f, ArmorVisual.Gloves, s_leather, 2, 2, "Gl", 10),
                Armor("Heavy Gauntlets", EquipSlot.Hands, ArmorType.Plate, 31f, 1f, ArmorVisual.Gauntlets, s_plate, 2, 2, "Gl", 32, magicResist: -5f),
                Armor("Cloth Pants", EquipSlot.Legs, ArmorType.Cloth, 30f, 3f, ArmorVisual.Pants, s_cloth, 2, 2, "Lg", 10),
                Armor("Leather Leggings", EquipSlot.Legs, ArmorType.Leather, 43f, 4f, ArmorVisual.Pants, s_leather, 2, 2, "Lg", 20),
                Armor("Plate Pants", EquipSlot.Legs, ArmorType.Plate, 75f, 8f, ArmorVisual.Greaves, s_plate, 2, 2, "Lg", 55),
                Armor("Adventurer Boots", EquipSlot.Feet, ArmorType.Leather, 23f, -6f, ArmorVisual.Boots, s_leather, 2, 2, "Bt", 12),
                Armor("Plate Boots", EquipSlot.Feet, ArmorType.Plate, 42f, -4f, ArmorVisual.PlateBoots, s_plate, 2, 2, "Bt", 38, magicResist: -5f),
                Armor("Adventurer Cloak", EquipSlot.Back, ArmorType.Cloth, 6f, 0f, ArmorVisual.Cloak, new Color(0.3f, 0.12f, 0.1f), 2, 3, "Ck", 15, modifiers: new[] { new StatModifier(StatType.Agility, 1f) }),
                Jewelry("Gem Necklace", EquipSlot.Necklace, "Nk", 60, new StatModifier(StatType.Will, 2f)),
                Jewelry("Gold Band", EquipSlot.Ring1, "Rg", 45, new StatModifier(StatType.Strength, 1f)),
                Jewelry("Gem Ring", EquipSlot.Ring1, "Rg", 45, new StatModifier(StatType.Agility, 1f)),

                Consumable("Bandage", ConsumableEffect.HealInstant, 15f, 0f, 4f, 3, "+", new Color(0.9f, 0.9f, 0.85f), 5, "Wrap a wound. Heals after a few seconds."),
                Consumable("Potion of Healing", ConsumableEffect.HealOverTime, 20f, 20f, 3f, 3, "Po", s_potion, 12, "Restores health over time."),
                Consumable("Potion of Protection", ConsumableEffect.Protection, 10f, 24f, 3f, 3, "Po", new Color(0.3f, 0.5f, 1f), 14, "A shield that absorbs physical damage."),
                Consumable("Surgical Kit", ConsumableEffect.HealInstant, 100f, 0f, 12f, 1, "+", new Color(0.7f, 0.2f, 0.2f), 30, "Full heal after a long, vulnerable procedure.", 2, 2),
                Consumable("Ale", ConsumableEffect.Haste, 6f, 15f, 3f, 2, "Al", new Color(0.85f, 0.6f, 0.2f), 8, "Liquid courage. Quickens the step."),

                Utility("Francisca Axe", UtilityKind.ThrowingWeapon, 18, 0.5f, 2, 1, 2, "Ax", s_steel, 12, "Thrown axe."),
                Utility("Throwing Knife", UtilityKind.ThrowingWeapon, 14, 0.4f, 2, 1, 2, "Dg", s_steel, 10, "Thrown knife."),
                Utility("Campfire Kit", UtilityKind.Campfire, 0, 4f, 1, 2, 2, "Cf", new Color(1f, 0.6f, 0.3f), 20, "Place a campfire and rest by it to heal."),
                Utility("Lockpick", UtilityKind.Lockpick, 0, 0f, 3, 1, 1, "Lp", s_steel, 6, "Opens one locked door or chest."),

                Treasure("Gold Coins", 1, 1, 25, 1, "$", s_gold),
                Treasure("Gold Coin Purse", 1, 1, 1, 100, "$$", s_gold),
                Treasure("Gold Coin Bag", 2, 2, 1, 500, "$$$", s_gold),
                Treasure("Ruby", 1, 1, 3, 12, "Gm", new Color(0.95f, 0.2f, 0.25f), true),
                Treasure("Emerald", 1, 1, 3, 12, "Gm", new Color(0.2f, 0.85f, 0.35f), true),
                Treasure("Sapphire", 1, 1, 3, 12, "Gm", new Color(0.25f, 0.4f, 0.95f), true),
                Treasure("Diamond", 1, 1, 3, 25, "Dm", new Color(0.9f, 0.95f, 1f), true),
                Treasure("Gold Goblet", 1, 1, 1, 35, "Gb", s_gold),
                Treasure("Gold Candlestick", 1, 1, 1, 50, "Cs", s_gold),
                Treasure("Ancient Scroll", 1, 1, 3, 60, "Sc", new Color(0.85f, 0.75f, 0.5f), true)
            };

            return items;
        }

        private static ItemDef Weapon(string name, string prefix, WeaponClass weaponClass, int width, int height, float movePenalty, string glyph,
            Color color, int value, string description, bool twoHanded = false, bool offHand = false, float light = 0f, string shield = null,
            StatModifier[] modifiers = null)
        {
            return new ItemDef
            {
                Name = name, Kind = ItemKind.Weapon, WeaponPrefix = prefix, ShieldPrefix = shield, WeaponClass = weaponClass, Width = width, Height = height,
                MovePenalty = movePenalty, Glyph = glyph, Color = color, Value = value, Description = description, TwoHanded = twoHanded, OffHand = offHand,
                LightRange = light, RollsRarity = true, Modifiers = modifiers != null ? new List<StatModifier>(modifiers) : new List<StatModifier>()
            };
        }

        private static ItemDef Armor(string name, EquipSlot slot, ArmorType type, float armor, float movePenalty, ArmorVisual visual, Color visualColor,
            int width, int height, string glyph, int value, float magicResist = 0f, StatModifier[] modifiers = null)
        {
            return new ItemDef
            {
                Name = name, Kind = ItemKind.Armor, Slot = slot, ArmorType = type, Armor = armor, MovePenalty = movePenalty, Visual = visual,
                VisualColor = visualColor, Width = width, Height = height, Glyph = glyph, Color = visualColor * 1.3f, Value = value, MagicResist = magicResist,
                RollsRarity = true, Modifiers = modifiers != null ? new List<StatModifier>(modifiers) : new List<StatModifier>()
            };
        }

        private static ItemDef Jewelry(string name, EquipSlot slot, string glyph, int value, params StatModifier[] modifiers)
        {
            return new ItemDef
            {
                Name = name, Kind = ItemKind.Armor, Slot = slot, ArmorType = ArmorType.Cloth, Glyph = glyph, Color = s_gold, Value = value,
                Rarity = ItemRarity.Uncommon, RollsRarity = true, Modifiers = new List<StatModifier>(modifiers), Visual = ArmorVisual.None
            };
        }

        private static ItemDef Consumable(string name, ConsumableEffect effect, float magnitude, float duration, float useTime, int stack, string glyph,
            Color color, int value, string description, int width = 1, int height = 1)
        {
            return new ItemDef
            {
                Name = name, Kind = ItemKind.Consumable, Effect = effect, Magnitude = magnitude, Duration = duration, UseTime = useTime, Stack = stack,
                Glyph = glyph, Color = color, Value = value, Description = description, Width = width, Height = height, RollsRarity = true
            };
        }

        private static ItemDef Utility(string name, UtilityKind kind, int damage, float useTime, int stack, int width, int height, string glyph, Color color,
            int value, string description)
        {
            return new ItemDef
            {
                Name = name, Kind = ItemKind.Utility, Utility = kind, Damage = damage, UseTime = useTime, Stack = stack, Width = width, Height = height,
                Glyph = glyph, Color = color, Value = value, Description = description
            };
        }

        private static ItemDef Treasure(string name, int width, int height, int stack, int value, string glyph, Color color, bool rolls = false)
        {
            return new ItemDef
            {
                Name = name, Kind = ItemKind.Treasure, Width = width, Height = height, Stack = stack, Value = value, Glyph = glyph, Color = color,
                RollsRarity = rolls, Description = "Sells for gold back in town."
            };
        }

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
                        Skill("Second Wind", "Recover 40% of max health over 12s.", AbilityKind.Heal, 50f, 12f, 60f, "Hp", new Color(0.3f, 1f, 0.4f)),
                        Skill("Sprint", "+39 move speed for 6s.", AbilityKind.Buff, 39f, 6f, 28f, "Sp", new Color(1f, 0.9f, 0.3f), StatusEffectKind.Haste)
                    },
                    Perks = new[]
                    {
                        new PerkDef("Defense Mastery", "+15 armor rating.", new StatModifier(StatType.ArmorRating, 15f)),
                        new PerkDef("Swift", "+10 move speed from lighter armor handling.", new StatModifier(StatType.MoveSpeed, 10f)),
                        new PerkDef("Combo Attack", "+5% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.05f)),
                        new PerkDef("Barricade", "+25 magic resistance.", new StatModifier(StatType.MagicResistance, 25f))
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
                        Skill("Rage", "+10 strength and +7% move speed for 8s, armor lowered.", AbilityKind.Buff, 10f, 8f, 28f, "Rg", new Color(1f, 0.3f, 0.2f), StatusEffectKind.Rage),
                        Skill("Reckless Attack", "+15 physical power for 8s.", AbilityKind.Buff, 15f, 8f, 24f, "At", new Color(1f, 0.5f, 0.2f), StatusEffectKind.Power)
                    },
                    Perks = new[]
                    {
                        new PerkDef("Two-Hander", "+5% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.05f)),
                        new PerkDef("Iron Will", "+75 magic resistance.", new StatModifier(StatType.MagicResistance, 75f)),
                        new PerkDef("Robust", "+10% max health.", new StatModifier(StatType.MaxHealth, 10f)),
                        new PerkDef("Savage", "+10 physical power.", new StatModifier(StatType.PhysicalPower, 10f))
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
                        Skill("Tumbling", "Backflip away from danger.", AbilityKind.Dash, 7f, 0f, 8f, "Tu", new Color(0.8f, 0.9f, 1f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Back Attack", "+10% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.1f)),
                        new PerkDef("Dagger Mastery", "+5 physical power.", new StatModifier(StatType.PhysicalPower, 5f)),
                        new PerkDef("Creep", "+10 move speed.", new StatModifier(StatType.MoveSpeed, 10f)),
                        new PerkDef("Poisoned Weapon", "+5% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.05f))
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
                        Projectile("Multishot", "Fire five arrows in a cone.", false, 1, 22f, 0.3f, 20f, 30f, -9.81f, ProjectileKind.Arrow, DamageType.Physical, 5, 12f, "MS", new Color(0.8f, 0.9f, 0.5f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Sharpshooter", "+10% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.1f)),
                        new PerkDef("Nimble Hands", "+15% action speed.", new StatModifier(StatType.ActionSpeed, 15f)),
                        new PerkDef("Ranged Weapons Mastery", "+5 physical power.", new StatModifier(StatType.PhysicalPower, 5f)),
                        new PerkDef("Windfletch", "+5% action speed.", new StatModifier(StatType.ActionSpeed, 5f))
                    },
                    Kit = new[] { ("Recurve Bow", EquipSlot.Weapon1Main, 1, true), ("Arming Sword", EquipSlot.Weapon2Main, 1, true), ("Leather Cap", EquipSlot.Head, 1, true), ("Doublet", EquipSlot.Chest, 1, true), ("Leather Leggings", EquipSlot.Legs, 1, true), ("Bandage", EquipSlot.Utility1, 3, true), ("Campfire Kit", EquipSlot.Utility2, 1, true) },
                    Weapons = new[] { WeaponClass.Bow, WeaponClass.Crossbow, WeaponClass.Sword, WeaponClass.Spear, WeaponClass.Torch, WeaponClass.Dagger },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather }
                },
                new ClassDef
                {
                    Id = 4, Name = "Wizard", Description = "Glass cannon of arcane fire and frost.",
                    Stats = new ClassStats(6, 7, 15, 17, 20, 25, 15), Color = new Color(0.45f, 0.5f, 0.95f), Body = new Color(0.35f, 0.35f, 0.55f),
                    Skills = new[]
                    {
                        Skill("Arcane Shield", "Absorb 15 damage for 12s.", AbilityKind.Shield, 15f, 12f, 25f, "Sh", new Color(0.5f, 0.7f, 1f)),
                        Skill("Arcane Surge", "+30% action speed for 12s.", AbilityKind.Buff, 30f, 12f, 18f, "Ar", new Color(0.7f, 0.5f, 1f), StatusEffectKind.ActionSpeed)
                    },
                    Spells = new[]
                    {
                        Projectile("Zap", "15 lightning + burn.", true, 5, 1f, 1.25f, 15f, 40f, 0f, ProjectileKind.Magic, DamageType.Magical, 1, 0f, "Zp", new Color(0.8f, 0.8f, 1f), StatusEffectKind.Burn, 3f, 3f),
                        Projectile("Ice Bolt", "20 ice, slows the target.", true, 5, 1f, 1.25f, 20f, 28f, 0f, ProjectileKind.Ice, DamageType.Magical, 1, 0f, "Ic", new Color(0.6f, 0.9f, 1f), StatusEffectKind.Slow, 20f, 1.5f),
                        Projectile("Magic Missile", "Three arcane darts.", true, 10, 1f, 1.25f, 10f, 30f, 0f, ProjectileKind.Magic, DamageType.Magical, 3, 5f, "MM", new Color(0.7f, 0.6f, 1f)),
                        Projectile("Fireball", "30 fire with splash and burn.", true, 4, 1f, 2f, 30f, 20f, -2f, ProjectileKind.Fire, DamageType.Magical, 1, 0f, "Mc", new Color(1f, 0.5f, 0.1f), StatusEffectKind.Burn, 6f, 2f, 1.6f),
                        Spell("Haste", "+15 move speed and action speed for 6s.", AbilityKind.Buff, 4, 0.75f, 15f, 6f, "Sp", new Color(1f, 0.9f, 0.4f), StatusEffectKind.Haste)
                    },
                    Perks = new[]
                    {
                        new PerkDef("Quick Chant", "+15% action speed.", new StatModifier(StatType.ActionSpeed, 15f)),
                        new PerkDef("Mana Surge", "+10% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.1f)),
                        new PerkDef("Glass Cannon", "-10% health, +20 magical power.", new StatModifier(StatType.MaxHealth, -10f), new StatModifier(StatType.MagicalPower, 20f)),
                        new PerkDef("Sage", "+3 knowledge.", new StatModifier(StatType.Knowledge, 3f))
                    },
                    Kit = new[] { ("Magic Staff", EquipSlot.Weapon1Main, 1, true), ("Rondel Dagger", EquipSlot.Weapon2Main, 1, true), ("Wizard Hat", EquipSlot.Head, 1, true), ("Frock", EquipSlot.Chest, 1, true), ("Cloth Pants", EquipSlot.Legs, 1, true), ("Potion of Protection", EquipSlot.Utility1, 1, true), ("Bandage", EquipSlot.Utility2, 3, true) },
                    Weapons = new[] { WeaponClass.Staff, WeaponClass.Dagger, WeaponClass.Crossbow, WeaponClass.Torch },
                    Armor = new[] { ArmorType.Cloth }
                },
                new ClassDef
                {
                    Id = 5, Name = "Cleric", Description = "Holy healer in heavy armor. Bane of the undead.",
                    Stats = new ClassStats(11, 13, 12, 14, 23, 20, 12), Color = new Color(0.95f, 0.9f, 0.6f), Body = new Color(0.75f, 0.72f, 0.65f),
                    Skills = new[]
                    {
                        Skill("Divine Protection", "Absorb 40 damage for 6s.", AbilityKind.Shield, 40f, 6f, 45f, "DP", new Color(1f, 0.95f, 0.6f)),
                        Area("Holy Purification", "100 divine damage to undead within 7.5m.", false, 1, 45f, 0.5f, 100f, 7.5f, DamageType.Magical, "Ho", new Color(1f, 0.9f, 0.4f))
                    },
                    Spells = new[]
                    {
                        Spell("Lesser Heal", "Heal 20.", AbilityKind.Heal, 4, 1.25f, 20f, 0f, "+", new Color(0.6f, 1f, 0.6f)),
                        Spell("Bless", "+2 strength for 30s.", AbilityKind.Buff, 5, 0.75f, 2f, 30f, "Ar", new Color(1f, 1f, 0.7f), StatusEffectKind.Strength),
                        Spell("Protection", "Absorb 20 damage for 8s.", AbilityKind.Shield, 5, 0.75f, 20f, 8f, "Sh", new Color(0.8f, 0.9f, 1f)),
                        Projectile("Holy Strike", "20 divine damage.", true, 4, 1f, 2f, 20f, 30f, 0f, ProjectileKind.Holy, DamageType.Magical, 1, 0f, "HS", new Color(1f, 0.95f, 0.5f)),
                        Spell("Holy Light", "Heal 35.", AbilityKind.Heal, 3, 1.75f, 35f, 0f, "Ho", new Color(1f, 1f, 0.8f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Advanced Healer", "+4 magical power.", new StatModifier(StatType.MagicalPower, 4f)),
                        new PerkDef("Holy Aura", "+15 armor and magic resistance.", new StatModifier(StatType.ArmorRating, 15f), new StatModifier(StatType.MagicResistance, 15f)),
                        new PerkDef("Blunt Weapon Mastery", "+10 physical power.", new StatModifier(StatType.PhysicalPower, 10f)),
                        new PerkDef("Perseverance", "+10 armor rating.", new StatModifier(StatType.ArmorRating, 10f))
                    },
                    Kit = new[] { ("Flanged Mace", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Torch", EquipSlot.Weapon2Main, 1, true), ("Kettle Hat", EquipSlot.Head, 1, true), ("Adventurer Tunic", EquipSlot.Chest, 1, true), ("Plate Boots", EquipSlot.Feet, 1, true), ("Bandage", EquipSlot.Utility1, 3, true) },
                    Weapons = new[] { WeaponClass.Mace, WeaponClass.Staff, WeaponClass.Shield, WeaponClass.Torch },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather, ArmorType.Chain, ArmorType.Plate }
                },
                new ClassDef
                {
                    Id = 6, Name = "Warlock", Description = "Dark magic paid for in blood.",
                    Stats = new ClassStats(11, 14, 14, 15, 22, 15, 14), Color = new Color(0.6f, 0.2f, 0.7f), Body = new Color(0.4f, 0.25f, 0.4f),
                    Skills = new[]
                    {
                        Skill("Phantomize", "Fade from sight for 4s.", AbilityKind.Invisibility, 1f, 4f, 28f, "Ph", new Color(0.7f, 0.4f, 0.9f)),
                        Skill("Blow of Corruption", "+12 physical power for 8s.", AbilityKind.Buff, 12f, 8f, 24f, "At", new Color(0.8f, 0.3f, 0.6f), StatusEffectKind.Power)
                    },
                    Spells = new[]
                    {
                        Projectile("Bolt of Darkness", "20 dark damage.", true, 10, 1f, 1f, 20f, 30f, 0f, ProjectileKind.Dark, DamageType.Magical, 1, 0f, "Mc", new Color(0.5f, 0.2f, 0.7f)),
                        Projectile("Curse of Pain", "15 damage over 8s.", true, 6, 1f, 1f, 5f, 34f, 0f, ProjectileKind.Dark, DamageType.Magical, 1, 0f, "CP", new Color(0.7f, 0.2f, 0.5f), StatusEffectKind.Burn, 15f, 8f),
                        Spell("Power of Sacrifice", "+10 physical power for 12s.", AbilityKind.Buff, 5, 1f, 10f, 12f, "PS", new Color(0.9f, 0.3f, 0.4f), StatusEffectKind.Power),
                        Spell("Eldritch Shield", "Absorb 25 damage for 10s.", AbilityKind.Shield, 4, 0.75f, 25f, 10f, "Sh", new Color(0.6f, 0.3f, 0.8f)),
                        Spell("Life Drain", "Regain 20 health over 5s.", AbilityKind.Heal, 5, 1.5f, 20f, 5f, "Hp", new Color(0.8f, 0.2f, 0.3f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Antimagic", "+60 magic resistance.", new StatModifier(StatType.MagicResistance, 60f)),
                        new PerkDef("Dark Enhancement", "+20% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.2f)),
                        new PerkDef("Malice", "+3 will.", new StatModifier(StatType.Will, 3f)),
                        new PerkDef("Shadow Touch", "+5% physical damage.", new StatModifier(StatType.PhysicalDamageBonus, 0.05f))
                    },
                    Kit = new[] { ("Falchion", EquipSlot.Weapon1Main, 1, true), ("Magic Staff", EquipSlot.Weapon2Main, 1, true), ("Frock", EquipSlot.Chest, 1, true), ("Cloth Pants", EquipSlot.Legs, 1, true), ("Adventurer Boots", EquipSlot.Feet, 1, true), ("Bandage", EquipSlot.Utility1, 3, true) },
                    Weapons = new[] { WeaponClass.Sword, WeaponClass.Staff, WeaponClass.Dagger, WeaponClass.Shield, WeaponClass.Torch },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather }
                },
                new ClassDef
                {
                    Id = 7, Name = "Bard", Description = "Songs that bolster allies and shatter enemies.",
                    Stats = new ClassStats(13, 13, 13, 20, 11, 20, 15), Color = new Color(0.9f, 0.6f, 0.8f), Body = new Color(0.6f, 0.45f, 0.5f),
                    Skills = new[]
                    {
                        Area("Dissonance", "10 damage to all enemies within 6m.", false, 1, 24f, 0.4f, 10f, 6f, DamageType.Magical, "Di", new Color(1f, 0.7f, 0.9f)),
                        Skill("Encore", "+2 strength for 60s.", AbilityKind.Buff, 2f, 60f, 40f, "En", new Color(1f, 0.85f, 0.6f), StatusEffectKind.Strength)
                    },
                    Spells = new[]
                    {
                        Spell("Beats of Alacrity", "+6 move speed for 20s.", AbilityKind.Buff, 4, 1f, 6f, 20f, "Be", new Color(1f, 0.9f, 0.5f), StatusEffectKind.Haste),
                        Spell("Harmonic Shield", "Absorb 15 damage for 20s.", AbilityKind.Shield, 4, 1f, 15f, 20f, "Sh", new Color(0.8f, 0.8f, 1f)),
                        Area("Piercing Shrill", "20 damage within 5m.", true, 4, 1f, 1f, 20f, 5f, DamageType.Physical, "Sh", new Color(1f, 0.6f, 0.7f)),
                        Spell("Song of Shadow", "Invisible for 10s.", AbilityKind.Invisibility, 4, 1.5f, 1f, 10f, "Hi", new Color(0.6f, 0.5f, 0.8f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Rapier Mastery", "+3 physical power, +5% action speed.", new StatModifier(StatType.PhysicalPower, 3f), new StatModifier(StatType.ActionSpeed, 5f)),
                        new PerkDef("Dancing Feet", "+10 move speed.", new StatModifier(StatType.MoveSpeed, 10f)),
                        new PerkDef("Melodic Protection", "+20 armor rating.", new StatModifier(StatType.ArmorRating, 20f)),
                        new PerkDef("Lore Mastery", "+5 resourcefulness.", new StatModifier(StatType.Resourcefulness, 5f))
                    },
                    Kit = new[] { ("Arming Sword", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Crossbow", EquipSlot.Weapon2Main, 1, true), ("Woolen Cap", EquipSlot.Head, 1, true), ("Doublet", EquipSlot.Chest, 1, true), ("Bandage", EquipSlot.Utility1, 3, true), ("Ale", EquipSlot.Utility2, 2, true) },
                    Weapons = new[] { WeaponClass.Sword, WeaponClass.Dagger, WeaponClass.Crossbow, WeaponClass.Bow, WeaponClass.Shield, WeaponClass.Torch },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather }
                },
                new ClassDef
                {
                    Id = 8, Name = "Druid", Description = "Nature's wrath and nature's mercy.",
                    Stats = new ClassStats(12, 13, 12, 12, 18, 20, 18), Color = new Color(0.5f, 0.8f, 0.4f), Body = new Color(0.45f, 0.55f, 0.4f),
                    Skills = new[]
                    {
                        Skill("Wild Fury", "+15 physical power for 10s.", AbilityKind.Buff, 15f, 10f, 32f, "WF", new Color(0.7f, 1f, 0.5f), StatusEffectKind.Power),
                        Skill("Rush", "Lunge forward.", AbilityKind.Dash, -7f, 0f, 24f, "Sp", new Color(0.6f, 0.9f, 0.5f))
                    },
                    Spells = new[]
                    {
                        Spell("Nature's Touch", "Heal 30 over 12s.", AbilityKind.Heal, 4, 0.75f, 30f, 12f, "NT", new Color(0.6f, 1f, 0.6f)),
                        Spell("Barkskin", "Absorb 20 damage for 10s.", AbilityKind.Shield, 4, 0.75f, 20f, 10f, "Sh", new Color(0.6f, 0.45f, 0.3f)),
                        Projectile("Dreamfire", "15 spirit damage with splash.", true, 4, 1f, 1f, 15f, 26f, 0f, ProjectileKind.Magic, DamageType.Magical, 1, 0f, "Df", new Color(0.5f, 1f, 0.7f), StatusEffectKind.None, 0f, 0f, 1f),
                        Projectile("Entangling Vines", "Roots the target briefly.", true, 2, 1f, 1.25f, 5f, 24f, 0f, ProjectileKind.Magic, DamageType.Magical, 1, 0f, "Vn", new Color(0.4f, 0.7f, 0.3f), StatusEffectKind.Slow, 90f, 1.5f),
                        Spell("Restore", "Heal 20 over 12s.", AbilityKind.Heal, 3, 1.5f, 20f, 12f, "+", new Color(0.7f, 1f, 0.7f))
                    },
                    Perks = new[]
                    {
                        new PerkDef("Enhanced Wildness", "+5 physical power, +20 armor.", new StatModifier(StatType.PhysicalPower, 5f), new StatModifier(StatType.ArmorRating, 20f)),
                        new PerkDef("Thorn Coat", "+10 armor rating.", new StatModifier(StatType.ArmorRating, 10f)),
                        new PerkDef("Natural Healing", "+2 vigor.", new StatModifier(StatType.Vigor, 2f)),
                        new PerkDef("Sun and Moon", "+3 vigor, +5 magical power.", new StatModifier(StatType.Vigor, 3f), new StatModifier(StatType.MagicalPower, 5f))
                    },
                    Kit = new[] { ("Spear", EquipSlot.Weapon1Main, 1, true), ("Magic Staff", EquipSlot.Weapon2Main, 1, true), ("Adventurer Tunic", EquipSlot.Chest, 1, true), ("Leather Leggings", EquipSlot.Legs, 1, true), ("Bandage", EquipSlot.Utility1, 3, true), ("Campfire Kit", EquipSlot.Utility2, 1, true) },
                    Weapons = new[] { WeaponClass.Staff, WeaponClass.Spear, WeaponClass.Mace, WeaponClass.Dagger, WeaponClass.Torch },
                    Armor = new[] { ArmorType.Cloth, ArmorType.Leather }
                },
                new ClassDef
                {
                    Id = 9, Name = "Sorcerer", Description = "Elemental caster on cooldowns instead of charges.",
                    Stats = new ClassStats(10, 10, 10, 18, 25, 20, 12), Color = new Color(0.3f, 0.8f, 0.9f), Body = new Color(0.3f, 0.45f, 0.55f),
                    Skills = new[]
                    {
                        Skill("Sorcery Combat", "+25% action speed for 8s.", AbilityKind.Buff, 25f, 8f, 30f, "Ar", new Color(0.5f, 0.9f, 1f), StatusEffectKind.ActionSpeed),
                        Skill("Mana Fold", "+10 move speed for 8s.", AbilityKind.Buff, 10f, 8f, 24f, "Sp", new Color(0.7f, 0.9f, 1f), StatusEffectKind.Haste)
                    },
                    Spells = new[]
                    {
                        Projectile("Fire Arrow", "10 fire. 12s cooldown.", true, 99, 12f, 0.75f, 10f, 35f, 0f, ProjectileKind.Fire, DamageType.Magical, 1, 0f, "Sp", new Color(1f, 0.5f, 0.2f), StatusEffectKind.Burn, 3f, 2f),
                        Projectile("Water Bolt", "15 water, slows. 12s cooldown.", true, 99, 12f, 0.75f, 15f, 30f, 0f, ProjectileKind.Ice, DamageType.Magical, 1, 0f, "Wb", new Color(0.4f, 0.6f, 1f), StatusEffectKind.Slow, 20f, 1f),
                        Projectile("Ice Spear", "30 ice. 15s cooldown.", true, 99, 15f, 0.75f, 30f, 34f, 0f, ProjectileKind.Ice, DamageType.Magical, 1, 0f, "Ic", new Color(0.7f, 0.9f, 1f)),
                        Area("Eruption", "20 earth damage within 3m. 15s cooldown.", true, 99, 15f, 1f, 20f, 3f, DamageType.Magical, "Er", new Color(0.8f, 0.5f, 0.3f)),
                        Spell("Stone Skin", "Absorb 30 damage for 12s. 20s cooldown.", AbilityKind.Shield, 99, 0.75f, 30f, 12f, "Sh", new Color(0.6f, 0.6f, 0.55f), cooldown: 20f)
                    },
                    Perks = new[]
                    {
                        new PerkDef("Apex of Sorcery", "+10 magical power.", new StatModifier(StatType.MagicalPower, 10f)),
                        new PerkDef("Time Distortion", "+10% action speed.", new StatModifier(StatType.ActionSpeed, 10f)),
                        new PerkDef("Mana Flow", "+10% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.1f)),
                        new PerkDef("Lightning Mastery", "+5% magical damage.", new StatModifier(StatType.MagicalDamageBonus, 0.05f))
                    },
                    Kit = new[] { ("Magic Staff", EquipSlot.Weapon1Main, 1, true), ("Falchion", EquipSlot.Weapon2Main, 1, true), ("Wizard Hat", EquipSlot.Head, 1, true), ("Frock", EquipSlot.Chest, 1, true), ("Cloth Pants", EquipSlot.Legs, 1, true), ("Bandage", EquipSlot.Utility1, 3, true) },
                    Weapons = new[] { WeaponClass.Staff, WeaponClass.Sword, WeaponClass.Torch },
                    Armor = new[] { ArmorType.Cloth }
                }
            };
        }

        private static AbilityDef Skill(string name, string description, AbilityKind kind, float magnitude, float duration, float cooldown, string glyph,
            Color color, StatusEffectKind effect = StatusEffectKind.None)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = kind, Magnitude = magnitude, Duration = duration, Cooldown = cooldown, Glyph = glyph,
                Color = color, Effect = effect, CastTime = 0.3f
            };
        }

        private static AbilityDef Spell(string name, string description, AbilityKind kind, int charges, float castTime, float magnitude, float duration,
            string glyph, Color color, StatusEffectKind effect = StatusEffectKind.None, float cooldown = 1f)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = kind, IsSpell = true, Charges = charges, CastTime = castTime, Magnitude = magnitude,
                Duration = duration, Glyph = glyph, Color = color, Effect = effect, Cooldown = cooldown
            };
        }

        private static AbilityDef Projectile(string name, string description, bool isSpell, int charges, float cooldown, float castTime, float damage,
            float speed, float gravity, ProjectileKind kind, DamageType damageType, int count, float spread, string glyph, Color color,
            StatusEffectKind hitEffect = StatusEffectKind.None, float hitMagnitude = 0f, float hitDuration = 0f, float radius = 0f)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = AbilityKind.Projectile, IsSpell = isSpell, Charges = charges, Cooldown = cooldown,
                CastTime = castTime, Magnitude = damage, Speed = speed, Gravity = gravity, Projectile = kind, DamageType = damageType, Count = count,
                Spread = spread, Glyph = glyph, Color = color, HitEffect = hitEffect, HitMagnitude = hitMagnitude, HitDuration = hitDuration, Radius = radius
            };
        }

        private static AbilityDef Area(string name, string description, bool isSpell, int charges, float cooldown, float castTime, float damage, float radius,
            DamageType damageType, string glyph, Color color)
        {
            return new AbilityDef
            {
                Name = name, Description = description, Kind = AbilityKind.AreaDamage, IsSpell = isSpell, Charges = charges, Cooldown = cooldown,
                CastTime = castTime, Magnitude = damage, Radius = radius, DamageType = damageType, Glyph = glyph, Color = color
            };
        }
    }
}
