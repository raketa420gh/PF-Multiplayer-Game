using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Dark Swarm on a map: the rim of the safe circle and a shade over everything outside it.
    /// The rect spans the whole shade, so a RectMask2D culls it only when it is really out of the window.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SwarmZoneGraphic : MaskableGraphic
    {
        [SerializeField]
        private Color _shade = new(0.3f, 0.05f, 0.4f, 0.45f);

        [SerializeField]
        private float _thickness = 3f;

        [SerializeField]
        private float _shadeReach = 2000f;

        [SerializeField]
        private int _segments = 96;

        private float _radius = -1f;

        /// Places the circle of a floor: origin is the world point under the parent's centre, scale is pixels per metre.
        public void Show(MatchComponent match, int floor, Vector3 origin, float scale)
        {
            bool isVisible = match != null && match.IsSwarmActive(floor);

            if (enabled != isVisible)
                enabled = isVisible;

            if (!isVisible)
                return;

            Vector3 center = match.GetSwarmCenter(floor) - origin;
            float radius = match.GetSafeRadius(floor) * scale;
            rectTransform.anchoredPosition = new Vector2(center.x, center.z) * scale;
            rectTransform.sizeDelta = Vector2.one * (radius + _shadeReach) * 2f;

            if (Mathf.Approximately(_radius, radius))
                return;

            _radius = radius;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float radius = Mathf.Max(_radius, 0f);
            Ring(vh, Mathf.Max(0f, radius - _thickness), radius, color);
            Ring(vh, radius, radius + _shadeReach, _shade);
        }

        private void Ring(VertexHelper vh, float inner, float outer, Color32 tint)
        {
            int start = vh.currentVertCount;

            for (int i = 0; i <= _segments; i++)
            {
                float angle = i * Mathf.PI * 2f / _segments;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(direction * inner, tint, Vector2.zero);
                vh.AddVert(direction * outer, tint, Vector2.zero);
            }

            for (int i = 0; i < _segments; i++)
            {
                int a = start + i * 2;
                vh.AddTriangle(a, a + 1, a + 3);
                vh.AddTriangle(a, a + 3, a + 2);
            }
        }
    }
}
