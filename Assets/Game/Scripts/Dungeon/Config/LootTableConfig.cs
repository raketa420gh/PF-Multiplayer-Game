using System;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    [Serializable]
    public sealed class LootEntry
    {
        public ItemConfig Item => _item;
        public float Weight => _weight;
        public int MinCount => _minCount;
        public int MaxCount => _maxCount;

        [SerializeField]
        private ItemConfig _item;

        [SerializeField]
        private float _weight = 1f;

        [SerializeField]
        private int _minCount = 1;

        [SerializeField]
        private int _maxCount = 1;
    }

    /// Source -> rarity roll -> item roll, as on the wiki. Rarity weights shift towards colored loot for ornate chests.
    [CreateAssetMenu(menuName = "Game/Dungeon/Loot Table")]
    public sealed class LootTableConfig : ScriptableObject
    {
        public LootEntry[] Entries => _entries;

        [SerializeField]
        private LootEntry[] _entries = Array.Empty<LootEntry>();

        [SerializeField]
        private int _minRolls = 1;

        [SerializeField]
        private int _maxRolls = 3;

        [SerializeField]
        private float[] _rarityWeights = { 20f, 45f, 22f, 9f, 3f, 0.9f, 0.1f };

        [SerializeField]
        private float _emptyChance;

        /// Rolled loot lies unsearched: the adventurer who opens the container discovers it item by item.
        public void Roll(InventoryComponent inventory, int seed)
        {
            System.Random random = new System.Random(seed);

            if (random.NextDouble() < _emptyChance || _entries.Length == 0)
                return;

            int rolls = random.Next(_minRolls, _maxRolls + 1);

            for (int i = 0; i < rolls; i++)
            {
                LootEntry entry = Pick(random);

                if (entry == null || entry.Item == null)
                    continue;

                inventory.TryAdd(RollStack(entry, random).WithHidden(true));
            }
        }

        /// Weapons and armor get a seed for their random modifiers; stackable loot stays plain so it merges.
        public ItemStack RollStack(LootEntry entry, System.Random random)
        {
            ItemConfig item = entry.Item;
            int count = random.Next(entry.MinCount, entry.MaxCount + 1);
            ItemRarity rarity = item.CanRollRarity ? RollRarity(random) : item.BaseRarity;
            bool hasAffixes = item.CanRollRarity && item.Kind is ItemKind.Weapon or ItemKind.Armor;

            return ItemStack.Create(item, count, rarity, hasAffixes ? random.Next(1, ushort.MaxValue + 1) : 0);
        }

        public ItemRarity RollRarity(System.Random random)
        {
            float total = 0f;

            foreach (float weight in _rarityWeights)
                total += weight;

            float roll = (float)random.NextDouble() * total;

            for (int i = 0; i < _rarityWeights.Length; i++)
            {
                roll -= _rarityWeights[i];

                if (roll <= 0f)
                    return (ItemRarity)i;
            }

            return ItemRarity.Common;
        }

        private LootEntry Pick(System.Random random)
        {
            float total = 0f;

            foreach (LootEntry entry in _entries)
                total += entry.Weight;

            float roll = (float)random.NextDouble() * total;

            foreach (LootEntry entry in _entries)
            {
                roll -= entry.Weight;

                if (roll <= 0f)
                    return entry;
            }

            return _entries[^1];
        }
    }
}
