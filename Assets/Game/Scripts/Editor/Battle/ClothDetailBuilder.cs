using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Tiling cloth surface over the outfit atlases: URP Lit detail maps (grey x2 albedo and normal) from cgbookcase CC0 materials.
    /// The atlas keeps the colours and folds, the detail adds the weave or the leather grain seen up close.
    internal static class ClothDetailBuilder
    {
        public const string Fabric = "Fabric08";
        public const string Leather = "BrownLeather01";

        /// Unzipped cgbookcase downloads, one folder per material (Docs/06_Art/Free_PBR_Sources.md).
        private const string SourceFolder = "Assets/SpecialFolder/Textures/Cgbookcase";
        private const string TexturesFolder = "Assets/Game/Textures/Battle";
        private const int MaxSize = 1024;

        /// Repeats per atlas: one atlas unit spans about 2.4 m of the outfit, a tile about 15 cm of linen or 30 cm of leather.
        private static readonly (string name, float tiling, float normal, float albedo)[] s_sources =
        {
            (Fabric, 16f, 1.5f, 0.6f), (Leather, 8f, 0.8f, 0.6f)
        };

        /// Gives the material the detail of a source; without the downloaded source the material stays as it is.
        /// The ORM keeps the detail off metal.
        public static void Apply(Material material, string source, string ormPath)
        {
            (string name, float tiling, float normal, float albedo) = System.Array.Find(s_sources, entry => entry.name == source);
            string color = FindMap(name, "_BaseColor");
            string normalMap = FindMap(name, "_Normal");

            if (color == null || normalMap == null)
            {
                Debug.LogWarning($"[{nameof(ClothDetailBuilder)}] {name} is not in {SourceFolder}, {material.name} has no cloth detail");

                return;
            }

            material.SetTexture("_DetailAlbedoMap", Albedo(name, color));
            material.SetTexture("_DetailNormalMap", Normal(normalMap));
            material.SetTexture("_DetailMask", Mask(ormPath));
            material.SetTextureScale("_DetailAlbedoMap", Vector2.one * tiling);
            material.SetFloat("_DetailAlbedoMapScale", albedo);
            material.SetFloat("_DetailNormalMapScale", normal);
            material.DisableKeyword("_DETAIL_MULX2");
            material.EnableKeyword("_DETAIL_SCALED");
            EditorUtility.SetDirty(material);
        }

        private static string FindMap(string name, string suffix)
        {
            string folder = $"{SourceFolder}/{name}";

            if (!AssetDatabase.IsValidFolder(folder))
                return null;

            foreach (string guid in AssetDatabase.FindAssets($"{suffix} t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetFileNameWithoutExtension(path).EndsWith(suffix))
                    return path;
            }

            return null;
        }

        /// Luminance over its mean, 0.5 = no change under the x2 detail blend; linear, so the shader reads it as it was written.
        private static Texture2D Albedo(string name, string colorPath)
        {
            string path = $"{TexturesFolder}/T_{name}_DetailAlbedo.png";

            if (!File.Exists(path))
            {
                Color32[] colors = BattleCharacterBuilder.ReadPixels(colorPath);
                float[] luminance = new float[colors.Length];
                float mean = 0f;

                for (int i = 0; i < colors.Length; i++)
                    mean += luminance[i] = 0.2126f * colors[i].r + 0.7152f * colors[i].g + 0.0722f * colors[i].b;

                mean /= colors.Length;

                for (int i = 0; i < colors.Length; i++)
                {
                    byte grey = (byte)Mathf.Clamp(Mathf.RoundToInt(127.5f * luminance[i] / mean), 0, 255);
                    colors[i] = new Color32(grey, grey, grey, 255);
                }

                int size = (int)Mathf.Sqrt(colors.Length);
                Texture2D pixels = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                pixels.SetPixels32(colors);
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                Object.DestroyImmediate(pixels);
                AssetDatabase.ImportAsset(path);
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false;
            importer.maxTextureSize = MaxSize;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// Detail mask (alpha, atlas UV): the ORM's metallic (blue) taken away, so buckles and studs stay plain metal.
        private static Texture2D Mask(string ormPath)
        {
            string path = $"{TexturesFolder}/{Path.GetFileNameWithoutExtension(ormPath)}_DetailMask.png";

            if (!File.Exists(path))
            {
                Color32[] colors = BattleCharacterBuilder.ReadPixels(ormPath);

                for (int i = 0; i < colors.Length; i++)
                    colors[i] = new Color32(255, 255, 255, (byte)(255 - colors[i].b));

                int size = (int)Mathf.Sqrt(colors.Length);
                Texture2D pixels = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                pixels.SetPixels32(colors);
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                Object.DestroyImmediate(pixels);
                AssetDatabase.ImportAsset(path);
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// cgbookcase normals are DirectX: the importer flips green for Unity's OpenGL convention.
        private static Texture2D Normal(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.NormalMap;
            importer.flipGreenChannel = true;
            importer.maxTextureSize = MaxSize;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
