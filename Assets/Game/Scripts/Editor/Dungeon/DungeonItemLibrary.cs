using System.Collections.Generic;
using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
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
        public bool IsFocus;

        public EquipSlot Slot;
        public ArmorType ArmorType;
        public float Armor;
        public float MagicResist;
        public ArmorVisual Visual;
        /// Outfit pieces: the skinned parts of the character model they show, the cloth material and the set. Outfit pieces
        /// and weapons: the classes that may use them (null = everyone).
        public OutfitPart[] Parts;
        public string Material;
        public string Set;
        public string[] Classes;

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
        /// Symbol of DungeonAbilityIconBuilder; null picks one by the ability kind.
        public string Icon;
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
        /// Symbol of DungeonAbilityIconBuilder; null picks one by the first modifier.
        public string Icon;
        /// Icon colour; the class colour when not set.
        public Color? Color;

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
        public CastFocus Focus = CastFocus.Magic;
    }

    /// Items, classes, abilities and perks of the prototype.
    internal static class DungeonItemLibrary
    {
        /// Every drink takes as long as its animation.
        private const float DrinkTime = BattleAnimationLibrary.DrinkTime;

        private const string MysticPeasantMaterial = OutfitDyeBuilder.Prefix + "PeasantMystic";
        private const string MysticRangerMaterial = OutfitDyeBuilder.Prefix + "RangerMystic";
        private const string OccultistMaterial = OutfitDyeBuilder.Prefix + "RangerOccultist";
        private const string MarauderMaterial = OutfitDyeBuilder.Prefix + "RangerMarauder";
        private const string BerserkerMaterial = OutfitDyeBuilder.Prefix + "RangerBerserker";
        private const string IroncladMaterial = OutfitDyeBuilder.Prefix + "RangerIronclad";
        private const string DevoutPeasantMaterial = OutfitDyeBuilder.Prefix + "PeasantDevout";
        private const string DevoutRangerMaterial = OutfitDyeBuilder.Prefix + "RangerDevout";

        private const string Barbarian = "Barbarian";
        private const string Wizard = "Wizard";
        private const string Sellsword = "Sellsword";
        private const string Chaplain = "Chaplain";

        private static readonly Color s_steel = new(0.8f, 0.82f, 0.88f);
        private static readonly Color s_wood = new(0.65f, 0.45f, 0.25f);
        private static readonly Color s_potion = new(0.9f, 0.25f, 0.3f);
        private static readonly Color s_gold = new(1f, 0.82f, 0.3f);
        private static readonly Color s_silver = new(0.85f, 0.86f, 0.9f);

        private static readonly OutfitPart[] s_hood = { OutfitPart.RangerHood };
        private static readonly OutfitPart[] s_bracers = { OutfitPart.RangerBracers };
        private static readonly OutfitPart[] s_shirt = { OutfitPart.PeasantBody, OutfitPart.PeasantArms };
        private static readonly OutfitPart[] s_trousers = { OutfitPart.PeasantLegs };
        private static readonly OutfitPart[] s_shoes = { OutfitPart.PeasantFeet };
        private static readonly OutfitPart[] s_leggings = { OutfitPart.RangerLegs };
        private static readonly OutfitPart[] s_boots = { OutfitPart.RangerBoots };

        public static List<ItemDef> CreateItems()
        {
            List<ItemDef> items = new()
            {
                Weapon("Arming Sword", DungeonWeaponLibrary.ArmingSword, WeaponClass.Sword, 1, 3, 20f, "Sw", s_steel, 25, "A reliable one-handed sword. Pairs with a shield.", shield: DungeonWeaponLibrary.SwordShield),
                Weapon("Falchion", DungeonWeaponLibrary.Falchion, WeaponClass.Sword, 1, 3, 25f, "Sw", s_steel, 30, "Heavy curved blade with strong slashes."),
                Weapon("Longsword", DungeonWeaponLibrary.Longsword, WeaponClass.Sword, 1, 4, 30f, "2H", s_steel, 45, "Two-handed sword with a fast three-hit chain.", twoHanded: true),
                Weapon("Zweihander", DungeonWeaponLibrary.Greatsword, WeaponClass.Sword, 1, 4, 40f, "2H", s_steel, 60, "Massive greatsword. Slow, wide, devastating.", twoHanded: true),
                Weapon("Battle Axe", DungeonWeaponLibrary.BattleAxe, WeaponClass.Axe, 2, 4, 30f, "Ax", s_steel, 50, "Two-handed double axe. A cut, a backswing and an overhead chop.", twoHanded: true),
                Weapon("Spear", DungeonWeaponLibrary.Spear, WeaponClass.Spear, 1, 4, 40f, "Sp", s_wood, 40, "Long reach thrusts keep enemies at bay.", twoHanded: true),
                Weapon("Flanged Mace", DungeonWeaponLibrary.Mace, WeaponClass.Mace, 1, 3, 20f, "Mc", s_steel, 28, "Blunt one-hander. Staggers on the third hit.", shield: DungeonWeaponLibrary.MaceShield),
                Weapon("Rondel Dagger", DungeonWeaponLibrary.Dagger, WeaponClass.Dagger, 1, 2, 10f, "Dg", s_steel, 18, "Quick stabs. Weak, but barely slows you down."),
                Weapon("Recurve Bow", DungeonWeaponLibrary.Bow, WeaponClass.Bow, 1, 3, 40f, "Bw", s_wood, 45, "Draw and release. Arrows fall with distance.", twoHanded: true),
                Weapon("Crossbow", DungeonWeaponLibrary.Crossbow, WeaponClass.Crossbow, 2, 3, 50f, "Xb", s_wood, 55, "Hard-hitting bolt, slow reload.", twoHanded: true),
                Weapon("Magic Staff", DungeonWeaponLibrary.Staff, WeaponClass.Staff, 1, 4, 20f, "St", new Color(0.5f, 0.7f, 1f), 50, "Caster focus. Also a decent club.", twoHanded: true, focus: true, modifiers: new[] { new StatModifier(StatType.MagicalPower, 4f) }, classes: new[] { Wizard, Chaplain }),
                Weapon("Torch", DungeonWeaponLibrary.Torch, WeaponClass.Torch, 1, 3, 5f, "Tr", new Color(1f, 0.6f, 0.2f), 2, "Lights the way. Can be swung in a pinch.", light: 9f),
                Weapon("Round Shield", null, WeaponClass.Shield, 2, 3, 13f, "Sh", s_wood, 30, "Blocks with a one-handed weapon in the main hand.", offHand: true, modifiers: new[] { new StatModifier(StatType.ArmorRating, 20f) }),
                Weapon("Spellbook", DungeonWeaponLibrary.Spellbook, WeaponClass.Spellbook, 2, 2, 10f, "Bk", new Color(0.55f, 0.35f, 0.75f), 40, "Magical focus. Hold it to cast readied spells; it can bash in a pinch.", twoHanded: true, focus: true, modifiers: new[] { new StatModifier(StatType.MagicalPower, 2f) }),
                Weapon("Crystal Ball", null, WeaponClass.CrystalBall, 2, 2, 15f, "Cb", new Color(0.6f, 0.85f, 1f), 45, "Off-hand magical focus. Cast with a one-handed weapon in the main hand.", offHand: true, focus: true, modifiers: new[] { new StatModifier(StatType.MagicalPower, 5f) }),
                Weapon("Lute", DungeonWeaponLibrary.Lute, WeaponClass.Instrument, 2, 3, 10f, "Lt", new Color(0.8f, 0.6f, 0.3f), 35, "Bard instrument. Songs are performed with it in hand.", twoHanded: true),

                Jewelry("Gem Necklace", EquipSlot.Necklace, "Nk", 60, new StatModifier(StatType.Knowledge, 2f)),
                Jewelry("Gold Band", EquipSlot.Ring1, "Rg", 45, new StatModifier(StatType.Strength, 1f)),
                Jewelry("Gem Ring", EquipSlot.Ring1, "Rg", 45, new StatModifier(StatType.Dexterity, 1f)),

                Consumable("Bandage", ConsumableEffect.HealInstant, 15f, 0f, 4f, 3, "+", new Color(0.9f, 0.9f, 0.85f), 5, "Wrap a wound. Heals after a few seconds."),
                Consumable("Potion of Healing", ConsumableEffect.HealOverTime, 20f, 20f, DrinkTime, 3, "Po", s_potion, 12, "Restores health over time."),
                Consumable("Potion of Protection", ConsumableEffect.Protection, 10f, 24f, DrinkTime, 3, "Po", new Color(0.3f, 0.5f, 1f), 14, "A shield that absorbs physical damage."),
                Consumable("Surgical Kit", ConsumableEffect.HealInstant, 100f, 0f, 12f, 1, "+", new Color(0.7f, 0.2f, 0.2f), 30, "Full heal after a long, vulnerable procedure.", 2, 2),
                Consumable("Ale", ConsumableEffect.Haste, 6f, 15f, DrinkTime, 2, "Al", new Color(0.85f, 0.6f, 0.2f), 8, "Liquid courage. Quickens the step."),

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

                // Second wave of Dark and Darker gear; the weapons play the clips of an older weapon with their own model and damage.
                Weapon("Short Sword", DungeonWeaponLibrary.ShortSword, WeaponClass.Sword, 1, 2, 15f, "Sw", s_steel, 20, "Light blade. Easy on the feet, short on reach.", shield: DungeonWeaponLibrary.SwordShield),
                Weapon("Rapier", DungeonWeaponLibrary.Rapier, WeaponClass.Sword, 1, 3, 18f, "Rp", s_steel, 34, "Long thin blade that outreaches heavier swords.", shield: DungeonWeaponLibrary.SwordShield, modifiers: new[] { new StatModifier(StatType.ActionSpeed, 2f) }),
                Weapon("Viking Sword", DungeonWeaponLibrary.VikingSword, WeaponClass.Sword, 1, 3, 25f, "Sw", s_steel, 38, "Broad northern blade. Slow cuts that bite deep."),
                Weapon("Hatchet", DungeonWeaponLibrary.Hatchet, WeaponClass.Axe, 1, 2, 18f, "Ax", s_wood, 22, "One-handed axe for close, ugly work."),
                Weapon("Morning Star", DungeonWeaponLibrary.MorningStar, WeaponClass.Mace, 1, 3, 23f, "Mc", s_steel, 36, "Spiked mace head. Staggers on the third hit.", shield: DungeonWeaponLibrary.MaceShield),
                Weapon("Castillon Dagger", DungeonWeaponLibrary.CastillonDagger, WeaponClass.Dagger, 1, 2, 10f, "Dg", s_steel, 26, "Wide dagger with a longer blade."),
                Weapon("Stiletto Dagger", DungeonWeaponLibrary.Stiletto, WeaponClass.Dagger, 1, 2, 8f, "Dg", s_steel, 24, "Needle point. Weak cuts, quick hands.", modifiers: new[] { new StatModifier(StatType.ActionSpeed, 3f) }),
                Weapon("Felling Axe", DungeonWeaponLibrary.FellingAxe, WeaponClass.Axe, 2, 3, 26f, "Ax", s_wood, 34, "Woodcutter's axe. Lighter than a battle axe.", twoHanded: true),
                Weapon("War Maul", DungeonWeaponLibrary.WarMaul, WeaponClass.Mace, 2, 4, 40f, "Ml", s_steel, 65, "Two-handed hammer. Slow, crushing, staggering.", twoHanded: true),
                Weapon("Halberd", DungeonWeaponLibrary.Halberd, WeaponClass.Spear, 1, 4, 45f, "Hb", s_steel, 60, "Axe blade on a spear shaft. Long reach, heavy thrusts.", twoHanded: true),
                Weapon("Buckler", null, WeaponClass.Shield, 2, 2, 6f, "Bk", s_steel, 22, "Small shield that barely slows you down.", offHand: true, modifiers: new[] { new StatModifier(StatType.ArmorRating, 12f) }),
                Weapon("Heater Shield", null, WeaponClass.Shield, 2, 3, 18f, "Sh", s_steel, 45, "Heavy knightly shield.", offHand: true, modifiers: new[] { new StatModifier(StatType.ArmorRating, 30f) }),

                Jewelry("Fox Pendant", EquipSlot.Necklace, "Nk", new Color(0.95f, 0.55f, 0.25f), 70, new StatModifier(StatType.Agility, 2f)),
                Jewelry("Ox Pendant", EquipSlot.Necklace, "Nk", new Color(0.75f, 0.45f, 0.3f), 70, new StatModifier(StatType.Strength, 2f)),
                Jewelry("Bear Pendant", EquipSlot.Necklace, "Nk", new Color(0.6f, 0.4f, 0.25f), 70, new StatModifier(StatType.Vitality, 2f)),
                Jewelry("Owl Pendant", EquipSlot.Necklace, "Nk", new Color(0.7f, 0.75f, 0.95f), 70, new StatModifier(StatType.Knowledge, 2f)),
                Jewelry("Ring of Courage", EquipSlot.Ring1, "Rg", new Color(0.95f, 0.4f, 0.3f), 60, new StatModifier(StatType.Strength, 2f)),
                Jewelry("Ring of Vitality", EquipSlot.Ring1, "Rg", new Color(0.45f, 0.9f, 0.45f), 60, new StatModifier(StatType.MaxHealth, 5f)),
                Jewelry("Ring of Finesse", EquipSlot.Ring1, "Rg", new Color(0.95f, 0.85f, 0.4f), 60, new StatModifier(StatType.Dexterity, 2f)),
                Jewelry("Ring of Wisdom", EquipSlot.Ring1, "Rg", new Color(0.5f, 0.65f, 1f), 60, new StatModifier(StatType.Knowledge, 2f)),

                Consumable("Troll's Blood", ConsumableEffect.HealOverTime, 45f, 30f, DrinkTime, 1, "Tb", new Color(0.35f, 0.75f, 0.3f), 40, "Thick green blood. Slowly knits even grave wounds."),
                Consumable("Potion of Invisibility", ConsumableEffect.Invisibility, 0f, 8f, DrinkTime, 2, "Po", new Color(0.8f, 0.85f, 0.95f), 30, "Monsters and players lose sight of you for a few seconds."),

                Treasure("Silver Chalice", 1, 1, 1, 28, "Ch", s_silver),
                Treasure("Gold Crown", 2, 2, 1, 220, "Cr", s_gold),
                Treasure("Gold Ingot", 1, 1, 3, 90, "In", s_gold),
                Treasure("Silver Ingot", 1, 1, 3, 50, "In", s_silver),
                Treasure("Pearl Necklace", 1, 1, 1, 80, "Pn", new Color(0.95f, 0.93f, 0.88f), true),
                Treasure("Gold Ore", 1, 1, 5, 18, "Or", new Color(0.8f, 0.65f, 0.3f))
            };

            // New items go to the end: item ids are list positions and saved stashes refer to them.
            AddOutfits(items);
            items.Add(Weapon("Horseman's Axe", DungeonWeaponLibrary.HorsemansAxe, WeaponClass.Axe, 1, 3, 20f, "Ax", s_steel, 40,
                "Long bearded axe for one hand. Wide chops round the head. Barbarians only.", classes: new[] { Barbarian }));
            items.Add(Weapon("Quarterstaff", DungeonWeaponLibrary.Quarterstaff, WeaponClass.Staff, 1, 4, 30f, "Qs", s_wood, 30,
                "Plain two-handed staff. Four swinging blows in a row, each a little harder than the last.", twoHanded: true));
            AddSecondOutfits(items);

            return items;
        }

        /// Six outfits of five pieces, all cut from the peasant and the ranger of the character pack: the two as they come for
        /// everyone, four dyed and recombined for the classes. A full class set lifts its attribute to the threshold of 30.
        private static void AddOutfits(List<ItemDef> items)
        {
            // Plain cloth: next to no protection, nothing in the way.
            AddOutfit(items, "Peasant", null, ArmorType.Cloth, BattleCharacterBuilder.PeasantMaterial, BattleCharacterBuilder.RangerAltMaterial, new Color(0.75f, 0.7f, 0.6f),
                Piece("Peasant Hood", s_hood, 12f, 0f, 8),
                Piece("Peasant Shirt", s_shirt, 25f, 1f, 12),
                Piece("Peasant Wraps", s_bracers, 8f, 0f, 6),
                Piece("Peasant Trousers", s_trousers, 18f, 1f, 10),
                Piece("Peasant Boots", s_shoes, 10f, -3f, 8));

            // Leather for any class: decent armour, quick hands and feet.
            AddOutfit(items, "Ranger", null, ArmorType.Leather, null, BattleCharacterBuilder.RangerMaterial, new Color(0.4f, 0.65f, 0.3f),
                Piece("Ranger Hood", s_hood, 26f, 1f, 18, 0f, new StatModifier(StatType.Dexterity, 1f)),
                Piece("Ranger Jerkin", new[] { OutfitPart.RangerBody, OutfitPart.RangerArms, OutfitPart.RangerBelt1, OutfitPart.RangerPauldron }, 52f, 4f, 36, 0f,
                    new StatModifier(StatType.Agility, 1f)),
                Piece("Ranger Bracers", s_bracers, 18f, 0f, 14, 0f, new StatModifier(StatType.Dexterity, 1f)),
                Piece("Ranger Leggings", s_leggings, 40f, 3f, 24, 0f, new StatModifier(StatType.Agility, 1f)),
                Piece("Ranger Boots", s_boots, 20f, -7f, 22));

            // Wizard, Spirit +7 (23 -> 30): magical power, magical damage and healing.
            AddOutfit(items, "Mystic", new[] { Wizard }, ArmorType.Cloth, MysticPeasantMaterial, MysticRangerMaterial, new Color(0.4f, 0.45f, 0.95f),
                Piece("Mystic Cowl", s_hood, 16f, 1f, 30, 10f, new StatModifier(StatType.Spirit, 1f)),
                Piece("Mystic Vestments", s_shirt, 32f, 2f, 55, 25f, new StatModifier(StatType.Spirit, 2f), new StatModifier(StatType.MagicalPower, 2f)),
                Piece("Mystic Wraps", s_bracers, 10f, 0f, 26, 5f, new StatModifier(StatType.Spirit, 1f)),
                Piece("Mystic Trousers", s_trousers, 24f, 1f, 34, 10f, new StatModifier(StatType.Spirit, 2f)),
                Piece("Mystic Shoes", s_shoes, 10f, -5f, 28, 5f, new StatModifier(StatType.Spirit, 1f)));

            // Wizard, Knowledge +9 (21 -> 30): faster casts, more charges and a free first spell.
            AddOutfit(items, "Occultist", new[] { Wizard }, ArmorType.Leather, null, OccultistMaterial, new Color(0.6f, 0.3f, 0.75f),
                Piece("Occultist Hood", s_hood, 20f, 1f, 32, 5f, new StatModifier(StatType.Knowledge, 2f)),
                Piece("Occultist Coat", new[] { OutfitPart.RangerBody, OutfitPart.RangerArms, OutfitPart.RangerBelt2 }, 40f, 4f, 60, 15f,
                    new StatModifier(StatType.Knowledge, 3f)),
                Piece("Occultist Bracers", s_bracers, 14f, 0f, 28, 0f, new StatModifier(StatType.Knowledge, 1f), new StatModifier(StatType.MagicalPower, 2f)),
                Piece("Occultist Leggings", s_leggings, 30f, 2f, 38, 5f, new StatModifier(StatType.Knowledge, 2f)),
                Piece("Occultist Boots", s_boots, 16f, -4f, 30, 0f, new StatModifier(StatType.Knowledge, 1f)));

            // Barbarian, Strength +9 (21 -> 30): physical power and +10% physical damage. The heaviest clothes there are.
            AddOutfit(items, "Marauder", new[] { Barbarian }, ArmorType.Leather, null, MarauderMaterial, new Color(0.8f, 0.25f, 0.2f),
                Piece("Marauder Hood", s_hood, 30f, 2f, 34, 0f, new StatModifier(StatType.Strength, 2f)),
                Piece("Marauder Jerkin", new[] { OutfitPart.RangerBody, OutfitPart.RangerBelt1, OutfitPart.RangerBelt2, OutfitPart.RangerPauldron }, 70f, 7f, 65, 0f,
                    new StatModifier(StatType.Strength, 3f)),
                Piece("Marauder Bracers", s_bracers, 22f, 0f, 30, 0f, new StatModifier(StatType.Strength, 1f), new StatModifier(StatType.PhysicalPower, 2f)),
                Piece("Marauder Leggings", s_leggings, 48f, 4f, 40, 0f, new StatModifier(StatType.Strength, 2f)),
                Piece("Marauder Boots", s_boots, 26f, -2f, 32, 0f, new StatModifier(StatType.Strength, 1f)));

            // Barbarian, Vitality +8 (22 -> 30): health, rest heals twice as fast, and some action speed.
            // Straps on a bare chest protect next to nothing.
            AddOutfit(items, "Berserker", new[] { Barbarian }, ArmorType.Leather, BattleCharacterBuilder.PeasantAltMaterial, BerserkerMaterial, new Color(0.75f, 0.7f, 0.65f),
                Piece("Berserker Hood", s_hood, 18f, 0f, 30, 0f, new StatModifier(StatType.Vitality, 1f)),
                Piece("Berserker Harness", new[] { OutfitPart.RangerBelt1, OutfitPart.RangerBelt2, OutfitPart.RangerPauldron }, 22f, 0f, 60, 0f,
                    new StatModifier(StatType.Vitality, 3f), new StatModifier(StatType.ActionSpeed, 4f)),
                Piece("Berserker Bracers", s_bracers, 14f, 0f, 28, 0f, new StatModifier(StatType.Vitality, 1f), new StatModifier(StatType.ActionSpeed, 2f)),
                Piece("Berserker Trousers", s_trousers, 26f, 1f, 36, 0f, new StatModifier(StatType.Vitality, 2f)),
                Piece("Berserker Boots", s_shoes, 14f, -6f, 30, 0f, new StatModifier(StatType.Vitality, 1f)));
        }

        /// Outfits of the classes that came after the first weapons were appended to the list.
        private static void AddSecondOutfits(List<ItemDef> items)
        {
            // The Chaplain wears the body armour of the plate, but neither the coif nor the vambraces.
            string[] plated = { Sellsword, Chaplain };

            // Sellsword: the only plate there is. Twice the armour of leather and the slowest to walk in; Vitality +6 (18 -> 24).
            AddOutfit(items, "Ironclad", new[] { Sellsword }, ArmorType.Plate, null, IroncladMaterial, new Color(0.72f, 0.76f, 0.84f),
                Piece("Ironclad Coif", s_hood, 42f, 3f, 60, 0f, new StatModifier(StatType.Vitality, 1f)),
                For(plated, Piece("Ironclad Cuirass", new[] { OutfitPart.RangerBody, OutfitPart.RangerArms, OutfitPart.RangerBelt1, OutfitPart.RangerPauldron }, 115f, 11f, 120, 0f,
                    new StatModifier(StatType.Vitality, 2f))),
                Piece("Ironclad Vambraces", s_bracers, 32f, 1f, 55, 0f, new StatModifier(StatType.Vitality, 1f)),
                For(plated, Piece("Ironclad Greaves", s_leggings, 78f, 7f, 80, 0f, new StatModifier(StatType.Vitality, 1f))),
                For(plated, Piece("Ironclad Sabatons", s_boots, 38f, 2f, 60, 0f, new StatModifier(StatType.Vitality, 1f))));

            // Chaplain, Spirit +9 (21 -> 30): magical power, stronger prayers and healing. White linen that stops next to nothing.
            AddOutfit(items, "Devout", new[] { Chaplain }, ArmorType.Cloth, DevoutPeasantMaterial, DevoutRangerMaterial, new Color(0.95f, 0.88f, 0.6f),
                Piece("Devout Cowl", s_hood, 14f, 1f, 30, 10f, new StatModifier(StatType.Spirit, 2f)),
                Piece("Devout Vestments", s_shirt, 30f, 2f, 55, 20f, new StatModifier(StatType.Spirit, 3f)),
                Piece("Devout Wraps", s_bracers, 10f, 0f, 26, 5f, new StatModifier(StatType.Spirit, 1f)),
                Piece("Devout Trousers", s_trousers, 22f, 1f, 34, 10f, new StatModifier(StatType.Spirit, 2f)),
                Piece("Devout Shoes", s_shoes, 10f, -5f, 28, 0f, new StatModifier(StatType.Spirit, 1f)));
        }

        /// A piece with its own wearers instead of the ones of its outfit.
        private static ItemDef For(string[] classes, ItemDef piece)
        {
            piece.Classes = classes;

            return piece;
        }

        /// Pieces come in slot order: head, chest, hands, legs, feet. Peasant parts wear the peasant cloth, ranger parts the ranger one.
        private static void AddOutfit(List<ItemDef> items, string set, string[] classes, ArmorType type, string peasantCloth, string rangerCloth, Color color,
            params ItemDef[] pieces)
        {
            string[] glyphs = { "Hd", "Ch", "Gl", "Lg", "Bt" };
            int index = items.FindAll(item => item.Parts != null).Count;

            for (int i = 0; i < pieces.Length; i++)
            {
                ItemDef piece = pieces[i];
                piece.Set = set;
                piece.Classes ??= classes;
                piece.ArmorType = type;
                piece.Slot = (EquipSlot)i;
                piece.Visual = (ArmorVisual)((int)ArmorVisual.PeasantHead + index + i);
                piece.Material = piece.Parts[0] <= OutfitPart.PeasantFeet ? peasantCloth : rangerCloth;
                piece.Glyph = glyphs[i];
                piece.Color = color;
                piece.Height = piece.Slot == EquipSlot.Chest ? 3 : 2;
                items.Add(piece);
            }
        }

        private static ItemDef Piece(string name, OutfitPart[] parts, float armor, float movePenalty, int value, float magicResist = 0f,
            params StatModifier[] modifiers)
        {
            return new ItemDef
            {
                Name = name, Kind = ItemKind.Armor, Parts = parts, Armor = armor, MovePenalty = movePenalty, Value = value, MagicResist = magicResist,
                Width = 2, RollsRarity = true, Modifiers = new List<StatModifier>(modifiers)
            };
        }

        private static ItemDef Weapon(string name, string prefix, WeaponClass weaponClass, int width, int height, float movePenalty, string glyph,
            Color color, int value, string description, bool twoHanded = false, bool offHand = false, float light = 0f, string shield = null,
            StatModifier[] modifiers = null, string[] classes = null, bool focus = false)
        {
            return new ItemDef
            {
                Name = name, Kind = ItemKind.Weapon, Classes = classes, WeaponPrefix = prefix, ShieldPrefix = shield, WeaponClass = weaponClass, Width = width, Height = height,
                MovePenalty = movePenalty, Glyph = glyph, Color = color, Value = value, Description = description, TwoHanded = twoHanded, OffHand = offHand,
                LightRange = light, IsFocus = focus, RollsRarity = true, Modifiers = modifiers != null ? new List<StatModifier>(modifiers) : new List<StatModifier>()
            };
        }

        private static ItemDef Jewelry(string name, EquipSlot slot, string glyph, int value, params StatModifier[] modifiers)
        {
            return Jewelry(name, slot, glyph, s_gold, value, modifiers);
        }

        private static ItemDef Jewelry(string name, EquipSlot slot, string glyph, Color color, int value, params StatModifier[] modifiers)
        {
            return new ItemDef
            {
                Name = name, Kind = ItemKind.Armor, Slot = slot, ArmorType = ArmorType.Cloth, Glyph = glyph, Color = color, Value = value,
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
