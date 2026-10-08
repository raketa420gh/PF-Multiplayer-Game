using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    public sealed class EquipSlotView : MonoBehaviour
    {
        public EquipSlot Slot => _slot;
        public RectTransform Rect => (RectTransform)transform;
        public ItemView Item => _item;

        [SerializeField]
        private EquipSlot _slot;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private TMP_Text _label;

        [SerializeField]
        private ItemView _item;

        /// The off-hand slot beside a main-hand one: a two-handed weapon in this slot spans both.
        [SerializeField]
        private EquipSlotView _pair;

        public void Setup(EquipSlot slot, string label)
        {
            _slot = slot;
            _label.text = label;
        }

        public void Bind(InventoryView owner, InventoryComponent inventory)
        {
            ItemStack stack = inventory.GetEquipped(_slot);
            ItemConfig config = inventory.GetConfig(stack);
            bool hasItem = config != null;

            bool isSpanned = _pair != null && inventory.IsSlotBlocked(_pair.Slot);
            float width = isSpanned ? _pair.Rect.anchoredPosition.x + _pair.Rect.sizeDelta.x - Rect.anchoredPosition.x : Rect.sizeDelta.x;
            ((RectTransform)_item.transform).sizeDelta = new Vector2(width, Rect.sizeDelta.y);
            gameObject.SetActive(!inventory.IsSlotBlocked(_slot));
            _item.gameObject.SetActive(hasItem);
            _label.gameObject.SetActive(!hasItem);

            if (hasItem)
                _item.Bind(owner, inventory, stack, config, -1, _slot, 0f);

            _background.color = inventory.IsSlotBlocked(_slot) ? new Color(0.25f, 0.1f, 0.1f, 0.8f) : new Color(0f, 0f, 0f, 0.6f);
        }

        public void SetHighlight(bool isValid, bool isActive)
        {
            _background.color = !isActive ? new Color(0f, 0f, 0f, 0.6f)
                : isValid ? new Color(0.3f, 0.7f, 0.3f, 0.7f) : new Color(0.8f, 0.2f, 0.2f, 0.7f);
        }
    }
}
