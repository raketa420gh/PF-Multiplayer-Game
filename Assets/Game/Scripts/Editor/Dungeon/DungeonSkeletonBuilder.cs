using System.Linq;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// The Tripo skeleton as the body of skeleton monsters: a humanoid model over the fighter rig that copies the rig's pose.
    internal static class DungeonSkeletonBuilder
    {
        public const string SkullPath = SkullFolder + "/FlyingSkull.fbx";

        private const string MaterialsFolder = DungeonPropBuilder.MaterialsFolder + "/Skeleton";
        private const string BodyFolder = "Assets/SpecialFolder/Models/Monsters/SkeletonCrossbowman";
        private const string BodyPath = BodyFolder + "/SkeletonCrossbowman.fbx";
        private const string SkullFolder = "Assets/SpecialFolder/Models/Monsters/FlyingSkull";
        // Tripo exports the body about 1 m tall: scaled to the fighter rig's height.
        private const float BodyScale = 1.75f;

        /// The Tripo skull of the flying head.
        public static Material SkullMaterial => TripoMaterial("FlyingSkull", SkullFolder + "/Textures/FlyingSkull_");

        /// The Tripo skeleton, rigged humanoid out of the box, as the body of every skeleton monster; weapons and hitboxes keep riding the rig.
        public static Transform Attach(GameObject root, Animator rig)
        {
            Avatar avatar = Import(BodyPath, BodyScale);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BodyPath), rig.transform.parent);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(model.GetComponent<Animator>());
            model.name = "Skeleton";
            Material material = TripoMaterial("SkeletonCrossbowman", BodyFolder + "/Textures/SkeletonCrossbowman_");

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }

            Retarget(root, rig, model, avatar);

            return model.transform;
        }

        /// Places a humanoid model where the rig is, hides the rig's own body and makes the model copy the rig's pose.
        public static void Retarget(GameObject root, Animator rig, GameObject model, Avatar avatar)
        {
            model.transform.SetParent(rig.transform.parent, false);
            model.transform.localPosition = rig.transform.localPosition;
            model.transform.localRotation = rig.transform.localRotation;
            model.transform.localScale = rig.transform.localScale;
            BattleEditorUtility.SetLayerRecursively(model, rig.gameObject.layer);

            foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            SerializedObject so = new SerializedObject(root.AddComponent<RetargetedModelComponent>());
            BattleEditorUtility.Set(so, "_source", rig);
            BattleEditorUtility.Set(so, "_avatar", avatar);
            BattleEditorUtility.Set(so, "_model", model.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// Tripo ships generic rigs; the pose copy needs them humanoid.
        private static Avatar Import(string path, float scale)
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);

            if (importer.animationType != ModelImporterAnimationType.Human || !Mathf.Approximately(importer.globalScale, scale)
                || importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importAnimation = false;
                importer.globalScale = scale;
                importer.SaveAndReimport();
            }

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().First();

            if (!avatar.isValid || !avatar.isHuman)
                throw new System.InvalidOperationException($"Skeleton avatar of {path} is not a valid humanoid");

            return avatar;
        }

        private static Material TripoMaterial(string name, string textures)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                BattleEditorUtility.EnsureFolder(MaterialsFolder);
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textures + "Color.jpg"));
            material.SetTexture("_BumpMap", DungeonKitBuilder.Normal(textures + "Normal.png"));
            material.SetTexture("_MetallicGlossMap", Linear(textures + "MetallicSmoothness.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);

            return material;
        }

        private static Texture2D Linear(string path)
        {
            BattleCharacterBuilder.SetLinear(path);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
