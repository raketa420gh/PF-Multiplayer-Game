using System;
using System.Collections.Generic;
using System.IO;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Dungeon
{
    /// Generates the particle textures, materials, meshes and light template of the ability effects and the config that holds them.
    internal static class AbilityVfxBuilder
    {
        public const string ConfigPath = "Assets/Game/Configs/Dungeon/AbilityVfx.asset";
        private const string TexturesFolder = "Assets/Game/Textures/Vfx";
        private const string MaterialsFolder = "Assets/Game/Materials/Vfx";
        private const string MeshesFolder = "Assets/Game/Meshes/Vfx";
        private const string PrefabsFolder = "Assets/Game/Prefabs/Vfx";
        private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const int Size = 128;

        private sealed class MeshWriter
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<Vector2> Uvs = new();
            public readonly List<int>[] Triangles;

            public MeshWriter(int submeshes)
            {
                Triangles = new List<int>[submeshes];

                for (int i = 0; i < submeshes; i++)
                    Triangles[i] = new List<int>();
            }

            /// Surface of revolution around Y; the profile runs (radius, height) from the bottom up.
            public void Lathe(int submesh, Vector2[] profile, int segments = 16)
            {
                int start = Vertices.Count;
                int row = segments + 1;

                for (int i = 0; i < profile.Length; i++)
                {
                    for (int s = 0; s <= segments; s++)
                    {
                        float angle = s / (float)segments * Mathf.PI * 2f;
                        Vertices.Add(new Vector3(Mathf.Cos(angle) * profile[i].x, profile[i].y, Mathf.Sin(angle) * profile[i].x));
                        Uvs.Add(new Vector2(s / (float)segments, i / (profile.Length - 1f)));
                    }
                }

                for (int i = 0; i < profile.Length - 1; i++)
                {
                    for (int s = 0; s < segments; s++)
                    {
                        int a = start + i * row + s;
                        Quad(submesh, a, a + row, a + row + 1, a + 1);
                    }
                }
            }

            public void Box(int submesh, Vector3 center, Vector3 size)
            {
                Vector3 half = size * 0.5f;

                foreach (Vector3 normal in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
                {
                    Vector3 up = Mathf.Abs(normal.y) > 0.5f ? Vector3.forward : Vector3.up;
                    Vector3 right = Vector3.Cross(normal, up);
                    Vector3 Corner(float x, float y) => center + Vector3.Scale(normal + right * x + up * y, half);
                    int a = Vertices.Count;
                    Vertices.AddRange(new[] { Corner(-1f, -1f), Corner(-1f, 1f), Corner(1f, 1f), Corner(1f, -1f) });
                    Uvs.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
                    Quad(submesh, a, a + 1, a + 2, a + 3);
                }
            }

            /// Two-sided sheet from a grid of points (columns x rows).
            public void Sheet(int submesh, Vector3[,] grid)
            {
                int columns = grid.GetLength(0);
                int rows = grid.GetLength(1);

                for (int side = 0; side < 2; side++)
                {
                    int start = Vertices.Count;

                    for (int x = 0; x < columns; x++)
                    {
                        for (int y = 0; y < rows; y++)
                        {
                            Vertices.Add(grid[x, y]);
                            Uvs.Add(new Vector2(x / (columns - 1f), y / (rows - 1f)));
                        }
                    }

                    for (int x = 0; x < columns - 1; x++)
                    {
                        for (int y = 0; y < rows - 1; y++)
                        {
                            int a = start + x * rows + y;

                            if (side == 0)
                                Quad(submesh, a, a + 1, a + rows + 1, a + rows);
                            else
                                Quad(submesh, a, a + rows, a + rows + 1, a + 1);
                        }
                    }
                }
            }

            public void Write(Mesh mesh)
            {
                mesh.Clear();
                mesh.SetVertices(Vertices);
                mesh.SetUVs(0, Uvs);
                mesh.subMeshCount = Triangles.Length;

                for (int i = 0; i < Triangles.Length; i++)
                    mesh.SetTriangles(Triangles[i], i);

                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }

            private void Quad(int submesh, int a, int b, int c, int d)
            {
                Triangles[submesh].AddRange(new[] { a, b, c, a, c, d });
            }
        }

        [MenuItem("Tools/Game/Dungeon/Build Ability Vfx")]
        public static void BuildMenu()
        {
            Build();
        }

        /// The built config, built now when there is none yet.
        public static AbilityVfxConfig Load()
        {
            AbilityVfxConfig config = AssetDatabase.LoadAssetAtPath<AbilityVfxConfig>(ConfigPath);

            return config != null ? config : Build();
        }

        public static AbilityVfxConfig Build()
        {
            foreach (string folder in new[] { TexturesFolder, MaterialsFolder, MeshesFolder, PrefabsFolder, Path.GetDirectoryName(ConfigPath)?.Replace('\\', '/') })
                BattleEditorUtility.EnsureFolder(folder);

            Texture2D glow = Texture("Glow", Size, (u, v) => Alpha(Mathf.Pow(Mathf.Clamp01(1f - Length(u, v)), 2.2f)));
            Texture2D smoke = Texture("Smoke", Size, Smoke);
            Texture2D flame = Texture("Flame", Size, Flame);
            Texture2D spark = Texture("Spark", 64, (u, v) =>
            {
                float fall = Mathf.Clamp01(1f - Length(u, v));

                return Alpha(Mathf.Clamp01(Mathf.Pow(fall, 4f) * 1.2f + Mathf.Pow(fall, 1.5f) * 0.25f));
            });
            Texture2D ember = Texture("Ember", 64, (u, v) => Alpha(Mathf.Clamp01((1f - Length(u, v) * 1.25f) * 1.6f) * Mathf.Clamp01(0.4f + 0.9f * Fbm(u * 4f + 9f, v * 4f + 1f))));
            Texture2D ring = Texture("Ring", 256, (u, v) =>
            {
                float d = Length(u, v);

                return Alpha(Mathf.Clamp01(Mathf.Pow(Band(d, 0.84f, 0.07f), 1.5f) + 0.35f * Mathf.Pow(Band(d, 0.8f, 0.2f), 2f)));
            });
            Texture2D rune = Texture("Rune", 256, Rune);
            Texture2D sigil = Texture("Sigil", 256, Sigil);
            Texture2D crystal = Texture("Crystal", Size, Crystal);
            Texture2D drop = Texture("Drop", 64, Drop);
            Texture2D decal = Texture("Decal", Size, Decal);
            Texture2D beam = Texture("Beam", 64, (u, v) => Alpha(Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v)), 2f)));

            AbilityVfxConfig config = BattleEditorUtility.LoadOrCreate<AbilityVfxConfig>(ConfigPath);
            SerializedObject so = new SerializedObject(config);
            BattleEditorUtility.Set(so, "_glow", Particle("Glow", glow, true));
            BattleEditorUtility.Set(so, "_flame", Particle("Flame", flame, true));
            BattleEditorUtility.Set(so, "_spark", Particle("Spark", spark, true));
            BattleEditorUtility.Set(so, "_ember", Particle("Ember", ember, true));
            BattleEditorUtility.Set(so, "_ring", Particle("Ring", ring, true));
            BattleEditorUtility.Set(so, "_rune", Particle("Rune", rune, true));
            BattleEditorUtility.Set(so, "_runeDark", Particle("RuneDark", rune, false));
            BattleEditorUtility.Set(so, "_sigil", Particle("Sigil", sigil, true));
            BattleEditorUtility.Set(so, "_crystal", Particle("Crystal", crystal, false));
            BattleEditorUtility.Set(so, "_smoke", Particle("Smoke", smoke, false));
            BattleEditorUtility.Set(so, "_drop", Particle("Drop", drop, false));
            BattleEditorUtility.Set(so, "_decal", Particle("Decal", decal, false));
            BattleEditorUtility.Set(so, "_beam", Particle("Beam", beam, true));
            BattleEditorUtility.Set(so, "_solid", Lit("Solid", Color.white, 0.35f, false));
            BattleEditorUtility.Set(so, "_glass", Lit("Glass", new Color(0.85f, 0.95f, 0.9f, 0.28f), 0.95f, true));
            BattleEditorUtility.Set(so, "_light", LightTemplate());
            BattleEditorUtility.Set(so, "_flaskMesh", SaveMesh("Flask", FlaskMesh()));
            BattleEditorUtility.Set(so, "_netMesh", SaveMesh("Net", NetMesh()));
            BattleEditorUtility.Set(so, "_bannerMesh", SaveMesh("Banner", BannerMesh()));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            return config;
        }

        /// Adds (or re-wires) the status effect looks on a fighter prefab root being built.
        public static void Attach(GameObject fighterRoot, StatusEffectComponent effects, AbilityVfxConfig config)
        {
            StatusEffectVisualComponent visual = fighterRoot.GetComponent<StatusEffectVisualComponent>();

            if (visual == null)
                visual = fighterRoot.AddComponent<StatusEffectVisualComponent>();

            Animator animator = fighterRoot.GetComponentInChildren<Animator>();
            Transform head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            SerializedObject so = new SerializedObject(visual);
            BattleEditorUtility.Set(so, "_effects", effects);
            BattleEditorUtility.Set(so, "_config", config);
            BattleEditorUtility.Set(so, "_head", head);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Texture2D Texture(string name, int size, Func<float, float, Color> sampler)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = sampler((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            string path = $"{TexturesFolder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material LoadMaterial(string name, string shader)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                material.shader = Shader.Find(shader);

                return material;
            }

            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);

            return material;
        }

        /// Transparent two-sided particle material: additive (SrcAlpha, One) or alpha blended (SrcAlpha, OneMinusSrcAlpha).
        private static Material Particle(string name, Texture2D texture, bool isAdditive)
        {
            Material material = LoadMaterial(name, ParticleShader);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            // BaseShaderGUI.BlendMode: Alpha = 0, Premultiply = 1, Additive = 2, Multiply = 3.
            material.SetFloat("_Blend", isAdditive ? 2f : 0f);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_ColorMode", 0f);
            material.SetFloat("_AlphaClip", 0f);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material Lit(string name, Color color, float smoothness, bool isTransparent)
        {
            Material material = LoadMaterial(name, LitShader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Surface", isTransparent ? 1f : 0f);
            material.SetFloat("_Blend", 0f);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            EditorUtility.SetDirty(material);

            return material;
        }

        /// Point light the particle lights copy: unit range and intensity, scaled per effect by the lights module.
        private static Light LightTemplate()
        {
            GameObject go = new GameObject("VfxLight");
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 1f;
            light.intensity = 1f;
            light.color = Color.white;
            light.shadows = LightShadows.None;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsFolder}/VfxLight.prefab");
            Object.DestroyImmediate(go);

            return prefab.GetComponent<Light>();
        }

        private static Mesh SaveMesh(string name, MeshWriter writer)
        {
            string path = $"{MeshesFolder}/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh != null)
            {
                writer.Write(mesh);
                EditorUtility.SetDirty(mesh);

                return mesh;
            }

            mesh = new Mesh { name = name };
            writer.Write(mesh);
            AssetDatabase.CreateAsset(mesh, path);

            return mesh;
        }

        private static MeshWriter FlaskMesh()
        {
            MeshWriter writer = new MeshWriter(3);
            writer.Lathe(0, new Vector2[]
            {
                new(0f, -0.07f), new(0.03f, -0.07f), new(0.044f, -0.06f), new(0.05f, -0.035f), new(0.05f, 0f), new(0.044f, 0.022f),
                new(0.028f, 0.038f), new(0.015f, 0.048f), new(0.014f, 0.085f), new(0.019f, 0.088f), new(0.019f, 0.095f), new(0.013f, 0.095f)
            });
            writer.Lathe(1, new Vector2[] { new(0f, -0.066f), new(0.028f, -0.066f), new(0.041f, -0.057f), new(0.046f, -0.034f), new(0.046f, 0f), new(0.04f, 0.018f), new(0f, 0.018f) });
            writer.Lathe(2, new Vector2[] { new(0f, 0.078f), new(0.012f, 0.078f), new(0.012f, 0.106f), new(0.01f, 0.11f), new(0f, 0.11f) }, 10);

            return writer;
        }

        private static MeshWriter NetMesh()
        {
            const float half = 0.3f;
            MeshWriter writer = new MeshWriter(2);

            for (int i = 0; i < 5; i++)
            {
                float offset = Mathf.Lerp(-half, half, i / 4f);
                writer.Box(0, new Vector3(0f, 0f, offset), new Vector3(half * 2f, 0.008f, 0.01f));
                writer.Box(0, new Vector3(offset, 0.004f, 0f), new Vector3(0.01f, 0.008f, half * 2f));
            }

            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                    writer.Box(1, new Vector3(x * half, 0f, z * half), Vector3.one * 0.045f);
            }

            return writer;
        }

        private static MeshWriter BannerMesh()
        {
            const int columns = 7;
            const int rows = 10;
            const float width = 0.42f;
            const float top = 2.36f;
            MeshWriter writer = new MeshWriter(3);
            writer.Lathe(0, new Vector2[] { new(0f, -0.15f), new(0.03f, -0.1f), new(0.032f, 2.55f), new(0f, 2.55f) }, 10);
            writer.Lathe(2, new Vector2[] { new(0f, 2.55f), new(0.04f, 2.55f), new(0.045f, 2.6f), new(0.02f, 2.65f), new(0.008f, 2.85f), new(0f, 2.9f) }, 10);
            writer.Box(0, new Vector3(0f, top + 0.02f, 0f), new Vector3(width * 2f + 0.12f, 0.04f, 0.04f));
            writer.Box(2, new Vector3(-width - 0.07f, top + 0.02f, 0f), new Vector3(0.04f, 0.06f, 0.06f));
            writer.Box(2, new Vector3(width + 0.07f, top + 0.02f, 0f), new Vector3(0.04f, 0.06f, 0.06f));

            Vector3[,] cloth = new Vector3[columns, rows];

            for (int x = 0; x < columns; x++)
            {
                float px = Mathf.Lerp(-width, width, x / (columns - 1f));
                // Swallowtail: the hem rises towards the middle.
                float bottom = 1.15f + 0.28f * (1f - Mathf.Abs(px) / width);

                for (int y = 0; y < rows; y++)
                {
                    float py = Mathf.Lerp(bottom, top, y / (rows - 1f));
                    float drape = (top - py) / (top - 1.15f);
                    cloth[x, y] = new Vector3(px, py, 0.045f + 0.04f * Mathf.Sin(px * 7f + py * 3f) * drape);
                }
            }

            writer.Sheet(1, cloth);

            return writer;
        }

        private static Color Alpha(float alpha)
        {
            return new Color(1f, 1f, 1f, alpha);
        }

        private static float Length(float u, float v)
        {
            return Mathf.Sqrt(u * u + v * v);
        }

        /// Soft band of the given half width around a centre value.
        private static float Band(float value, float center, float width)
        {
            float t = Mathf.Clamp01(1f - Mathf.Abs(value - center) / width);

            return t * t * (3f - 2f * t);
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);

            return Vector2.Distance(p, a + ab * t);
        }

        private static float Fbm(float x, float y)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float frequency = 1f;

            for (int i = 0; i < 4; i++)
            {
                sum += Mathf.PerlinNoise(x * frequency + 37.1f * i + 100f, y * frequency + 11.7f * i + 100f) * amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return sum / 0.9375f;
        }

        private static Color Smoke(float u, float v)
        {
            float n = Fbm(u * 1.8f + 3f, v * 1.8f + 7f);
            float fall = Mathf.Clamp01(1f - Length(u, v));
            fall = fall * fall * (3f - 2f * fall);
            float shade = 0.7f + 0.3f * n;

            return new Color(shade, shade, shade, Mathf.Clamp01(fall * Mathf.Clamp01(n * 1.6f - 0.15f) * 1.3f));
        }

        /// Tongue of flame: round at the bottom, wavering, thinning to a tip at the top.
        private static Color Flame(float u, float v)
        {
            float t = (v + 1f) * 0.5f;
            float halfWidth = 0.6f * Mathf.Sqrt(1f - t) * Mathf.Sqrt(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f)));
            float x = u + (Fbm(t * 3f, 1.3f) - 0.5f) * 0.4f * t;
            float core = halfWidth > 0.001f ? Mathf.Clamp01(1f - Mathf.Abs(x) / halfWidth) : 0f;
            float alpha = Mathf.Pow(core, 0.8f) * Mathf.Clamp01(0.7f + 0.5f * Fbm(u * 3f, v * 3f + 2f));
            float shade = 0.85f + 0.15f * core;

            return new Color(shade, shade, shade, Mathf.Clamp01(alpha));
        }

        /// Ritual circle: double outer ring with glyph marks, an inscribed hexagram and an inner ring.
        private static Color Rune(float u, float v)
        {
            Vector2 p = new(u, v);
            float d = p.magnitude;
            float alpha = Band(d, 0.94f, 0.02f) + Band(d, 0.82f, 0.014f) + Band(d, 0.3f, 0.014f);
            float sector = (Mathf.Atan2(v, u) / (Mathf.PI * 2f) + 1f) * 24f;
            int index = Mathf.FloorToInt(sector) % 24;
            float local = sector - Mathf.Floor(sector);

            if (d > 0.85f && d < 0.91f)
            {
                alpha += (index % 3) switch
                {
                    0 => Band(local, 0.5f, 0.12f),
                    1 => Band(local, 0.35f, 0.07f) * Band(d, 0.88f, 0.02f) + Band(local, 0.65f, 0.07f) * Band(d, 0.88f, 0.02f),
                    _ => Band(local, 0.5f, 0.07f) * Band(d, 0.875f, 0.028f)
                };
            }

            for (int triangle = 0; triangle < 2; triangle++)
            {
                for (int i = 0; i < 3; i++)
                {
                    float a0 = (90f + triangle * 180f + i * 120f) * Mathf.Deg2Rad;
                    float a1 = a0 + 120f * Mathf.Deg2Rad;
                    Vector2 from = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * 0.82f;
                    Vector2 to = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 0.82f;
                    alpha += Band(Segment(p, from, to), 0f, 0.013f);
                }
            }

            return Alpha(Mathf.Clamp01(alpha) * (d < 1f ? 1f : 0f));
        }

        /// Hunter's mark: a ring with crosshair ticks, a diamond and a dot at the centre.
        private static Color Sigil(float u, float v)
        {
            Vector2 p = new(u, v);
            float d = p.magnitude;
            float alpha = Band(d, 0.62f, 0.035f) + Band(Mathf.Abs(u) + Mathf.Abs(v), 0.24f, 0.03f) + Band(d, 0f, 0.07f);

            foreach (Vector2 axis in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
                alpha += Band(Segment(p, axis * 0.42f, axis * 0.92f), 0f, 0.03f);

            return Alpha(Mathf.Clamp01(alpha));
        }

        /// Ice shard: a long vertical diamond crossed by a short one, lighter on one facet.
        private static Color Crystal(float u, float v)
        {
            float tall = 1f - (Mathf.Abs(u) / 0.22f + Mathf.Abs(v) / 0.95f);
            float wide = (1f - (Mathf.Abs(u) / 0.55f + Mathf.Abs(v) / 0.16f)) * 0.8f;
            float shape = Mathf.Max(tall, wide);
            float shade = (u < 0f ? 1f : 0.8f) * (0.8f + 0.2f * Mathf.Clamp01(shape * 2f));

            return new Color(shade, shade, shade, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shape / 0.15f)));
        }

        /// Droplet / bubble: denser rim and a small highlight.
        private static Color Drop(float u, float v)
        {
            float d = Length(u, v);
            float body = Mathf.Clamp01((1f - d) / 0.12f);
            float rim = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.55f) / 0.4f));
            float highlight = Mathf.Clamp01(1f - Length(u + 0.35f, v - 0.35f) / 0.22f);
            float shade = 0.75f + 0.25f * highlight;

            return new Color(shade, shade, shade, Mathf.Clamp01(body * (0.7f + 0.3f * rim) + highlight));
        }

        /// Puddle with a ragged edge.
        private static Color Decal(float u, float v)
        {
            float edge = 0.72f + 0.5f * (Fbm(u * 2.5f + 5f, v * 2.5f + 5f) - 0.5f);
            float detail = Fbm(u * 6f, v * 6f);
            float alpha = Mathf.Clamp01((edge - Length(u, v)) / 0.2f) * (0.75f + 0.25f * detail);
            float shade = 0.85f + 0.15f * detail;

            return new Color(shade, shade, shade, alpha);
        }
    }
}
