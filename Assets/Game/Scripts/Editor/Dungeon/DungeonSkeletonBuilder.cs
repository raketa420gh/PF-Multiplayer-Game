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
        private const string BodyFolder = "Assets/Game/Models/Monsters/Skeleton";
        private const string BodyPath = BodyFolder + "/Skeleton.fbx";
        private const string SkullFolder = "Assets/Game/Models/Monsters/FlyingSkull";
        // Tripo exports the body about 1 m tall: scaled to the fighter rig's height.
        private const float BodyScale = 1.75f;

        /// The heap a dormant skeleton lies in: on its knees, doubled over them, the skull on the floor, the arms limp along the body.
        /// Muscles by their HumanTrait names; the rest are 0.
        private static readonly (string, float)[] s_folded =
        {
            ("Spine Front-Back", -1f), ("Chest Front-Back", -1f), ("UpperChest Front-Back", -0.8f), ("Neck Nod Down-Up", -0.6f), ("Head Nod Down-Up", -0.4f),
            ("Spine Left-Right", 0.25f), ("Chest Twist Left-Right", 0.25f), ("Head Tilt Left-Right", 0.4f), ("Head Turn Left-Right", -0.7f),
            ("Left Upper Leg Front-Back", -1f), ("Right Upper Leg Front-Back", -0.9f), ("Left Upper Leg In-Out", 0.35f), ("Right Upper Leg In-Out", 0.15f),
            ("Left Lower Leg Stretch", -1f), ("Right Lower Leg Stretch", -1f), ("Left Foot Up-Down", -1f), ("Right Foot Up-Down", -1f),
            ("Left Arm Down-Up", -1f), ("Right Arm Down-Up", -0.75f), ("Left Arm Front-Back", -0.2f), ("Left Forearm Stretch", -0.3f), ("Right Forearm Stretch", 0.1f)
        };

        private static readonly Vector3 s_foldedEuler = new(72f, 12f, 6f);

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
            Material material = TripoMaterial("Skeleton", BodyFolder + "/Textures/Skeleton_");

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }

            Retarget(root, rig, model, avatar);

            return model.transform;
        }

        /// Makes the skeleton lie folded until it wakes and fold back when it dies (FoldedBodyComponent): bakes the folded pose into the component,
        /// its lowest bone 5 cm over the floor.
        public static void AddFold(GameObject root, Animator rig, MonsterComponent monster)
        {
            Transform[] bones = rig.GetComponentsInChildren<Transform>(true);
            (Vector3, Quaternion)[] rest = bones.Select(bone => (bone.localPosition, bone.localRotation)).ToArray();
            HumanPoseHandler handler = new HumanPoseHandler(rig.avatar, rig.transform);
            HumanPose pose = default;
            handler.GetHumanPose(ref pose);
            System.Array.Clear(pose.muscles, 0, pose.muscles.Length);

            foreach ((string muscle, float value) in s_folded)
                pose.muscles[System.Array.IndexOf(HumanTrait.MuscleName, muscle)] = value;

            pose.bodyRotation = Quaternion.Euler(s_foldedEuler);
            pose.bodyPosition = Vector3.up;
            handler.SetHumanPose(ref pose);
            float lowest = Enumerable.Range(0, (int)HumanBodyBones.LastBone).Select(bone => rig.GetBoneTransform((HumanBodyBones)bone))
                .Where(bone => bone != null).Min(bone => bone.position.y) - rig.transform.position.y;
            handler.Dispose();

            // The prefab keeps the rig at rest: the fighter measures its body on it.
            for (int i = 0; i < bones.Length; i++)
                bones[i].SetLocalPositionAndRotation(rest[i].Item1, rest[i].Item2);

            SerializedObject so = new SerializedObject(root.AddComponent<FoldedBodyComponent>());
            BattleEditorUtility.Set(so, "_monster", monster);
            BattleEditorUtility.Set(so, "_anim", root.GetComponent<Game.Scripts.Battle.FighterAnimComponent>());
            BattleEditorUtility.Set(so, "_rig", rig);
            BattleEditorUtility.Set(so, "_muscles", pose.muscles);
            BattleEditorUtility.Set(so, "_bodyPosition", Vector3.up * (1f - (lowest - 0.05f) / rig.humanScale));
            so.FindProperty("_bodyRotation").quaternionValue = pose.bodyRotation;
            so.ApplyModifiedPropertiesWithoutUndo();
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
