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
        public int HealthCost;
        public float LifeSteal;
        public string SpawnPrefab;
        public float Stagger;
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
        public CastFocus Focus = CastFocus.Magic;
    }

    /// Items, classes, abilities and perks of the prototype.
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
                Weapon("Spellbook", DungeonWeaponLibrary.Spellbook, WeaponClass.Spellbook, 2, 2, 10f, "Bk", new Color(0.55f, 0.35f, 0.75f), 40, "Magical focus. Hold it to cast readied spells; it can bash in a pinch.", twoHanded: true, modifiers: new[] { new StatModifier(StatType.MagicalPower, 2f) }),
                Weapon("Crystal Ball", null, WeaponClass.CrystalBall, 2, 2, 15f, "Cb", new Color(0.6f, 0.85f, 1f), 45, "Off-hand magical focus. Cast with a one-handed weapon in the main hand.", offHand: true, modifiers: new[] { new StatModifier(StatType.MagicalPower, 5f) }),
                Weapon("Lute", DungeonWeaponLibrary.Lute, WeaponClass.Instrument, 2, 3, 10f, "Lt", new Color(0.8f, 0.6f, 0.3f), 35, "Bard instrument. Songs are performed with it in hand.", twoHanded: true),

                Armor("Woolen Cap", EquipSlot.Head, ArmorType.Cloth, 20f, 2f, ArmorVisual.Cap, new Color(0.35f, 0.3f, 0.25f), 2, 2, "Hd", 8),
                Armor("Rogue Cowl", EquipSlot.Head, ArmorType.Cloth, 25f, 2f, ArmorVisual.Hood, new Color(0.2f, 0.2f, 0.22f), 2, 2, "Hd", 14, modifiers: new[] { new StatModifier(StatType.Reflex, 1f) }),
                Armor("Wizard Hat", EquipSlot.Head, ArmorType.Cloth, 20f, 2f, ArmorVisual.WizardHood, new Color(0.25f, 0.2f, 0.45f), 2, 2, "Hd", 14, modifiers: new[] { new StatModifier(StatType.Insight, 1f) }),
                Armor("Leather Cap", EquipSlot.Head, ArmorType.Leather, 31f, 3f, ArmorVisual.Cap, s_leather, 2, 2, "Hd", 15),
                Armor("Kettle Hat", EquipSlot.Head, ArmorType.Plate, 30f, 3f, ArmorVisual.Helmet, s_plate, 2, 2, "Hd", 22),
                Armor("Great Helm", EquipSlot.Head, ArmorType.Plate, 49f, 7f, ArmorVisual.GreatHelm, s_plate, 2, 2, "Hd", 40),
                Armor("Adventurer Tunic", EquipSlot.Chest, ArmorType.Cloth, 33f, 3f, ArmorVisual.Tunic, new Color(0.45f, 0.4f, 0.3f), 2, 3, "Ch", 12),
                Armor("Frock", EquipSlot.Chest, ArmorType.Cloth, 42f, 4f, ArmorVisual.Robe, new Color(0.3f, 0.25f, 0.5f), 2, 3, "Ch", 25, magicResist: 25f),
                Armor("Doublet", EquipSlot.Chest, ArmorType.Leather, 44f, 4f, ArmorVisual.LeatherChest, new Color(0.4f, 0.25f, 0.15f), 2, 3, "Ch", 28, modifiers: new[] { new StatModifier(StatType.Craft, 1f) }),
                Armor("Heavy Gambeson", EquipSlot.Chest, ArmorType.Leather, 85f, 8f, ArmorVisual.Gambeson, new Color(0.5f, 0.42f, 0.3f), 2, 3, "Ch", 45),
                Armor("Templar Armor", EquipSlot.Chest, ArmorType.Plate, 81f, 9f, ArmorVisual.PlateChest, new Color(0.75f, 0.75f, 0.8f), 2, 3, "Ch", 70, magicResist: 20f),
                Armor("Dark Plate Armor", EquipSlot.Chest, ArmorType.Plate, 101f, 14f, ArmorVisual.PlateChest, new Color(0.25f, 0.25f, 0.3f), 2, 3, "Ch", 110),
                Armor("Leather Gloves", EquipSlot.Hands, ArmorType.Leather, 15f, 0f, ArmorVisual.Gloves, s_leather, 2, 2, "Gl", 10),
                Armor("Heavy Gauntlets", EquipSlot.Hands, ArmorType.Plate, 31f, 1f, ArmorVisual.Gauntlets, s_plate, 2, 2, "Gl", 32, magicResist: -5f),
                Armor("Cloth Pants", EquipSlot.Legs, ArmorType.Cloth, 30f, 3f, ArmorVisual.Pants, s_cloth, 2, 2, "Lg", 10),
                Armor("Leather Leggings", EquipSlot.Legs, ArmorType.Leather, 43f, 4f, ArmorVisual.LeatherPants, s_leather, 2, 2, "Lg", 20),
                Armor("Plate Pants", EquipSlot.Legs, ArmorType.Plate, 75f, 8f, ArmorVisual.Greaves, s_plate, 2, 2, "Lg", 55),
                Armor("Adventurer Boots", EquipSlot.Feet, ArmorType.Leather, 23f, -6f, ArmorVisual.Boots, s_leather, 2, 2, "Bt", 12),
                Armor("Plate Boots", EquipSlot.Feet, ArmorType.Plate, 42f, -4f, ArmorVisual.PlateBoots, s_plate, 2, 2, "Bt", 38, magicResist: -5f),
                Armor("Adventurer Cloak", EquipSlot.Back, ArmorType.Cloth, 6f, 0f, ArmorVisual.Cloak, new Color(0.3f, 0.12f, 0.1f), 2, 3, "Ck", 15, modifiers: new[] { new StatModifier(StatType.Reflex, 1f) }),
                Jewelry("Gem Necklace", EquipSlot.Necklace, "Nk", 60, new StatModifier(StatType.Resonance, 2f)),
                Jewelry("Gold Band", EquipSlot.Ring1, "Rg", 45, new StatModifier(StatType.Grip, 1f)),
                Jewelry("Gem Ring", EquipSlot.Ring1, "Rg", 45, new StatModifier(StatType.Reflex, 1f)),

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
                Treasure("Ancient Scroll", 1, 1, 3, 60, "Sc", new Color(0.85f, 0.75f, 0.5f), true),

                // Appended last: item ids are list positions and saved stashes refer to them.
                Armor("Cloth Shoes", EquipSlot.Feet, ArmorType.Cloth, 12f, -4f, ArmorVisual.Shoes, s_cloth, 2, 2, "Bt", 8)
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
    }
}
