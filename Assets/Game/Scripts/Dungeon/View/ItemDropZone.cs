using System;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A spot of the inventory screen that takes dragged items (a stash page tab, the sell counter); its owner decides what happens.
    public sealed class ItemDropZone : MonoBehaviour
    {
        public event Action<ItemView> OnPreview;
        public event Action<ItemView> OnDropped;

        /// The item dragged over the zone, null once it has left.
        public void Preview(ItemView item)
        {
            OnPreview?.Invoke(item);
        }

        public void Drop(ItemView item)
        {
            OnDropped?.Invoke(item);
        }
    }
}
