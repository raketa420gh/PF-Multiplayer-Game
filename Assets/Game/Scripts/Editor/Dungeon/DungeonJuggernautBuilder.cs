using System.Linq;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// The Iron Juggernaut pack as a monster body on its own animator: the pack's idle, walk, run and fall, a punch, a flinch and a death from the
    /// animation library, all humanoid so the library takes retarget onto the pack's skeleton.
    internal static class DungeonJuggernautBuilder
    {
        public const string AttackClip = "Punch_Cross";
        /// Normalized time of the fist at full reach in the punch.
        public const float AttackImpact = 0.27f;

        private const string PackFolder = "Assets/SpecialFolder/3D Models/IronSpear Content/Iron_Juggernaut";
        private const string ModelPath = PackFolder + "/Meshes/MidPoly.fbx";
        private const string AnimationsFolder = PackFolder + "/Animations";
        private const string MaterialsFolder = DungeonPropBuilder.MaterialsFolder + "/Juggernaut";
        private const string ControllerFolder = "Assets/Game/Animations/Dungeon";
        private const string ControllerPath = ControllerFolder + "/Juggernaut.controller";
        private const string MaskPath = ControllerFolder + "/JuggernautUpperBody.mask";
        private static readonly string[] s_loops = { "MM_Idle", "MM_Walk_Fwd", "MM_Run_Fwd", "MM_Fall_Loop" };

        /// Puts the model under the fighter's model pivot and returns its animator.
        public static Animator Attach(Transform parent, Vector3 localPosition, Quaternion localRotation, int layer)
        {
            Avatar avatar = Import();
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), parent);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Juggernaut";
            model.transform.localPosition = localPosition;
            model.transform.localRotation = localRotation;
            BattleEditorUtility.SetLayerRecursively(model, layer);

            foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(Convert).ToArray();
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }

            Animator animator = model.GetComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = BuildController();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            return animator;
        }

        /// The pack ships generic rigs: the model gets a humanoid avatar, its takes reuse it and loop in place.
        private static Avatar Import()
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false;
                importer.SaveAndReimport();
            }

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().First();

            if (!avatar.isValid || !avatar.isHuman)
                throw new System.InvalidOperationException($"Juggernaut avatar of {ModelPath} is not a valid humanoid");

            foreach (string take in s_loops)
            {
                ModelImporter clipImporter = (ModelImporter)AssetImporter.GetAtPath($"{AnimationsFolder}/{take}.FBX");

                if (clipImporter.animationType == ModelImporterAnimationType.Human && clipImporter.sourceAvatar == avatar)
                    continue;

                clipImporter.animationType = ModelImporterAnimationType.Human;
                clipImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                clipImporter.sourceAvatar = avatar;
                ModelImporterClipAnimation clip = clipImporter.defaultClipAnimations[0];
                clip.name = take;
                clip.loopTime = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clipImporter.clipAnimations = new[] { clip };
                clipImporter.SaveAndReimport();
            }

            return avatar;
        }

        private static AnimationClip LoadTake(string take)
        {
            return AssetDatabase.LoadAllAssetsAtPath($"{AnimationsFolder}/{take}.FBX").OfType<AnimationClip>().First(clip => clip.name == take);
        }

        /// Base layer: locomotion blended by ground speed, fall, the attack scrubbed by AttackTime, death. Upper-body layer: the flinch.
        private static AnimatorController BuildController()
        {
            BattleEditorUtility.EnsureFolder(ControllerFolder);
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("AttackTime", AnimatorControllerParameterType.Float);

            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            AnimationClip walk = LoadTake("MM_Walk_Fwd");
            AnimationClip run = LoadTake("MM_Run_Fwd");
            tree.AddChild(LoadTake("MM_Idle"), 0f);
            tree.AddChild(walk, Mathf.Max(0.5f, walk.averageSpeed.magnitude));
            tree.AddChild(run, Mathf.Max(walk.averageSpeed.magnitude + 1f, run.averageSpeed.magnitude));

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            machine.defaultState = locomotion;
            machine.AddState("Fall").motion = LoadTake("MM_Fall_Loop");
            AnimatorState attack = machine.AddState("Attack");
            attack.motion = BattleEditorUtility.LoadLibraryClip(AttackClip);
            attack.timeParameterActive = true;
            attack.timeParameter = "AttackTime";
            machine.AddState("Death").motion = BattleEditorUtility.LoadLibraryClip("Death01");

            controller.AddLayer("Hit");
            AnimatorControllerLayer[] layers = controller.layers;
            layers[1].defaultWeight = 1f;
            layers[1].avatarMask = BuildUpperBodyMask();
            controller.layers = layers;
            AnimatorStateMachine hitMachine = layers[1].stateMachine;
            AnimatorState empty = hitMachine.AddState("Empty");
            AnimatorState hit = hitMachine.AddState("Hit");
            hit.motion = BattleEditorUtility.LoadLibraryClip("Hit_Chest");
            hit.writeDefaultValues = false;
            empty.writeDefaultValues = false;
            hitMachine.defaultState = empty;
            AnimatorStateTransition back = hit.AddTransition(empty);
            back.hasExitTime = true;
            back.exitTime = 0.8f;
            back.duration = 0.15f;
            EditorUtility.SetDirty(controller);

            return controller;
        }

        private static AvatarMask BuildUpperBodyMask()
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);

            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, MaskPath);
            }

            for (AvatarMaskBodyPart part = 0; part < AvatarMaskBodyPart.LastBodyPart; part++)
                mask.SetHumanoidBodyPartActive(part, part is not (AvatarMaskBodyPart.Root or AvatarMaskBodyPart.LeftLeg or AvatarMaskBodyPart.RightLeg
                    or AvatarMaskBodyPart.LeftFootIK or AvatarMaskBodyPart.RightFootIK));

            EditorUtility.SetDirty(mask);

            return mask;
        }

        /// The pack's built-in Standard materials as URP Lit, one per source material.
        private static Material Convert(Material source)
        {
            BattleEditorUtility.EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/{source.name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            Texture albedo = source.GetTexture("_MainTex");
            Texture metallic = source.GetTexture("_MetallicGlossMap");
            Texture normal = source.GetTexture("_BumpMap");
            Texture occlusion = source.GetTexture("_OcclusionMap");

            foreach (Texture mask in new[] { metallic, occlusion }.Where(texture => texture != null))
                BattleCharacterBuilder.SetLinear(AssetDatabase.GetAssetPath(mask));

            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", albedo != null ? Color.white : source.GetColor("_Color"));
            material.SetTexture("_MetallicGlossMap", metallic);
            material.SetFloat("_Metallic", metallic != null ? 1f : 0f);
            material.SetFloat("_Smoothness", metallic != null ? 1f : 0.2f);
            material.SetTexture("_BumpMap", normal != null ? DungeonKitBuilder.Normal(AssetDatabase.GetAssetPath(normal)) : null);
            material.SetTexture("_OcclusionMap", occlusion);
            SetKeyword(material, "_NORMALMAP", normal != null);
            SetKeyword(material, "_METALLICSPECGLOSSMAP", metallic != null);
            SetKeyword(material, "_OCCLUSIONMAP", occlusion != null);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static void SetKeyword(Material material, string keyword, bool isEnabled)
        {
            if (isEnabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }
    }
}
