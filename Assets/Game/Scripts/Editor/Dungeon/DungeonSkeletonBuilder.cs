using System.Linq;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// The Stylized Skeleton pack as the body of skeleton monsters: a humanoid model over the fighter rig that copies the rig's pose.
    internal static class DungeonSkeletonBuilder
    {
        private const string PackFolder = "Assets/SpecialFolder/3D Models/Stylized Skeleton";
        private const string ModelPath = PackFolder + "/Mesh/SKM_Skeleton_Var_1.fbx";
        private const string TexturePrefix = PackFolder + "/Textures/T_Skeleton_Variant_1_";
        private const string MaterialsFolder = DungeonPropBuilder.MaterialsFolder + "/Skeleton";

        /// One of the pack's bone colourings: A sand, B ash, C frost, D soot. Cracks glow with the colour of the flying head's eyes.
        public static Material Material(string variant)
        {
            BattleEditorUtility.EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/Skeleton{variant}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            Texture2D mask = Linear(TexturePrefix + "Mask_Unity.png");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturePrefix}{variant}_Albedo.png"));
            material.SetTexture("_BumpMap", DungeonKitBuilder.Normal(TexturePrefix + "N_OpenGL.png"));
            material.SetTexture("_MetallicGlossMap", mask);
            material.SetTexture("_OcclusionMap", mask);
            material.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePrefix + "Emissive.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(0.45f, 1f, 0.6f) * 1.5f);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);

            return material;
        }

        /// Puts the skeleton over the rig and hides the rig's own body; weapons and hitboxes keep riding the rig.
        public static Transform Attach(GameObject root, Animator rig, Material material)
        {
            Avatar avatar = Import();
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), rig.transform.parent);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(model.GetComponent<Animator>());
            model.name = "Skeleton";
            model.transform.localPosition = rig.transform.localPosition;
            model.transform.localRotation = rig.transform.localRotation;
            model.transform.localScale = rig.transform.localScale;
            BattleEditorUtility.SetLayerRecursively(model, rig.gameObject.layer);

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }

            foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            SerializedObject so = new SerializedObject(root.AddComponent<RetargetedModelComponent>());
            BattleEditorUtility.Set(so, "_source", rig);
            BattleEditorUtility.Set(so, "_avatar", avatar);
            BattleEditorUtility.Set(so, "_model", model.transform);
            so.ApplyModifiedPropertiesWithoutUndo();

            return model.transform;
        }

        /// The pack ships a generic rig; the pose copy needs it humanoid.
        private static Avatar Import()
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importAnimation = false;
                importer.SaveAndReimport();
            }

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().First();

            if (!avatar.isValid || !avatar.isHuman)
                throw new System.InvalidOperationException($"Skeleton avatar of {ModelPath} is not a valid humanoid");

            return avatar;
        }

        private static Texture2D Linear(string path)
        {
            BattleCharacterBuilder.SetLinear(path);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
