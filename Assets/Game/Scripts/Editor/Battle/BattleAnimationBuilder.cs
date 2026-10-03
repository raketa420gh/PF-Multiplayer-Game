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

        public const string ZombieControllerPath = BattleEditorUtility.AnimationsFolder + "/Zombie.overrideController";

        private const int BodyMuscleCount = 55;
        private const float FingerCurl = -0.8f;
        private const float MaxStrideScale = 2f;
        private const float SprintSpeed = 1.44f;
        private const string UpperLayerName = "Upper";
        private const string HitLayerName = "Hit";

        private static readonly string[] s_rootCurves = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };

        [MenuItem("Tools/Game/Battle/Build Animations")]
        public static void Build()
        {
            Build(Game.Scripts.Editor.Dungeon.DungeonWeaponLibrary.CreateAll());
        }

        public static void Build(WeaponDefinition[] weapons)
        {
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.AnimationsFolder);
            BattleCharacterBuilder.Build();

            using BattlePoseRig rig = new BattlePoseRig();

            AnimatorController controller = CreateController();
            BuildLocomotion(rig, controller);
            AnimatorStateMachine upper = controller.layers[1].stateMachine;

            System.Collections.Generic.HashSet<string> built = new();

            foreach (WeaponDefinition weapon in weapons)
            {
                if (built.Add(weapon.Prefix))
                    BuildWeapon(rig, upper, weapon);
            }

            BuildActions(upper);
            AddKeyed(rig, upper, FighterAnimComponent.CastFirstPersonState, 2f, BattleAnimationLibrary.CastKeys());
            AddKeyed(rig, upper, FighterAnimComponent.UseFirstPersonState, 2f, BattleAnimationLibrary.UseKeys());
            BuildHitReactions(controller.layers[2].stateMachine);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(BattleAnimationBuilder)}] Animations built in {BattleEditorUtility.AnimationsFolder}");
        }

        private static AnimatorController CreateController()
        {
            AssetDatabase.DeleteAsset(BattleEditorUtility.ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BattleEditorUtility.ControllerPath);

            controller.AddParameter(FighterAnimComponent.MoveXParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(FighterAnimComponent.MoveYParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(FighterAnimComponent.CrouchParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(FighterAnimComponent.MirrorParam, AnimatorControllerParameterType.Bool);
            controller.AddParameter(FighterAnimComponent.ActionSpeedParam, AnimatorControllerParameterType.Float);
            controller.AddLayer(UpperLayerName);
            controller.AddLayer(HitLayerName);

            AnimatorControllerLayer[] layers = controller.layers;
            layers[1].defaultWeight = 1f;
            layers[1].avatarMask = CreateMask("UpperBody", AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers);
            layers[2].defaultWeight = 0f;
            layers[2].avatarMask = CreateMask("Torso", AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head);
            controller.layers = layers;

            return controller;
        }

        private static AvatarMask CreateMask(string name, params AvatarMaskBodyPart[] parts)
        {
            string path = $"{BattleEditorUtility.AnimationsFolder}/{name}.mask";
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);

            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, path);
            }

            for (AvatarMaskBodyPart part = 0; part < AvatarMaskBodyPart.LastBodyPart; part++)
                mask.SetHumanoidBodyPartActive(part, Array.IndexOf(parts, part) >= 0);

            EditorUtility.SetDirty(mask);

            return mask;
        }

        /// Legs follow the animation library where it has a matching clip; strafes stay procedural, backpedal is the forward clip reversed.
        private static void BuildLocomotion(BattlePoseRig rig, AnimatorController controller)
        {
            const float crouch = BattleAnimationLibrary.CrouchDrop;

            MovementConfig movement = BattleEditorUtility.LoadOrCreate<MovementConfig>($"{BattleEditorUtility.ConfigsFolder}/Movement.asset");
            float runSpeed = movement.RunSpeed;
            float walkSpeed = runSpeed * movement.WalkMultiplier;
            float crouchSpeed = walkSpeed * movement.CrouchMultiplier;
            float backpedal = movement.BackpedalMultiplier;

            AnimationClip idle = RecordLegs(rig, "Idle", "Idle_Loop", 0.03f);
            AnimationClip walkForward = RecordLegs(rig, "WalkForward", "Walk_Loop", 0.08f, walkSpeed);
            AnimationClip walkBack = RecordLegs(rig, "WalkBack", "Walk_Loop", 0.08f, -walkSpeed * backpedal);
            AnimationClip walkLeft = RecordWalk(rig, "WalkLeft", 0.4f, Vector2.left, 0.34f, 0.1f, 0.06f);
            AnimationClip walkRight = RecordWalk(rig, "WalkRight", 0.4f, Vector2.right, 0.34f, 0.1f, 0.06f);
            AnimationClip run = RecordLegs(rig, "Run", "Jog_Fwd_Loop", 0.1f, runSpeed, 8f);
            AnimationClip sprint = RecordLegs(rig, "Sprint", "Sprint_Loop", 0.1f, runSpeed * SprintSpeed, 8f);
            AnimationClip crouchIdle = RecordLegs(rig, "CrouchIdle", "Crouch_Idle_Loop", crouch);
            AnimationClip crouchForward = RecordLegs(rig, "CrouchForward", "Crouch_Fwd_Loop", crouch, crouchSpeed);
            AnimationClip crouchBack = RecordLegs(rig, "CrouchBack", "Crouch_Fwd_Loop", crouch, -crouchSpeed * backpedal);
            AnimationClip crouchLeft = RecordWalk(rig, "CrouchLeft", 0.44f, Vector2.left, 0.26f, 0.08f, crouch);
            AnimationClip crouchRight = RecordWalk(rig, "CrouchRight", 0.44f, Vector2.right, 0.26f, 0.08f, crouch);
            AnimationClip air = RecordLegs(rig, "Air", "Jump_Loop", 0f);

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
            stand.AddChild(run, new Vector2(0f, 2.5f));
            stand.AddChild(sprint, new Vector2(0f, 2.5f * SprintSpeed));

            BlendTree crouched = CreateMoveTree(root, "Crouch", 1f);
            crouched.AddChild(crouchIdle, Vector2.zero);
            crouched.AddChild(crouchForward, new Vector2(0f, 0.53f));
            crouched.AddChild(crouchBack, new Vector2(0f, -0.37f));
            crouched.AddChild(crouchLeft, new Vector2(-0.45f, 0f));
            crouched.AddChild(crouchRight, new Vector2(0.45f, 0f));

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            stateMachine.defaultState = locomotion;
            stateMachine.AddState(FighterAnimComponent.AirState).motion = air;
            stateMachine.AddState(FighterAnimComponent.DeathState).motion = BattleEditorUtility.LoadLibraryClip("Death01");

            BuildZombie(controller, idle, RecordLegs(rig, "ZombieIdle", "Zombie_Idle_Loop", 0.03f),
                new[] { walkForward, run, sprint }, RecordLegs(rig, "ZombieWalk", "Zombie_Walk_Fwd_Loop", 0.08f));
        }

        /// Puts the legs of a library clip on the simulation body. The feet keep their place relative to the hips, while the
        /// hips stay where the combat model has them (only the vertical bob is taken over): the upper body, the hitboxes and
        /// the weapon traces are the same whatever the legs do. A non-zero speed retimes the cycle so the stride covers that
        /// ground speed (negative = played backwards).
        private static AnimationClip RecordLegs(BattlePoseRig rig, string name, string library, float drop, float speed = 0f, float lean = 0f)
        {
            const int samples = 24;
            AnimationClip source = BattleEditorUtility.LoadLibraryClip(library);
            float height = 0f;
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < samples; i++)
            {
                BodyPose pose = rig.SampleLegs(source, source.length * i / samples);
                height += pose.Hips.y / samples;
                min = Mathf.Min(min, pose.LeftFoot.z);
                max = Mathf.Max(max, pose.LeftFoot.z);
            }

            // Two steps per loop: the cycle is as long as its stride needs at the given speed.
            float stride = (max - min) * 2f;
            float duration = speed == 0f ? source.length : Mathf.Clamp(stride / Mathf.Abs(speed), source.length / MaxStrideScale, source.length);

            return Record(rig, name, duration, source.isLooping, time =>
            {
                float phase = time / duration;
                BodyPose pose = rig.SampleLegs(source, source.length * (speed < 0f ? 1f - phase : phase));
                pose.Hips = new Vector3(0f, pose.Hips.y - height - drop, 0f);
                pose.HipsEuler = new Vector3(lean, 0f, 0f);

                return pose;
            });
        }

        /// Busy actions as others see them come from the library as is: spells and levers with the off hand, throws with the main
        /// hand. Casting and drinking happen beside the head, so the player's own view keeps the generated in-view poses.
        private static void BuildActions(AnimatorStateMachine stateMachine)
        {
            AnimatorState cast = AddState(stateMachine, FighterAnimComponent.CastState, BattleEditorUtility.LoadLibraryClip("Spell_Simple_Enter"));
            AnimatorState castLoop = AddState(stateMachine, FighterAnimComponent.CastState + "Loop", BattleEditorUtility.LoadLibraryClip("Spell_Simple_Idle_Loop"));
            AnimatorStateTransition transition = cast.AddTransition(castLoop);
            transition.hasExitTime = true;
            transition.exitTime = 0.9f;
            transition.duration = 0.1f;

            AddState(stateMachine, FighterAnimComponent.InteractState, BattleEditorUtility.LoadLibraryClip("Interact_Loop"));
            AddState(stateMachine, FighterAnimComponent.OpenState, BattleEditorUtility.LoadLibraryClip("Chest_Open"));
            AddState(stateMachine, FighterAnimComponent.ThrowState, BattleEditorUtility.LoadLibraryClip("Throw"));
            AddState(stateMachine, FighterAnimComponent.PickUpState, BattleEditorUtility.LoadLibraryClip("PickUp_Table"));

            // The belt item sits in the right hand while the library drinks with the left: the state is flipped for good.
            // Mirroring a looped clip starts it half a cycle later, which the offset takes back.
            AnimatorState use = AddState(stateMachine, FighterAnimComponent.UseState, BattleEditorUtility.LoadLibraryClip("Consume_Loop"));
            use.mirrorParameterActive = false;
            use.mirror = true;
            use.cycleOffset = 0.5f;
        }

        private static void BuildHitReactions(AnimatorStateMachine stateMachine)
        {
            stateMachine.AddState(FighterAnimComponent.HitChestState).motion = BattleEditorUtility.LoadLibraryClip("Hit_Chest");
            stateMachine.AddState(FighterAnimComponent.HitHeadState).motion = BattleEditorUtility.LoadLibraryClip("Hit_Head");
        }

        /// Same state machine with shambling legs for the zombie.
        private static void BuildZombie(AnimatorController controller, AnimationClip idle, AnimationClip zombieIdle, AnimationClip[] moves, AnimationClip zombieWalk)
        {
            AnimatorOverrideController zombie = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ZombieControllerPath);

            if (zombie == null)
            {
                zombie = new AnimatorOverrideController();
                AssetDatabase.CreateAsset(zombie, ZombieControllerPath);
            }

            zombie.runtimeAnimatorController = controller;
            zombie[idle] = zombieIdle;

            foreach (AnimationClip move in moves)
                zombie[move] = zombieWalk;

            EditorUtility.SetDirty(zombie);
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
                float drawTime = weapon.DrawTime > 0f ? weapon.DrawTime : BattleAnimationLibrary.FullDrawTime;
                float reloadTime = weapon.ReloadTime > 0f ? weapon.ReloadTime : BattleAnimationLibrary.ReloadTime;
                AddKeyed(rig, stateMachine, prefix + FighterAnimComponent.DrawSuffix, drawTime + 0.1f,
                    BattleAnimationLibrary.DrawKeys(weapon, drawTime));
                AddKeyed(rig, stateMachine, prefix + FighterAnimComponent.ReleaseSuffix, reloadTime,
                    BattleAnimationLibrary.ReleaseKeys(weapon, reloadTime));
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
            state.speedParameterActive = true;
            state.speedParameter = FighterAnimComponent.ActionSpeedParam;

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

            // Keys sit on the simulation's 60 Hz grid, so the trace sampler and tick-aligned playback read authored poses, not blends.
            for (int frame = 0; frame <= frames; frame++)
            {
                float time = Mathf.Min(frame / FrameRate, duration);
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

            // Hands that hold something close into a grip; the spread muscles stay neutral.
            for (int i = BodyMuscleCount; i < HumanTrait.MuscleCount && evaluate(0f).HasHands; i++)
            {
                if (HumanTrait.MuscleName[i].Contains("Stretched"))
                    SetCurve(clip, HumanTrait.MuscleName[i], AnimationCurve.Constant(0f, duration, FingerCurl));
            }

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
