using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Builds every dungeon config (items, classes, abilities, monsters, loot) and every networked prefab.
    internal static class DungeonContentBuilder
    {
        public const string ConfigsFolder = "Assets/Game/Configs/Dungeon";
        public const string ItemsFolder = ConfigsFolder + "/Items";
        public const string AbilitiesFolder = ConfigsFolder + "/Abilities";
        public const string ClassesFolder = ConfigsFolder + "/Classes";
        public const string LootFolder = ConfigsFolder + "/Loot";
        public const string MonstersFolder = ConfigsFolder + "/Monsters";
        public const string ArmorFolder = DungeonPropBuilder.PrefabsFolder + "/Armor";
        public const string DatabasePath = ConfigsFolder + "/ItemDatabase.asset";
        public const string DungeonConfigPath = ConfigsFolder + "/Dungeon.asset";
        public const string InteractableLayer = "Interactable";

        public static string Prefab(string name) => $"{DungeonPropBuilder.PrefabsFolder}/{name}.prefab";

        public static void Build()
        {
            foreach (string folder in new[] { ConfigsFolder, ItemsFolder, AbilitiesFolder, ClassesFolder, LootFolder, MonstersFolder, DungeonPropBuilder.PrefabsFolder, ArmorFolder })
                BattleEditorUtility.EnsureFolder(folder);

            BattleEditorUtility.EnsureLayer(InteractableLayer);
            BattleContentBuilder.Loadout[] loadouts = BattleContentBuilder.BuildWeapons(out GameObject arrow, out GameObject orb);
            Dictionary<string, WeaponConfig> weapons = new();

            foreach (BattleContentBuilder.Loadout loadout in loadouts)
                weapons[loadout.Name] = loadout.Config;

            ItemDatabase database = BuildItems(weapons);
            DungeonConfig config = BattleEditorUtility.LoadOrCreate<DungeonConfig>(DungeonConfigPath);
            ClassConfig[] classes = BuildClasses(database);
            Dictionary<string, LootTableConfig> loot = BuildLootTables(database);
            GameObject worldItem = BuildWorldItem(database);
            GameObject campfire = BuildCampfire();
            GameObject corpse = BuildCorpse(database, classes);
            GameObject[] armorPieces = BuildArmorPieces();

            BuildAdventurer(loadouts, arrow, orb, database, classes, config, weapons, worldItem, corpse, campfire, armorPieces);
            BuildMonster(loadouts, arrow, orb, "SkeletonSwordsman", "Skeleton Swordsman", 117, 1.5f, 285f, 1f, 10f, false, true, 5, 25, loot["Monster"], DungeonPropBuilder.Bone, 0.98f, worldItem);
            BuildMonster(loadouts, arrow, orb, "SkeletonArcher", "Skeleton Archer", 70, 1f, 280f, 1f, 14f, true, false, 2, 25, loot["Monster"], DungeonPropBuilder.Bone, 0.98f, worldItem);
            BuildMonster(loadouts, arrow, orb, "Zombie", "Zombie", 168, 4.5f, 160f, 0.85f, 8f, false, false, 4, 30, loot["Monster"], DungeonPropBuilder.ZombieSkin, 1.05f, worldItem);

            BuildSession(database, classes, config);
            BuildMatch(config);
            BuildContainer("SmallOakChest", "Small Oak Chest", loot["ChestCommon"], 1.6f, false);
            BuildContainer("LargeOakChest", "Large Oak Chest", loot["ChestLarge"], 2.2f, false);
            BuildContainer("GoldenChest", "Golden Chest", loot["ChestOrnate"], 3f, true);
            BuildCoffin(loot["Coffin"]);
            BuildBarrel(loot["Barrel"]);
            BuildCrate(loot["Barrel"]);
            BuildBookshelf(loot["Bookshelf"]);
            BuildDoor();
            BuildPortal("EscapePortal", PortalKind.Escape, DungeonPropBuilder.PortalBlue, config.EscapePortalTime > 0f);
            BuildPortal("DescendPortal", PortalKind.Descend, DungeonPropBuilder.PortalRed, false);
            BuildShrine(ShrineKind.Health, 100f, 0f);
            BuildShrine(ShrineKind.Protection, 30f, 60f);
            BuildShrine(ShrineKind.Power, 15f, 60f);
            BuildShrine(ShrineKind.Speed, 10f, 60f);
            BuildLever();
            BuildSpikeTrap();
            BuildBladeTrap();

            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(DungeonContentBuilder)}] Dungeon content built");
        }

        private static ItemDatabase BuildItems(Dictionary<string, WeaponConfig> weapons)
        {
            List<ItemDef> defs = DungeonItemLibrary.CreateItems();
            ItemConfig[] configs = new ItemConfig[defs.Count];

            for (int i = 0; i < defs.Count; i++)
            {
                ItemDef def = defs[i];
                string path = $"{ItemsFolder}/{Sanitize(def.Name)}.asset";
                ItemConfig config = def.Kind switch
                {
                    ItemKind.Weapon => BattleEditorUtility.LoadOrCreate<WeaponItemConfig>(path),
                    ItemKind.Armor => BattleEditorUtility.LoadOrCreate<ArmorItemConfig>(path),
                    ItemKind.Consumable => BattleEditorUtility.LoadOrCreate<ConsumableItemConfig>(path),
                    ItemKind.Utility => BattleEditorUtility.LoadOrCreate<UtilityItemConfig>(path),
                    _ => BattleEditorUtility.LoadOrCreate<TreasureItemConfig>(path)
                };

                SerializedObject so = new SerializedObject(config);
                BattleEditorUtility.Set(so, "_id", i + 1);
                BattleEditorUtility.Set(so, "_displayName", def.Name);
                BattleEditorUtility.Set(so, "_description", def.Description);
                BattleEditorUtility.Set(so, "_baseRarity", def.Rarity);
                BattleEditorUtility.Set(so, "_width", def.Width);
                BattleEditorUtility.Set(so, "_height", def.Height);
                BattleEditorUtility.Set(so, "_maxStack", def.Stack);
                BattleEditorUtility.Set(so, "_value", def.Value);
                BattleEditorUtility.Set(so, "_iconColor", def.Color);
                BattleEditorUtility.Set(so, "_iconGlyph", def.Glyph);
                BattleEditorUtility.Set(so, "_canRollRarity", def.RollsRarity);
                SetModifiers(so, "_modifiers", def.Modifiers);

                switch (def.Kind)
                {
                    case ItemKind.Weapon:
                        BattleEditorUtility.Set(so, "_weapon", def.WeaponPrefix != null ? weapons[def.WeaponPrefix] : null);
                        BattleEditorUtility.Set(so, "_weaponWithShield", def.ShieldPrefix != null ? weapons[def.ShieldPrefix] : null);
                        BattleEditorUtility.Set(so, "_weaponClass", def.WeaponClass);
                        BattleEditorUtility.Set(so, "_isTwoHanded", def.TwoHanded);
                        BattleEditorUtility.Set(so, "_isOffHand", def.OffHand);
                        BattleEditorUtility.Set(so, "_moveSpeedPenalty", def.MovePenalty);
                        BattleEditorUtility.Set(so, "_damageType", DamageType.Physical);
                        BattleEditorUtility.Set(so, "_lightRange", def.LightRange);
                        break;
                    case ItemKind.Armor:
                        BattleEditorUtility.Set(so, "_slot", def.Slot);
                        BattleEditorUtility.Set(so, "_armorType", def.ArmorType);
                        BattleEditorUtility.Set(so, "_armorRating", def.Armor);
                        BattleEditorUtility.Set(so, "_magicResistance", def.MagicResist);
                        BattleEditorUtility.Set(so, "_moveSpeedPenalty", -def.MovePenalty);
                        BattleEditorUtility.Set(so, "_visual", def.Visual);
                        BattleEditorUtility.Set(so, "_visualColor", def.VisualColor);
                        break;
                    case ItemKind.Consumable:
                        BattleEditorUtility.Set(so, "_effect", def.Effect);
                        BattleEditorUtility.Set(so, "_magnitude", def.Magnitude);
                        BattleEditorUtility.Set(so, "_duration", def.Duration);
                        BattleEditorUtility.Set(so, "_useTime", def.UseTime);
                        break;
                    case ItemKind.Utility:
                        BattleEditorUtility.Set(so, "_utilityKind", def.Utility);
                        BattleEditorUtility.Set(so, "_damage", def.Damage);
                        BattleEditorUtility.Set(so, "_useTime", def.UseTime);
                        break;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                configs[i] = config;
            }

            ItemDatabase database = BattleEditorUtility.LoadOrCreate<ItemDatabase>(DatabasePath);
            BattleEditorUtility.Set(database, "_items", configs);

            return database;
        }

        private static void SetModifiers(SerializedObject so, string property, IList<StatModifier> modifiers)
        {
            SerializedProperty array = so.FindProperty(property);
            array.arraySize = modifiers.Count;

            for (int i = 0; i < modifiers.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Stat").intValue = (int)modifiers[i].Stat;
                element.FindPropertyRelative("Value").floatValue = modifiers[i].Value;
            }
        }

        private static AbilityConfig BuildAbility(AbilityDef def)
        {
            AbilityConfig config = BattleEditorUtility.LoadOrCreate<AbilityConfig>($"{AbilitiesFolder}/{Sanitize(def.Name)}.asset");
            SerializedObject so = new SerializedObject(config);
            BattleEditorUtility.Set(so, "_displayName", def.Name);
            BattleEditorUtility.Set(so, "_description", def.Description);
            BattleEditorUtility.Set(so, "_kind", def.Kind);
            BattleEditorUtility.Set(so, "_isSpell", def.IsSpell);
            BattleEditorUtility.Set(so, "_charges", def.Charges);
            BattleEditorUtility.Set(so, "_cooldown", def.Cooldown);
            BattleEditorUtility.Set(so, "_castTime", def.CastTime);
            BattleEditorUtility.Set(so, "_magnitude", def.Magnitude);
            BattleEditorUtility.Set(so, "_duration", def.Duration);
            BattleEditorUtility.Set(so, "_radius", def.Radius);
            BattleEditorUtility.Set(so, "_effect", def.Effect);
            BattleEditorUtility.Set(so, "_damageType", def.DamageType);
            BattleEditorUtility.Set(so, "_projectileSpeed", def.Speed);
            BattleEditorUtility.Set(so, "_projectileGravity", def.Gravity);
            BattleEditorUtility.Set(so, "_projectileCount", def.Count);
            BattleEditorUtility.Set(so, "_projectileSpread", def.Spread);
            BattleEditorUtility.Set(so, "_projectileKind", def.Projectile);
            BattleEditorUtility.Set(so, "_effectMagnitude", def.HitMagnitude);
            BattleEditorUtility.Set(so, "_effectDuration", def.HitDuration);
            BattleEditorUtility.Set(so, "_color", def.Color);
            BattleEditorUtility.Set(so, "_glyph", def.Glyph);

            if (def.Kind == AbilityKind.Projectile && def.HitEffect != StatusEffectKind.None)
                BattleEditorUtility.Set(so, "_effect", def.HitEffect);

            so.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }

        private static ClassConfig[] BuildClasses(ItemDatabase database)
        {
            ClassDef[] defs = DungeonItemLibrary.CreateClasses();
            ClassConfig[] configs = new ClassConfig[defs.Length];

            for (int i = 0; i < defs.Length; i++)
            {
                ClassDef def = defs[i];
                ClassConfig config = BattleEditorUtility.LoadOrCreate<ClassConfig>($"{ClassesFolder}/{def.Name}.asset");
                SerializedObject so = new SerializedObject(config);
                BattleEditorUtility.Set(so, "_id", def.Id);
                BattleEditorUtility.Set(so, "_displayName", def.Name);
                BattleEditorUtility.Set(so, "_description", def.Description);
                BattleEditorUtility.Set(so, "_color", def.Color);
                BattleEditorUtility.Set(so, "_bodyColor", def.Body);
                SerializedProperty stats = so.FindProperty("_baseStats");
                stats.FindPropertyRelative("Strength").intValue = def.Stats.Strength;
                stats.FindPropertyRelative("Vigor").intValue = def.Stats.Vigor;
                stats.FindPropertyRelative("Agility").intValue = def.Stats.Agility;
                stats.FindPropertyRelative("Dexterity").intValue = def.Stats.Dexterity;
                stats.FindPropertyRelative("Will").intValue = def.Stats.Will;
                stats.FindPropertyRelative("Knowledge").intValue = def.Stats.Knowledge;
                stats.FindPropertyRelative("Resourcefulness").intValue = def.Stats.Resourcefulness;

                List<AbilityConfig> skills = new();
                List<AbilityConfig> spells = new();

                foreach (AbilityDef skill in def.Skills)
                    skills.Add(BuildAbility(skill));

                foreach (AbilityDef spell in def.Spells)
                    spells.Add(BuildAbility(spell));

                BattleEditorUtility.Set(so, "_skills", skills);
                BattleEditorUtility.Set(so, "_spells", spells);

                SerializedProperty perks = so.FindProperty("_perks");
                perks.arraySize = def.Perks.Length;

                for (int p = 0; p < def.Perks.Length; p++)
                {
                    SerializedProperty perk = perks.GetArrayElementAtIndex(p);
                    perk.FindPropertyRelative("_name").stringValue = def.Perks[p].Name;
                    perk.FindPropertyRelative("_description").stringValue = def.Perks[p].Description;
                    SerializedProperty modifiers = perk.FindPropertyRelative("_modifiers");
                    modifiers.arraySize = def.Perks[p].Modifiers.Length;

                    for (int m = 0; m < def.Perks[p].Modifiers.Length; m++)
                    {
                        SerializedProperty element = modifiers.GetArrayElementAtIndex(m);
                        element.FindPropertyRelative("Stat").intValue = (int)def.Perks[p].Modifiers[m].Stat;
                        element.FindPropertyRelative("Value").floatValue = def.Perks[p].Modifiers[m].Value;
                    }
                }

                SerializedProperty kit = so.FindProperty("_startingKit");
                kit.arraySize = def.Kit.Length;

                for (int k = 0; k < def.Kit.Length; k++)
                {
                    SerializedProperty entry = kit.GetArrayElementAtIndex(k);
                    entry.FindPropertyRelative("_item").objectReferenceValue = database.Find(def.Kit[k].item);
                    entry.FindPropertyRelative("_slot").intValue = (int)def.Kit[k].slot;
                    entry.FindPropertyRelative("_count").intValue = def.Kit[k].count;
                    entry.FindPropertyRelative("_isEquipped").boolValue = def.Kit[k].equipped;
                }

                SerializedProperty weapons = so.FindProperty("_allowedWeapons");
                weapons.arraySize = def.Weapons.Length;

                for (int w = 0; w < def.Weapons.Length; w++)
                    weapons.GetArrayElementAtIndex(w).intValue = (int)def.Weapons[w];

                SerializedProperty armor = so.FindProperty("_allowedArmor");
                armor.arraySize = def.Armor.Length;

                for (int a = 0; a < def.Armor.Length; a++)
                    armor.GetArrayElementAtIndex(a).intValue = (int)def.Armor[a];

                so.ApplyModifiedPropertiesWithoutUndo();
                configs[i] = config;
            }

            return configs;
        }

        private static Dictionary<string, LootTableConfig> BuildLootTables(ItemDatabase database)
        {
            (string name, float weight, int min, int max)[] common =
            {
                ("Gold Coins", 8f, 3, 14), ("Bandage", 4f, 1, 3), ("Potion of Healing", 3f, 1, 2), ("Potion of Protection", 1.5f, 1, 1), ("Lockpick", 1.5f, 1, 2),
                ("Ale", 1f, 1, 1), ("Throwing Knife", 1f, 1, 2), ("Francisca Axe", 0.8f, 1, 2), ("Torch", 1.5f, 1, 1), ("Campfire Kit", 0.6f, 1, 1),
                ("Arming Sword", 1f, 1, 1), ("Falchion", 0.7f, 1, 1), ("Flanged Mace", 0.7f, 1, 1), ("Rondel Dagger", 1f, 1, 1), ("Recurve Bow", 0.6f, 1, 1), ("Round Shield", 0.8f, 1, 1),
                ("Spear", 0.5f, 1, 1), ("Longsword", 0.5f, 1, 1), ("Crossbow", 0.4f, 1, 1), ("Magic Staff", 0.5f, 1, 1), ("Battle Axe", 0.4f, 1, 1), ("Zweihander", 0.3f, 1, 1),
                ("Leather Cap", 1f, 1, 1), ("Woolen Cap", 0.8f, 1, 1), ("Rogue Cowl", 0.5f, 1, 1), ("Wizard Hat", 0.5f, 1, 1), ("Kettle Hat", 0.6f, 1, 1),
                ("Adventurer Tunic", 1f, 1, 1), ("Doublet", 0.8f, 1, 1), ("Frock", 0.5f, 1, 1), ("Heavy Gambeson", 0.4f, 1, 1), ("Leather Gloves", 0.8f, 1, 1),
                ("Cloth Pants", 0.8f, 1, 1), ("Leather Leggings", 0.7f, 1, 1), ("Adventurer Boots", 0.8f, 1, 1), ("Adventurer Cloak", 0.6f, 1, 1),
                ("Ruby", 0.5f, 1, 1), ("Emerald", 0.5f, 1, 1), ("Sapphire", 0.5f, 1, 1), ("Gold Goblet", 0.5f, 1, 1)
            };
            (string name, float weight, int min, int max)[] ornate =
            {
                ("Gold Coin Purse", 4f, 1, 2), ("Gold Coin Bag", 1f, 1, 1), ("Gold Coins", 3f, 10, 25), ("Diamond", 1.5f, 1, 2), ("Ruby", 2f, 1, 3), ("Emerald", 2f, 1, 3), ("Sapphire", 2f, 1, 3),
                ("Gold Candlestick", 2f, 1, 1), ("Gold Goblet", 2f, 1, 1), ("Ancient Scroll", 1.5f, 1, 1), ("Gem Necklace", 1f, 1, 1), ("Gold Band", 1f, 1, 1), ("Gem Ring", 1f, 1, 1),
                ("Templar Armor", 0.8f, 1, 1), ("Dark Plate Armor", 0.5f, 1, 1), ("Great Helm", 0.8f, 1, 1), ("Heavy Gauntlets", 0.8f, 1, 1), ("Plate Pants", 0.8f, 1, 1), ("Plate Boots", 0.8f, 1, 1),
                ("Zweihander", 0.8f, 1, 1), ("Longsword", 1f, 1, 1), ("Battle Axe", 0.8f, 1, 1), ("Crossbow", 0.8f, 1, 1), ("Magic Staff", 0.8f, 1, 1), ("Surgical Kit", 1.5f, 1, 1)
            };
            (string name, float weight, int min, int max)[] coffin =
            {
                ("Gold Coins", 5f, 2, 10), ("Ruby", 1f, 1, 1), ("Sapphire", 1f, 1, 1), ("Gold Band", 0.8f, 1, 1), ("Gem Necklace", 0.6f, 1, 1), ("Ancient Scroll", 1f, 1, 1),
                ("Rondel Dagger", 1f, 1, 1), ("Arming Sword", 0.8f, 1, 1), ("Adventurer Cloak", 0.8f, 1, 1), ("Bandage", 2f, 1, 2), ("Gold Goblet", 1f, 1, 1)
            };
            (string name, float weight, int min, int max)[] barrel =
            {
                ("Gold Coins", 4f, 1, 6), ("Bandage", 3f, 1, 2), ("Potion of Healing", 2f, 1, 1), ("Ale", 2f, 1, 2), ("Torch", 2f, 1, 1), ("Throwing Knife", 1f, 1, 2), ("Lockpick", 1f, 1, 1)
            };
            (string name, float weight, int min, int max)[] bookshelf =
            {
                ("Ancient Scroll", 4f, 1, 2), ("Potion of Protection", 2f, 1, 1), ("Potion of Healing", 2f, 1, 1), ("Gold Coins", 2f, 2, 8), ("Wizard Hat", 0.6f, 1, 1), ("Magic Staff", 0.5f, 1, 1), ("Sapphire", 1f, 1, 1)
            };
            (string name, float weight, int min, int max)[] monster =
            {
                ("Gold Coins", 5f, 1, 6), ("Bandage", 2f, 1, 1), ("Ruby", 0.5f, 1, 1), ("Rondel Dagger", 0.5f, 1, 1), ("Potion of Healing", 1f, 1, 1)
            };

            return new Dictionary<string, LootTableConfig>
            {
                ["ChestCommon"] = BuildLootTable(database, "ChestCommon", common, 2, 4, new[] { 15f, 45f, 25f, 10f, 4f, 0.9f, 0.1f }, 0f),
                ["ChestLarge"] = BuildLootTable(database, "ChestLarge", common, 3, 6, new[] { 8f, 40f, 30f, 14f, 6f, 1.7f, 0.3f }, 0f),
                ["ChestOrnate"] = BuildLootTable(database, "ChestOrnate", ornate, 3, 6, new[] { 0f, 0f, 35f, 35f, 20f, 8f, 2f }, 0f),
                ["Coffin"] = BuildLootTable(database, "Coffin", coffin, 1, 3, new[] { 15f, 45f, 25f, 10f, 4f, 0.9f, 0.1f }, 0.15f),
                ["Barrel"] = BuildLootTable(database, "Barrel", barrel, 1, 2, new[] { 30f, 55f, 12f, 3f, 0f, 0f, 0f }, 0.3f),
                ["Bookshelf"] = BuildLootTable(database, "Bookshelf", bookshelf, 1, 3, new[] { 10f, 45f, 30f, 10f, 4f, 1f, 0f }, 0.2f),
                ["Monster"] = BuildLootTable(database, "Monster", monster, 1, 1, new[] { 25f, 55f, 15f, 4f, 1f, 0f, 0f }, 0f)
            };
        }

        private static LootTableConfig BuildLootTable(ItemDatabase database, string name, (string name, float weight, int min, int max)[] entries,
            int minRolls, int maxRolls, float[] rarityWeights, float emptyChance)
        {
            LootTableConfig table = BattleEditorUtility.LoadOrCreate<LootTableConfig>($"{LootFolder}/{name}.asset");
            SerializedObject so = new SerializedObject(table);
            SerializedProperty array = so.FindProperty("_entries");
            array.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                ItemConfig item = database.Find(entries[i].name);

                if (item == null)
                    Debug.LogWarning($"Loot table {name}: unknown item {entries[i].name}");

                element.FindPropertyRelative("_item").objectReferenceValue = item;
                element.FindPropertyRelative("_weight").floatValue = entries[i].weight;
                element.FindPropertyRelative("_minCount").intValue = entries[i].min;
                element.FindPropertyRelative("_maxCount").intValue = entries[i].max;
            }

            BattleEditorUtility.Set(so, "_minRolls", minRolls);
            BattleEditorUtility.Set(so, "_maxRolls", maxRolls);
            BattleEditorUtility.Set(so, "_rarityWeights", rarityWeights);
            BattleEditorUtility.Set(so, "_emptyChance", emptyChance);
            so.ApplyModifiedPropertiesWithoutUndo();

            return table;
        }

        private static InventoryComponent AddInventory(GameObject root, ItemDatabase database, int width, int height, bool hasEquipment, string name)
        {
            GameObject holder = BattleEditorUtility.CreateChild(name, root.transform);
            InventoryComponent inventory = holder.AddComponent<InventoryComponent>();
            SerializedObject so = new SerializedObject(inventory);
            BattleEditorUtility.Set(so, "_database", database);
            BattleEditorUtility.Set(so, "_width", width);
            BattleEditorUtility.Set(so, "_height", height);
            BattleEditorUtility.Set(so, "_hasEquipment", hasEquipment);
            so.ApplyModifiedPropertiesWithoutUndo();

            return inventory;
        }

        private static BoxCollider AddInteractCollider(GameObject root, Vector3 center, Vector3 size)
        {
            GameObject trigger = BattleEditorUtility.CreateChild("Interact", root.transform);
            trigger.layer = LayerMask.NameToLayer(InteractableLayer);
            BoxCollider collider = trigger.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = center;
            collider.size = size;

            return collider;
        }

        private static void BuildAdventurer(BattleContentBuilder.Loadout[] loadouts, GameObject arrow, GameObject orb, ItemDatabase database,
            ClassConfig[] classes, DungeonConfig config, Dictionary<string, WeaponConfig> weapons, GameObject worldItem, GameObject corpse, GameObject campfire,
            GameObject[] armorPieces)
        {
            BattleContentBuilder.FighterParts parts = BattleContentBuilder.CreateFighter(loadouts, arrow, orb, 2, "Adventurer");
            GameObject root = parts.Root;
            BattleEditorUtility.Set(parts.Fighter, "_respawnDelay", 0f);

            InventoryComponent inventory = AddInventory(root, database, 10, 4, true, "Inventory");
            InventoryActionsComponent actions = root.AddComponent<InventoryActionsComponent>();
            BattleEditorUtility.Set(actions, "_inventory", inventory);
            StatusEffectComponent effects = root.AddComponent<StatusEffectComponent>();
            BattleEditorUtility.Set(effects, "_health", parts.Health);

            AdventurerComponent adventurer = root.AddComponent<AdventurerComponent>();
            SerializedObject so = new SerializedObject(adventurer);
            BattleEditorUtility.Set(so, "_fighter", parts.Fighter);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_actions", actions);
            BattleEditorUtility.Set(so, "_effects", effects);
            BattleEditorUtility.Set(so, "_database", database);
            BattleEditorUtility.Set(so, "_classes", classes);
            BattleEditorUtility.Set(so, "_config", config);
            BattleEditorUtility.Set(so, "_interactMask", (LayerMask)(1 << LayerMask.NameToLayer(InteractableLayer)));
            BattleEditorUtility.Set(so, "_worldItemPrefab", worldItem.GetComponent<NetworkObject>());
            BattleEditorUtility.Set(so, "_corpsePrefab", corpse.GetComponent<NetworkObject>());
            BattleEditorUtility.Set(so, "_campfirePrefab", campfire.GetComponent<NetworkObject>());
            BattleEditorUtility.Set(so, "_fistsWeapon", weapons[DungeonWeaponLibrary.Fists]);
            so.ApplyModifiedPropertiesWithoutUndo();

            Transform rightHand = parts.Animator.GetBoneTransform(HumanBodyBones.RightHand);
            Light torchLight = DungeonPropBuilder.PointLight(rightHand, new Vector3(0f, 0.35f, 0.1f), new Color(1f, 0.65f, 0.3f), 9f, 2.6f, false, true);
            torchLight.enabled = false;

            AdventurerVisualComponent visual = root.AddComponent<AdventurerVisualComponent>();
            so = new SerializedObject(visual);
            BattleEditorUtility.Set(so, "_adventurer", adventurer);
            BattleEditorUtility.Set(so, "_animator", parts.Animator);
            BattleEditorUtility.Set(so, "_body", parts.Renderer);
            BattleEditorUtility.Set(so, "_torchLight", torchLight);
            SerializedProperty pieces = so.FindProperty("_pieces");
            (ArmorVisual visual, int prefab, HumanBodyBones bone, bool mirrored)[] mapping = ArmorMapping();
            pieces.arraySize = mapping.Length;

            for (int i = 0; i < mapping.Length; i++)
            {
                SerializedProperty element = pieces.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Visual").intValue = (int)mapping[i].visual;
                element.FindPropertyRelative("Prefab").objectReferenceValue = armorPieces[mapping[i].prefab];
                element.FindPropertyRelative("Bone").intValue = (int)mapping[i].bone;
                element.FindPropertyRelative("IsMirrored").boolValue = mapping[i].mirrored;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            BattleContentBuilder.SavePrefab(root, Prefab("Adventurer"));
        }

        private static (ArmorVisual, int, HumanBodyBones, bool)[] ArmorMapping()
        {
            return new[]
            {
                (ArmorVisual.Hood, 0, HumanBodyBones.Head, false), (ArmorVisual.Cap, 1, HumanBodyBones.Head, false), (ArmorVisual.Helmet, 2, HumanBodyBones.Head, false),
                (ArmorVisual.GreatHelm, 3, HumanBodyBones.Head, false), (ArmorVisual.Tunic, 4, HumanBodyBones.Spine, false), (ArmorVisual.LeatherChest, 5, HumanBodyBones.Spine, false),
                (ArmorVisual.ChainChest, 5, HumanBodyBones.Spine, false), (ArmorVisual.PlateChest, 6, HumanBodyBones.Spine, false),
                (ArmorVisual.Gloves, 7, HumanBodyBones.LeftHand, false), (ArmorVisual.Gloves, 7, HumanBodyBones.RightHand, true),
                (ArmorVisual.Gauntlets, 8, HumanBodyBones.LeftHand, false), (ArmorVisual.Gauntlets, 8, HumanBodyBones.RightHand, true),
                (ArmorVisual.Pants, 9, HumanBodyBones.LeftUpperLeg, false), (ArmorVisual.Pants, 9, HumanBodyBones.RightUpperLeg, true),
                (ArmorVisual.Greaves, 10, HumanBodyBones.LeftLowerLeg, false), (ArmorVisual.Greaves, 10, HumanBodyBones.RightLowerLeg, true),
                (ArmorVisual.Boots, 11, HumanBodyBones.LeftFoot, false), (ArmorVisual.Boots, 11, HumanBodyBones.RightFoot, true),
                (ArmorVisual.PlateBoots, 12, HumanBodyBones.LeftFoot, false), (ArmorVisual.PlateBoots, 12, HumanBodyBones.RightFoot, true),
                (ArmorVisual.Cloak, 13, HumanBodyBones.UpperChest, false)
            };
        }

        /// Primitive armor pieces parented to bones: index order matches ArmorMapping.
        private static GameObject[] BuildArmorPieces()
        {
            Material cloth = BattleEditorUtility.GetMaterial("ArmorCloth", new Color(0.5f, 0.45f, 0.4f), 0f, 0.2f);
            Material metal = BattleEditorUtility.GetMaterial("ArmorMetal", new Color(0.7f, 0.72f, 0.78f), 0.8f, 0.6f);
            Material leather = BattleEditorUtility.GetMaterial("ArmorLeather", new Color(0.45f, 0.3f, 0.18f), 0f, 0.35f);

            return new[]
            {
                Piece("Armor_Hood", PrimitiveType.Sphere, new Vector3(0f, 0.08f, -0.02f), new Vector3(0.26f, 0.3f, 0.27f), cloth),
                Piece("Armor_Cap", PrimitiveType.Sphere, new Vector3(0f, 0.11f, 0f), new Vector3(0.25f, 0.2f, 0.26f), leather),
                Piece("Armor_Helmet", PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0f), new Vector3(0.27f, 0.26f, 0.28f), metal),
                Piece("Armor_GreatHelm", PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.28f, 0.16f, 0.28f), metal),
                Piece("Armor_Tunic", PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(0.4f, 0.4f, 0.3f), cloth),
                Piece("Armor_LeatherChest", PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(0.42f, 0.4f, 0.32f), leather),
                Piece("Armor_PlateChest", PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(0.46f, 0.42f, 0.36f), metal),
                Piece("Armor_Gloves", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.05f), new Vector3(0.11f, 0.11f, 0.16f), leather),
                Piece("Armor_Gauntlets", PrimitiveType.Cube, new Vector3(0f, 0f, 0.05f), new Vector3(0.12f, 0.12f, 0.2f), metal),
                Piece("Armor_Pants", PrimitiveType.Capsule, new Vector3(0f, -0.2f, 0f), new Vector3(0.18f, 0.24f, 0.18f), cloth),
                Piece("Armor_Greaves", PrimitiveType.Capsule, new Vector3(0f, -0.2f, 0f), new Vector3(0.16f, 0.22f, 0.16f), metal),
                Piece("Armor_Boots", PrimitiveType.Cube, new Vector3(0f, -0.03f, 0.06f), new Vector3(0.13f, 0.1f, 0.26f), leather),
                Piece("Armor_PlateBoots", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.06f), new Vector3(0.15f, 0.12f, 0.28f), metal),
                Piece("Armor_Cloak", PrimitiveType.Cube, new Vector3(0f, -0.35f, -0.17f), new Vector3(0.44f, 0.9f, 0.03f), cloth)
            };
        }

        private static GameObject Piece(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject root = BattleEditorUtility.CreatePrimitive(type, name, null, position, Vector3.zero, scale, material);
            root.GetComponent<Renderer>().lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{ArmorFolder}/{name}.prefab");
            Object.DestroyImmediate(root);

            return prefab;
        }

        private static void BuildMonster(BattleContentBuilder.Loadout[] loadouts, GameObject arrow, GameObject orb, string name, string displayName,
            int health, float damage, float moveSpeed, float actionSpeed, float aggro, bool isRanged, bool canBlock, int weaponIndex, int experience,
            LootTableConfig lootTable, Material body, float scale, GameObject worldItem)
        {
            MonsterConfig config = BattleEditorUtility.LoadOrCreate<MonsterConfig>($"{MonstersFolder}/{name}.asset");
            SerializedObject so = new SerializedObject(config);
            BattleEditorUtility.Set(so, "_displayName", displayName);
            BattleEditorUtility.Set(so, "_maxHealth", health);
            BattleEditorUtility.Set(so, "_damageMultiplier", damage);
            BattleEditorUtility.Set(so, "_moveSpeed", moveSpeed);
            BattleEditorUtility.Set(so, "_actionSpeed", actionSpeed);
            BattleEditorUtility.Set(so, "_aggroRange", aggro);
            BattleEditorUtility.Set(so, "_leashRange", 24f);
            BattleEditorUtility.Set(so, "_armorReduction", -0.22f);
            BattleEditorUtility.Set(so, "_magicReduction", -0.17f);
            BattleEditorUtility.Set(so, "_weaponIndex", weaponIndex);
            BattleEditorUtility.Set(so, "_experience", experience);
            BattleEditorUtility.Set(so, "_lootTable", lootTable);
            BattleEditorUtility.Set(so, "_isRanged", isRanged);
            BattleEditorUtility.Set(so, "_canBlock", canBlock);
            BattleEditorUtility.Set(so, "_bodyMaterial", body);
            BattleEditorUtility.Set(so, "_scale", scale);
            so.ApplyModifiedPropertiesWithoutUndo();

            BattleContentBuilder.FighterParts parts = BattleContentBuilder.CreateFighter(loadouts, arrow, orb, 1, name);
            GameObject root = parts.Root;
            BattleEditorUtility.Set(parts.Fighter, "_respawnDelay", 0f);
            parts.Renderer.sharedMaterial = body;
            parts.Animator.transform.localScale = Vector3.one * scale;

            MonsterComponent monster = root.AddComponent<MonsterComponent>();
            so = new SerializedObject(monster);
            BattleEditorUtility.Set(so, "_config", config);
            BattleEditorUtility.Set(so, "_fighter", parts.Fighter);
            BattleEditorUtility.Set(so, "_worldItemPrefab", worldItem.GetComponent<NetworkObject>());
            so.ApplyModifiedPropertiesWithoutUndo();

            MonsterBrainComponent brain = root.AddComponent<MonsterBrainComponent>();
            so = new SerializedObject(brain);
            BattleEditorUtility.Set(so, "_fighter", parts.Fighter);
            BattleEditorUtility.Set(so, "_monster", monster);
            BattleEditorUtility.Set(so, "_sightMask", (LayerMask)1);
            BattleEditorUtility.Set(so, "_doorMask", (LayerMask)(1 << LayerMask.NameToLayer(InteractableLayer)));
            so.ApplyModifiedPropertiesWithoutUndo();

            BattleContentBuilder.SavePrefab(root, Prefab(name));
        }

        private static GameObject BuildCorpse(ItemDatabase database, ClassConfig[] classes)
        {
            GameObject root = new GameObject("Corpse");
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), root.transform);
            model.name = "Model";
            Animator animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(BattleEditorUtility.ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            SkinnedMeshRenderer renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            renderer.sharedMaterial = BattleEditorUtility.GetMaterial("FighterBody", new Color(0.62f, 0.66f, 0.72f));
            renderer.updateWhenOffscreen = true;

            InventoryComponent inventory = AddInventory(root, database, 10, 4, true, "Inventory");
            ContainerComponent container = root.AddComponent<ContainerComponent>();
            SerializedObject so = new SerializedObject(container);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_displayName", "Dead Adventurer");
            BattleEditorUtility.Set(so, "_openTime", 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            AddInteractCollider(root, new Vector3(0f, 0.3f, 0.6f), new Vector3(1f, 0.6f, 1.8f));

            CorpseComponent corpse = root.AddComponent<CorpseComponent>();
            so = new SerializedObject(corpse);
            BattleEditorUtility.Set(so, "_container", container);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_animator", animator);
            BattleEditorUtility.Set(so, "_bodyRenderer", renderer);
            BattleEditorUtility.Set(so, "_classes", classes);
            so.ApplyModifiedPropertiesWithoutUndo();

            return BattleContentBuilder.SavePrefab(root, Prefab("Corpse"));
        }

        private static void BuildSession(ItemDatabase database, ClassConfig[] classes, DungeonConfig config)
        {
            GameObject root = new GameObject("PlayerSession");
            root.AddComponent<NetworkObject>();
            InventoryComponent kit = AddInventory(root, database, 10, 4, true, "Kit");
            InventoryComponent stash = AddInventory(root, database, 12, 5, false, "Stash");
            InventoryActionsComponent actions = root.AddComponent<InventoryActionsComponent>();
            BattleEditorUtility.Set(actions, "_inventory", kit);

            PlayerSessionComponent session = root.AddComponent<PlayerSessionComponent>();
            SerializedObject so = new SerializedObject(session);
            BattleEditorUtility.Set(so, "_kit", kit);
            BattleEditorUtility.Set(so, "_stash", stash);
            BattleEditorUtility.Set(so, "_actions", actions);
            BattleEditorUtility.Set(so, "_classes", classes);
            BattleEditorUtility.Set(so, "_config", config);
            so.ApplyModifiedPropertiesWithoutUndo();

            BattleContentBuilder.SavePrefab(root, Prefab("PlayerSession"));
        }

        private static void BuildMatch(DungeonConfig config)
        {
            GameObject root = new GameObject("Match");
            root.AddComponent<NetworkObject>();
            BattleEditorUtility.Set(root.AddComponent<MatchComponent>(), "_config", config);
            BattleContentBuilder.SavePrefab(root, Prefab("Match"));
        }

        private static GameObject BuildWorldItem(ItemDatabase database)
        {
            GameObject root = DungeonPropBuilder.LootSack();
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            WorldItemComponent item = root.AddComponent<WorldItemComponent>();
            SerializedObject so = new SerializedObject(item);
            BattleEditorUtility.Set(so, "_database", database);
            BattleEditorUtility.Set(so, "_renderer", root.GetComponentInChildren<MeshRenderer>());
            so.ApplyModifiedPropertiesWithoutUndo();
            AddInteractCollider(root, new Vector3(0f, 0.2f, 0f), new Vector3(0.6f, 0.5f, 0.6f));

            return BattleContentBuilder.SavePrefab(root, Prefab("WorldItem"));
        }

        private static GameObject BuildCampfire()
        {
            GameObject root = DungeonPropBuilder.Campfire();
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            CampfireComponent campfire = root.AddComponent<CampfireComponent>();
            BattleEditorUtility.Set(campfire, "_light", root.GetComponentInChildren<Light>());
            AddInteractCollider(root, new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 1.6f));

            return BattleContentBuilder.SavePrefab(root, Prefab("Campfire"));
        }

        private static void SetupContainer(GameObject root, string displayName, LootTableConfig table, float openTime, Transform lid, Vector3 lidOpen, bool removeWhenEmpty, int width, int height)
        {
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
            InventoryComponent inventory = AddInventory(root, database, width, height, false, "Inventory");
            ContainerComponent container = root.AddComponent<ContainerComponent>();
            SerializedObject so = new SerializedObject(container);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_displayName", displayName);
            BattleEditorUtility.Set(so, "_openTime", openTime);
            BattleEditorUtility.Set(so, "_lootTable", table);
            BattleEditorUtility.Set(so, "_lid", lid);
            BattleEditorUtility.Set(so, "_lidOpenEuler", lidOpen);
            BattleEditorUtility.Set(so, "_isRemovedWhenEmpty", removeWhenEmpty);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildContainer(string name, string displayName, LootTableConfig table, float openTime, bool isGolden)
        {
            float width = name.StartsWith("Large") || isGolden ? 1.5f : 1.1f;
            GameObject root = DungeonPropBuilder.Chest(name, width, 0.7f, 0.75f, isGolden ? DungeonPropBuilder.Gold : DungeonPropBuilder.DarkWood, out Transform lid);
            SetupContainer(root, displayName, table, openTime, lid, new Vector3(-100f, 0f, 0f), false, 6, 4);
            AddInteractCollider(root, new Vector3(0f, 0.4f, 0f), new Vector3(width + 0.2f, 0.9f, 0.9f));
            BattleContentBuilder.SavePrefab(root, Prefab(name));
        }

        private static void BuildCoffin(LootTableConfig table)
        {
            GameObject root = DungeonPropBuilder.Coffin(out Transform lid);
            SetupContainer(root, "Coffin", table, 2f, lid, new Vector3(0f, 0f, 75f), false, 5, 3);
            AddInteractCollider(root, new Vector3(0f, 0.4f, 0f), new Vector3(1.1f, 0.9f, 2.4f));
            BattleContentBuilder.SavePrefab(root, Prefab("Coffin"));
        }

        private static void BuildBarrel(LootTableConfig table)
        {
            GameObject root = DungeonPropBuilder.Barrel();
            SetupContainer(root, "Barrel", table, 0.8f, null, Vector3.zero, false, 4, 2);
            AddInteractCollider(root, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 1f, 0.9f));
            BattleContentBuilder.SavePrefab(root, Prefab("Barrel"));
        }

        private static void BuildCrate(LootTableConfig table)
        {
            GameObject root = DungeonPropBuilder.Crate();
            SetupContainer(root, "Crate", table, 0.8f, null, Vector3.zero, false, 4, 2);
            AddInteractCollider(root, new Vector3(0f, 0.4f, 0f), new Vector3(1f, 0.9f, 1f));
            BattleContentBuilder.SavePrefab(root, Prefab("Crate"));
        }

        private static void BuildBookshelf(LootTableConfig table)
        {
            GameObject root = DungeonPropBuilder.Bookshelf();
            SetupContainer(root, "Bookshelf", table, 1.2f, null, Vector3.zero, false, 5, 2);
            AddInteractCollider(root, new Vector3(0f, 1.1f, 0.1f), new Vector3(1.7f, 2.2f, 0.7f));
            BattleContentBuilder.SavePrefab(root, Prefab("Bookshelf"));
        }

        private static void BuildDoor()
        {
            GameObject root = new GameObject("Door");
            root.AddComponent<NetworkObject>();
            GameObject hinge = BattleEditorUtility.CreateChild("Hinge", root.transform, new Vector3(-1.05f, 0f, 0f));
            GameObject leaf = DungeonPropBuilder.DoorLeaf();
            leaf.transform.SetParent(hinge.transform, false);
            leaf.isStatic = false;

            DoorComponent door = root.AddComponent<DoorComponent>();
            SerializedObject so = new SerializedObject(door);
            BattleEditorUtility.Set(so, "_leaf", hinge.transform);
            BattleEditorUtility.Set(so, "_blocker", leaf.GetComponent<MeshCollider>());
            so.ApplyModifiedPropertiesWithoutUndo();
            AddInteractCollider(root, new Vector3(0f, 1.5f, 0f), new Vector3(2.2f, 3f, 0.6f));
            BattleContentBuilder.SavePrefab(root, Prefab("Door"));
        }

        private static void BuildPortal(string name, PortalKind kind, Material material, bool singleUse)
        {
            GameObject root = DungeonPropBuilder.Portal(material);
            root.name = name;
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            PortalComponent portal = root.AddComponent<PortalComponent>();
            SerializedObject so = new SerializedObject(portal);
            BattleEditorUtility.Set(so, "_kind", kind);
            BattleEditorUtility.Set(so, "_activationTime", kind == PortalKind.Escape ? 3f : 2f);
            BattleEditorUtility.Set(so, "_isSingleUse", kind == PortalKind.Escape);
            BattleEditorUtility.Set(so, "_visual", root.transform.Find("Visual").gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            AddInteractCollider(root, new Vector3(0f, 1.3f, 0f), new Vector3(2.2f, 2.6f, 2.2f));
            BattleContentBuilder.SavePrefab(root, Prefab(name));
        }

        private static void BuildShrine(ShrineKind kind, float magnitude, float duration)
        {
            GameObject root = DungeonPropBuilder.Altar(DungeonPropBuilder.ShrineGlow);
            root.name = "Shrine" + kind;
            root.AddComponent<NetworkObject>();
            ShrineComponent shrine = root.AddComponent<ShrineComponent>();
            SerializedObject so = new SerializedObject(shrine);
            BattleEditorUtility.Set(so, "_kind", kind);
            BattleEditorUtility.Set(so, "_magnitude", magnitude);
            BattleEditorUtility.Set(so, "_duration", duration);
            BattleEditorUtility.Set(so, "_glow", root.transform.Find("Glow").gameObject);
            BattleEditorUtility.Set(so, "_prompt", "Pray");
            BattleEditorUtility.Set(so, "_holdTime", 2f);
            so.ApplyModifiedPropertiesWithoutUndo();
            AddInteractCollider(root, new Vector3(0f, 0.8f, 0f), new Vector3(1.8f, 1.8f, 1.2f));
            BattleContentBuilder.SavePrefab(root, Prefab(root.name));
        }

        private static void BuildLever()
        {
            GameObject root = DungeonPropBuilder.Lever(out Transform handle);
            root.AddComponent<NetworkObject>();
            LeverComponent lever = root.AddComponent<LeverComponent>();
            SerializedObject so = new SerializedObject(lever);
            BattleEditorUtility.Set(so, "_handle", handle);
            BattleEditorUtility.Set(so, "_holdTime", 0.6f);
            so.ApplyModifiedPropertiesWithoutUndo();
            AddInteractCollider(root, new Vector3(0f, 0.8f, 0f), new Vector3(0.8f, 1.6f, 0.8f));
            BattleContentBuilder.SavePrefab(root, Prefab("Lever"));
        }

        private static void BuildSpikeTrap()
        {
            GameObject root = DungeonPropBuilder.SpikeTrap(out Transform spikes);
            root.AddComponent<NetworkObject>();
            TrapComponent trap = root.AddComponent<TrapComponent>();
            SerializedObject so = new SerializedObject(trap);
            BattleEditorUtility.Set(so, "_kind", TrapKind.Spikes);
            BattleEditorUtility.Set(so, "_damage", 20);
            BattleEditorUtility.Set(so, "_period", 3f);
            BattleEditorUtility.Set(so, "_activeTime", 0.5f);
            BattleEditorUtility.Set(so, "_halfExtents", new Vector3(1f, 0.6f, 1f));
            BattleEditorUtility.Set(so, "_center", new Vector3(0f, 0.6f, 0f));
            BattleEditorUtility.Set(so, "_victimMask", (LayerMask)(1 << LayerMask.NameToLayer(BattleEditorUtility.CharacterLayer)));
            BattleEditorUtility.Set(so, "_moving", spikes);
            BattleEditorUtility.Set(so, "_restPosition", new Vector3(0f, -0.7f, 0f));
            BattleEditorUtility.Set(so, "_activePosition", new Vector3(0f, 0.05f, 0f));
            so.ApplyModifiedPropertiesWithoutUndo();
            BattleContentBuilder.SavePrefab(root, Prefab("SpikeTrap"));
        }

        private static void BuildBladeTrap()
        {
            GameObject root = DungeonPropBuilder.BladeTrap(out Transform pivot);
            root.AddComponent<NetworkObject>();
            TrapComponent trap = root.AddComponent<TrapComponent>();
            SerializedObject so = new SerializedObject(trap);
            BattleEditorUtility.Set(so, "_kind", TrapKind.SwingingBlade);
            BattleEditorUtility.Set(so, "_damage", 25);
            BattleEditorUtility.Set(so, "_period", 2.6f);
            BattleEditorUtility.Set(so, "_halfExtents", new Vector3(0.9f, 1.1f, 0.4f));
            BattleEditorUtility.Set(so, "_center", new Vector3(0f, 1.1f, 0f));
            BattleEditorUtility.Set(so, "_victimMask", (LayerMask)(1 << LayerMask.NameToLayer(BattleEditorUtility.CharacterLayer)));
            BattleEditorUtility.Set(so, "_moving", pivot);
            BattleEditorUtility.Set(so, "_swingEuler", new Vector3(0f, 0f, 70f));
            so.ApplyModifiedPropertiesWithoutUndo();
            BattleContentBuilder.SavePrefab(root, Prefab("BladeTrap"));
        }

        private static string Sanitize(string name)
        {
            return name.Replace(" ", string.Empty).Replace("'", string.Empty);
        }
    }
}
