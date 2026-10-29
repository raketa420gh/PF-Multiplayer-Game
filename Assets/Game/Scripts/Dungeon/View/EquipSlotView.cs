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

        public void Setup(EquipSlot slot, string label)
        {
            _slot = slot;
            _label.text = label;
        }

        /// A two-handed weapon lies in the main hand; the off hand beside it shows the same weapon dimmed and out of reach.
        public void Bind(InventoryView owner, InventoryComponent inventory)
        {
            bool isBlocked = inventory.IsSlotBlocked(_slot);
            EquipSlot source = isBlocked ? _slot - 1 : _slot;
            ItemStack stack = inventory.GetEquipped(source);
            ItemConfig config = inventory.GetConfig(stack);
            bool hasItem = config != null;

            ((RectTransform)_item.transform).sizeDelta = Rect.sizeDelta;
            _item.gameObject.SetActive(hasItem);
            _item.SetGhost(isBlocked);
            _label.gameObject.SetActive(!hasItem);

            if (hasItem)
                _item.Bind(owner, inventory, stack, config, -1, source, 0f);

            _background.color = isBlocked ? new Color(0.25f, 0.1f, 0.1f, 0.8f) : new Color(0f, 0f, 0f, 0.6f);
        }

        public void SetHighlight(bool isValid, bool isActive)
        {
            _background.color = !isActive ? new Color(0f, 0f, 0f, 0.6f)
                : isValid ? new Color(0.3f, 0.7f, 0.3f, 0.7f) : new Color(0.8f, 0.2f, 0.2f, 0.7f);
        }
    }
}
