using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// One item inside a grid or an equipment slot. Drag it onto grids and slots; right click for the quick action.
    public sealed class ItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        public ItemStack Stack => _stack;
        public ItemConfig Config => _config;
        public InventoryComponent Inventory => _inventory;
        public int BagIndex => _bagIndex;
        public EquipSlot Slot => _slot;
        public bool IsEquipped => _bagIndex < 0;
        public Vector2Int GrabCell => _grabCell;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private Image _frame;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _glyph;

        [SerializeField]
        private TMP_Text _count;

        [SerializeField]
        private CanvasGroup _group;

        private ItemStack _stack;
        private ItemConfig _config;
        private InventoryComponent _inventory;
        private int _bagIndex;
        private EquipSlot _slot;
        private float _cellSize;
        private Vector2Int _grabCell;
        private bool _isSplitting;
        private InventoryView _owner;

        public bool IsSplitting => _isSplitting;

        public void Bind(InventoryView owner, InventoryComponent inventory, ItemStack stack, ItemConfig config, int bagIndex, EquipSlot slot, float cellSize)
        {
            _owner = owner;
            _inventory = inventory;
            _stack = stack;
            _config = config;
            _bagIndex = bagIndex;
            _slot = slot;
            _cellSize = cellSize;

            RectTransform rect = (RectTransform)transform;
            bool isSlot = bagIndex < 0;
            rect.sizeDelta = isSlot ? rect.sizeDelta : new Vector2(config.Width * cellSize, config.Height * cellSize);

            if (!isSlot)
                rect.anchoredPosition = new Vector2(stack.X * cellSize, -stack.Y * cellSize);

            bool hasIcon = config.Icon != null;
            _background.color = hasIcon ? new Color(0.09f, 0.08f, 0.07f, 0.95f) : config.IconColor * new Color(0.45f, 0.45f, 0.45f, 1f);
            _frame.color = owner.Database.GetRarityColor(stack.RarityValue);
            _icon.enabled = hasIcon;
            _icon.sprite = config.Icon;
            _glyph.text = hasIcon ? string.Empty : config.IconGlyph;
            _glyph.color = config.IconColor;
            _count.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _isSplitting = Input.GetKey(KeyCode.LeftControl) && _stack.Count > 1;

            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 local);
            RectTransform rect = (RectTransform)transform;
            Vector2 topLeft = local + new Vector2(rect.pivot.x * rect.sizeDelta.x, (1f - rect.pivot.y) * rect.sizeDelta.y - rect.sizeDelta.y);
            _grabCell = IsEquipped ? Vector2Int.zero : new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt((local.x + rect.pivot.x * rect.sizeDelta.x) / _cellSize), 0, _config.Width - 1),
                Mathf.Clamp(Mathf.FloorToInt((rect.sizeDelta.y * (1f - rect.pivot.y) - local.y) / _cellSize), 0, _config.Height - 1));
            _group.alpha = 0.4f;
            _owner.BeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _owner.Drag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _group.alpha = 1f;
            _owner.EndDrag(this, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging)
                return;

            if (eventData.button == PointerEventData.InputButton.Right)
                _owner.QuickAction(this);
            else if (eventData.button == PointerEventData.InputButton.Left && Input.GetKey(KeyCode.LeftShift))
                _owner.QuickTransfer(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _owner.ShowTooltip(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _owner.HideTooltip();
        }
    }
}
