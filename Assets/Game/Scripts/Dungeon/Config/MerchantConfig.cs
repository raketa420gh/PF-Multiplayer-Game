using System;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A tavern merchant: every ware costs the same number of coins and comes in one rarity.
    [CreateAssetMenu(menuName = "Game/Dungeon/Merchant Config")]
    public sealed class MerchantConfig : ScriptableObject
    {
        public string DisplayName => _displayName;
        public string Description => _description;
        public Color Color => _color;
        public ItemConfig[] Wares => _wares;
        public ItemRarity Rarity => _rarity;
        public int Price => _price;

        [SerializeField]
        private string _displayName;

        [SerializeField, TextArea]
        private string _description;

        [SerializeField]
        private Color _color = Color.white;

        [SerializeField]
        private ItemConfig[] _wares = Array.Empty<ItemConfig>();

        [SerializeField]
        private ItemRarity _rarity = ItemRarity.Common;

        [SerializeField, Tooltip("Coins per purchase; stackable wares are sold as a full stack")]
        private int _price = 1;

        public bool Sells(ItemConfig item)
        {
            return Array.IndexOf(_wares, item) >= 0;
        }
    }
}
