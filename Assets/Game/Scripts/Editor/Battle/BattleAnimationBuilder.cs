using System;
using System.Collections.Generic;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    internal static class BattleAnimationBuilder
    {
        public const float FrameRate = 60f;

        private const int BodyMuscleCount = 55;
        private const float WalkSpeed = 3.2f;
        private const string UpperLayerName = "Upper";

        private static readonly string[] s_rootCurves = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };

        [MenuItem("Tools/Game/Battle/Build Animations")]
        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.AnimationsFolder);
            SetupModel();

            WeaponDefinition[] weapons =
            {
                BattleAnimationLibrary.CreateSwordShield(),
                BattleAnimationLibrary.CreateGreatsword(),
                BattleAnimationLibrary.CreateBow()
            };

            using BattlePoseRig rig = new BattlePoseRig();

            AnimatorController controller = CreateController();
            BuildLocomotion(rig, controller);
            AnimatorStateMachine upper = controller.layers[1].stateMachine;

            foreach (WeaponDefinition weapon in weapons)
                BuildWeapon(rig, upper, weapon);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(BattleAnimationBuilder)}] Animations built in {BattleEditorUtility.AnimationsFolder}");
        }

        public static void SetupModel()
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(BattleEditorUtility.ModelPath);

            if (importer.animationType == ModelImporterAnimationType.Human)
                return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.SaveAndReimport();
        }

        private static AnimatorController CreateController()
        {
            AssetDatabase.DeleteAsset(BattleEditorUtility.ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BattleEditorUtility.ControllerPath);

            controller.AddParameter(FighterAnimComponent.MoveXParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(FighterAnimComponent.MoveYParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(FighterAnimComponent.CrouchParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(FighterAnimComponent.MirrorParam, AnimatorControllerParameterType.Bool);
            controller.AddLayer(UpperLayerName);

            AnimatorControllerLayer[] layers = controller.layers;
            layers[1].defaultWeight = 1f;
            layers[1].avatarMask = CreateUpperMask();
            controller.layers = layers;

            return controller;
        }

        private static AvatarMask CreateUpperMask()
        {
            string path = $"{BattleEditorUtility.AnimationsFolder}/UpperBody.mask";
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);

            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, path);
            }

            for (AvatarMaskBodyPart part = 0; part < AvatarMaskBodyPart.LastBodyPart; part++)
            {
                bool isUpper = part is AvatarMaskBodyPart.Body or AvatarMaskBodyPart.Head or AvatarMaskBodyPart.LeftArm
                    or AvatarMaskBodyPart.RightArm or AvatarMaskBodyPart.LeftFingers or AvatarMaskBodyPart.RightFingers;
                mask.SetHumanoidBodyPartActive(part, isUpper);
            }

            EditorUtility.SetDirty(mask);

            return mask;
        }

        private static void BuildLocomotion(BattlePoseRig rig, AnimatorController controller)
        {
            const float crouch = BattleAnimationLibrary.CrouchDrop;

            AnimationClip idle = Record(rig, "Idle", 2f, true, BattleAnimationLibrary.Idle);
            AnimationClip walkForward = RecordWalk(rig, "WalkForward", 0.46f, Vector2.up, 0.7f, 0.12f, 0.08f);
            AnimationClip walkBack = RecordWalk(rig, "WalkBack", 0.5f, Vector2.down, 0.55f, 0.1f, 0.08f);
            AnimationClip walkLeft = RecordWalk(rig, "WalkLeft", 0.4f, Vector2.left, 0.34f, 0.1f, 0.06f);
            AnimationClip walkRight = RecordWalk(rig, "WalkRight", 0.4f, Vector2.right, 0.34f, 0.1f, 0.06f);
            AnimationClip run = RecordWalk(rig, "Run", 0.38f, Vector2.up, 1f, 0.2f, 0.1f, 8f);
            AnimationClip crouchIdle = Record(rig, "CrouchIdle", 2f, true, _ => BattleAnimationLibrary.Walk(0.25f, Vector2.zero, 0f, 0f, crouch));
            AnimationClip crouchForward = RecordWalk(rig, "CrouchForward", 0.5f, Vector2.up, 0.42f, 0.08f, crouch);
            AnimationClip crouchBack = RecordWalk(rig, "CrouchBack", 0.5f, Vector2.down, 0.3f, 0.08f, crouch);
            AnimationClip crouchLeft = RecordWalk(rig, "CrouchLeft", 0.44f, Vector2.left, 0.26f, 0.08f, crouch);
            AnimationClip crouchRight = RecordWalk(rig, "CrouchRight", 0.44f, Vector2.right, 0.26f, 0.08f, crouch);
            AnimationClip air = Record(rig, "Air", 0.5f, true, _ => BattleAnimationLibrary.Air());
            AnimationClip death = Record(rig, "Death", 0.9f, false, time => BattleAnimationLibrary.Death(time / 0.8f));

            AnimatorState locomotion = controller.CreateBlendTreeInController(FighterAnimComponent.LocomotionState, out BlendTree root, 0);
            root.blendType = BlendTreeType.Simple1D;
            root.blendParameter = FighterAnimComponent.CrouchParam;
            root.useAutomaticThresholds = false;

            BlendTree stand = CreateMoveTree(root, "Stand", 0f);
            stand.AddChild(idle, Vector2.zero);
            stand.AddChild(walkForward, new Vector2(0f, 1f));
            stand.AddChild(walkBack, new Vector2(0f, -0.7f));
            stand.AddChild(walkLeft, new Vector2(-0.85f, 0f));
            stand.AddChild(walkRight, new Vector2(0.85f, 0f));
            stand.AddChild(run, new Vector2(0f, 1.7f));

            BlendTree crouched = CreateMoveTree(root, "Crouch", 1f);
            crouched.AddChild(crouchIdle, Vector2.zero);
            crouched.AddChild(crouchForward, new Vector2(0f, 0.53f));
            crouched.AddChild(crouchBack, new Vector2(0f, -0.37f));
            crouched.AddChild(crouchLeft, new Vector2(-0.45f, 0f));
            crouched.AddChild(crouchRight, new Vector2(0.45f, 0f));

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            stateMachine.defaultState = locomotion;
            stateMachine.AddState(FighterAnimComponent.AirState).motion = air;
            stateMachine.AddState(FighterAnimComponent.DeathState).motion = death;
        }

        private static BlendTree CreateMoveTree(BlendTree parent, string name, float threshold)
        {
            BlendTree tree = parent.CreateBlendTreeChild(threshold);
            tree.name = name;
            tree.blendType = BlendTreeType.FreeformCartesian2D;
            tree.blendParameter = FighterAnimComponent.MoveXParam;
            tree.blendParameterY = FighterAnimComponent.MoveYParam;

            return tree;
        }

        private static void BuildWeapon(BattlePoseRig rig, AnimatorStateMachine stateMachine, WeaponDefinition weapon)
        {
            string prefix = weapon.Prefix;
            AddState(stateMachine, prefix + FighterAnimComponent.IdleSuffix,
                Record(rig, prefix + FighterAnimComponent.IdleSuffix, 1f, true, _ => weapon.Idle));

            for (int i = 0; i < weapon.Attacks.Length; i++)
            {
                AttackDefinition attack = weapon.Attacks[i];
                AddKeyed(rig, stateMachine, prefix + FighterAnimComponent.AttackSuffix + i, attack.Duration,
                    BattleAnimationLibrary.AttackKeys(weapon, attack));
            }

            if (weapon.CanBlock)
            {
                AddKeyed(rig, stateMachine, prefix + FighterAnimComponent.BlockSuffix, weapon.BlockRaise + 0.1f,
                    BattleAnimationLibrary.BlockKeys(weapon));
                AddKeyed(rig, stateMachine, prefix + FighterAnimComponent.BlockImpactSuffix, weapon.BlockImpact + weapon.BlockRecovery,
                    BattleAnimationLibrary.BlockImpactKeys(weapon));
            }

            if (weapon.Attacks.Length > 0)
            {
                AddKeyed(rig, stateMachine, prefix + FighterAnimComponent.DeflectSuffix, weapon.DeflectDuration,
                    BattleAnimationLibrary.DeflectKeys(weapon));
            }

            if (weapon.Kind == WeaponKind.Ranged)
            {
                AddKeyed(rig, stateMachine, prefix + FighterAnimComponent.DrawSuffix, BattleAnimationLibrary.FullDrawTime + 0.1f,
                    BattleAnimationLibrary.DrawKeys(weapon));
                AddKeyed(rig, stateMachine, prefix + FighterAnimComponent.ReleaseSuffix, BattleAnimationLibrary.ReloadTime,
                    BattleAnimationLibrary.ReleaseKeys(weapon));
            }
        }

        private static void AddKeyed(BattlePoseRig rig, AnimatorStateMachine stateMachine, string name, float duration, List<PoseKey> keys)
        {
            AddState(stateMachine, name, Record(rig, name, duration, false, time => BattleAnimationLibrary.Sample(keys, time)));
        }

        private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, AnimationClip clip)
        {
            AnimatorState state = stateMachine.AddState(name);
            state.motion = clip;
            state.mirrorParameterActive = true;
            state.mirrorParameter = FighterAnimComponent.MirrorParam;

            return state;
        }

        private static AnimationClip RecordWalk(BattlePoseRig rig, string name, float cycle, Vector2 direction, float stride,
            float lift, float drop, float lean = 0f)
        {
            return Record(rig, name, cycle, true,
                time => BattleAnimationLibrary.Walk(time / cycle, direction, stride, lift, drop, lean));
        }

        private static AnimationClip Record(BattlePoseRig rig, string name, float duration, bool isLoop, Func<float, BodyPose> evaluate)
        {
            int frames = Mathf.Max(1, Mathf.CeilToInt(duration * FrameRate));
            AnimationCurve[] muscles = new AnimationCurve[BodyMuscleCount];
            AnimationCurve[] root = new AnimationCurve[s_rootCurves.Length];

            for (int i = 0; i < muscles.Length; i++)
                muscles[i] = new AnimationCurve();

            for (int i = 0; i < root.Length; i++)
                root[i] = new AnimationCurve();

            Quaternion previousRotation = Quaternion.identity;

            for (int frame = 0; frame <= frames; frame++)
            {
                float time = duration * frame / frames;
                rig.Apply(evaluate(isLoop && frame == frames ? 0f : time));
                HumanPose pose = rig.Capture();

                Quaternion rotation = pose.bodyRotation;

                if (frame > 0 && Quaternion.Dot(previousRotation, rotation) < 0f)
                    rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);

                previousRotation = rotation;

                for (int i = 0; i < muscles.Length; i++)
                    muscles[i].AddKey(time, pose.muscles[i]);

                root[0].AddKey(time, pose.bodyPosition.x);
                root[1].AddKey(time, pose.bodyPosition.y);
                root[2].AddKey(time, pose.bodyPosition.z);
                root[3].AddKey(time, rotation.x);
                root[4].AddKey(time, rotation.y);
                root[5].AddKey(time, rotation.z);
                root[6].AddKey(time, rotation.w);
            }

            AnimationClip clip = GetClip(name);
            clip.ClearCurves();
            clip.frameRate = FrameRate;

            for (int i = 0; i < muscles.Length; i++)
                SetCurve(clip, HumanTrait.MuscleName[i], muscles[i]);

            for (int i = 0; i < root.Length; i++)
                SetCurve(clip, s_rootCurves[i], root[i]);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = isLoop;
            settings.loopBlendOrientation = true;
            settings.loopBlendPositionY = true;
            settings.loopBlendPositionXZ = true;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);

            return clip;
        }

        private static AnimationClip GetClip(string name)
        {
            string path = $"{BattleEditorUtility.AnimationsFolder}/{name}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip != null)
                return clip;

            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);

            return clip;
        }

        private static void SetCurve(AnimationClip clip, string property, AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0f);

            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), property), curve);
        }
    }
}
