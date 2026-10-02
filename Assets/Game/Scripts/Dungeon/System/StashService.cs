using System;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Local persistence of the player's stash, kit and profile between runs (PlayerPrefs, prototype scope).
    public static class StashService
    {
        private const string KitKey = "dad.kit";
        private const string StashKey = "dad.stash";
        private const string LevelKey = "dad.level";
        private const string ExperienceKey = "dad.xp";
        private const string ClassKey = "dad.class";
        private const string NameKey = "dad.name";

        private static readonly byte[] s_buffer = new byte[(InventoryComponent.Capacity + InventoryComponent.EquipmentCapacity) * ItemStack.ByteSize + 2];

        public static byte[] LoadKit() => Load(KitKey);
        public static byte[] LoadStash() => Load(StashKey);
        public static int LoadLevel() => PlayerPrefs.GetInt(LevelKey, 1);
        public static int LoadExperience() => PlayerPrefs.GetInt(ExperienceKey, 0);
        public static byte LoadClass() => (byte)PlayerPrefs.GetInt(ClassKey, 0);
        public static string LoadName() => PlayerPrefs.GetString(NameKey, string.Empty);

        public static void SaveKit(InventoryComponent inventory) => Save(KitKey, inventory);
        public static void SaveStash(InventoryComponent inventory) => Save(StashKey, inventory);

        public static void SaveProfile(int level, int experience, int classId, string name)
        {
            PlayerPrefs.SetInt(LevelKey, level);
            PlayerPrefs.SetInt(ExperienceKey, experience);
            PlayerPrefs.SetInt(ClassKey, classId);
            PlayerPrefs.SetString(NameKey, name);
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(KitKey);
            PlayerPrefs.DeleteKey(StashKey);
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.DeleteKey(ExperienceKey);
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

            byte[] items = new byte[length - 2];
            Array.Copy(data, 2, items, 0, items.Length);
            inventory.Deserialize(items, data[0], data[1]);
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
