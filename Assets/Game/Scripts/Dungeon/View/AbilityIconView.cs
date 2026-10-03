using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Perk diamond or skill square: an icon (or a glyph when there is none) on a framed plate that reports clicks and hover to its page.
    public sealed class AbilityIconView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<AbilityIconView, PointerEventData.InputButton> OnClicked;
        public event Action<AbilityIconView, bool> OnHovered;

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

        private int _index = -1;
        private string _tooltip;

        public void Set(int index, Sprite icon, string glyph, Color color, string tooltip)
        {
            _index = index;
            _tooltip = tooltip;
            _icon.enabled = icon != null;
            _icon.sprite = icon;
            _glyph.text = icon != null ? string.Empty : glyph;
            _glyph.color = color;
        }

        public void Clear()
        {
            Set(-1, null, string.Empty, Color.white, null);
        }

        public void SetSelected(bool isSelected)
        {
            _frame.color = isSelected ? _selectedColor : _normalColor;
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
    }
}
