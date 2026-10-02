using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Glowing boundary of the Dark Swarm on the local adventurer's floor: a translucent ring that follows the safe radius.
    public sealed class SwarmWallView : MonoBehaviour
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private MeshFilter _filter;

        [SerializeField]
        private MeshRenderer _renderer;

        [SerializeField]
        private int _segments = 72;

        [SerializeField]
        private float _height = 6f;

        [SerializeField]
        private float _floorDrop = -26f;

        private Mesh _mesh;
        private float _shownRadius = -1f;

        private void Awake()
        {
            _mesh = new Mesh { name = "SwarmWall" };
            _filter.sharedMesh = _mesh;
            _renderer.enabled = false;
        }

        private void LateUpdate()
        {
            MatchComponent match = _context.Match;
            AdventurerComponent adventurer = _context.LocalAdventurer;
            bool isVisible = match != null && match.IsRunning && adventurer != null && adventurer.Object != null && adventurer.Object.IsValid;

            if (_renderer.enabled != isVisible)
                _renderer.enabled = isVisible;

            if (!isVisible)
                return;

            int floor = adventurer.Floor;
            float radius = match.GetSafeRadius(floor);
            Vector3 center = match.GetSwarmCenter(floor);
            center.y = floor == 1 ? 0f : _floorDrop;
            transform.position = center;

            if (!Mathf.Approximately(_shownRadius, radius))
                Rebuild(Mathf.Max(radius, 0.5f));

            float pulse = 0.8f + Mathf.Sin(Time.time * 2f) * 0.2f;
            _renderer.material.SetColor("_EmissionColor", new Color(0.55f, 0.1f, 0.35f) * pulse);
        }

        private void Rebuild(float radius)
        {
            _shownRadius = radius;
            Vector3[] vertices = new Vector3[(_segments + 1) * 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[_segments * 12];

            for (int i = 0; i <= _segments; i++)
            {
                float angle = i / (float)_segments * Mathf.PI * 2f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * radius;
                vertices[i * 2 + 1] = direction * radius + Vector3.up * _height;
                uvs[i * 2] = new Vector2(i / (float)_segments * 8f, 0f);
                uvs[i * 2 + 1] = new Vector2(i / (float)_segments * 8f, 1f);
            }

            for (int i = 0; i < _segments; i++)
            {
                int a = i * 2;
                int t = i * 12;
                triangles[t] = a;
                triangles[t + 1] = a + 1;
                triangles[t + 2] = a + 2;
                triangles[t + 3] = a + 1;
                triangles[t + 4] = a + 3;
                triangles[t + 5] = a + 2;
                triangles[t + 6] = a + 2;
                triangles[t + 7] = a + 1;
                triangles[t + 8] = a;
                triangles[t + 9] = a + 2;
                triangles[t + 10] = a + 3;
                triangles[t + 11] = a + 1;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
    }
}
