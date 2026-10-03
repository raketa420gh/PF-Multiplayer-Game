using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Radar polygon of the hexagram: one corner per attribute, clockwise from the top, pushed out by its value.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HexChartGraphic : MaskableGraphic
    {
        private const int Corners = StatSheet.AttributeCount;

        [SerializeField, Tooltip("Distance of a corner whose value is 1")]
        private float _radius = 140f;

        [SerializeField]
        private float _outline = 2.5f;

        [SerializeField, Range(0f, 1f)]
        private float _fillAlpha = 0.42f;

        [SerializeField]
        private Color[] _colors = new Color[Corners];

        private readonly float[] _values = { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };

        public void SetValue(int corner, float value)
        {
            value = Mathf.Clamp(value, 0.06f, 1.12f);

            if (Mathf.Approximately(_values[corner], value))
                return;

            _values[corner] = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Vector2 center = rectTransform.rect.center;
            Color middle = color;
            middle.a *= _fillAlpha * 0.35f;
            helper.AddVert(center, middle, Vector2.zero);

            for (int i = 0; i < Corners; i++)
            {
                Color fill = _colors[i] * color;
                fill.a *= _fillAlpha;
                helper.AddVert(center + Corner(i, 0f), fill, Vector2.zero);
            }

            for (int i = 0; i < Corners; i++)
                helper.AddTriangle(0, 1 + i, 1 + (i + 1) % Corners);

            // Outline: a strip between every corner and the same corner pulled towards the center.
            for (int i = 0; i < Corners; i++)
            {
                Color line = _colors[i] * color;
                helper.AddVert(center + Corner(i, 0f), line, Vector2.zero);
                helper.AddVert(center + Corner(i, _outline), line, Vector2.zero);
            }

            int first = 1 + Corners;

            for (int i = 0; i < Corners; i++)
            {
                int a = first + i * 2;
                int b = first + (i + 1) % Corners * 2;
                helper.AddTriangle(a, b, b + 1);
                helper.AddTriangle(a, b + 1, a + 1);
            }
        }

        private Vector2 Corner(int index, float inset)
        {
            float angle = index * Mathf.PI * 2f / Corners;

            return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * Mathf.Max(0f, _values[index] * _radius - inset);
        }
    }
}
