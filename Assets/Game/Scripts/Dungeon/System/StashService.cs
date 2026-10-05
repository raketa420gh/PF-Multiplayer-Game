using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Local persistence of the account's characters between runs (PlayerPrefs, prototype scope). Each of the SlotCount character slots
    /// keeps its own stash, kit, profile and build; the parameterless calls work on the slot picked on the character select screen.
    public static class StashService
    {
        public const int SlotCount = 3;

        private const string SlotKey = "dad.slot";
        private const string KitKey = "kit";
        private const string StashKey = "stash";
        private const string LevelKey = "level";
        private const string ExperienceKey = "xp";
        private const string ClassKey = "class";
        private const string NameKey = "name";
        private const string SkillAKey = "skillA";
        private const string SkillBKey = "skillB";
        private const string PerksKey = "perks";
        private const string SpellsKey = "spells";
        private const string ItemsKey = "dad.items";
        /// Version of the item list the saves refer to, see Migrate.
        private const int ItemsVersion = 2;

        public static int Slot => s_slot;

        private static readonly byte[] s_buffer = new byte[(InventoryComponent.Capacity + InventoryComponent.EquipmentCapacity) * ItemStack.ByteSize + 2];
        private static int s_slot = Mathf.Clamp(PlayerPrefs.GetInt(SlotKey, 0), 0, SlotCount - 1);

        static StashService()
        {
            Migrate();
        }

        public static bool HasCharacter(int slot) => PlayerPrefs.HasKey(Key(slot, ClassKey));
        public static byte LoadClass(int slot) => (byte)PlayerPrefs.GetInt(Key(slot, ClassKey), 0);
        public static int LoadLevel(int slot) => PlayerPrefs.GetInt(Key(slot, LevelKey), 1);

        public static byte[] LoadKit() => Load(Key(s_slot, KitKey));
        public static byte[] LoadStash(int page) => Load(StashPageKey(page));
        public static int LoadLevel() => LoadLevel(s_slot);
        public static int LoadExperience() => PlayerPrefs.GetInt(Key(s_slot, ExperienceKey), 0);
        public static byte LoadClass() => LoadClass(s_slot);
        public static string LoadName() => PlayerPrefs.GetString(Key(s_slot, NameKey), string.Empty);
        public static byte LoadSkillA() => (byte)PlayerPrefs.GetInt(Key(s_slot, SkillAKey), 0);
        public static byte LoadSkillB() => (byte)PlayerPrefs.GetInt(Key(s_slot, SkillBKey), 1);
        public static int LoadPerkMask() => PlayerPrefs.GetInt(Key(s_slot, PerksKey), 1);
        public static int LoadSpellMask() => PlayerPrefs.GetInt(Key(s_slot, SpellsKey), ClassConfig.DefaultSpellMask);

        public static void SelectSlot(int slot)
        {
            s_slot = slot;
            PlayerPrefs.SetInt(SlotKey, slot);
            PlayerPrefs.Save();
        }

        /// A new character is only its class; the kit, stash and build start from the class defaults on the first tavern visit.
        public static void CreateCharacter(int slot, byte classId)
        {
            DeleteCharacter(slot);
            PlayerPrefs.SetInt(Key(slot, ClassKey), classId);
            PlayerPrefs.SetInt(Key(slot, LevelKey), 1);
            PlayerPrefs.Save();
        }

        public static void DeleteCharacter(int slot)
        {
            foreach (string name in new[] { KitKey, LevelKey, ExperienceKey, ClassKey, NameKey, SkillAKey, SkillBKey, PerksKey, SpellsKey })
                PlayerPrefs.DeleteKey(Key(slot, name));

            for (int page = 0; page < PlayerSessionComponent.StashPages; page++)
                PlayerPrefs.DeleteKey(StashPageKey(slot, page));

            PlayerPrefs.Save();
        }

        public static void SaveBuild(int skillA, int skillB, int perkMask, int spellMask)
        {
            PlayerPrefs.SetInt(Key(s_slot, SkillAKey), skillA);
            PlayerPrefs.SetInt(Key(s_slot, SkillBKey), skillB);
            PlayerPrefs.SetInt(Key(s_slot, PerksKey), perkMask);
            PlayerPrefs.SetInt(Key(s_slot, SpellsKey), spellMask);
        }

        public static void SaveKit(InventoryComponent inventory) => Save(Key(s_slot, KitKey), inventory);
        public static void SaveStash(int page, InventoryComponent inventory) => Save(StashPageKey(page), inventory);

        public static void SaveProfile(int level, int experience, int classId, string name)
        {
            PlayerPrefs.SetInt(Key(s_slot, LevelKey), level);
            PlayerPrefs.SetInt(Key(s_slot, ExperienceKey), experience);
            PlayerPrefs.SetInt(Key(s_slot, ClassKey), classId);
            PlayerPrefs.SetString(Key(s_slot, NameKey), name);
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key(s_slot, KitKey));
            for (int page = 0; page < PlayerSessionComponent.StashPages; page++)
                PlayerPrefs.DeleteKey(StashPageKey(page));

            PlayerPrefs.DeleteKey(Key(s_slot, LevelKey));
            PlayerPrefs.DeleteKey(Key(s_slot, ExperienceKey));
        }

        /// Layout: [bagCount][equipmentCount][items...]
        public static byte[] Serialize(InventoryComponent inventory)
        {
            inventory.Serialize(s_buffer, out int bagCount, out int equipmentCount);
            int length = (bagCount + equipmentCount) * ItemStack.ByteSize;
            byte[] data = new byte[length + 2];
            data[0] = (byte)bagCount;
            data[1] = (byte)equipmentCount;
            Array.Copy(s_buffer, 0, data, 2, length);

            return data;
        }

        public static void Deserialize(InventoryComponent inventory, byte[] data, int length)
        {
            if (data == null || length < 2)
            {
                inventory.Clear();

                return;
            }

            // The exact payload size tells the item format apart, see InventoryComponent.Deserialize.
            byte[] items = new byte[length - 2];
            Array.Copy(data, 2, items, 0, items.Length);
            inventory.Deserialize(items, data[0], data[1]);
        }

        /// The first slot keeps the keys of the single character the account used to have, so old saves load into it.
        private static string Key(int slot, string name)
        {
            return slot == 0 ? "dad." + name : $"dad{slot + 1}.{name}";
        }

        private static string StashPageKey(int page) => StashPageKey(s_slot, page);

        /// The first page keeps the key of the single stash it used to be, so old saves load into it.
        private static string StashPageKey(int slot, int page)
        {
            return Key(slot, page == 0 ? StashKey : StashKey + (page + 1));
        }

        /// Item ids are positions in the item list. Version 2 cut the old clothes out of it and everything behind them moved up:
        /// older saves get their ids shifted and lose the pieces that no longer exist.
        private static void Migrate()
        {
            if (PlayerPrefs.GetInt(ItemsKey, 1) >= ItemsVersion)
                return;

            for (int slot = 0; slot < SlotCount; slot++)
            {
                Migrate(Key(slot, KitKey));

                for (int page = 0; page < PlayerSessionComponent.StashPages; page++)
                    Migrate(StashPageKey(slot, page));
            }

            PlayerPrefs.SetInt(ItemsKey, ItemsVersion);
            PlayerPrefs.Save();
        }

        private static void Migrate(string key)
        {
            byte[] data = Load(key);
            int count = data.Length < 2 ? 0 : data[0] + data[1];

            if (count == 0)
                return;

            int size = (data.Length - 2) / count;
            List<byte> result = new() { 0, 0 };

            for (int i = 0; i < count; i++)
            {
                int offset = 2 + i * size;
                int id = MigrateId(data[offset] | (data[offset + 1] << 8));

                if (id == 0)
                    continue;

                data[offset] = (byte)id;
                data[offset + 1] = (byte)(id >> 8);
                result.AddRange(new ArraySegment<byte>(data, offset, size));
                result[i < data[0] ? 0 : 1]++;
            }

            PlayerPrefs.SetString(key, Convert.ToBase64String(result.ToArray()));
        }

        /// The removed clothes sat in three runs of the old list: 17-36, 59 and 72-97.
        private static int MigrateId(int id)
        {
            return id switch
            {
                <= 16 => id,
                <= 36 => 0,
                <= 58 => id - 20,
                59 => 0,
                <= 71 => id - 21,
                <= 97 => 0,
                _ => id - 47
            };
        }

        private static void Save(string key, InventoryComponent inventory)
        {
            PlayerPrefs.SetString(key, Convert.ToBase64String(Serialize(inventory)));
            PlayerPrefs.Save();
        }

        private static byte[] Load(string key)
        {
            string text = PlayerPrefs.GetString(key, string.Empty);

            if (string.IsNullOrEmpty(text))
                return Array.Empty<byte>();

            try
            {
                return Convert.FromBase64String(text);
            }
            catch (FormatException)
            {
                return Array.Empty<byte>();
            }
        }
    }
}
