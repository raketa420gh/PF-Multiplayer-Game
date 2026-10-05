using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Paper doll + bag + optional second grid (chest, corpse or stash). Works for the adventurer and for the lobby kit.
    public sealed class InventoryView : DisplayableView
    {
        public ItemDatabase Database => _database;
        public InventoryComponent Primary => _primary;

        [SerializeField]
        private ItemDatabase _database;

        [SerializeField]
        private ItemGridView _bagGrid;

        [SerializeField]
        private ItemGridView _otherGrid;

        [SerializeField]
        private TMP_Text _otherTitle;

        [SerializeField]
        private GameObject _otherPanel;

        [SerializeField]
        private Button _takeAllButton;

        [SerializeField]
        private Button _sortButton;

        [SerializeField]
        private TMP_Text _valueText;

        [SerializeField]
        private Image _dragGhostIcon;

        [SerializeField]
        private EquipSlotView[] _slots;

        [SerializeField]
        private StatsView _statsView;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private RectTransform _tooltip;

        [SerializeField]
        private TMP_Text _tooltipText;

        [SerializeField]
        private RectTransform _dragGhost;

        [SerializeField]
        private TMP_Text _dragGhostText;

        [SerializeField]
        private Canvas _canvas;

        private InventoryComponent _primary;
        private InventoryComponent _other;
        private InventoryActionsComponent _actions;
        private AdventurerComponent _searcher;
        private bool _allowWorldDrop;
        private ItemView _dragged;
        private ItemView _tooltipItem;
        private ItemDropZone _hoveredZone;
        private readonly List<RaycastResult> _raycastResults = new();

        private void Awake()
        {
            _takeAllButton.onClick.AddListener(TakeAll);
            _sortButton.onClick.AddListener(Sort);
            _tooltip.gameObject.SetActive(false);
            _dragGhost.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_primary == null || _primary.Object == null || !_primary.Object.IsValid)
                return;

            if (_other != null && (_other.Object == null || !_other.Object.IsValid))
                SetOther(null, string.Empty);

            // The hovered item was taken away or its container closed.
            if (_tooltipItem != null && !_tooltipItem.isActiveAndEnabled)
                HideTooltip();

            if (_other != null && _searcher != null && _searcher.Object != null && _searcher.Object.IsValid)
                _otherGrid.SetSearch(_searcher.SearchIndex, _searcher.SearchProgress);

            if (_valueText != null)
                _valueText.text = $"Gear value {_primary.TotalValue()}g";
        }

        public void Bind(InventoryComponent primary, InventoryActionsComponent actions, AdventurerStats stats, string title, bool allowWorldDrop)
        {
            if (_primary != null)
                _primary.OnChanged -= RefreshSlots;

            _primary = primary;
            _actions = actions;
            _allowWorldDrop = allowWorldDrop;
            _statsView.Bind(stats);
            _titleText.text = title;
            _bagGrid.Bind(this, primary);

            if (_primary != null)
                _primary.OnChanged += RefreshSlots;

            RefreshSlots();
        }

        /// The adventurer whose search of the other grid is shown on its unsearched items.
        public void SetSearcher(AdventurerComponent searcher)
        {
            _searcher = searcher;
        }

        public void SetOther(InventoryComponent other, string title)
        {
            _other = other;
            _otherPanel.SetActive(other != null);
            _otherTitle.text = title;
            _otherGrid.Bind(this, other);
        }

        public override void Hide()
        {
            base.Hide();
            HideTooltip();
            _dragGhost.gameObject.SetActive(false);
            _dragged = null;
        }

        public void BeginDrag(ItemView item, PointerEventData eventData)
        {
            _dragged = item;
            _dragGhost.gameObject.SetActive(true);
            _dragGhost.sizeDelta = new Vector2(item.Config.Width * _bagGrid.CellSize, item.Config.Height * _bagGrid.CellSize);
            _dragGhostText.text = item.Config.Icon != null ? string.Empty : item.Config.IconGlyph;
            _dragGhostText.color = item.Config.IconColor;
            _dragGhostIcon.enabled = item.Config.Icon != null;
            _dragGhostIcon.sprite = item.Config.Icon;
            HideTooltip();
            Drag(eventData);
        }

        public void Drag(PointerEventData eventData)
        {
            if (_dragged == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_canvas.transform, eventData.position, _canvas.worldCamera, out Vector2 local);
            _dragGhost.anchoredPosition = local - new Vector2(_dragged.GrabCell.x * _bagGrid.CellSize + _bagGrid.CellSize * 0.5f,
                -(_dragged.GrabCell.y * _bagGrid.CellSize + _bagGrid.CellSize * 0.5f));

            ItemGridView grid = FindUnderPointer<ItemGridView>(eventData, out EquipSlotView slot);
            _bagGrid.ClearHighlight();
            _otherGrid.ClearHighlight();

            foreach (EquipSlotView view in _slots)
                view.SetHighlight(false, false);

            ItemDropZone zone = FindDropZone(eventData);

            if (zone != _hoveredZone)
                _hoveredZone?.Preview(null);

            _hoveredZone = zone;
            zone?.Preview(_dragged);

            if (grid != null && grid.Inventory != null && grid.TryGetCell(eventData.position, _canvas.worldCamera, out int x, out int y))
            {
                x -= _dragged.GrabCell.x;
                y -= _dragged.GrabCell.y;
                bool isValid = grid.Inventory.CanPlace(_dragged.Config, x, y, grid.Inventory == _dragged.Inventory ? _dragged.BagIndex : -1);
                grid.Highlight(x, y, _dragged.Config.Width, _dragged.Config.Height, isValid || grid.Inventory.FindBagIndexAt(x, y) >= 0);
            }
            else if (slot != null)
            {
                slot.SetHighlight(_dragged.Config.CanEquip(slot.Slot) && !_primary.IsSlotBlocked(slot.Slot), true);
            }
        }

        public void EndDrag(ItemView item, PointerEventData eventData)
        {
            _dragGhost.gameObject.SetActive(false);
            _bagGrid.ClearHighlight();
            _otherGrid.ClearHighlight();

            foreach (EquipSlotView view in _slots)
                view.SetHighlight(false, false);

            _hoveredZone?.Preview(null);
            _hoveredZone = null;

            if (_dragged != item || _actions == null)
                return;

            _dragged = null;
            ItemDropZone zone = FindDropZone(eventData);

            if (zone != null)
            {
                zone.Drop(item);

                return;
            }

            ItemGridView grid = FindUnderPointer<ItemGridView>(eventData, out EquipSlotView slot);

            if (grid != null && grid.TryGetCell(eventData.position, _canvas.worldCamera, out int x, out int y))
            {
                x -= item.GrabCell.x;
                y -= item.GrabCell.y;

                if (item.IsEquipped)
                    _actions.RpcUnequip(item.Slot, grid.Inventory.Id, x, y);
                else if (item.IsSplitting)
                    _actions.RpcSplit(item.Inventory.Id, item.BagIndex, grid.Inventory.Id, x, y, item.Stack.Count / 2);
                else
                    _actions.RpcMove(item.Inventory.Id, item.BagIndex, grid.Inventory.Id, x, y);

                return;
            }

            if (slot != null)
            {
                if (item.IsEquipped)
                    _actions.RpcSwapEquipment(item.Slot, slot.Slot);
                else
                    _actions.RpcEquip(item.Inventory.Id, item.BagIndex, slot.Slot);

                return;
            }

            if (!_allowWorldDrop || IsPointerOverUi(eventData))
                return;

            if (item.IsEquipped)
                _actions.RpcDropEquipment(item.Slot);
            else
                _actions.RpcDrop(item.Inventory.Id, item.BagIndex);
        }

        public void QuickAction(ItemView item)
        {
            if (_actions == null)
                return;

            if (item.IsEquipped)
            {
                _actions.RpcUnequip(item.Slot, _primary.Id, -1, -1);

                return;
            }

            if (IsBeltLoot(item))
            {
                _actions.RpcUse(item.Inventory.Id, item.BagIndex);

                return;
            }

            EquipSlot target = FindEquipSlot(item.Config);

            if (target != EquipSlot.Count)
                _actions.RpcEquip(item.Inventory.Id, item.BagIndex, target);
        }

        public void QuickTransfer(ItemView item)
        {
            if (_actions == null || item.IsEquipped)
                return;

            InventoryComponent target = item.Inventory == _primary ? _other : _primary;

            if (target == null)
                return;

            if (target == _primary && IsBeltLoot(item))
            {
                _actions.RpcUse(item.Inventory.Id, item.BagIndex);

                return;
            }

            for (int y = 0; y < target.Height; y++)
            {
                for (int x = 0; x < target.Width; x++)
                {
                    if (!target.CanPlace(item.Config, x, y))
                        continue;

                    _actions.RpcMove(item.Inventory.Id, item.BagIndex, target.Id, x, y);

                    return;
                }
            }
        }

        public void ShowTooltip(ItemView item)
        {
            _tooltipItem = item;
            _tooltip.gameObject.SetActive(true);
            _tooltipText.text = ItemTooltip.Build(_database, item.Config, item.Stack);
            _tooltip.position = item.transform.position;
        }

        public void HideTooltip()
        {
            _tooltipItem = null;
            _tooltip.gameObject.SetActive(false);
        }

        /// A view was rebound to another stack: the tooltip follows if it is the hovered one.
        public void RefreshTooltip(ItemView item)
        {
            if (_tooltipItem == item)
                ShowTooltip(item);
        }

        private void TakeAll()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.4f);

            if (_other != null && _actions != null)
                _actions.RpcTakeAll(_other.Id);
        }

        private void Sort()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.4f);

            if (_primary != null && _actions != null)
                _actions.RpcSort(_primary.Id);
        }

        private void RefreshSlots()
        {
            if (_primary == null || _primary.Object == null || !_primary.Object.IsValid)
                return;

            foreach (EquipSlotView slot in _slots)
                slot.Bind(this, _primary);
        }

        /// In the dungeon consumables and utilities are stowed on the belt instead of being used from the bag.
        private bool IsBeltLoot(ItemView item)
        {
            return _allowWorldDrop && item.Config.Kind is ItemKind.Consumable or ItemKind.Utility;
        }

        private EquipSlot FindEquipSlot(ItemConfig config)
        {
            for (EquipSlot slot = 0; slot < EquipSlot.Count; slot++)
            {
                if (config.CanEquip(slot) && _primary.GetEquipped(slot).IsEmpty && !_primary.IsSlotBlocked(slot))
                    return slot;
            }

            for (EquipSlot slot = 0; slot < EquipSlot.Count; slot++)
            {
                if (config.CanEquip(slot) && !_primary.IsSlotBlocked(slot))
                    return slot;
            }

            return EquipSlot.Count;
        }

        private T FindUnderPointer<T>(PointerEventData eventData, out EquipSlotView slot) where T : Component
        {
            slot = null;
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);

            foreach (RaycastResult result in _raycastResults)
            {
                T grid = result.gameObject.GetComponentInParent<T>();

                if (grid != null)
                    return grid;

                EquipSlotView slotView = result.gameObject.GetComponentInParent<EquipSlotView>();

                if (slotView != null)
                {
                    slot = slotView;

                    return null;
                }
            }

            return null;
        }

        /// Only the topmost element counts: a zone hidden under another panel takes nothing.
        private ItemDropZone FindDropZone(PointerEventData eventData)
        {
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);

            return _raycastResults.Count > 0 ? _raycastResults[0].gameObject.GetComponentInParent<ItemDropZone>() : null;
        }

        private bool IsPointerOverUi(PointerEventData eventData)
        {
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);

            foreach (RaycastResult result in _raycastResults)
            {
                if (result.gameObject.GetComponentInParent<InventoryView>() == this && result.gameObject != gameObject)
                    return true;
            }

            return false;
        }
    }
}
