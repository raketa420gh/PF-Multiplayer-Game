using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Minimal procedural mesh assembler with world-scaled UVs, saved as mesh assets.
    internal sealed class DungeonMeshBuilder
    {
        public const string Folder = "Assets/Game/Meshes/Dungeon";

        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<int> _triangles = new();
        private readonly float _uvScale;

        public DungeonMeshBuilder(float uvScale = 0.5f)
        {
            _uvScale = uvScale;
        }

        public DungeonMeshBuilder Box(Vector3 center, Vector3 size)
        {
            Vector3 half = size * 0.5f;
            Quad(center + Vector3.forward * half.z, Vector3.forward, Vector3.right * half.x, Vector3.up * half.y);
            Quad(center - Vector3.forward * half.z, Vector3.back, Vector3.left * half.x, Vector3.up * half.y);
            Quad(center + Vector3.right * half.x, Vector3.right, Vector3.back * half.z, Vector3.up * half.y);
            Quad(center - Vector3.right * half.x, Vector3.left, Vector3.forward * half.z, Vector3.up * half.y);
            Quad(center + Vector3.up * half.y, Vector3.up, Vector3.right * half.x, Vector3.forward * half.z);
            Quad(center - Vector3.up * half.y, Vector3.down, Vector3.right * half.x, Vector3.back * half.z);

            return this;
        }

        public DungeonMeshBuilder Cylinder(Vector3 center, float radius, float height, int segments, float topRadius = -1f)
        {
            if (topRadius < 0f)
                topRadius = radius;

            int start = _vertices.Count;
            float circumference = Mathf.PI * 2f * radius;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 bottom = center + direction * radius - Vector3.up * height * 0.5f;
                Vector3 top = center + direction * topRadius + Vector3.up * height * 0.5f;
                float u = i / (float)segments * circumference * _uvScale;

                _vertices.Add(bottom);
                _normals.Add(direction);
                _uvs.Add(new Vector2(u, 0f));
                _vertices.Add(top);
                _normals.Add(direction);
                _uvs.Add(new Vector2(u, height * _uvScale));
            }

            for (int i = 0; i < segments; i++)
            {
                int a = start + i * 2;
                _triangles.Add(a);
                _triangles.Add(a + 1);
                _triangles.Add(a + 2);
                _triangles.Add(a + 1);
                _triangles.Add(a + 3);
                _triangles.Add(a + 2);
            }

            Disc(center + Vector3.up * height * 0.5f, topRadius, segments, Vector3.up);
            Disc(center - Vector3.up * height * 0.5f, radius, segments, Vector3.down);

            return this;
        }

        public DungeonMeshBuilder Quad(Vector3 center, Vector3 normal, Vector3 right, Vector3 up)
        {
            int start = _vertices.Count;
            float width = right.magnitude * 2f * _uvScale;
            float height = up.magnitude * 2f * _uvScale;

            _vertices.Add(center - right - up);
            _vertices.Add(center - right + up);
            _vertices.Add(center + right + up);
            _vertices.Add(center + right - up);

            for (int i = 0; i < 4; i++)
                _normals.Add(normal);

            _uvs.Add(new Vector2(0f, 0f));
            _uvs.Add(new Vector2(0f, height));
            _uvs.Add(new Vector2(width, height));
            _uvs.Add(new Vector2(width, 0f));

            // Wind the triangles so the visible side always matches the requested normal.
            bool flip = Vector3.Dot(Vector3.Cross(up, right), normal) < 0f;
            _triangles.Add(start);
            _triangles.Add(start + (flip ? 2 : 1));
            _triangles.Add(start + (flip ? 1 : 2));
            _triangles.Add(start);
            _triangles.Add(start + (flip ? 3 : 2));
            _triangles.Add(start + (flip ? 2 : 3));

            return this;
        }

        public DungeonMeshBuilder DoubleQuad(Vector3 center, Vector3 normal, Vector3 right, Vector3 up)
        {
            Quad(center, normal, right, up);
            Quad(center, -normal, -right, up);

            return this;
        }

        public DungeonMeshBuilder Wedge(Vector3 center, Vector3 size)
        {
            Vector3 half = size * 0.5f;
            int start = _vertices.Count;
            Vector3[] p =
            {
                center + new Vector3(-half.x, -half.y, -half.z), center + new Vector3(half.x, -half.y, -half.z),
                center + new Vector3(half.x, -half.y, half.z), center + new Vector3(-half.x, -half.y, half.z),
                center + new Vector3(-half.x, half.y, half.z), center + new Vector3(half.x, half.y, half.z)
            };
            Vector3 slopeNormal = Vector3.Cross(p[4] - p[0], p[1] - p[0]).normalized;
            AddTriangle(p[0], p[4], p[1], slopeNormal);
            AddTriangle(p[1], p[4], p[5], slopeNormal);
            AddTriangle(p[3], p[2], p[5], Vector3.forward);
            AddTriangle(p[3], p[5], p[4], Vector3.forward);
            AddTriangle(p[0], p[1], p[2], Vector3.down);
            AddTriangle(p[0], p[2], p[3], Vector3.down);
            AddTriangle(p[0], p[3], p[4], Vector3.left);
            AddTriangle(p[1], p[5], p[2], Vector3.right);

            return this;
        }

        public Mesh Save(string name)
        {
            BattleEditorUtilityShim.EnsureFolder(Folder);
            string path = $"{Folder}/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.indexFormat = _vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh.name = name;
            EditorUtility.SetDirty(mesh);

            return mesh;
        }

        private void Disc(Vector3 center, float radius, int segments, Vector3 normal)
        {
            int centerIndex = _vertices.Count;
            _vertices.Add(center);
            _normals.Add(normal);
            _uvs.Add(Vector2.zero);

            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                _vertices.Add(center + direction * radius);
                _normals.Add(normal);
                _uvs.Add(new Vector2(direction.x, direction.z) * radius * _uvScale);
            }

            for (int i = 0; i < segments; i++)
            {
                _triangles.Add(centerIndex);

                if (normal.y > 0f)
                {
                    _triangles.Add(centerIndex + 1 + i + 1);
                    _triangles.Add(centerIndex + 1 + i);
                }
                else
                {
                    _triangles.Add(centerIndex + 1 + i);
                    _triangles.Add(centerIndex + 1 + i + 1);
                }
            }
        }

        private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        {
            int start = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);

            for (int i = 0; i < 3; i++)
                _normals.Add(normal);

            Vector3 tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.9f ? Vector3.forward : Vector3.up).normalized;
            Vector3 bitangent = Vector3.Cross(normal, tangent);
            _uvs.Add(new Vector2(Vector3.Dot(a, tangent), Vector3.Dot(a, bitangent)) * _uvScale);
            _uvs.Add(new Vector2(Vector3.Dot(b, tangent), Vector3.Dot(b, bitangent)) * _uvScale);
            _uvs.Add(new Vector2(Vector3.Dot(c, tangent), Vector3.Dot(c, bitangent)) * _uvScale);

            _triangles.Add(start);
            _triangles.Add(start + 1);
            _triangles.Add(start + 2);
        }
    }

    internal static class BattleEditorUtilityShim
    {
        public static void EnsureFolder(string path)
        {
            Battle.BattleEditorUtility.EnsureFolder(path);
        }
    }
}
