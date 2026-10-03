using System.Collections.Generic;
using System.Text;
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
        private TMP_Text _statsText;

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
        private AdventurerStats _stats;
        private ClassConfig _class;
        private bool _allowWorldDrop;
        private ItemView _dragged;
        private readonly List<RaycastResult> _raycastResults = new();
        private readonly StringBuilder _builder = new();

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

            RefreshStats();

            if (_valueText != null)
                _valueText.text = $"Gear value {_primary.TotalValue()}g";
        }

        public void Bind(InventoryComponent primary, InventoryActionsComponent actions, AdventurerStats stats, ClassConfig config, string title, bool allowWorldDrop)
        {
            if (_primary != null)
                _primary.OnChanged -= RefreshSlots;

            _primary = primary;
            _actions = actions;
            _stats = stats;
            _class = config;
            _allowWorldDrop = allowWorldDrop;
            _titleText.text = title;
            _bagGrid.Bind(this, primary);

            if (_primary != null)
                _primary.OnChanged += RefreshSlots;

            RefreshSlots();
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

            if (grid != null && grid.TryGetCell(eventData.position, _canvas.worldCamera, out int x, out int y))
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

            if (_dragged != item || _actions == null)
                return;

            _dragged = null;
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
                if (item.Config.Kind is ItemKind.Consumable or ItemKind.Utility && _allowWorldDrop)
                    _actions.RpcUseEquipped(item.Slot);
                else
                    _actions.RpcUnequip(item.Slot, _primary.Id, -1, -1);

                return;
            }

            if (item.Config.Kind is ItemKind.Consumable or ItemKind.Utility && _allowWorldDrop && item.Inventory == _primary)
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
            _tooltip.gameObject.SetActive(true);
            _tooltipText.text = BuildTooltip(item.Config, item.Stack);
            _tooltip.position = item.transform.position;
        }

        public void HideTooltip()
        {
            _tooltip.gameObject.SetActive(false);
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

        private void RefreshStats()
        {
            if (_stats == null || _class == null)
            {
                _statsText.text = string.Empty;

                return;
            }

            ClassStats attributes = _stats.Attributes;
            _builder.Clear();
            _builder.AppendLine($"<b>{_class.DisplayName}</b>");
            _builder.AppendLine($"Strength {attributes.Strength}    Vigor {attributes.Vigor}");
            _builder.AppendLine($"Agility {attributes.Agility}    Dexterity {attributes.Dexterity}");
            _builder.AppendLine($"Will {attributes.Will}    Knowledge {attributes.Knowledge}");
            _builder.AppendLine($"Resourcefulness {attributes.Resourcefulness}");
            _builder.AppendLine();
            _builder.AppendLine($"Health {_stats.MaxHealth}");
            _builder.AppendLine($"Armor Rating {_stats.ArmorRating:0}  (PDR {_stats.PhysicalReduction * 100f:0}%)");
            _builder.AppendLine($"Magic Resist {_stats.MagicResistance:0}  (MDR {_stats.MagicalReduction * 100f:0}%)");
            _builder.AppendLine($"Move Speed {_stats.MoveSpeedRating:0}  ({_stats.MoveSpeedMultiplier * 100f:0}%)");
            _builder.AppendLine($"Physical Power {_stats.PhysicalPower:0}  ({(_stats.GetDamageMultiplier(Battle.DamageType.Physical) - 1f) * 100f:+0;-0}%)");
            _builder.AppendLine($"Magical Power {_stats.MagicalPower:0}  ({(_stats.GetDamageMultiplier(Battle.DamageType.Magical) - 1f) * 100f:+0;-0}%)");
            _builder.AppendLine($"Action Speed {(_stats.ActionSpeed - 1f) * 100f:+0;-0}%   Cast Speed {(_stats.CastSpeed - 1f) * 100f:+0;-0}%");
            _builder.AppendLine($"Interaction Speed {(_stats.InteractionSpeed - 1f) * 100f:+0;-0}%");
            _statsText.text = _builder.ToString();
        }

        private string BuildTooltip(ItemConfig config, ItemStack stack)
        {
            _builder.Clear();
            Color color = _database.GetRarityColor(stack.RarityValue);
            _builder.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(color)}><b>{config.DisplayName}</b></color>  <size=80%>{stack.RarityValue} {config.Kind}</size>");
            int tier = Mathf.Max(0, stack.Rarity - (int)ItemRarity.Common);

            switch (config)
            {
                case WeaponItemConfig weapon:
                    int damage = weapon.Weapon != null && weapon.Weapon.Attacks.Length > 0 ? weapon.Weapon.Attacks[0].Damage : weapon.Weapon != null ? weapon.Weapon.Ranged.MaxDamage : 0;
                    _builder.AppendLine($"{(weapon.IsTwoHanded ? "Two-handed" : weapon.IsOffHand ? "Off-hand" : "One-handed")} {weapon.WeaponClass}");

                    if (damage > 0)
                        _builder.AppendLine($"Damage {damage + tier}  ({weapon.DamageType})");

                    if (weapon.MoveSpeedPenalty > 0f)
                        _builder.AppendLine($"Move Speed -{weapon.MoveSpeedPenalty:0}");

                    if (weapon.LightRange > 0f)
                        _builder.AppendLine("Light source");
                    break;
                case ArmorItemConfig armor:
                    _builder.AppendLine($"{armor.ArmorType} {armor.Slot}");
                    _builder.AppendLine($"Armor Rating {armor.ArmorRating + tier * 2f:0}");

                    if (armor.MagicResistance != 0f)
                        _builder.AppendLine($"Magic Resistance {armor.MagicResistance:+0;-0}");

                    if (armor.MoveSpeedPenalty != 0f)
                        _builder.AppendLine($"Move Speed {armor.MoveSpeedPenalty:+0;-0}");
                    break;
                case ConsumableItemConfig consumable:
                    _builder.AppendLine(consumable.Effect switch
                    {
                        ConsumableEffect.HealInstant => $"Heals {consumable.Magnitude + tier * 4f:0} after {consumable.UseTime:0.#}s",
                        ConsumableEffect.HealOverTime => $"Heals {consumable.Magnitude:0} over {Mathf.Max(1f, consumable.Duration - tier * 2.5f):0.#}s",
                        ConsumableEffect.Protection => $"Absorbs {consumable.Magnitude + tier * 5f:0} damage for {consumable.Duration:0}s",
                        _ => $"+{consumable.Magnitude:0} move speed for {consumable.Duration:0}s"
                    });
                    break;
                case UtilityItemConfig utility:
                    _builder.AppendLine(utility.UtilityKind.ToString());

                    if (utility.Damage > 0)
                        _builder.AppendLine($"Thrown damage {utility.Damage}");
                    break;
            }

            foreach (StatModifier modifier in config.Modifiers)
                _builder.AppendLine($"<color=#8fd>{modifier.Stat} {modifier.Value:+0.#;-0.#}</color>");

            if (!string.IsNullOrEmpty(config.Description))
                _builder.AppendLine($"<i><size=80%>{config.Description}</size></i>");

            _builder.Append($"<size=80%>Value {config.Value}g   {config.Width}x{config.Height}</size>");

            return _builder.ToString();
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
