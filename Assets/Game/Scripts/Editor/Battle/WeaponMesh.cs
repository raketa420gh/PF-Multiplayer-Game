using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Geometry of one weapon: parts are lofted, revolved or cut from plate and baked into a single mesh with a submesh per
    /// material. UVs count texture tiles of TileSize metres, so every surface gets the same texel density, and a loft wraps
    /// a whole number of tiles around itself to hide the seam.
    internal sealed class WeaponMesh
    {
        public const string Folder = BattleEditorUtility.ModelsFolder + "/Weapons";
        public const float TileSize = 0.125f;
        public const float Hard = 25f;
        public const float Soft = 60f;

        /// Placement of the parts added next, in weapon space.
        public Matrix4x4 Matrix = Matrix4x4.identity;

        private readonly List<Vector3> _positions = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<float> _smoothing = new();
        private readonly List<int> _triangleMaterials = new();
        private readonly List<Material> _materials = new();

        public static Vector3[] Ellipse(Vector3 center, Vector3 axisA, Vector3 axisB, float radiusA, float radiusB, int sides, float phase = 0f)
        {
            Vector3[] ring = new Vector3[sides];

            for (int i = 0; i < sides; i++)
            {
                float angle = (i + phase) * Mathf.PI * 2f / sides;
                ring[i] = center + axisA * (Mathf.Cos(angle) * radiusA) + axisB * (Mathf.Sin(angle) * radiusB);
            }

            return ring;
        }

        /// Skin over a row of closed rings with the same number of points.
        public void Loft(Material material, IReadOnlyList<Vector3[]> rings, float smoothAngle = Soft, bool capStart = true, bool capEnd = true)
        {
            int count = rings[0].Length;
            Vector3[] centers = new Vector3[rings.Count];
            float[] around = new float[count + 1];
            float perimeter = 0f;

            for (int i = 0; i < rings.Count; i++)
            {
                float length = 0f;

                foreach (Vector3 point in rings[i])
                    centers[i] += point / count;

                for (int j = 0; j < count; j++)
                    length += Vector3.Distance(rings[i][j], rings[i][(j + 1) % count]);

                if (length <= perimeter)
                    continue;

                perimeter = length;
                around[0] = 0f;

                for (int j = 0; j < count; j++)
                    around[j + 1] = around[j] + Vector3.Distance(rings[i][j], rings[i][(j + 1) % count]) / length;
            }

            float tiles = Mathf.Max(1f, Mathf.Round(perimeter / TileSize));
            float along = 0f;

            for (int i = 0; i < rings.Count - 1; i++)
            {
                float step = 0f;

                for (int j = 0; j < count; j++)
                    step += Vector3.Distance(rings[i][j], rings[i + 1][j]) / count;

                float next = along + step / TileSize;
                Vector3 axis = (centers[i] + centers[i + 1]) * 0.5f;

                for (int j = 0; j < count; j++)
                {
                    int k = (j + 1) % count;
                    Vector3 a = rings[i][j];
                    Vector3 b = rings[i][k];
                    Vector3 c = rings[i + 1][k];
                    Vector3 d = rings[i + 1][j];
                    Vector3 outward = (a + b + c + d) * 0.25f - axis;
                    Vector2 ua = new Vector2(around[j] * tiles, along);
                    Vector2 ub = new Vector2(around[j + 1] * tiles, along);
                    Vector2 uc = new Vector2(around[j + 1] * tiles, next);
                    Vector2 ud = new Vector2(around[j] * tiles, next);

                    Triangle(material, a, b, c, ua, ub, uc, outward, smoothAngle);
                    Triangle(material, a, c, d, ua, uc, ud, outward, smoothAngle);
                }

                along = next;
            }

            if (capStart)
                Cap(material, rings[0], centers[0], centers[0] - centers[Mathf.Min(1, rings.Count - 1)]);

            if (capEnd)
                Cap(material, rings[^1], centers[^1], centers[^1] - centers[Mathf.Max(rings.Count - 2, 0)]);
        }

        /// Body of revolution around +Z: the profile lists (z, radius); squash flattens it along X.
        public void Revolve(Material material, IReadOnlyList<Vector2> profile, int sides = 12, float squash = 1f, float smoothAngle = Soft)
        {
            List<Vector3[]> rings = new(profile.Count);

            foreach (Vector2 point in profile)
                rings.Add(Ellipse(new Vector3(0f, 0f, point.x), Vector3.right, Vector3.up, point.y * squash, point.y, sides));

            Loft(material, rings, smoothAngle);
        }

        /// Sweeps an ellipse along a path; radii are (across, along the up hint) at every path point.
        public void Tube(Material material, IReadOnlyList<Vector3> path, IReadOnlyList<Vector2> radii, int sides, Vector3 up,
            float smoothAngle = Soft, bool isClosed = false)
        {
            List<Vector3[]> rings = new(path.Count + 1);
            Vector3 normal = up;

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 previous = path[isClosed ? (i + path.Count - 1) % path.Count : Mathf.Max(i - 1, 0)];
                Vector3 next = path[isClosed ? (i + 1) % path.Count : Mathf.Min(i + 1, path.Count - 1)];
                Vector3 tangent = (next - previous).normalized;
                normal = Vector3.ProjectOnPlane(normal, tangent).normalized;
                Vector3 side = Vector3.Cross(normal, tangent);
                rings.Add(Ellipse(path[i], side, normal, radii[i].x, radii[i].y, sides));
            }

            if (isClosed)
                rings.Add(rings[0]);

            Loft(material, rings, smoothAngle, !isClosed, !isClosed);
        }

        public void Rod(Material material, Vector3 from, Vector3 to, float radiusFrom, float radiusTo, int sides = 8, float smoothAngle = Soft)
        {
            Vector3 up = Mathf.Abs(Vector3.Dot((to - from).normalized, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up;
            Tube(material, new[] { from, to }, new[] { Vector2.one * radiusFrom, Vector2.one * radiusTo }, sides, up, smoothAngle);
        }

        /// Plate in the YZ plane: outline points are (y, z) with their own half thickness, fanned around a centre, so a
        /// thick centre with a zero-thickness rim makes a wedge that ends in a cutting edge.
        public void Plate(Material material, IReadOnlyList<Vector2> outline, IReadOnlyList<float> halfThickness, Vector2 center,
            float centerHalfThickness, float smoothAngle = Hard)
        {
            int count = outline.Count;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 hub = new Vector3(centerHalfThickness * side, center.x, center.y);

                for (int i = 0; i < count; i++)
                {
                    int k = (i + 1) % count;
                    Vector3 a = new Vector3(halfThickness[i] * side, outline[i].x, outline[i].y);
                    Vector3 b = new Vector3(halfThickness[k] * side, outline[k].x, outline[k].y);
                    Triangle(material, hub, a, b, center / TileSize, outline[i] / TileSize, outline[k] / TileSize, Vector3.right * side, smoothAngle);
                }
            }

            for (int i = 0; i < count; i++)
            {
                int k = (i + 1) % count;

                if (halfThickness[i] <= 0f && halfThickness[k] <= 0f)
                    continue;

                Vector3 a = new Vector3(halfThickness[i], outline[i].x, outline[i].y);
                Vector3 b = new Vector3(halfThickness[k], outline[k].x, outline[k].y);
                Vector3 c = new Vector3(-halfThickness[k], outline[k].x, outline[k].y);
                Vector3 d = new Vector3(-halfThickness[i], outline[i].x, outline[i].y);
                Vector2 middle = (outline[i] + outline[k]) * 0.5f - center;
                Vector3 outward = new Vector3(0f, middle.x, middle.y);
                Vector2 ua = new Vector2(halfThickness[i], outline[i].x + outline[i].y) / TileSize;
                Vector2 ub = new Vector2(halfThickness[k], outline[k].x + outline[k].y) / TileSize;
                Vector2 uc = new Vector2(-halfThickness[k], outline[k].x + outline[k].y) / TileSize;
                Vector2 ud = new Vector2(-halfThickness[i], outline[i].x + outline[i].y) / TileSize;

                Triangle(material, a, b, c, ua, ub, uc, outward, smoothAngle);
                Triangle(material, a, c, d, ua, uc, ud, outward, smoothAngle);
            }
        }

        /// Flat disc or dome facing +Z with UVs stretched over its square: once for a painted face, uvTiles times for a tiling one.
        public void Face(Material material, float radius, float z, float bulge, int sides, int rings, bool isFront, float uvTiles = 1f)
        {
            for (int ring = 0; ring < rings; ring++)
            {
                float inner = (float)ring / rings;
                float outer = (ring + 1f) / rings;

                for (int i = 0; i < sides; i++)
                {
                    float from = i * Mathf.PI * 2f / sides;
                    float to = (i + 1) * Mathf.PI * 2f / sides;
                    Vector3 a = FacePoint(radius, z, bulge, inner, from, out Vector2 ua);
                    Vector3 b = FacePoint(radius, z, bulge, inner, to, out Vector2 ub);
                    Vector3 c = FacePoint(radius, z, bulge, outer, to, out Vector2 uc);
                    Vector3 d = FacePoint(radius, z, bulge, outer, from, out Vector2 ud);
                    Vector3 outward = isFront ? Vector3.forward : Vector3.back;

                    Triangle(material, a, b, c, ua * uvTiles, ub * uvTiles, uc * uvTiles, outward, Soft);
                    Triangle(material, a, c, d, ua * uvTiles, uc * uvTiles, ud * uvTiles, outward, Soft);
                }
            }
        }

        public void Triangle(Material material, Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 outward, float smoothAngle)
        {
            a = Matrix.MultiplyPoint3x4(a);
            b = Matrix.MultiplyPoint3x4(b);
            c = Matrix.MultiplyPoint3x4(c);
            Vector3 normal = Vector3.Cross(b - a, c - a);

            if (normal.sqrMagnitude < 1e-16f)
                return;

            if (Vector3.Dot(normal, Matrix.MultiplyVector(outward)) < 0f)
            {
                (b, c) = (c, b);
                (ub, uc) = (uc, ub);
            }

            int index = _materials.IndexOf(material);

            if (index < 0)
            {
                index = _materials.Count;
                _materials.Add(material);
            }

            _positions.Add(a);
            _positions.Add(b);
            _positions.Add(c);
            _uvs.Add(ua);
            _uvs.Add(ub);
            _uvs.Add(uc);
            _smoothing.Add(Mathf.Cos(smoothAngle * Mathf.Deg2Rad));
            _triangleMaterials.Add(index);
        }

        /// Bakes the parts into the mesh asset of that name and hangs it under the parent.
        public GameObject Attach(Transform parent, string assetName, string childName = "Model")
        {
            GameObject go = BattleEditorUtility.CreateChild(childName, parent);
            go.AddComponent<MeshFilter>().sharedMesh = Bake(assetName);
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = _materials.ToArray();

            return go;
        }

        private static Vector3 FacePoint(float radius, float z, float bulge, float t, float angle, out Vector2 uv)
        {
            Vector2 flat = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * t;
            uv = flat * 0.5f + Vector2.one * 0.5f;

            return new Vector3(flat.x * radius, flat.y * radius, z + bulge * (1f - t * t));
        }

        private void Cap(Material material, Vector3[] ring, Vector3 center, Vector3 outward)
        {
            if (outward == Vector3.zero)
                return;

            Vector3 axisU = (ring[0] - center).normalized;
            Vector3 axisV = Vector3.Cross(outward.normalized, axisU);

            for (int j = 0; j < ring.Length; j++)
            {
                Vector3 a = ring[j];
                Vector3 b = ring[(j + 1) % ring.Length];
                Vector2 ua = new Vector2(Vector3.Dot(a - center, axisU), Vector3.Dot(a - center, axisV)) / TileSize;
                Vector2 ub = new Vector2(Vector3.Dot(b - center, axisU), Vector3.Dot(b - center, axisV)) / TileSize;
                Triangle(material, center, a, b, Vector2.zero, ua, ub, outward, Hard);
            }
        }

        private Mesh Bake(string assetName)
        {
            int triangleCount = _triangleMaterials.Count;
            Vector3[] faceNormals = new Vector3[triangleCount];
            Dictionary<Vector3Int, List<int>> corners = new();

            for (int i = 0; i < triangleCount; i++)
            {
                faceNormals[i] = Vector3.Cross(_positions[i * 3 + 1] - _positions[i * 3], _positions[i * 3 + 2] - _positions[i * 3]);

                for (int j = 0; j < 3; j++)
                {
                    Vector3Int key = Quantize(_positions[i * 3 + j]);

                    if (!corners.TryGetValue(key, out List<int> list))
                        corners[key] = list = new List<int>();

                    list.Add(i);
                }
            }

            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int>[] indices = new List<int>[_materials.Count];
            Dictionary<(Vector3Int, Vector3Int, Vector2Int), int> welded = new();

            for (int i = 0; i < indices.Length; i++)
                indices[i] = new List<int>();

            for (int i = 0; i < triangleCount; i++)
            {
                Vector3 face = faceNormals[i].normalized;

                for (int j = 0; j < 3; j++)
                {
                    Vector3 position = _positions[i * 3 + j];
                    Vector2 uv = _uvs[i * 3 + j];
                    Vector3 normal = Vector3.zero;

                    // Area-weighted average over the neighbours within the smoothing angle of this face.
                    foreach (int other in corners[Quantize(position)])
                    {
                        if (Vector3.Dot(faceNormals[other].normalized, face) >= _smoothing[i])
                            normal += faceNormals[other];
                    }

                    normal.Normalize();
                    (Vector3Int, Vector3Int, Vector2Int) key = (Quantize(position), Quantize(normal * 0.01f),
                        new Vector2Int(Mathf.RoundToInt(uv.x * 4096f), Mathf.RoundToInt(uv.y * 4096f)));

                    if (!welded.TryGetValue(key, out int index))
                    {
                        index = vertices.Count;
                        welded[key] = index;
                        vertices.Add(position);
                        normals.Add(normal);
                        uvs.Add(uv);
                    }

                    indices[_triangleMaterials[i]].Add(index);
                }
            }

            BattleEditorUtility.EnsureFolder(Folder);
            string path = $"{Folder}/{assetName}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;

            if (isNew)
                mesh = new Mesh { name = assetName };
            else
                mesh.Clear();

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = indices.Length;

            for (int i = 0; i < indices.Length; i++)
                mesh.SetTriangles(indices[i], i);

            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            if (isNew)
                AssetDatabase.CreateAsset(mesh, path);
            else
                EditorUtility.SetDirty(mesh);

            return mesh;
        }

        private static Vector3Int Quantize(Vector3 value)
        {
            return new Vector3Int(Mathf.RoundToInt(value.x * 20000f), Mathf.RoundToInt(value.y * 20000f), Mathf.RoundToInt(value.z * 20000f));
        }
    }
}
