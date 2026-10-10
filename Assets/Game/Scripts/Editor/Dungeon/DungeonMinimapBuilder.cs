using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Top-down render of a floor turned into a parchment-style map texture.
    internal static class DungeonMinimapBuilder
    {
        public const string Folder = "Assets/Game/Textures/Minimap";
        private const int Size = 1024;

        public static Texture2D Render(Transform floor, float floorY, string name)
        {
            return Render(floor, Vector3.up * floorY, DungeonMapBuilder.WorldSize * 0.5f, Size, name, false);
        }

        /// Square of the given half side around center; small maps are rendered four times larger and scaled down. Readable
        /// maps can be stitched together at runtime (the cells of a generated floor).
        public static Texture2D Render(Transform floor, Vector3 center, float extent, int size, string name, bool isReadable)
        {
            BattleEditorUtilityShim.EnsureFolder(Folder);
            int samples = size < Size / 2 ? 4 : 1;
            int render = size * samples;
            GameObject cameraObject = new GameObject("MinimapCamera");
            GameObject lightObject = new GameObject("MinimapLight");
            string path = $"{Folder}/{name}.png";
            Material ink = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            ink.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.02f));
            System.Collections.Generic.List<(MeshRenderer renderer, Material material, bool enabled)> swapped = new();

            foreach (MeshRenderer renderer in floor.GetComponentsInChildren<MeshRenderer>())
            {
                bool isCeiling = renderer.gameObject.name.StartsWith("Ceiling");
                // Walls of pits outline the drop, so a pit reads on the map.
                bool isWall = renderer.gameObject.name.StartsWith("Wall") || renderer.gameObject.name.StartsWith("Pit Wall");

                if (!isCeiling && !isWall)
                    continue;

                swapped.Add((renderer, renderer.sharedMaterial, renderer.enabled));

                if (isCeiling)
                    renderer.enabled = false;
                else
                    renderer.sharedMaterial = ink;
            }

            bool asyncCompile = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;

            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = extent;
                camera.aspect = 1f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.08f, 0.06f, 0.04f, 1f);
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = DungeonPropBuilder.WallHeight + 3f;
                camera.cullingMask = 1;
                camera.transform.position = center + Vector3.up * (DungeonPropBuilder.WallHeight + 2f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.4f;
                lightObject.transform.rotation = Quaternion.Euler(80f, 20f, 0f);

                Color ambient = RenderSettings.ambientLight;
                bool fog = RenderSettings.fog;
                RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);
                RenderSettings.fog = false;

                RenderTexture texture = RenderTexture.GetTemporary(render, render, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                Texture2D image = new Texture2D(render, render, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0f, 0f, render, render), 0, 0);
                RenderTexture.active = null;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(texture);
                RenderSettings.ambientLight = ambient;
                RenderSettings.fog = fog;

                Stylize(image, render, samples > 1 ? 2 : 3);
                Texture2D map = samples > 1 ? Shrink(image, samples) : image;
                File.WriteAllBytes(path, map.EncodeToPNG());
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(map);
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncCompile;
                foreach ((MeshRenderer renderer, Material material, bool enabled) entry in swapped)
                {
                    entry.renderer.sharedMaterial = entry.material;
                    entry.renderer.enabled = entry.enabled;
                }

                Object.DestroyImmediate(ink);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(lightObject);
            }

            return Import(path, isReadable);
        }

        /// A finished map written as an asset: the parchment of a generated floor.
        public static Texture2D Save(Texture2D map, string name)
        {
            BattleEditorUtilityShim.EnsureFolder(Folder);
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, map.EncodeToPNG());

            return Import(path, false);
        }

        private static Texture2D Import(string path, bool isReadable)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.isReadable = isReadable;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = isReadable ? TextureImporterCompression.Uncompressed : importer.textureCompression;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// Ink-swapped walls become thick dark strokes; floors become flat parchment with faint seams.
        private static Texture2D Shrink(Texture2D image, int samples)
        {
            int size = image.width / samples;
            Color[] source = image.GetPixels();
            Color[] pixels = new Color[size * size];
            float weight = 1f / (samples * samples);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color sum = Color.clear;

                    for (int dy = 0; dy < samples; dy++)
                    {
                        for (int dx = 0; dx < samples; dx++)
                            sum += source[(y * samples + dy) * image.width + x * samples + dx];
                    }

                    pixels[y * size + x] = sum * weight;
                }
            }

            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            result.SetPixels(pixels);
            result.Apply();

            return result;
        }

        private static void Stylize(Texture2D image, int size, int radius)
        {
            Color[] pixels = image.GetPixels();
            float[] luminance = new float[pixels.Length];
            bool[] ink = new bool[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                luminance[i] = pixels[i].grayscale;
                ink[i] = luminance[i] < 0.06f;
            }

            bool[] thick = Dilate(ink, size, radius);
            Color parchment = new Color(0.82f, 0.74f, 0.55f);
            Color inkColor = new Color(0.2f, 0.13f, 0.07f);
            Color shade = new Color(0.68f, 0.58f, 0.4f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;

                    if (thick[index])
                    {
                        pixels[index] = inkColor;
                        continue;
                    }

                    float value = luminance[index];
                    float edge = 0f;

                    if (x > 0 && x < size - 1 && y > 0 && y < size - 1)
                    {
                        float dx = luminance[index + 1] - luminance[index - 1];
                        float dy = luminance[index + size] - luminance[index - size];
                        edge = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                    }

                    Color color = Color.Lerp(shade, parchment, Mathf.Clamp01(value * 1.6f));
                    pixels[index] = Color.Lerp(color, inkColor, edge * 0.25f);
                }
            }

            image.SetPixels(pixels);
            image.Apply();
        }

        private static bool[] Dilate(bool[] source, int size, int radius)
        {
            bool[] result = new bool[source.Length];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (!source[y * size + x])
                        continue;

                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < size && ny >= 0 && ny < size)
                                result[ny * size + nx] = true;
                        }
                    }
                }
            }

            return result;
        }
    }
}
