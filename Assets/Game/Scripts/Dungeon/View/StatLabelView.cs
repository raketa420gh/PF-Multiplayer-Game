using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Scripts.Dungeon
{
    /// One number of the character sheet: a hexagram node, a dot on its side or a list row. Reports hover to the sheet for the tooltip.
    public sealed class StatLabelView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<StatLabelView, bool> OnHovered;

        public StatId Stat => _stat;

        [SerializeField]
        private StatId _stat;

        [SerializeField]
        private TMP_Text _valueText;

        /// Dots of the hexagram have no text: their value is in the list and the tooltip.
        public void SetValue(string text)
        {
            if (_valueText != null)
                _valueText.text = text;
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
