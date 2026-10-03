using TMPro;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Character sheet: the six attributes on the corners of a hexagon, the edge stats between them, derived numbers in rows.
    /// Hovering any of them explains it.
    public sealed class StatsView : MonoBehaviour
    {
        [SerializeField]
        private StatLabelView[] _labels;

        [SerializeField]
        private HexChartGraphic _chart;

        [SerializeField]
        private RectTransform _tooltip;

        [SerializeField]
        private TMP_Text _tooltipText;

        [SerializeField, Tooltip("Attribute value that reaches the outer hexagon")]
        private float _chartScale = DungeonFormulas.Threshold;

        [SerializeField]
        private float _tooltipGap = 14f;

        private readonly Vector3[] _corners = new Vector3[4];
        private AdventurerStats _stats;
        private StatLabelView _hovered;

        private void Awake()
        {
            foreach (StatLabelView label in _labels)
                label.OnHovered += OnHovered;

            _tooltip.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _hovered = null;
            _tooltip.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_stats == null)
                return;

            foreach (StatLabelView label in _labels)
                label.SetValue(StatSheet.Format(_stats, label.Stat));

            for (int i = 0; i < StatSheet.AttributeCount; i++)
                _chart.SetValue(i, _stats.Attributes.Get((StatType)i) / _chartScale);

            if (_hovered != null)
                _tooltipText.text = StatSheet.Describe(_stats, _hovered.Stat);
        }

        public void Bind(AdventurerStats stats)
        {
            _stats = stats;
        }

        private void OnHovered(StatLabelView label, bool isHovered)
        {
            if (!isHovered)
            {
                if (_hovered == label)
                {
                    _hovered = null;
                    _tooltip.gameObject.SetActive(false);
                }

                return;
            }

            if (_stats == null)
                return;

            // The tooltip stands beside the sheet, on the side of the screen middle, level with the hovered stat.
            RectTransform area = (RectTransform)_tooltip.parent;
            Vector3 middle = area.TransformPoint(area.rect.center);
            Vector3 position = label.transform.position;
            bool isLeft = transform.position.x < middle.x;
            bool isLow = position.y < middle.y;
            ((RectTransform)transform).GetWorldCorners(_corners);
            position.x = _corners[isLeft ? 2 : 0].x;
            _hovered = label;
            _tooltipText.text = StatSheet.Describe(_stats, label.Stat);
            _tooltip.gameObject.SetActive(true);
            _tooltip.pivot = new Vector2(isLeft ? 0f : 1f, isLow ? 0f : 1f);
            _tooltip.position = position;
            _tooltip.anchoredPosition += new Vector2(isLeft ? _tooltipGap : -_tooltipGap, 0f);
        }
    }
}
