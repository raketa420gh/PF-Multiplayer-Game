using System;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    [CreateAssetMenu(menuName = "Game/Dungeon/Item Database")]
    public sealed class ItemDatabase : ScriptableObject
    {
        public ItemConfig[] Items => _items;

        [SerializeField]
        private ItemConfig[] _items = Array.Empty<ItemConfig>();

        [SerializeField]
        private Color[] _rarityColors =
        {
            new(0.55f, 0.55f, 0.55f), new(0.85f, 0.85f, 0.85f), new(0.35f, 0.75f, 0.35f), new(0.3f, 0.5f, 0.95f),
            new(0.65f, 0.35f, 0.9f), new(0.95f, 0.65f, 0.2f), new(0.95f, 0.35f, 0.25f)
        };

        public ItemConfig Get(int id)
        {
            return id > 0 && id <= _items.Length ? _items[id - 1] : null;
        }

        public T Get<T>(int id) where T : ItemConfig
        {
            return Get(id) as T;
        }

        public ItemConfig Find(string displayName)
        {
            foreach (ItemConfig item in _items)
            {
                if (item.DisplayName == displayName)
                    return item;
            }

            return null;
        }

        public Color GetRarityColor(ItemRarity rarity)
        {
            return _rarityColors[Mathf.Clamp((int)rarity, 0, _rarityColors.Length - 1)];
        }
    }
}
