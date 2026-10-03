using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Radial menu held open with the memory skill key: move the mouse to a sector, release to ready that spell or form.
    public sealed class SpellWheelView : DisplayableView
    {
        public int Selected => _selected;

        [SerializeField]
        private RectTransform _slotPrefab;

        [SerializeField]
        private RectTransform _slotsRoot;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _centerText;

        [SerializeField]
        private RectTransform _cursorMark;

        [SerializeField]
        private float _radius = 150f;

        [SerializeField]
        private float _deadZone = 25f;

        private readonly List<RectTransform> _slots = new();
        private readonly List<Image> _backgrounds = new();
        private readonly List<string> _names = new();
        private Vector2 _cursor;
        private int _selected = -1;

        public void Open(string title, IReadOnlyList<(string name, Sprite icon, string glyph, Color color, string detail)> entries, int current)
        {
            Show();
            _titleText.text = title;
            _cursor = Vector2.zero;
            _cursorMark.anchoredPosition = _cursor;
            _selected = current;
            _names.Clear();

            while (_slots.Count < entries.Count)
            {
                RectTransform slot = Instantiate(_slotPrefab, _slotsRoot);
                _slots.Add(slot);
                _backgrounds.Add(slot.GetComponent<Image>());
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                bool isUsed = i < entries.Count;
                _slots[i].gameObject.SetActive(isUsed);

                if (!isUsed)
                    continue;

                float angle = (i / (float)entries.Count) * Mathf.PI * 2f;
                _slots[i].anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * _radius;
                TMP_Text glyph = _slots[i].Find("Glyph").GetComponent<TMP_Text>();
                TMP_Text name = _slots[i].Find("Name").GetComponent<TMP_Text>();
                Image icon = _slots[i].Find("Icon").GetComponent<Image>();
                icon.enabled = entries[i].icon != null;
                icon.sprite = entries[i].icon;
                glyph.text = entries[i].icon != null ? string.Empty : entries[i].glyph;
                glyph.color = entries[i].color;
                name.text = entries[i].name + (string.IsNullOrEmpty(entries[i].detail) ? string.Empty : $"\n<size=70%>{entries[i].detail}</size>");
                _names.Add(entries[i].name);
            }

            Highlight();
        }

        public void Move(Vector2 delta)
        {
            _cursor += delta;

            if (_cursor.magnitude > _radius)
                _cursor = _cursor.normalized * _radius;

            _cursorMark.anchoredPosition = _cursor;

            if (_cursor.magnitude < _deadZone || _names.Count == 0)
                return;

            float angle = Mathf.Atan2(_cursor.x, _cursor.y);

            if (angle < 0f)
                angle += Mathf.PI * 2f;

            float sector = Mathf.PI * 2f / _names.Count;
            _selected = Mathf.RoundToInt(angle / sector) % _names.Count;
            Highlight();
        }

        private void Highlight()
        {
            for (int i = 0; i < _backgrounds.Count; i++)
                _backgrounds[i].color = i == _selected ? new Color(0.6f, 0.45f, 0.15f, 0.95f) : new Color(0.08f, 0.07f, 0.06f, 0.9f);

            _centerText.text = _selected >= 0 && _selected < _names.Count ? _names[_selected] : "Move to choose";
        }
    }
}
