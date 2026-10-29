using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Renders item models into inventory icon sprites with a temporary camera far away from the scene.
    /// The camera looks straight at the model's broadest side (along its thinnest axis), the longest axis follows the longer side of the item's cells.
    internal static class DungeonIconBuilder
    {
        public const string Folder = "Assets/Game/Textures/Icons";
        private const int CellSize = 128;
        private static readonly Vector3 s_stage = new(2000f, 2000f, 2000f);

        /// The shift moves the centre of the picture off the centre of the model, in shares of its size; euler turns the model before the axes are measured.
        /// An upright model (a figure) keeps the world up instead of its longest axis.
        public static Sprite Render(GameObject model, string name, int width, int height, float zoom = 1f, Vector3 euler = default, Vector3 shift = default,
            bool upright = false)
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
                    materials[i].DisableKeyword("_METALLICSPECGLOSSMAP");
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
                Vector3 extents = Vector3.Max(bounds.extents, Vector3.one * 0.01f);
                float radius = Mathf.Max(extents.x, extents.y, extents.z, 0.05f);
                Vector3 center = bounds.center + shift * radius;
                int thin = extents.x <= extents.y && extents.x <= extents.z ? 0 : extents.y <= extents.z ? 1 : 2;
                int along = (thin + 1) % 3, across = (thin + 2) % 3;

                if (extents[along] < extents[across])
                    (along, across) = (across, along);

                // Upright by default; a wider-than-tall item lays its longest axis across the picture.
                int up = upright && thin != 1 ? 1 : height >= width ? along : across;
                int side = up == along ? across : along;
                // Seen from the positive side of the thin axis (the front of a figure, the face of a shield), turned a little so flat faces keep their depth.
                Vector3 upAxis = Axis(up);
                Vector3 view = Quaternion.AngleAxis(15f, upAxis) * Quaternion.AngleAxis(-10f, Axis(side)) * Axis(thin);
                Quaternion rotation = Quaternion.LookRotation(-view, upAxis);
                float aspect = (float)width / height;
                float halfWide = 0f, halfHigh = 0f;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = Quaternion.Inverse(rotation) * (bounds.center - center + Vector3.Scale(extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2f - Vector3.one));
                    halfWide = Mathf.Max(halfWide, Mathf.Abs(corner.x));
                    halfHigh = Mathf.Max(halfHigh, Mathf.Abs(corner.y));
                }

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(halfHigh, halfWide / aspect) * 1.04f / zoom;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;
                camera.transform.SetPositionAndRotation(center + view * 6f, rotation);

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.6f;
                light.color = new Color(1f, 0.95f, 0.85f);
                lightObject.transform.rotation = camera.transform.rotation * Quaternion.Euler(30f, -35f, 0f);
                Light fill = new GameObject("IconFill").AddComponent<Light>();
                fill.transform.SetParent(lightObject.transform);
                fill.type = LightType.Directional;
                fill.intensity = 0.6f;
                fill.color = new Color(0.6f, 0.7f, 1f);
                fill.transform.rotation = camera.transform.rotation * Quaternion.Euler(-15f, 150f, 0f);

                Color ambient = RenderSettings.ambientLight;
                bool fog = RenderSettings.fog;
                RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.4f);
                RenderSettings.fog = false;

                int pixelsWide = width * CellSize, pixelsHigh = height * CellSize;
                RenderTexture texture = RenderTexture.GetTemporary(pixelsWide, pixelsHigh, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = texture;
                camera.aspect = aspect;
                camera.Render();
                RenderTexture.active = texture;
                Texture2D image = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0f, 0f, pixelsWide, pixelsHigh), 0, 0);
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

        private static Vector3 Axis(int index) => index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;

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
