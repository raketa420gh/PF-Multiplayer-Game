using System.Collections.Generic;
using System.Linq;
using Fusion;
using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using Unity.AI.Navigation;
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
        public const string MerchantsFolder = ConfigsFolder + "/Merchants";
        public const string ArmorFolder = DungeonPropBuilder.PrefabsFolder + "/Armor";
        public const string DatabasePath = ConfigsFolder + "/ItemDatabase.asset";
        public const string DungeonConfigPath = ConfigsFolder + "/Dungeon.asset";
        public const string InteractableLayer = "Interactable";

        public static string Prefab(string name) => $"{DungeonPropBuilder.PrefabsFolder}/{name}.prefab";

        public static void Build()
        {
            foreach (string folder in new[] { ConfigsFolder, ItemsFolder, AbilitiesFolder, ClassesFolder, LootFolder, MonstersFolder, MerchantsFolder, DungeonPropBuilder.PrefabsFolder, ArmorFolder })
                BattleEditorUtility.EnsureFolder(folder);

            BattleEditorUtility.EnsureLayer(InteractableLayer);
            DungeonKitBuilder.Build();
            BattleContentBuilder.Loadout[] loadouts = BattleContentBuilder.BuildWeapons(out GameObject arrow, out GameObject orb);
            Dictionary<string, WeaponConfig> weapons = new();

            foreach (BattleContentBuilder.Loadout loadout in loadouts)
                weapons[loadout.Name] = loadout.Config;

            ItemDatabase database = BuildItems(weapons);
            DungeonConfig config = BattleEditorUtility.LoadOrCreate<DungeonConfig>(DungeonConfigPath);
            BuildSwarmStages(config);
            BuildCaltrops();
            BuildSmokePot();
            ClassConfig[] classes = BuildClasses(database);
            Dictionary<string, LootTableConfig> loot = BuildLootTables(database);
            GameObject worldItem = BuildWorldItem(database);
            GameObject campfire = BuildCampfire();
            ArmorPieceSetConfig pieceSet = BuildArmorPieceSet();
            GameObject corpse = BuildCorpse(database, classes, pieceSet);
            BattleEditorUtility.EnsureLayer(DungeonUiBuilder.PreviewLayer);
            BuildPreviewRig(pieceSet);

            BuildAdventurer(loadouts, arrow, orb, database, classes, config, weapons, worldItem, corpse, campfire, pieceSet);
            Color rags = new Color(0.25f, 0.22f, 0.16f);
            BuildMonster(new MonsterDef { Name = "SkeletonSwordsman", DisplayName = "Skeleton Swordsman", Health = 117, Damage = 1.5f, MoveSpeed = 220f, ActionSpeed = 0.7f, Aggro = 10f, CanBlock = true, WeaponIndex = 5, Experience = 25, Loot = loot["Monster"], Body = DungeonPropBuilder.Bone, Scale = 0.98f,
                Attachments = new[] { (ArmorVisual.Skull, Color.black), (ArmorVisual.Ribcage, Color.black), (ArmorVisual.PeasantChest, rags) } }, loadouts, arrow, orb, database, pieceSet);
            BuildMonster(new MonsterDef { Name = "SkeletonArcher", DisplayName = "Skeleton Archer", Health = 70, Damage = 1f, MoveSpeed = 210f, ActionSpeed = 0.85f, Aggro = 14f, IsRanged = true, WeaponIndex = 2, Experience = 25, Loot = loot["Monster"], Body = DungeonPropBuilder.Bone, Scale = 0.98f,
                Attachments = new[] { (ArmorVisual.Skull, Color.black), (ArmorVisual.Ribcage, Color.black), (ArmorVisual.RangerHead, rags) } }, loadouts, arrow, orb, database, pieceSet);
            BuildFlyingHead(new MonsterDef { Name = "FlyingHead", DisplayName = "Flying Head", Health = 60, Damage = 1f, MoveSpeed = 230f, ActionSpeed = 1f, Aggro = 12f, Experience = 30, Loot = loot["Monster"], Scale = 1f,
                Voice = DungeonSound.Screech, Charge = 900f }, arrow, orb, database);
            BuildMonster(new MonsterDef { Name = "SkeletonChampion", DisplayName = "Skeleton Champion", Health = 525, Damage = 1.4f, MoveSpeed = 210f, ActionSpeed = 0.8f, Aggro = 13f, CanBlock = true, WeaponIndex = 15, Experience = 150, Loot = loot["Boss"], Body = DungeonPropBuilder.Bone, Scale = 1.28f,
                IsBoss = true, Lunge = 5f, Attachments = new[] { (ArmorVisual.Skull, Color.black), (ArmorVisual.MarauderHead, Color.gray), (ArmorVisual.MarauderChest, Color.gray), (ArmorVisual.MarauderHands, Color.gray), (ArmorVisual.MarauderLegs, Color.gray), (ArmorVisual.MarauderFeet, Color.gray) } }, loadouts, arrow, orb, database, pieceSet);

            BuildFigures();
            BuildSession(database, classes, config, BuildMerchants(database));
            BuildMatch(config);
            BuildContainer("SmallOakChest", "Small Oak Chest", loot["ChestCommon"], false);
            BuildContainer("LargeOakChest", "Large Oak Chest", loot["ChestLarge"], false);
            BuildContainer("GoldenChest", "Golden Chest", loot["ChestOrnate"], true);
            BuildCoffin(loot["Coffin"]);
            BuildBarrel(loot["Barrel"]);
            BuildCrate(loot["Barrel"]);
            BuildBookshelf(loot["Bookshelf"]);
            BuildDoor("Door", DungeonPropBuilder.DoorLeaf());
            BuildDoor("CellDoor", DungeonPropBuilder.CellDoorLeaf());
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
            Dictionary<ArmorVisual, GameObject> outfitModels = BuildOutfitModels(defs);

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
                GameObject model = ResolveModel(def, weapons, outfitModels, out float zoom, out Vector3 euler, out Vector3 shift);
                BattleEditorUtility.Set(so, "_worldModel", def.Kind == ItemKind.Armor ? null : model);
                BattleEditorUtility.Set(so, "_icon", model != null ? DungeonIconBuilder.Render(model, Sanitize(def.Name), zoom, euler, shift) : null);

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
                        BattleEditorUtility.Set(so, "_visualColor", Color.white);
                        BattleEditorUtility.Set(so, "_classes", System.Array.ConvertAll(def.Classes ?? new string[0],
                            name => BattleEditorUtility.LoadOrCreate<ClassConfig>($"{ClassesFolder}/{name}.asset")));
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

        /// Picks the 3D representation of an item: weapon attachments, outfit parts or a dedicated small model.
        private static GameObject ResolveModel(ItemDef def, Dictionary<string, WeaponConfig> weapons, Dictionary<ArmorVisual, GameObject> outfitModels,
            out float zoom, out Vector3 euler, out Vector3 shift)
        {
            zoom = 1f;
            euler = Vector3.zero;
            shift = Vector3.zero;

            switch (def.Kind)
            {
                case ItemKind.Weapon:
                    euler = new Vector3(0f, 0f, 0f);
                    string prefix = def.WeaponPrefix ?? (def.WeaponClass == WeaponClass.Shield ? DungeonWeaponLibrary.SwordShield : null);

                    if (prefix == null || !weapons.TryGetValue(prefix, out WeaponConfig weapon))
                        return null;

                    foreach (WeaponAttachment attachment in weapon.Attachments)
                    {
                        bool isShield = attachment.Socket is WeaponSocket.LeftShield or WeaponSocket.RightShield;

                        if (def.WeaponClass == WeaponClass.Shield == isShield)
                        {
                            euler = isShield ? new Vector3(-90f, 0f, 0f) : new Vector3(0f, -90f, -45f);

                            return attachment.Prefab;
                        }
                    }

                    return null;
                case ItemKind.Armor:
                    // The figure stands with its arms spread: sleeves are cropped to the body, of a pair of bracers one is shown.
                    bool hasSleeves = def.Parts != null && System.Array.Exists(def.Parts, part => part is OutfitPart.PeasantArms or OutfitPart.RangerArms);
                    zoom = def.Slot == EquipSlot.Hands ? 4.2f : hasSleeves ? 2.2f : 1.1f;
                    shift = def.Slot == EquipSlot.Hands ? Vector3.left * 0.8f : Vector3.zero;

                    return outfitModels.GetValueOrDefault(def.Visual);
                default:
                    zoom = 1.05f;

                    return DungeonItemModelBuilder.Build(def);
            }
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
            BattleEditorUtility.Set(so, "_icon", DungeonAbilityIconBuilder.Build(Sanitize(def.Name), def.Icon ?? DungeonAbilityIconBuilder.SymbolFor(def.Kind), def.Color));

            if (def.Kind == AbilityKind.Projectile && def.HitEffect != StatusEffectKind.None)
                BattleEditorUtility.Set(so, "_effect", def.HitEffect);

            BattleEditorUtility.Set(so, "_healthCost", def.HealthCost);
            BattleEditorUtility.Set(so, "_lifeSteal", def.LifeSteal);
            BattleEditorUtility.Set(so, "_staggerDuration", def.Stagger);
            BattleEditorUtility.Set(so, "_spawnPrefab", def.SpawnPrefab != null ? AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(def.SpawnPrefab)).GetComponent<NetworkObject>() : null);
            so.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }

        private static ClassConfig[] BuildClasses(ItemDatabase database)
        {
            ClassDef[] defs = DungeonClassLibrary.CreateClasses();
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
                stats.FindPropertyRelative("Flesh").intValue = def.Stats.Flesh;
                stats.FindPropertyRelative("Grip").intValue = def.Stats.Grip;
                stats.FindPropertyRelative("Reflex").intValue = def.Stats.Reflex;
                stats.FindPropertyRelative("Craft").intValue = def.Stats.Craft;
                stats.FindPropertyRelative("Insight").intValue = def.Stats.Insight;
                stats.FindPropertyRelative("Resonance").intValue = def.Stats.Resonance;

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
                    string symbol = def.Perks[p].Icon ?? (def.Perks[p].Modifiers.Length > 0 ? DungeonAbilityIconBuilder.SymbolFor(def.Perks[p].Modifiers[0].Stat) : "Chevrons");
                    perk.FindPropertyRelative("_icon").objectReferenceValue = DungeonAbilityIconBuilder.Build(Sanitize(def.Perks[p].Name), symbol, def.Perks[p].Color ?? def.Color);
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

                BattleEditorUtility.Set(so, "_castFocus", def.Focus);
                so.ApplyModifiedPropertiesWithoutUndo();
                configs[i] = config;
            }

            return configs;
        }

        private static Dictionary<string, LootTableConfig> BuildLootTables(ItemDatabase database)
        {
            List<ItemDef> defs = DungeonItemLibrary.CreateItems();

            // Every piece of the given outfits at one weight.
            IEnumerable<(string, float, int, int)> Pieces(float weight, params string[] sets)
            {
                return defs.Where(def => System.Array.IndexOf(sets, def.Set) >= 0).Select(def => (def.Name, weight, 1, 1));
            }

            (string name, float weight, int min, int max)[] common = new (string, float, int, int)[]
            {
                ("Gold Coins", 8f, 3, 14), ("Bandage", 4f, 1, 3), ("Potion of Healing", 3f, 1, 2), ("Potion of Protection", 1.5f, 1, 1), ("Lockpick", 1.5f, 1, 2),
                ("Ale", 1f, 1, 1), ("Throwing Knife", 1f, 1, 2), ("Francisca Axe", 0.8f, 1, 2), ("Torch", 1.5f, 1, 1), ("Campfire Kit", 0.6f, 1, 1),
                ("Arming Sword", 1f, 1, 1), ("Falchion", 0.7f, 1, 1), ("Flanged Mace", 0.7f, 1, 1), ("Rondel Dagger", 1f, 1, 1), ("Recurve Bow", 0.6f, 1, 1), ("Round Shield", 0.8f, 1, 1),
                ("Spear", 0.5f, 1, 1), ("Longsword", 0.5f, 1, 1), ("Crossbow", 0.4f, 1, 1), ("Magic Staff", 0.5f, 1, 1), ("Battle Axe", 0.4f, 1, 1), ("Zweihander", 0.3f, 1, 1),
                ("Ruby", 0.5f, 1, 1), ("Emerald", 0.5f, 1, 1), ("Sapphire", 0.5f, 1, 1), ("Gold Goblet", 0.5f, 1, 1),
                ("Short Sword", 0.9f, 1, 1), ("Rapier", 0.5f, 1, 1), ("Viking Sword", 0.5f, 1, 1), ("Hatchet", 0.8f, 1, 1), ("Morning Star", 0.5f, 1, 1),
                ("Castillon Dagger", 0.6f, 1, 1), ("Stiletto Dagger", 0.6f, 1, 1), ("Felling Axe", 0.5f, 1, 1), ("Halberd", 0.3f, 1, 1), ("Buckler", 0.6f, 1, 1),
                ("Potion of Invisibility", 0.6f, 1, 1), ("Silver Chalice", 0.6f, 1, 1), ("Gold Ore", 0.8f, 1, 3), ("Silver Ingot", 0.3f, 1, 1)
            }.Concat(Pieces(0.9f, "Peasant")).Concat(Pieces(0.6f, "Ranger")).Concat(Pieces(0.3f, "Mystic", "Occultist", "Marauder", "Berserker")).ToArray();
            (string name, float weight, int min, int max)[] ornate = new (string, float, int, int)[]
            {
                ("Gold Coin Purse", 4f, 1, 2), ("Gold Coin Bag", 1f, 1, 1), ("Gold Coins", 3f, 10, 25), ("Diamond", 1.5f, 1, 2), ("Ruby", 2f, 1, 3), ("Emerald", 2f, 1, 3), ("Sapphire", 2f, 1, 3),
                ("Gold Candlestick", 2f, 1, 1), ("Gold Goblet", 2f, 1, 1), ("Ancient Scroll", 1.5f, 1, 1), ("Gem Necklace", 1f, 1, 1), ("Gold Band", 1f, 1, 1), ("Gem Ring", 1f, 1, 1),
                ("Zweihander", 0.8f, 1, 1), ("Longsword", 1f, 1, 1), ("Battle Axe", 0.8f, 1, 1), ("Crossbow", 0.8f, 1, 1), ("Magic Staff", 0.8f, 1, 1), ("Surgical Kit", 1.5f, 1, 1),
                ("War Maul", 0.8f, 1, 1), ("Halberd", 0.8f, 1, 1), ("Rapier", 0.8f, 1, 1), ("Viking Sword", 0.8f, 1, 1), ("Heater Shield", 0.8f, 1, 1),
                ("Fox Pendant", 0.6f, 1, 1), ("Ox Pendant", 0.6f, 1, 1), ("Bear Pendant", 0.6f, 1, 1), ("Owl Pendant", 0.6f, 1, 1),
                ("Ring of Courage", 0.6f, 1, 1), ("Ring of Vitality", 0.6f, 1, 1), ("Ring of Finesse", 0.6f, 1, 1), ("Ring of Wisdom", 0.6f, 1, 1),
                ("Troll's Blood", 1f, 1, 1), ("Potion of Invisibility", 1f, 1, 2), ("Gold Crown", 0.5f, 1, 1), ("Gold Ingot", 1f, 1, 2), ("Pearl Necklace", 1f, 1, 1)
            }.Concat(Pieces(0.5f, "Ranger")).Concat(Pieces(0.7f, "Mystic", "Occultist", "Marauder", "Berserker")).ToArray();
            (string name, float weight, int min, int max)[] coffin =
            {
                ("Gold Coins", 5f, 2, 10), ("Ruby", 1f, 1, 1), ("Sapphire", 1f, 1, 1), ("Gold Band", 0.8f, 1, 1), ("Gem Necklace", 0.6f, 1, 1), ("Ancient Scroll", 1f, 1, 1),
                ("Rondel Dagger", 1f, 1, 1), ("Arming Sword", 0.8f, 1, 1), ("Peasant Hood", 0.8f, 1, 1), ("Bandage", 2f, 1, 2), ("Gold Goblet", 1f, 1, 1),
                ("Silver Chalice", 1f, 1, 1), ("Pearl Necklace", 0.5f, 1, 1), ("Ring of Vitality", 0.4f, 1, 1), ("Bear Pendant", 0.3f, 1, 1),
                ("Stiletto Dagger", 0.6f, 1, 1), ("Ranger Hood", 0.5f, 1, 1), ("Troll's Blood", 0.4f, 1, 1)
            };
            (string name, float weight, int min, int max)[] barrel =
            {
                ("Gold Coins", 4f, 1, 6), ("Bandage", 3f, 1, 2), ("Potion of Healing", 2f, 1, 1), ("Ale", 2f, 1, 2), ("Torch", 2f, 1, 1), ("Throwing Knife", 1f, 1, 2), ("Lockpick", 1f, 1, 1)
            };
            (string name, float weight, int min, int max)[] bookshelf = new (string, float, int, int)[]
            {
                ("Ancient Scroll", 4f, 1, 2), ("Potion of Protection", 2f, 1, 1), ("Potion of Healing", 2f, 1, 1), ("Gold Coins", 2f, 2, 8), ("Magic Staff", 0.5f, 1, 1), ("Sapphire", 1f, 1, 1),
                ("Potion of Invisibility", 1.5f, 1, 1), ("Owl Pendant", 0.3f, 1, 1), ("Ring of Wisdom", 0.3f, 1, 1)
            }.Concat(Pieces(0.4f, "Mystic", "Occultist")).ToArray();
            (string name, float weight, int min, int max)[] monster = new (string, float, int, int)[]
            {
                ("Gold Coins", 5f, 1, 6), ("Bandage", 2f, 1, 1), ("Ruby", 0.5f, 1, 1), ("Rondel Dagger", 0.5f, 1, 1), ("Potion of Healing", 1f, 1, 1),
                ("Short Sword", 0.4f, 1, 1), ("Hatchet", 0.4f, 1, 1), ("Gold Ore", 0.6f, 1, 2), ("Silver Chalice", 0.3f, 1, 1)
            }.Concat(Pieces(0.4f, "Peasant")).ToArray();

            return new Dictionary<string, LootTableConfig>
            {
                ["ChestCommon"] = BuildLootTable(database, "ChestCommon", common, 2, 4, new[] { 15f, 45f, 25f, 10f, 4f, 0.9f, 0.1f }, 0f),
                ["ChestLarge"] = BuildLootTable(database, "ChestLarge", common, 3, 6, new[] { 8f, 40f, 30f, 14f, 6f, 1.7f, 0.3f }, 0f),
                ["ChestOrnate"] = BuildLootTable(database, "ChestOrnate", ornate, 3, 6, new[] { 0f, 0f, 35f, 35f, 20f, 8f, 2f }, 0f),
                ["Coffin"] = BuildLootTable(database, "Coffin", coffin, 1, 3, new[] { 15f, 45f, 25f, 10f, 4f, 0.9f, 0.1f }, 0.15f),
                ["Barrel"] = BuildLootTable(database, "Barrel", barrel, 1, 2, new[] { 30f, 55f, 12f, 3f, 0f, 0f, 0f }, 0.3f),
                ["Bookshelf"] = BuildLootTable(database, "Bookshelf", bookshelf, 1, 3, new[] { 10f, 45f, 30f, 10f, 4f, 1f, 0f }, 0.2f),
                // Corpses are searched like chests: a common monster carries a thing or two, the boss a golden chest's worth.
                ["Monster"] = BuildLootTable(database, "Monster", monster, 1, 2, new[] { 25f, 55f, 15f, 4f, 1f, 0f, 0f }, 0.3f),
                ["Boss"] = BuildLootTable(database, "Boss", ornate, 3, 4, new[] { 0f, 0f, 30f, 35f, 22f, 10f, 3f }, 0f)
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
            ArmorPieceSetConfig pieceSet)
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
            BattleEditorUtility.Set(so, "_formWeapons", new[] { weapons[DungeonWeaponLibrary.BearClaws], weapons[DungeonWeaponLibrary.PantherClaws], weapons[DungeonWeaponLibrary.RatBite] });
            so.ApplyModifiedPropertiesWithoutUndo();

            Transform rightHand = parts.Animator.GetBoneTransform(HumanBodyBones.RightHand);
            Light torchLight = DungeonPropBuilder.PointLight(rightHand, new Vector3(0.14f, 0.08f, 0.5f), new Color(1f, 0.65f, 0.3f), 9f, 2.6f, false, true);
            torchLight.enabled = false;
            Transform leftHand = parts.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Light handGlow = DungeonPropBuilder.PointLight(leftHand, new Vector3(-0.1f, 0f, 0.02f), new Color(0.6f, 0.6f, 1f), 3f, 1.2f, false);
            handGlow.enabled = false;

            AdventurerVisualComponent visual = root.AddComponent<AdventurerVisualComponent>();
            so = new SerializedObject(visual);
            BattleEditorUtility.Set(so, "_adventurer", adventurer);
            BattleEditorUtility.Set(so, "_animator", parts.Animator);
            BattleEditorUtility.Set(so, "_model", parts.Model);
            BattleEditorUtility.Set(so, "_torchLight", torchLight);
            BattleEditorUtility.Set(so, "_handGlow", handGlow);
            BattleEditorUtility.Set(so, "_pieceSet", pieceSet);
            so.ApplyModifiedPropertiesWithoutUndo();
            BattleEditorUtility.Set(root.AddComponent<FootstepComponent>(), "_fighter", parts.Fighter);
            BattleEditorUtility.Set(root.AddComponent<AdventurerSoundComponent>(), "_adventurer", adventurer);
            BattleContentBuilder.SavePrefab(root, Prefab("Adventurer"));
        }

        public static GameObject BuildPreviewRig(ArmorPieceSetConfig pieceSet)
        {
            GameObject root = new GameObject("PreviewRig");
            int layer = LayerMask.NameToLayer(DungeonUiBuilder.PreviewLayer);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), root.transform);
            model.name = "Model";
            Animator animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(BattleEditorUtility.ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            BattlePoseRig.CreateSockets(animator);
            BattleEditorUtility.SetLayerRecursively(root, layer);

            return BattleContentBuilder.SavePrefab(root, Prefab("PreviewRig"));
        }

        private static ArmorPieceSetConfig BuildArmorPieceSet()
        {
            GameObject[] armorPieces = BuildArmorPieces();
            ArmorPieceSetConfig config = BattleEditorUtility.LoadOrCreate<ArmorPieceSetConfig>($"{ConfigsFolder}/ArmorPieces.asset");
            SerializedObject so = new SerializedObject(config);
            SerializedProperty entries = so.FindProperty("_entries");
            (ArmorVisual visual, int prefab, HumanBodyBones bone, bool mirrored)[] mapping = ArmorMapping();
            entries.arraySize = mapping.Length;

            for (int i = 0; i < mapping.Length; i++)
            {
                SerializedProperty element = entries.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Visual").intValue = (int)mapping[i].visual;
                element.FindPropertyRelative("Prefab").objectReferenceValue = armorPieces[mapping[i].prefab];
                element.FindPropertyRelative("Bone").intValue = (int)mapping[i].bone;
                element.FindPropertyRelative("IsMirrored").boolValue = mapping[i].mirrored;
            }

            SerializedProperty outfits = so.FindProperty("_outfits");
            outfits.arraySize = 0;

            foreach (ItemDef def in Outfits(DungeonItemLibrary.CreateItems()))
            {
                foreach (OutfitPart part in def.Parts)
                {
                    SerializedProperty element = outfits.GetArrayElementAtIndex(outfits.arraySize++);
                    element.FindPropertyRelative("Visual").intValue = (int)def.Visual;
                    element.FindPropertyRelative("Part").intValue = (int)part;
                    element.FindPropertyRelative("Material").objectReferenceValue = BattleCharacterBuilder.LoadMaterial(def.Material);
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }

        /// Clothes are skinned parts of the character model.
        private static IEnumerable<ItemDef> Outfits(List<ItemDef> defs)
        {
            return defs.Where(def => def.Parts != null);
        }

        /// Static T-pose figures of every outfit piece, used to render the item icons.
        private static Dictionary<ArmorVisual, GameObject> BuildOutfitModels(List<ItemDef> defs)
        {
            Dictionary<ArmorVisual, GameObject> models = new();

            foreach (ItemDef def in Outfits(defs))
            {
                GameObject root = BattleCharacterBuilder.CreateOutfitModel("Armor_" + def.Visual, BattleCharacterBuilder.LoadMaterial(def.Material), def.Parts);
                models[def.Visual] = PrefabUtility.SaveAsPrefabAsset(root, $"{ArmorFolder}/{root.name}.prefab");
                Object.DestroyImmediate(root);
            }

            return models;
        }

        private sealed class MonsterDef
        {
            public string Name;
            public string DisplayName;
            public int Health;
            public float Damage;
            public float MoveSpeed;
            public float ActionSpeed;
            public float Aggro;
            public bool IsRanged;
            public bool CanBlock;
            public int WeaponIndex;
            public int Experience;
            public LootTableConfig Loot;
            public Material Body;
            public float Scale;
            public bool IsBoss;
            public float Lunge;
            public float Charge;
            public DungeonSound Voice = DungeonSound.Rattle;
            public (ArmorVisual, Color)[] Attachments = System.Array.Empty<(ArmorVisual, Color)>();
        }

        private static (ArmorVisual, int, HumanBodyBones, bool)[] ArmorMapping()
        {
            return new[] { (ArmorVisual.Skull, 0, HumanBodyBones.Head, false), (ArmorVisual.Ribcage, 1, HumanBodyBones.Spine, false) };
        }

        /// What shows a monster's bones through its body, parented to bones: index order matches ArmorMapping.
        private static GameObject[] BuildArmorPieces()
        {
            Material dark = BattleEditorUtility.GetMaterial("ArmorDark", new Color(0.12f, 0.1f, 0.08f), 0.1f, 0.3f);
            const PrimitiveType sphere = PrimitiveType.Sphere;
            const PrimitiveType cube = PrimitiveType.Cube;

            return new[]
            {
                Composite("Armor_Skull", root =>
                {
                    Part(root, sphere, new Vector3(0.045f, 0.1f, 0.1f), new Vector3(0.06f, 0.05f, 0.04f), dark, default, true);
                    Part(root, sphere, new Vector3(-0.045f, 0.1f, 0.1f), new Vector3(0.06f, 0.05f, 0.04f), dark, default, true);
                    Part(root, cube, new Vector3(0f, 0.0f, 0.09f), new Vector3(0.1f, 0.03f, 0.05f), dark, default, true);
                    Part(root, cube, new Vector3(0f, 0.045f, 0.1f), new Vector3(0.08f, 0.01f, 0.03f), dark, default, true);
                }),
                Composite("Armor_Ribcage", root =>
                {
                    for (int i = 0; i < 4; i++)
                        Part(root, cube, new Vector3(0f, 0.28f - i * 0.055f, 0.14f), new Vector3(0.3f - i * 0.02f, 0.015f, 0.03f), dark, default, true);

                    Part(root, cube, new Vector3(0f, 0.2f, 0.15f), new Vector3(0.03f, 0.3f, 0.02f), dark, default, true);
                })
            };
        }

        private static GameObject Composite(string name, System.Action<Transform> build)
        {
            GameObject root = new GameObject(name);
            build(root.transform);

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{ArmorFolder}/{name}.prefab");
            Object.DestroyImmediate(root);

            return prefab;
        }

        private static void Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Vector3 euler = default, bool isFixed = false)
        {
            BattleEditorUtility.CreatePrimitive(type, isFixed ? "Fixed" : "Part", parent, position, euler, scale, material);
        }

        private static MonsterConfig BuildMonsterConfig(MonsterDef def)
        {
            MonsterConfig config = BattleEditorUtility.LoadOrCreate<MonsterConfig>($"{MonstersFolder}/{def.Name}.asset");
            SerializedObject so = new SerializedObject(config);
            BattleEditorUtility.Set(so, "_displayName", def.DisplayName);
            BattleEditorUtility.Set(so, "_maxHealth", def.Health);
            BattleEditorUtility.Set(so, "_damageMultiplier", def.Damage);
            BattleEditorUtility.Set(so, "_moveSpeed", def.MoveSpeed);
            BattleEditorUtility.Set(so, "_actionSpeed", def.ActionSpeed);
            BattleEditorUtility.Set(so, "_aggroRange", def.Aggro);
            BattleEditorUtility.Set(so, "_leashRange", def.IsBoss ? 16f : 24f);
            BattleEditorUtility.Set(so, "_armorReduction", def.IsBoss ? 0.1f : -0.22f);
            BattleEditorUtility.Set(so, "_magicReduction", def.IsBoss ? 0.1f : -0.17f);
            BattleEditorUtility.Set(so, "_weaponIndex", def.WeaponIndex);
            BattleEditorUtility.Set(so, "_experience", def.Experience);
            BattleEditorUtility.Set(so, "_lootTable", def.Loot);
            BattleEditorUtility.Set(so, "_isRanged", def.IsRanged);
            BattleEditorUtility.Set(so, "_canBlock", def.CanBlock);
            BattleEditorUtility.Set(so, "_bodyMaterial", def.Body);
            BattleEditorUtility.Set(so, "_scale", def.Scale);
            BattleEditorUtility.Set(so, "_isBoss", def.IsBoss);
            BattleEditorUtility.Set(so, "_lungeImpulse", def.Lunge);
            BattleEditorUtility.Set(so, "_chargeSpeed", def.Charge);
            BattleEditorUtility.Set(so, "_attackPause", def.IsBoss ? new Vector2(0.4f, 0.9f) : new Vector2(0.7f, 1.6f));
            BattleEditorUtility.Set(so, "_voice", def.Voice);
            SerializedProperty attachments = so.FindProperty("_attachments");
            attachments.arraySize = def.Attachments.Length;

            for (int i = 0; i < def.Attachments.Length; i++)
            {
                attachments.GetArrayElementAtIndex(i).FindPropertyRelative("Visual").intValue = (int)def.Attachments[i].Item1;
                attachments.GetArrayElementAtIndex(i).FindPropertyRelative("Color").colorValue = def.Attachments[i].Item2;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }

        private static void BuildMonster(MonsterDef def, BattleContentBuilder.Loadout[] loadouts, GameObject arrow, GameObject orb, ItemDatabase database,
            ArmorPieceSetConfig pieceSet)
        {
            MonsterConfig config = BuildMonsterConfig(def);
            BattleContentBuilder.FighterParts parts = BattleContentBuilder.CreateFighter(loadouts, arrow, orb, 1, def.Name);
            GameObject root = parts.Root;
            BattleEditorUtility.Set(parts.Fighter, "_respawnDelay", 0f);
            parts.Model.SetBodyMaterial(def.Body);
            parts.Animator.transform.localScale = Vector3.one * def.Scale;

            root.GetComponent<CharacterController>().radius = 0.3f * def.Scale;
            root.GetComponent<CharacterController>().height = 1.85f * def.Scale;
            root.GetComponent<CharacterController>().center = new Vector3(0f, 0.925f * def.Scale, 0f);
            parts.Move.Blocker.radius *= def.Scale;
            parts.Move.Blocker.height *= def.Scale;
            parts.Move.Blocker.center *= def.Scale;

            // Block boxes ride the scaled skeleton; Fusion hitboxes ignore transform scale, so their extents follow by hand.
            foreach (ZoneHitbox hitbox in root.GetComponentsInChildren<ZoneHitbox>(true))
            {
                if (hitbox.Zone == HitZone.Block)
                    hitbox.BoxExtents *= def.Scale;
            }

            AddMonsterLogic(parts, def, config, database, pieceSet, new Vector3(0f, 0.4f, -0.3f) * def.Scale, new Vector3(1.6f, 0.8f, 3f) * def.Scale);
            BattleEditorUtility.Set(root.AddComponent<FootstepComponent>(), "_fighter", parts.Fighter);
            BattleContentBuilder.SavePrefab(root, Prefab(def.Name));
        }

        /// A head without a body: the fighter stack keeps its simulation, the humanoid model and its views go away.
        private static void BuildFlyingHead(MonsterDef def, GameObject arrow, GameObject orb, ItemDatabase database)
        {
            const float height = 1.55f;
            const float scale = 1.7f;
            MonsterConfig config = BuildMonsterConfig(def);
            BattleContentBuilder.FighterParts parts = BattleContentBuilder.CreateFighter(new[] { BuildRam(height) }, arrow, orb, 1, def.Name);
            GameObject root = parts.Root;
            BattleEditorUtility.Set(parts.Fighter, "_respawnDelay", 0f);
            Object.DestroyImmediate(root.GetComponent<FighterAnimComponent>());
            Object.DestroyImmediate(root.GetComponent<WeaponViewComponent>());
            Object.DestroyImmediate(parts.Animator.gameObject);
            parts.Animator = null;
            root.GetComponent<CharacterController>().radius = 0.25f;
            parts.Move.Blocker.radius = 0.35f;

            ZoneHitbox skull = null;

            foreach (ZoneHitbox hitbox in root.GetComponentsInChildren<ZoneHitbox>(true))
            {
                if (hitbox.Zone == HitZone.Head)
                    skull = hitbox;
                else
                    Object.DestroyImmediate(hitbox.gameObject);
            }

            skull.transform.localPosition = new Vector3(0f, height, 0f);
            skull.SphereRadius = 0.16f * scale;
            BattleEditorUtility.Set(skull, "_zone", HitZone.Torso);
            parts.HitboxRoot.InitHitboxes();
            SerializedObject so = new SerializedObject(parts.Body);
            BattleEditorUtility.Set(so, "_upperHitboxes", new[] { skull.transform });
            BattleEditorUtility.Set(so, "_lowerHitboxes", new Transform[0]);
            so.ApplyModifiedPropertiesWithoutUndo();

            Transform head = BattleEditorUtility.CreateChild("Head", root.transform, new Vector3(0f, height, 0f)).transform;
            Transform model = BattleEditorUtility.CreateChild("Model", head).transform;
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(BattleCharacterBuilder.HeadMeshPath);
            Vector3 center = mesh.bounds.center + Vector3.up * mesh.bounds.extents.y * 0.25f;
            model.localScale = Vector3.one * scale;
            model.localPosition = -center * scale;
            DungeonPropBuilder.MeshObject("Skull", model, mesh, DungeonPropBuilder.Bone, default, default, false, false)
                .GetComponent<MeshRenderer>().sharedMaterials = new[] { DungeonPropBuilder.Bone, DungeonPropBuilder.Bone };

            Material dark = BattleEditorUtility.GetMaterial("ArmorDark", new Color(0.12f, 0.1f, 0.08f), 0.1f, 0.3f);
            Material eye = DungeonPropBuilder.Emissive("HeadEye", new Color(0.45f, 1f, 0.6f), 6f);
            Vector3 face = new Vector3(0f, mesh.bounds.center.y + mesh.bounds.extents.y * 0.3f, mesh.bounds.max.z);

            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 socket = face + new Vector3(side * 0.034f, 0.012f, -0.022f);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Socket", model, socket, Vector3.zero, new Vector3(0.046f, 0.04f, 0.03f), dark);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Eye", model, socket + Vector3.forward * 0.01f, Vector3.zero, Vector3.one * 0.02f, eye);
            }

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Nose", model, face + new Vector3(0f, -0.03f, -0.012f), Vector3.zero, new Vector3(0.018f, 0.026f, 0.02f), dark);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Mouth", model, face + new Vector3(0f, -0.075f, -0.024f), Vector3.zero, new Vector3(0.07f, 0.03f, 0.04f), dark);
            BattleEditorUtility.SetLayerRecursively(head.gameObject, root.layer);
            Light glow = DungeonPropBuilder.PointLight(head, new Vector3(0f, -0.05f, 0.6f), new Color(0.45f, 1f, 0.6f), 3.5f, 0.5f, false);

            FlyingHeadVisualComponent visual = root.AddComponent<FlyingHeadVisualComponent>();
            so = new SerializedObject(visual);
            BattleEditorUtility.Set(so, "_monster", AddMonsterLogic(parts, def, config, database, null, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.6f, 0.9f)));
            BattleEditorUtility.Set(so, "_head", head);
            BattleEditorUtility.Set(so, "_glow", glow);
            BattleEditorUtility.Set(so, "_hoverHeight", height);
            so.ApplyModifiedPropertiesWithoutUndo();
            BattleContentBuilder.SavePrefab(root, Prefab(def.Name));
        }

        /// The ram is not a swing of an animated weapon: its trace is a short ray ahead of the head, authored here.
        private static BattleContentBuilder.Loadout BuildRam(float height)
        {
            const string attack = "_attacks.Array.data[0].";
            Vector3 from = new Vector3(0f, height, 0.1f);
            Vector3 to = new Vector3(0f, height, 0.8f);
            WeaponConfig config = BattleEditorUtility.LoadOrCreate<WeaponConfig>($"{ConfigsFolder}/HeadRam.asset");
            SerializedObject so = new SerializedObject(config);
            BattleEditorUtility.Set(so, "_displayName", "Ram");
            BattleEditorUtility.Set(so, "_animationPrefix", "HeadRam");
            BattleEditorUtility.Set(so, "_deflectDuration", 1.2f);
            BattleEditorUtility.Set(so, "_reach", 6f);
            BattleEditorUtility.Set(so, "_impact", 6);
            so.FindProperty("_attacks").arraySize = 1;
            BattleEditorUtility.Set(so, attack + "_windupTime", 0.7f);
            BattleEditorUtility.Set(so, attack + "_activeTime", 0.4f);
            BattleEditorUtility.Set(so, attack + "_recoveryTime", 0.8f);
            BattleEditorUtility.Set(so, attack + "_comboWindowStart", 10f);
            BattleEditorUtility.Set(so, attack + "_comboWindowEnd", 10f);
            BattleEditorUtility.Set(so, attack + "_damage", 24);
            BattleEditorUtility.Set(so, attack + "_moveMultiplier", 1f);
            BattleEditorUtility.Set(so, attack + "_staggerDuration", 0.3f);
            BattleEditorUtility.Set(so, attack + "_traceSampleRate", BattleAnimationBuilder.FrameRate);
            BattleEditorUtility.Set(so, attack + "_traceBase", new[] { from, from });
            BattleEditorUtility.Set(so, attack + "_traceTip", new[] { to, to });
            BattleEditorUtility.Set(so, "_block._canBlock", false);
            so.ApplyModifiedPropertiesWithoutUndo();

            return new BattleContentBuilder.Loadout { Name = "HeadRam", Config = config };
        }

        /// The dead body is the loot container: its trigger covers what is left on the floor and wakes up on death.
        private static MonsterComponent AddMonsterLogic(BattleContentBuilder.FighterParts parts, MonsterDef def, MonsterConfig config, ItemDatabase database,
            ArmorPieceSetConfig pieceSet, Vector3 triggerCenter, Vector3 triggerSize)
        {
            GameObject root = parts.Root;
            InventoryComponent loot = AddInventory(root, database, 6, 4, false, "Loot");
            BoxCollider trigger = AddInteractCollider(root, triggerCenter, triggerSize);
            trigger.enabled = false;
            ContainerComponent corpse = root.AddComponent<ContainerComponent>();
            SerializedObject so = new SerializedObject(corpse);
            BattleEditorUtility.Set(so, "_inventory", loot);
            BattleEditorUtility.Set(so, "_displayName", def.DisplayName);
            BattleEditorUtility.Set(so, "_openVerb", "Loot");
            BattleEditorUtility.Set(so, "_body", parts.Health);
            BattleEditorUtility.Set(so, "_trigger", trigger);
            so.ApplyModifiedPropertiesWithoutUndo();

            MonsterComponent monster = root.AddComponent<MonsterComponent>();
            so = new SerializedObject(monster);
            BattleEditorUtility.Set(so, "_config", config);
            BattleEditorUtility.Set(so, "_fighter", parts.Fighter);
            BattleEditorUtility.Set(so, "_corpse", corpse);
            so.ApplyModifiedPropertiesWithoutUndo();

            MonsterBrainComponent brain = root.AddComponent<MonsterBrainComponent>();
            so = new SerializedObject(brain);
            BattleEditorUtility.Set(so, "_fighter", parts.Fighter);
            BattleEditorUtility.Set(so, "_monster", monster);
            BattleEditorUtility.Set(so, "_sightMask", (LayerMask)1);
            BattleEditorUtility.Set(so, "_doorMask", (LayerMask)(1 << LayerMask.NameToLayer(InteractableLayer)));
            so.ApplyModifiedPropertiesWithoutUndo();

            MonsterVisualComponent visual = root.AddComponent<MonsterVisualComponent>();
            so = new SerializedObject(visual);
            BattleEditorUtility.Set(so, "_monster", monster);
            BattleEditorUtility.Set(so, "_animator", parts.Animator);
            BattleEditorUtility.Set(so, "_pieceSet", pieceSet);
            so.ApplyModifiedPropertiesWithoutUndo();

            return monster;
        }

        /// Decor made of the character packs: outfits on armour stands, stone statues and fallen adventurers.
        private static void BuildFigures()
        {
            OutfitPart[] peasant = { OutfitPart.PeasantBody, OutfitPart.PeasantArms, OutfitPart.PeasantLegs, OutfitPart.PeasantFeet };
            OutfitPart[] ranger =
            {
                OutfitPart.RangerBody, OutfitPart.RangerArms, OutfitPart.RangerBracers, OutfitPart.RangerBelt1, OutfitPart.RangerBelt2,
                OutfitPart.RangerPauldron, OutfitPart.RangerHood, OutfitPart.RangerLegs, OutfitPart.RangerBoots
            };
            OutfitPart[] mage = { OutfitPart.PeasantBody, OutfitPart.PeasantArms, OutfitPart.PeasantLegs, OutfitPart.PeasantFeet, OutfitPart.RangerHood };
            OutfitPart[] rags = { OutfitPart.PeasantBody, OutfitPart.PeasantLegs, OutfitPart.PeasantFeet };

            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.ArmorStand("StandPeasantMale", false, "Idle_Loop", 0f, peasant), "StandPeasantMale");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.ArmorStand("StandPeasantFemale", true, "Idle_Talking_Loop", 1.2f, peasant), "StandPeasantFemale");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.ArmorStand("StandRangerMale", false, "Sword_Idle", 0f, ranger), "StandRangerMale");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.ArmorStand("StandRangerFemale", true, "Idle_FoldArms_Loop", 1f, ranger), "StandRangerFemale");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Statue("StatueGuardian", false, "Idle_Shield_Loop", 0f, ranger), "StatueGuardian");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Statue("StatueMage", true, "Spell_Simple_Idle_Loop", 0f, mage), "StatueMage");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Statue("StatuePilgrim", false, "Idle_Lantern_Loop", 0f, mage), "StatuePilgrim");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Fallen("FallenPeasant", false, rags), "FallenPeasant");
            DungeonPropBuilder.SavePrefab(DungeonPropBuilder.Fallen("FallenRanger", true, ranger), "FallenRanger");
        }

        private static GameObject BuildCorpse(ItemDatabase database, ClassConfig[] classes, ArmorPieceSetConfig pieceSet)
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

            InventoryComponent inventory = AddInventory(root, database, 10, 4, true, "Inventory");
            ContainerComponent container = root.AddComponent<ContainerComponent>();
            SerializedObject so = new SerializedObject(container);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_displayName", "Dead Adventurer");
            BattleEditorUtility.Set(so, "_openVerb", "Loot");
            so.ApplyModifiedPropertiesWithoutUndo();
            AddInteractCollider(root, new Vector3(0f, 0.3f, -0.55f), new Vector3(1f, 0.6f, 2f));

            CorpseComponent corpse = root.AddComponent<CorpseComponent>();
            so = new SerializedObject(corpse);
            BattleEditorUtility.Set(so, "_container", container);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_animator", animator);
            BattleEditorUtility.Set(so, "_model", model.GetComponent<CharacterModelComponent>());
            BattleEditorUtility.Set(so, "_pieceSet", pieceSet);
            BattleEditorUtility.Set(so, "_classes", classes);
            so.ApplyModifiedPropertiesWithoutUndo();

            return BattleContentBuilder.SavePrefab(root, Prefab("Corpse"));
        }

        /// Swarm stages shrink to fixed shares of the floor radius, so they follow the map size.
        private static void BuildSwarmStages(DungeonConfig config)
        {
            (float start, float share)[] stages = { (150f, 0.82f), (330f, 0.48f), (510f, 0.22f), (660f, 0f) };
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("_swarmStages").arraySize = stages.Length;

            for (int i = 0; i < stages.Length; i++)
            {
                string path = $"_swarmStages.Array.data[{i}].";
                BattleEditorUtility.Set(so, path + "StartTime", stages[i].start);
                BattleEditorUtility.Set(so, path + "Duration", 60f);
                BattleEditorUtility.Set(so, path + "Radius", DungeonMapBuilder.FloorRadius * stages[i].share);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// Tavern merchants. The quartermaster is a developer stall: every item of the game in common quality for one coin.
        private static MerchantConfig[] BuildMerchants(ItemDatabase database)
        {
            MerchantConfig quartermaster = BattleEditorUtility.LoadOrCreate<MerchantConfig>($"{MerchantsFolder}/Quartermaster.asset");
            SerializedObject so = new SerializedObject(quartermaster);
            BattleEditorUtility.Set(so, "_displayName", "The Quartermaster");
            BattleEditorUtility.Set(so, "_description", "Developer stall: every item there is, in common quality, a single coin apiece.");
            BattleEditorUtility.Set(so, "_color", new Color(0.85f, 0.72f, 0.45f));
            BattleEditorUtility.Set(so, "_wares", database.Items);
            BattleEditorUtility.Set(so, "_rarity", ItemRarity.Common);
            BattleEditorUtility.Set(so, "_price", 1);
            so.ApplyModifiedPropertiesWithoutUndo();

            return new[] { quartermaster };
        }

        private static void BuildSession(ItemDatabase database, ClassConfig[] classes, DungeonConfig config, MerchantConfig[] merchants)
        {
            GameObject root = new GameObject("PlayerSession");
            root.AddComponent<NetworkObject>();
            InventoryComponent kit = AddInventory(root, database, 10, 4, true, "Kit");
            InventoryComponent[] stashes = new InventoryComponent[PlayerSessionComponent.StashPages];

            for (int i = 0; i < stashes.Length; i++)
                stashes[i] = AddInventory(root, database, 12, 5, false, i == 0 ? "Stash" : "Stash" + (i + 1));

            InventoryActionsComponent actions = root.AddComponent<InventoryActionsComponent>();
            BattleEditorUtility.Set(actions, "_inventory", kit);

            PlayerSessionComponent session = root.AddComponent<PlayerSessionComponent>();
            SerializedObject so = new SerializedObject(session);
            BattleEditorUtility.Set(so, "_kit", kit);
            BattleEditorUtility.Set(so, "_stashes", stashes);
            BattleEditorUtility.Set(so, "_actions", actions);
            BattleEditorUtility.Set(so, "_classes", classes);
            BattleEditorUtility.Set(so, "_config", config);
            BattleEditorUtility.Set(so, "_merchants", merchants);
            BattleEditorUtility.Set(so, "_currency", database.Find("Gold Coins"));
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
            Transform modelRoot = BattleEditorUtility.CreateChild("ModelRoot", root.transform, new Vector3(0f, 0.02f, 0f)).transform;
            SerializedObject so = new SerializedObject(item);
            BattleEditorUtility.Set(so, "_database", database);
            BattleEditorUtility.Set(so, "_renderer", root.GetComponentInChildren<MeshRenderer>());
            BattleEditorUtility.Set(so, "_modelRoot", modelRoot);
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

        private static void SetupContainer(GameObject root, string displayName, LootTableConfig table, Transform lid, Vector3 lidOpen, bool removeWhenEmpty, int width, int height)
        {
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
            InventoryComponent inventory = AddInventory(root, database, width, height, false, "Inventory");
            ContainerComponent container = root.AddComponent<ContainerComponent>();
            SerializedObject so = new SerializedObject(container);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_displayName", displayName);
            BattleEditorUtility.Set(so, "_lootTable", table);
            BattleEditorUtility.Set(so, "_lid", lid);
            BattleEditorUtility.Set(so, "_lidOpenEuler", lidOpen);
            BattleEditorUtility.Set(so, "_isRemovedWhenEmpty", removeWhenEmpty);
            so.ApplyModifiedPropertiesWithoutUndo();
            BattleEditorUtility.Set(root.AddComponent<InteractableSoundComponent>(), "_container", container);
        }

        private static void BuildContainer(string name, string displayName, LootTableConfig table, bool isGolden)
        {
            float width = name.StartsWith("Large") || isGolden ? 1.5f : 1.1f;
            GameObject root = DungeonPropBuilder.Chest(name, width, isGolden ? DungeonPropBuilder.Gold : null, out Transform lid);
            SetupContainer(root, displayName, table, lid, new Vector3(-110f, 0f, 0f), false, 6, 4);
            AddInteractCollider(root, new Vector3(0f, 0.4f, 0f), new Vector3(width + 0.2f, 0.9f, 0.9f));
            BattleContentBuilder.SavePrefab(root, Prefab(name));
        }

        private static void BuildCoffin(LootTableConfig table)
        {
            GameObject root = DungeonPropBuilder.Coffin(out Transform lid);
            SetupContainer(root, "Coffin", table, lid, new Vector3(0f, 0f, 75f), false, 5, 3);
            AddInteractCollider(root, new Vector3(0f, 0.4f, 0f), new Vector3(1.1f, 0.9f, 2.4f));
            BattleContentBuilder.SavePrefab(root, Prefab("Coffin"));
        }

        private static void BuildBarrel(LootTableConfig table)
        {
            GameObject root = DungeonPropBuilder.Barrel();
            SetupContainer(root, "Barrel", table, null, Vector3.zero, false, 4, 2);
            AddInteractCollider(root, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 1f, 0.9f));
            BattleContentBuilder.SavePrefab(root, Prefab("Barrel"));
        }

        private static void BuildCrate(LootTableConfig table)
        {
            GameObject root = DungeonPropBuilder.Crate();
            SetupContainer(root, "Crate", table, null, Vector3.zero, false, 4, 2);
            AddInteractCollider(root, new Vector3(0f, 0.4f, 0f), new Vector3(1f, 0.9f, 1f));
            BattleContentBuilder.SavePrefab(root, Prefab("Crate"));
        }

        private static void BuildBookshelf(LootTableConfig table)
        {
            GameObject root = DungeonPropBuilder.Bookshelf();
            SetupContainer(root, "Bookshelf", table, null, Vector3.zero, false, 5, 2);
            AddInteractCollider(root, new Vector3(0f, 1.1f, 0.1f), new Vector3(1.7f, 2.2f, 0.7f));
            BattleContentBuilder.SavePrefab(root, Prefab("Bookshelf"));
        }

        private static void BuildDoor(string name, GameObject leaf)
        {
            GameObject root = new GameObject(name);
            root.AddComponent<NetworkObject>();
            // A closed leaf would cut the NavMesh in two; monsters shove doors open on their way instead.
            root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            GameObject hinge = BattleEditorUtility.CreateChild("Hinge", root.transform, new Vector3(-1.05f, 0f, 0f));
            leaf.transform.SetParent(hinge.transform, false);
            leaf.isStatic = false;

            DoorComponent door = root.AddComponent<DoorComponent>();
            SerializedObject so = new SerializedObject(door);
            BattleEditorUtility.Set(so, "_leaf", hinge.transform);
            BattleEditorUtility.Set(so, "_blocker", leaf.GetComponent<MeshCollider>());
            so.ApplyModifiedPropertiesWithoutUndo();
            AddInteractCollider(root, new Vector3(0f, 1.5f, 0f), new Vector3(2.2f, 3f, 0.6f));
            BattleEditorUtility.Set(root.AddComponent<InteractableSoundComponent>(), "_door", door);
            BattleContentBuilder.SavePrefab(root, Prefab(name));
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
            AudioSource hum = root.AddComponent<AudioSource>();
            hum.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(DungeonAudioBuilder.Path("Portal"));
            hum.loop = true;
            hum.playOnAwake = false;
            hum.spatialBlend = 1f;
            hum.rolloffMode = AudioRolloffMode.Linear;
            hum.minDistance = 2f;
            hum.maxDistance = 18f;
            hum.volume = 0.5f;
            InteractableSoundComponent sound = root.AddComponent<InteractableSoundComponent>();
            so = new SerializedObject(sound);
            BattleEditorUtility.Set(so, "_portal", portal);
            BattleEditorUtility.Set(so, "_loop", hum);
            so.ApplyModifiedPropertiesWithoutUndo();
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
            BattleEditorUtility.Set(root.AddComponent<InteractableSoundComponent>(), "_lever", lever);
            BattleContentBuilder.SavePrefab(root, Prefab("Lever"));
        }

        private static void BuildCaltrops()
        {
            Material steel = DungeonPropBuilder.RustyMetal;
            GameObject root = new GameObject("Caltrops");
            System.Random random = new System.Random(5);

            for (int i = 0; i < 7; i++)
            {
                Vector3 position = new Vector3(((float)random.NextDouble() - 0.5f) * 1.6f, 0.08f, ((float)random.NextDouble() - 0.5f) * 1.6f);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Spike" + i, root.transform, position, new Vector3(45f, random.Next(0, 90), 45f), new Vector3(0.04f, 0.18f, 0.04f), steel);
            }

            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            TrapComponent trap = root.AddComponent<TrapComponent>();
            SerializedObject so = new SerializedObject(trap);
            BattleEditorUtility.Set(so, "_kind", TrapKind.Caltrops);
            BattleEditorUtility.Set(so, "_damage", 6);
            BattleEditorUtility.Set(so, "_period", 1f);
            BattleEditorUtility.Set(so, "_halfExtents", new Vector3(1f, 0.5f, 1f));
            BattleEditorUtility.Set(so, "_center", new Vector3(0f, 0.4f, 0f));
            BattleEditorUtility.Set(so, "_victimMask", (LayerMask)(1 << LayerMask.NameToLayer(BattleEditorUtility.CharacterLayer)));
            BattleEditorUtility.Set(so, "_lifetime", 30f);
            BattleEditorUtility.Set(so, "_slowMagnitude", 40f);
            BattleEditorUtility.Set(so, "_slowDuration", 2f);
            so.ApplyModifiedPropertiesWithoutUndo();
            BattleContentBuilder.SavePrefab(root, Prefab("Caltrops"));
        }

        private static void BuildSmokePot()
        {
            GameObject root = new GameObject("SmokePot");
            ParticleSystem smoke = BattleEditorUtility.CreateChild("Smoke", root.transform).AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.35f, 0.38f, 0.7f), new Color(0.2f, 0.2f, 0.22f, 0.6f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            ParticleSystem.EmissionModule emission = smoke.emission;
            emission.rateOverTime = 30f;
            ParticleSystem.ShapeModule shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2.5f;
            ParticleSystemRenderer renderer = smoke.GetComponent<ParticleSystemRenderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{DungeonTextureBuilder.Folder}/Flame.png"));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, $"{DungeonPropBuilder.MaterialsFolder}/Smoke.mat");
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            root.AddComponent<SmokeCloudComponent>();
            BattleContentBuilder.SavePrefab(root, Prefab("SmokePot"));
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
