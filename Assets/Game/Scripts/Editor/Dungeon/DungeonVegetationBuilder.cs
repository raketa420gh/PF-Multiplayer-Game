using System.Collections.Generic;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Procedural plants and stones for the cursed village: spruces, broadleaf trees, dead trees, drowned swamp trees, bushes,
    /// boulders, standing stones, stumps and logs.
    /// Trees are terrain tree prototypes (mesh + material on the root, a capsule around the trunk); foliage is alpha-tested cards
    /// with normals pointing out of the crown, so a clump is shaded like one volume.
    internal static class DungeonVegetationBuilder
    {
        public const string PrefabsFolder = DungeonPropBuilder.PrefabsFolder + "/Vegetation";
        public const int Variants = 4;

        public static Material Bark => DungeonPropBuilder.Textured("Bark", "Bark", 1f, 0.08f);
        public static Material RockMaterial => DungeonPropBuilder.Textured("Rock", "Rock", 0.35f, 0.12f);
        public static Material Needles => Foliage("Needles", "SpruceSpray", new Color(0.85f, 0.9f, 0.85f));
        public static Material Leaves => Foliage("Leaves", "LeafCluster", new Color(0.85f, 0.85f, 0.8f));

        /// Two submeshes (bark, foliage) with world-scaled bark UVs.
        private sealed class PlantMesh
        {
            private readonly List<Vector3> _vertices = new();
            private readonly List<Vector3> _normals = new();
            private readonly List<Vector2> _uvs = new();
            private readonly List<int>[] _triangles = { new(), new() };

            /// Tapered tube through the points; radii per point.
            public void Tube(IReadOnlyList<Vector3> points, IReadOnlyList<float> radii, int segments)
            {
                int start = _vertices.Count;
                float length = 0f;

                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 forward = (i < points.Count - 1 ? points[i + 1] - points[i] : points[i] - points[i - 1]).normalized;
                    Vector3 side = Vector3.Cross(forward, Mathf.Abs(forward.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                    Vector3 other = Vector3.Cross(forward, side);

                    if (i > 0)
                        length += (points[i] - points[i - 1]).magnitude;

                    for (int s = 0; s <= segments; s++)
                    {
                        float angle = s / (float)segments * Mathf.PI * 2f;
                        Vector3 normal = side * Mathf.Cos(angle) + other * Mathf.Sin(angle);
                        _vertices.Add(points[i] + normal * radii[i]);
                        _normals.Add(normal);
                        _uvs.Add(new Vector2(s / (float)segments * Mathf.Max(1f, Mathf.Round(radii[0] * 8f)), length * 0.8f));
                    }
                }

                for (int i = 0; i < points.Count - 1; i++)
                {
                    for (int s = 0; s < segments; s++)
                    {
                        int a = start + i * (segments + 1) + s;
                        int b = a + segments + 1;
                        _triangles[0].AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                    }
                }
            }

            /// Foliage card around its centre; both faces render through the material, the normal is given.
            public void Card(Vector3 center, Vector3 right, Vector3 up, Vector3 normal, bool fromBase = false)
            {
                int start = _vertices.Count;
                Vector3 bottom = fromBase ? center : center - up;
                Vector3 top = fromBase ? center + up * 2f : center + up;
                _vertices.AddRange(new[] { bottom - right, top - right, top + right, bottom + right });
                _uvs.AddRange(new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });

                for (int i = 0; i < 4; i++)
                    _normals.Add(normal.normalized);

                _triangles[1].AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            public Mesh Save(string name)
            {
                string path = $"{DungeonMeshBuilder.Folder}/{name}.asset";
                BattleEditorUtility.EnsureFolder(DungeonMeshBuilder.Folder);
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

                if (mesh == null)
                {
                    mesh = new Mesh();
                    AssetDatabase.CreateAsset(mesh, path);
                }

                mesh.Clear();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uvs);
                mesh.subMeshCount = _triangles[1].Count > 0 ? 2 : 1;
                mesh.SetTriangles(_triangles[0], 0);

                if (mesh.subMeshCount == 2)
                    mesh.SetTriangles(_triangles[1], 1);

                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                mesh.name = name;
                EditorUtility.SetDirty(mesh);

                return mesh;
            }
        }

        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(PrefabsFolder);

            for (int i = 0; i < Variants; i++)
            {
                SaveTree($"Spruce{i}", Spruce(i), Needles, 0.3f);
                SaveTree($"Broadleaf{i}", Broadleaf(i), Leaves, 0.32f);
                SaveTree($"DeadTree{i}", DeadTree(i), null, 0.25f);
                SaveTree($"Bush{i}", Bush(i), Leaves, 0f);
                SaveTree($"SwampTree{i}", SwampTree(i), null, 0.45f);
                SaveRock($"Menhir{i}", i + 40, new Vector3(1.3f, 4.4f + i * 0.5f, 0.9f));
                SaveRock($"Boulder{i}", i, new Vector3(2.2f, 1.4f, 1.8f) * (1f + i * 0.35f));
                SaveRock($"Stone{i}", i + 10, new Vector3(0.7f, 0.45f, 0.6f) * (1f + i * 0.3f));
            }

            SaveRock("Cliff0", 31, new Vector3(9f, 7f, 7f));
            SaveRock("Cliff1", 32, new Vector3(12f, 9f, 8f));
            SaveLog("Stump", true);
            SaveLog("Log", false);
            Debug.Log($"[{nameof(DungeonVegetationBuilder)}] Vegetation built in {PrefabsFolder}");
        }

        public static GameObject Load(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/{name}.prefab");

            if (prefab == null)
                throw new System.ArgumentException($"Vegetation prefab '{name}' not found");

            return prefab;
        }

        /// Tall dark spruce: a tapering trunk and whorls of drooping branch sprays, shorter towards the top.
        private static Mesh Spruce(int seed)
        {
            System.Random random = new System.Random(100 + seed);
            PlantMesh mesh = new PlantMesh();
            float height = 15f + seed * 2.2f;
            float radius = 0.26f + seed * 0.04f;
            Vector3 crown = Vector3.up * height * 0.45f;
            mesh.Tube(new[] { Vector3.down * 0.3f, Vector3.up * height * 0.5f, Vector3.up * height }, new[] { radius * 1.25f, radius * 0.6f, 0.03f }, 8);

            for (float y = 2.2f + (float)random.NextDouble(); y < height - 0.4f; y += 0.42f + (float)random.NextDouble() * 0.12f)
            {
                float t = y / height;
                float length = Mathf.Pow(1f - t, 0.85f) * 4.4f + 0.5f;
                int count = 5 + random.Next(3);
                float offset = (float)random.NextDouble() * 360f;

                for (int i = 0; i < count; i++)
                {
                    float yaw = offset + i * 360f / count + ((float)random.NextDouble() - 0.5f) * 30f;
                    float droop = 12f + (float)random.NextDouble() * 18f + (1f - t) * 10f;
                    Vector3 direction = Quaternion.Euler(droop, yaw, 0f) * Vector3.forward;
                    Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                    Vector3 roll = Quaternion.AngleAxis(((float)random.NextDouble() - 0.5f) * 40f, direction) * side;
                    Vector3 basePoint = Vector3.up * y;
                    Vector3 normal = Vector3.Lerp(Vector3.up, (basePoint + direction * length * 0.5f - crown).normalized, 0.6f);
                    mesh.Card(basePoint, roll * length * 0.42f, direction * length * 0.5f, normal, true);
                }
            }

            mesh.Card(Vector3.up * (height - 0.6f), Vector3.right * 0.5f, Vector3.up * 0.8f, Vector3.up, true);
            mesh.Card(Vector3.up * (height - 0.6f), Vector3.forward * 0.5f, Vector3.up * 0.8f, Vector3.up, true);

            return mesh.Save($"Spruce{seed}");
        }

        /// Old oak-like tree: a short bent trunk splitting into limbs, leaf clumps at the ends of the twigs.
        private static Mesh Broadleaf(int seed)
        {
            System.Random random = new System.Random(200 + seed);
            PlantMesh mesh = new PlantMesh();
            float trunk = 3.5f + seed * 0.6f;
            Vector3 top = new Vector3(Rand(random, 0.6f), trunk, Rand(random, 0.6f));
            float radius = 0.38f + seed * 0.05f;
            mesh.Tube(new[] { Vector3.down * 0.3f, Vector3.up * trunk * 0.5f + new Vector3(Rand(random, 0.3f), 0f, Rand(random, 0.3f)), top }, new[] { radius * 1.3f, radius, radius * 0.8f }, 10);
            Vector3 crown = top + Vector3.up * 3.5f;
            List<(Vector3 point, Vector3 direction)> tips = new();
            int limbs = 4 + random.Next(2);

            for (int i = 0; i < limbs; i++)
            {
                Vector3 direction = Quaternion.Euler(-35f - (float)random.NextDouble() * 30f, i * 360f / limbs + Rand(random, 25f), 0f) * Vector3.forward;
                Branch(mesh, random, top, direction, 5f + (float)random.NextDouble() * 2f, radius * 0.6f, 2, tips);
            }

            foreach ((Vector3 point, Vector3 direction) in tips)
            {
                for (int k = 0; k < 2; k++)
                {
                    Vector3 center = point + new Vector3(Rand(random, 0.8f), Rand(random, 0.6f), Rand(random, 0.8f));
                    Quaternion spin = Quaternion.Euler(Rand(random, 180f), Rand(random, 180f), Rand(random, 180f));
                    float size = 1.3f + (float)random.NextDouble() * 0.8f;
                    mesh.Card(center, spin * Vector3.right * size, spin * Vector3.up * size, Vector3.Lerp((center - crown).normalized, Vector3.up, 0.3f));
                }
            }

            return mesh.Save($"Broadleaf{seed}");
        }

        /// Bare, gnarled tree of the swamp and the graveyard: thin crooked limbs splitting three times.
        private static Mesh DeadTree(int seed)
        {
            System.Random random = new System.Random(300 + seed);
            PlantMesh mesh = new PlantMesh();
            float trunk = 2.5f + seed * 0.7f;
            float radius = 0.24f + seed * 0.04f;
            Vector3 lean = new Vector3(Rand(random, 0.9f), 0f, Rand(random, 0.9f));
            Vector3 top = Vector3.up * trunk + lean;
            mesh.Tube(new[] { Vector3.down * 0.3f, Vector3.up * trunk * 0.5f + lean * 0.3f + new Vector3(Rand(random, 0.2f), 0f, Rand(random, 0.2f)), top }, new[] { radius * 1.4f, radius, radius * 0.75f }, 8);
            List<(Vector3, Vector3)> tips = new();
            int limbs = 3 + random.Next(2);

            for (int i = 0; i < limbs; i++)
            {
                Vector3 direction = Quaternion.Euler(-30f - (float)random.NextDouble() * 35f, i * 360f / limbs + Rand(random, 35f), 0f) * Vector3.forward;
                Branch(mesh, random, top, direction, 3.5f + (float)random.NextDouble() * 2.5f, radius * 0.55f, 3, tips);
            }

            return mesh.Save($"DeadTree{seed}");
        }

        /// Huge drowned tree of the bog: a thick leaning trunk on a flare of arching roots, broken top, long crooked bare limbs.
        private static Mesh SwampTree(int seed)
        {
            System.Random random = new System.Random(500 + seed);
            PlantMesh mesh = new PlantMesh();
            float trunk = 5f + seed * 1.1f;
            float radius = 0.5f + seed * 0.08f;
            Vector3 lean = new Vector3(Rand(random, 1.4f), 0f, Rand(random, 1.4f));
            Vector3 middle = Vector3.up * trunk * 0.5f + lean * 0.35f + new Vector3(Rand(random, 0.4f), 0f, Rand(random, 0.4f));
            Vector3 top = Vector3.up * trunk + lean;
            mesh.Tube(new[] { Vector3.down * 0.4f, Vector3.up * 1.2f, middle, top, top + Vector3.up * 1.2f + lean * 0.2f }, new[] { radius * 1.6f, radius * 1.15f, radius, radius * 0.75f, radius * 0.35f }, 10);
            int roots = 6 + random.Next(3);

            for (int i = 0; i < roots; i++)
            {
                float yaw = i * 360f / roots + Rand(random, 20f);
                Vector3 out1 = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                float reach = 1.6f + (float)random.NextDouble() * 1.6f;
                Vector3 start = Vector3.up * (0.9f + (float)random.NextDouble() * 0.7f) + out1 * radius * 0.7f;
                Vector3 arch = out1 * reach * 0.55f + Vector3.up * (0.5f + (float)random.NextDouble() * 0.4f);
                Vector3 end = out1 * reach + Vector3.down * 0.6f;
                mesh.Tube(new[] { start, arch, end }, new[] { radius * 0.38f, radius * 0.28f, radius * 0.12f }, 6);
            }

            List<(Vector3, Vector3)> tips = new();
            int limbs = 4 + random.Next(3);

            for (int i = 0; i < limbs; i++)
            {
                float t = 0.55f + i / (float)limbs * 0.45f;
                Vector3 from = Vector3.Lerp(middle, top, (t - 0.5f) * 2f);
                Vector3 direction = Quaternion.Euler(-12f - (float)random.NextDouble() * 40f, i * 360f / limbs + Rand(random, 40f), 0f) * Vector3.forward;
                Branch(mesh, random, from, direction, 4f + (float)random.NextDouble() * 3.5f, radius * 0.45f, 3, tips);
            }

            return mesh.Save($"SwampTree{seed}");
        }

        private static Mesh Bush(int seed)
        {
            System.Random random = new System.Random(400 + seed);
            PlantMesh mesh = new PlantMesh();
            float radius = 0.9f + seed * 0.25f;
            Vector3 center = Vector3.up * radius * 0.55f;

            for (int i = 0; i < 14 + seed * 3; i++)
            {
                Vector3 offset = new Vector3(Rand(random, radius), Rand(random, radius * 0.5f), Rand(random, radius));
                Quaternion spin = Quaternion.Euler(Rand(random, 180f), Rand(random, 180f), Rand(random, 180f));
                float size = 0.7f + (float)random.NextDouble() * 0.4f;
                mesh.Card(center + offset, spin * Vector3.right * size, spin * Vector3.up * size, Vector3.Lerp(offset.normalized, Vector3.up, 0.4f));
            }

            return mesh.Save($"Bush{seed}");
        }

        /// A crooked limb in segments that bends at random, splitting in two until depth runs out; collects its twig tips.
        private static void Branch(PlantMesh mesh, System.Random random, Vector3 start, Vector3 direction, float length, float radius, int depth, List<(Vector3, Vector3)> tips)
        {
            const int segments = 4;
            List<Vector3> points = new() { start };
            List<float> radii = new() { radius };
            Vector3 point = start;

            for (int i = 1; i <= segments; i++)
            {
                direction = (direction + new Vector3(Rand(random, 0.35f), Rand(random, 0.2f) + 0.05f, Rand(random, 0.35f))).normalized;
                point += direction * length / segments;
                points.Add(point);
                radii.Add(Mathf.Lerp(radius, radius * 0.35f, i / (float)segments));
            }

            mesh.Tube(points, radii, depth > 1 ? 6 : 4);

            if (depth <= 0 || radius < 0.03f)
            {
                tips.Add((point, direction));

                return;
            }

            for (int i = 0; i < 2; i++)
            {
                int at = 2 + random.Next(segments - 1);
                Vector3 fork = Quaternion.AngleAxis(Rand(random, 50f) + (i == 0 ? 30f : -30f), Vector3.up) * Quaternion.AngleAxis(Rand(random, 30f), Vector3.Cross(direction, Vector3.up)) * direction;
                Branch(mesh, random, points[at], fork, length * 0.6f, radii[at] * 0.8f, depth - 1, tips);
            }
        }

        private static void SaveTree(string name, Mesh mesh, Material foliage, float trunk)
        {
            GameObject root = new GameObject(name);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            Material bark = Bark;
            bark.enableInstancing = true;
            renderer.sharedMaterials = foliage != null ? new[] { bark, foliage } : new[] { bark };
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            // A LOD group lets the terrain draw a plain mesh tree (no Nature shader, no billboard) and drop it when tiny.
            root.AddComponent<LODGroup>().SetLODs(new[] { new LOD(0.012f, new Renderer[] { renderer }) });

            if (trunk > 0f)
            {
                CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
                capsule.radius = trunk;
                capsule.height = 6f;
                capsule.center = Vector3.up * 3f;
            }

            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        /// Boulder: a sphere dented by noise and squashed to its size, its texture projected along the dominant axis.
        private static void SaveRock(string name, int seed, Vector3 size)
        {
            Mesh mesh = Boulder(name, seed, size);
            GameObject root = new GameObject(name);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            Material rock = RockMaterial;
            rock.enableInstancing = true;
            renderer.sharedMaterial = rock;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            root.AddComponent<MeshCollider>().sharedMesh = mesh;
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        private static Mesh Boulder(string name, int seed, Vector3 size)
        {
            const int rings = 12;
            const int sectors = 18;
            List<Vector3> vertices = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();
            float offset = seed * 13.7f;

            for (int r = 0; r <= rings; r++)
            {
                float phi = r / (float)rings * Mathf.PI;

                for (int s = 0; s <= sectors; s++)
                {
                    float theta = s / (float)sectors * Mathf.PI * 2f;
                    Vector3 direction = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    float bump = DungeonTextureBuilder.Noise(direction.x * 0.9f + offset, direction.z * 0.9f + direction.y * 0.7f + offset, 1.6f, 3);
                    // Flat facets: quantised noise gives broken planes like split stone.
                    float facet = Mathf.Round(bump * 5f) / 5f;
                    float scale = 0.75f + Mathf.Lerp(bump, facet, 0.5f) * 0.5f;
                    Vector3 point = Vector3.Scale(direction * scale, size * 0.5f);
                    point.y = Mathf.Max(point.y, -size.y * 0.25f) + size.y * 0.3f;
                    vertices.Add(point);
                    uvs.Add(new Vector2(theta / Mathf.PI * size.x * 0.5f, point.y * 0.7f));
                }
            }

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sectors; s++)
                {
                    int a = r * (sectors + 1) + s;
                    int b = a + sectors + 1;
                    triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            }

            string path = $"{DungeonMeshBuilder.Folder}/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh.name = name;
            EditorUtility.SetDirty(mesh);

            return mesh;
        }

        private static void SaveLog(string name, bool isStump)
        {
            PlantMesh mesh = new PlantMesh();

            if (isStump)
                mesh.Tube(new[] { Vector3.down * 0.2f, Vector3.up * 0.5f, Vector3.up * 0.65f }, new[] { 0.5f, 0.4f, 0.36f }, 10);
            else
                mesh.Tube(new[] { new Vector3(0f, 0.3f, -2.5f), new Vector3(0.1f, 0.32f, 0f), new Vector3(0f, 0.28f, 2.5f) }, new[] { 0.34f, 0.3f, 0.26f }, 10);

            GameObject root = new GameObject(name);
            Mesh saved = mesh.Save(name);
            root.AddComponent<MeshFilter>().sharedMesh = saved;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Bark;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = saved.bounds.center;
            box.size = saved.bounds.size;
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        private static Material Foliage(string name, string texture, Color tint)
        {
            string path = $"{DungeonPropBuilder.MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{DungeonTextureBuilder.Folder}/{texture}.png"));
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", 0.15f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.45f);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            return material;
        }

        private static float Rand(System.Random random, float range)
        {
            return ((float)random.NextDouble() * 2f - 1f) * range;
        }
    }
}
