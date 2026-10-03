using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Scripts.Dungeon
{
    /// One number of the character sheet: a hexagram node or a list row. Reports hover to the sheet for the tooltip.
    public sealed class StatLabelView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<StatLabelView, bool> OnHovered;

        public StatId Stat => _stat;

        [SerializeField]
        private StatId _stat;

        [SerializeField]
        private TMP_Text _valueText;

        public void SetValue(string text)
        {
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
