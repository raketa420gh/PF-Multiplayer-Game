using Game.Scripts.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// One item inside a grid or an equipment slot. Drag it onto grids and slots; right click for the quick action.
    /// Unsearched loot shows an eye instead of the item and a ring that fills while it is being discovered.
    public sealed class ItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        public ItemStack Stack => _stack;
        public ItemConfig Config => _config;
        public InventoryComponent Inventory => _inventory;
        public int BagIndex => _bagIndex;
        public EquipSlot Slot => _slot;
        public bool IsEquipped => _bagIndex < 0;
        /// Where the pointer took the item, 0..1 from its top-left corner.
        public Vector2 GrabPoint => _grabPoint;

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

        [SerializeField]
        private GameObject _hiddenRoot;

        [SerializeField]
        private Image _searchFill;

        private static readonly Color s_hiddenFrame = new(0.35f, 0.31f, 0.25f);
        private ItemStack _stack;
        private ItemConfig _config;
        private InventoryComponent _inventory;
        private int _bagIndex;
        private EquipSlot _slot;
        private Vector2 _grabPoint;
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

            RectTransform rect = (RectTransform)transform;
            bool isSlot = bagIndex < 0;
            rect.sizeDelta = isSlot ? rect.sizeDelta : new Vector2(config.Width * cellSize, config.Height * cellSize);

            if (!isSlot)
                rect.anchoredPosition = new Vector2(stack.X * cellSize, -stack.Y * cellSize);

            bool isHidden = stack.IsHidden;
            bool hasIcon = config.Icon != null;
            _hiddenRoot.SetActive(isHidden);
            _searchFill.fillAmount = 0f;
            _background.color = hasIcon || isHidden ? new Color(0.09f, 0.08f, 0.07f, 0.95f) : config.IconColor * new Color(0.45f, 0.45f, 0.45f, 1f);
            _frame.color = isHidden ? s_hiddenFrame : owner.Database.GetRarityColor(stack.RarityValue);
            _icon.enabled = hasIcon && !isHidden;
            _icon.sprite = config.Icon;
            _glyph.text = hasIcon || isHidden ? string.Empty : config.IconGlyph;
            _glyph.color = config.IconColor;
            _count.text = stack.Count > 1 && !isHidden ? stack.Count.ToString() : string.Empty;
            owner.RefreshTooltip(this);
        }

        /// A dimmed copy that neither drags nor shows a tooltip: drops fall through to the slot under it.
        public void SetGhost(bool isGhost)
        {
            _group.alpha = isGhost ? 0.4f : 1f;
            _group.blocksRaycasts = !isGhost;
        }

        public void SetSearchProgress(float progress)
        {
            _searchFill.fillAmount = progress;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _stack.IsHidden)
                return;

            _isSplitting = Input.GetKey(KeyCode.LeftControl) && _stack.Count > 1;

            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 local);
            Rect rect = ((RectTransform)transform).rect;
            _grabPoint = new Vector2(Mathf.InverseLerp(rect.xMin, rect.xMax, local.x), Mathf.InverseLerp(rect.yMax, rect.yMin, local.y));
            _group.alpha = 0.4f;
            DungeonAudioComponent.PlayUi(_config.PickSound(ItemSound.Drag));
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
            if (eventData.dragging || _stack.IsHidden)
                return;

            if (eventData.button == PointerEventData.InputButton.Right)
                _owner.QuickAction(this);
            else if (eventData.button == PointerEventData.InputButton.Left)
                _owner.QuickTransfer(this, Input.GetKey(KeyCode.LeftShift));
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
