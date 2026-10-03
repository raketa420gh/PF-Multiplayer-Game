using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Renders item models into inventory icon sprites with a temporary camera far away from the scene.
    internal static class DungeonIconBuilder
    {
        public const string Folder = "Assets/Game/Textures/Icons";
        private const int Size = 128;
        private static readonly Vector3 s_stage = new(2000f, 2000f, 2000f);

        public static Sprite Render(GameObject model, string name, float zoom = 1f, Vector3 euler = default, float lift = 0f)
        {
            BattleEditorUtilityShim.EnsureFolder(Folder);
            GameObject instance = Object.Instantiate(model, s_stage, Quaternion.Euler(euler));
            GameObject cameraObject = new GameObject("IconCamera");
            GameObject lightObject = new GameObject("IconLight");
            string path = $"{Folder}/{name}.png";
            System.Collections.Generic.List<Material> copies = new();

            // Metals have nothing to reflect on the stage; soften them so the base colour reads.
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].HasProperty("_Metallic"))
                        continue;

                    materials[i] = new Material(materials[i]);
                    materials[i].SetFloat("_Metallic", materials[i].GetFloat("_Metallic") * 0.35f);
                    materials[i].SetFloat("_Smoothness", Mathf.Min(materials[i].GetFloat("_Smoothness"), 0.6f));
                    copies.Add(materials[i]);
                }

                renderer.sharedMaterials = materials;
            }

            bool asyncCompile = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;

            try
            {
                Bounds bounds = CalculateBounds(instance);
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z, 0.05f);
                Vector3 center = bounds.center + Vector3.up * lift * radius;

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = radius * 1.15f / zoom;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;
                camera.transform.position = center + new Vector3(0.9f, 0.9f, -1.1f).normalized * 6f;
                camera.transform.LookAt(center);

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.6f;
                light.color = new Color(1f, 0.95f, 0.85f);
                lightObject.transform.rotation = Quaternion.Euler(45f, -40f, 0f);
                Light fill = new GameObject("IconFill").AddComponent<Light>();
                fill.transform.SetParent(lightObject.transform);
                fill.type = LightType.Directional;
                fill.intensity = 0.6f;
                fill.color = new Color(0.6f, 0.7f, 1f);
                fill.transform.rotation = Quaternion.Euler(-20f, 140f, 0f);

                Color ambient = RenderSettings.ambientLight;
                bool fog = RenderSettings.fog;
                RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.4f);
                RenderSettings.fog = false;

                RenderTexture texture = RenderTexture.GetTemporary(Size, Size, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                Texture2D image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
                image.Apply();
                RenderTexture.active = null;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(texture);
                RenderSettings.ambientLight = ambient;
                RenderSettings.fog = fog;

                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncCompile;
                Object.DestroyImmediate(instance);

                foreach (Material copy in copies)
                    Object.DestroyImmediate(copy);

                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(lightObject);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Bounds bounds = default;
            bool hasBounds = false;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer)
                    continue;

                if (hasBounds)
                    bounds.Encapsulate(renderer.bounds);
                else
                    bounds = renderer.bounds;

                hasBounds = true;
            }

            return hasBounds ? bounds : new Bounds(root.transform.position, Vector3.one * 0.2f);
        }
    }
}
