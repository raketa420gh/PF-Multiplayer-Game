using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Textured materials shared by all weapon models. The tint multiplies the texture set, so one set serves several metals and woods.
    internal static class WeaponMaterials
    {
        public static Material Steel => Get("Steel", WeaponTextureBuilder.Steel, Color.white, 0.9f);
        public static Material Iron => Get("Iron", WeaponTextureBuilder.Iron, Color.white, 0.8f);
        public static Material Brass => Get("Brass", WeaponTextureBuilder.Steel, new Color(1f, 0.74f, 0.38f), 0.9f);
        public static Material Wood => Get("Wood", WeaponTextureBuilder.Wood, Color.white, 0f);
        public static Material DarkWood => Get("DarkWood", WeaponTextureBuilder.Wood, new Color(0.5f, 0.4f, 0.34f), 0f);
        public static Material Yew => Get("Yew", WeaponTextureBuilder.Wood, new Color(0.95f, 0.72f, 0.5f), 0f);
        public static Material Leather => Get("Leather", WeaponTextureBuilder.Wrap, Color.white, 0f);
        public static Material DarkLeather => Get("DarkLeather", WeaponTextureBuilder.Wrap, new Color(0.45f, 0.42f, 0.42f), 0f);
        public static Material Hide => Get("Hide", WeaponTextureBuilder.Hide, new Color(0.62f, 0.45f, 0.3f), 0f);
        public static Material BookCover => Get("BookCover", WeaponTextureBuilder.Hide, new Color(0.5f, 0.2f, 0.56f), 0f);
        public static Material Rags => Get("Rags", WeaponTextureBuilder.Cloth, new Color(0.2f, 0.16f, 0.12f), 0f);
        public static Material String => Get("String", WeaponTextureBuilder.Cloth, new Color(0.9f, 0.86f, 0.72f), 0f);
        public static Material ShieldFace => Get("ShieldFace", WeaponTextureBuilder.ShieldFace, Color.white, 0f);
        public static Material EcuFace => Get("EcuFace", WeaponTextureBuilder.EcuFace, Color.white, 0f);
        public static Material EcuInside => Get("EcuInside", WeaponTextureBuilder.Wood, new Color(0.22f, 0.42f, 0.4f), 0f);
        public static Material Feather => Get("Feather", WeaponTextureBuilder.Feather, new Color(0.75f, 0.2f, 0.16f), 0f);
        public static Material Horn => Get("Horn", WeaponTextureBuilder.Hide, new Color(1f, 1f, 1f), 0f, 1.6f);

        /// The metallic value only matters where the mask is switched off, as the icon renderer does.
        private static Material Get(string name, string texture, Color tint, float metallic, float smoothness = 1f)
        {
            WeaponTextureBuilder.EnsureBuilt();
            string path = $"{BattleEditorUtility.MaterialsFolder}/Weapon{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            Texture2D mask = WeaponTextureBuilder.Load(texture, "_m");
            material.SetColor("_BaseColor", tint);
            material.SetTexture("_BaseMap", WeaponTextureBuilder.Load(texture));
            material.SetTexture("_BumpMap", WeaponTextureBuilder.Load(texture, "_n"));
            material.SetTexture("_MetallicGlossMap", mask);
            material.SetTexture("_OcclusionMap", mask);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
            EditorUtility.SetDirty(material);

            return material;
        }
    }
}
