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
            BattleEditorUtilityShim.EnsureFolder(Folder);
            float extent = DungeonMapBuilder.WorldSize * 0.5f;
            GameObject cameraObject = new GameObject("MinimapCamera");
            GameObject lightObject = new GameObject("MinimapLight");
            string path = $"{Folder}/{name}.png";
            Material ink = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            ink.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.02f));
            System.Collections.Generic.List<(MeshRenderer renderer, Material material, bool enabled)> swapped = new();

            foreach (MeshRenderer renderer in floor.GetComponentsInChildren<MeshRenderer>())
            {
                bool isCeiling = renderer.gameObject.name.StartsWith("Ceiling");
                bool isWall = renderer.gameObject.name.StartsWith("Wall");

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
                camera.transform.position = new Vector3(0f, floorY + DungeonPropBuilder.WallHeight + 2f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.4f;
                lightObject.transform.rotation = Quaternion.Euler(80f, 20f, 0f);

                Color ambient = RenderSettings.ambientLight;
                bool fog = RenderSettings.fog;
                RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);
                RenderSettings.fog = false;

                RenderTexture texture = RenderTexture.GetTemporary(Size, Size, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                Texture2D image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
                RenderTexture.active = null;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(texture);
                RenderSettings.ambientLight = ambient;
                RenderSettings.fog = fog;

                Stylize(image);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
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

            return Import(path);
        }

        private static Texture2D Import(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// Ink-swapped walls become thick dark strokes; floors become flat parchment with faint seams.
        private static void Stylize(Texture2D image)
        {
            Color[] pixels = image.GetPixels();
            float[] luminance = new float[pixels.Length];
            bool[] ink = new bool[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                luminance[i] = pixels[i].grayscale;
                ink[i] = luminance[i] < 0.06f;
            }

            bool[] thick = Dilate(ink, 3);
            Color parchment = new Color(0.82f, 0.74f, 0.55f);
            Color inkColor = new Color(0.2f, 0.13f, 0.07f);
            Color shade = new Color(0.68f, 0.58f, 0.4f);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int index = y * Size + x;

                    if (thick[index])
                    {
                        pixels[index] = inkColor;
                        continue;
                    }

                    float value = luminance[index];
                    float edge = 0f;

                    if (x > 0 && x < Size - 1 && y > 0 && y < Size - 1)
                    {
                        float dx = luminance[index + 1] - luminance[index - 1];
                        float dy = luminance[index + Size] - luminance[index - Size];
                        edge = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                    }

                    Color color = Color.Lerp(shade, parchment, Mathf.Clamp01(value * 1.6f));
                    pixels[index] = Color.Lerp(color, inkColor, edge * 0.25f);
                }
            }

            image.SetPixels(pixels);
            image.Apply();
        }

        private static bool[] Dilate(bool[] source, int radius)
        {
            bool[] result = new bool[source.Length];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (!source[y * Size + x])
                        continue;

                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < Size && ny >= 0 && ny < Size)
                                result[ny * Size + nx] = true;
                        }
                    }
                }
            }

            return result;
        }
    }
}
