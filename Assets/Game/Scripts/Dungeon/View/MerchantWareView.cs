using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// One ware on a merchant's counter: the item icon with its price. Click buys, hover shows the details.
    public sealed class MerchantWareView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<MerchantWareView> OnClicked;
        public event Action<MerchantWareView, bool> OnHovered;

        public ItemConfig Item => _item;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private Image _frame;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _glyph;

        [SerializeField]
        private TMP_Text _priceText;

        [SerializeField]
        private Color _normalColor = new(0.09f, 0.08f, 0.07f, 0.95f);

        [SerializeField]
        private Color _hoverColor = new(0.26f, 0.21f, 0.13f, 0.95f);

        private ItemConfig _item;

        public void Set(ItemConfig item, Color rarityColor, int price)
        {
            _item = item;
            _frame.color = rarityColor;
            _icon.enabled = item.Icon != null;
            _icon.sprite = item.Icon;
            _glyph.text = item.Icon != null ? string.Empty : item.IconGlyph;
            _glyph.color = item.IconColor;
            _priceText.text = $"{price}g";
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging && eventData.button == PointerEventData.InputButton.Left)
                OnClicked?.Invoke(this);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            _background.color = _hoverColor;
            OnHovered?.Invoke(this, true);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            _background.color = _normalColor;
            OnHovered?.Invoke(this, false);
        }
    }
}
