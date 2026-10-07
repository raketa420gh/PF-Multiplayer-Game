using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Perk diamond or skill square: an icon (or a glyph when there is none) on a framed plate that reports clicks, hover,
    /// drags and drops to its page.
    public sealed class AbilityIconView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public event Action<AbilityIconView, PointerEventData.InputButton> OnClicked;
        public event Action<AbilityIconView, bool> OnHovered;
        public event Action<AbilityIconView, PointerEventData> OnDragStarted;
        public event Action<AbilityIconView, PointerEventData> OnDragged;
        public event Action<AbilityIconView> OnDragEnded;
        /// Raised by the icon something was dropped on.
        public event Action<AbilityIconView> OnDropped;

        /// Index in the class pool, -1 while the icon is an empty slot.
        public int Index => _index;
        public string Tooltip => _tooltip;

        [SerializeField]
        private Image _frame;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _glyph;

        [SerializeField]
        private TMP_Text _badge;

        [SerializeField]
        private Color _normalColor = new(0.5f, 0.42f, 0.3f);

        [SerializeField]
        private Color _selectedColor = new(1f, 0.86f, 0.55f);

        [SerializeField]
        private Color _targetColor = new(0.55f, 0.95f, 0.6f);

        private int _index = -1;
        private string _tooltip;
        private bool _isSelected;

        public void Set(int index, Sprite icon, string glyph, Color color, string tooltip)
        {
            _index = index;
            _tooltip = tooltip;
            _icon.enabled = icon != null;
            _icon.sprite = icon;
            _glyph.text = icon != null ? string.Empty : glyph;
            _glyph.color = color;
        }

        /// Shows a skill or a spell of the class pool with its tooltip.
        public void SetAbility(int index, AbilityConfig ability)
        {
            string detail = ability.IsSpell && !ability.IsCooldownBased ? $"{ability.Charges} charges · cast {ability.CastTime:0.##}s"
                : ability.Cooldown > 0f ? $"Cooldown {ability.Cooldown:0}s" : string.Empty;
            Set(index, ability.Icon, ability.Glyph, ability.Color, $"<b>{ability.DisplayName}</b>\n{ability.Description}\n<size=80%><color=#9a927f>{detail}</color></size>");
        }

        public void Clear()
        {
            Set(-1, null, string.Empty, Color.white, null);
        }

        /// Takes over the look of another icon: the copy that follows the cursor during a drag.
        public void CopyFrom(AbilityIconView source)
        {
            Set(source._index, source._icon.sprite, source._glyph.text, source._glyph.color, null);
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            _frame.color = isSelected ? _selectedColor : _normalColor;
        }

        /// Marks the slot as a place the dragged icon can be dropped on.
        public void SetTargeted(bool isTargeted)
        {
            _frame.color = isTargeted ? _targetColor : _isSelected ? _selectedColor : _normalColor;
        }

        public void SetBadge(string text)
        {
            _badge.text = text;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke(this, eventData.button);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            OnHovered?.Invoke(this, true);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            OnHovered?.Invoke(this, false);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            OnDragStarted?.Invoke(this, eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            OnDragged?.Invoke(this, eventData);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            OnDragEnded?.Invoke(this);
        }

        void IDropHandler.OnDrop(PointerEventData eventData)
        {
            OnDropped?.Invoke(this);
        }
    }
}
