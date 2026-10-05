using System;
using System.Collections.Generic;
using System.Linq;
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
        private const float FingerCurl = -0.8f;
        private const float FingerOpen = 0.4f;
        private const float MaxStrideScale = 2f;
        private const float SprintSpeed = 1.44f;
        private const float BandageCycle = 0.9f;
        private const float IdleDrop = FighterAnimComponent.IdleDrop;
        private const float FootworkStride = 0.5f;
        private const float FootworkBob = 0.25f;
        /// A cut whose grip ends less than this far to the side of where it was raised comes down from above.
        private const float OverheadSide = 0.3f;
        private const float LibraryFrame = 1f / 30f;
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
                    BuildWeapon(rig, upper, controller.layers[0].stateMachine, weapon);
            }

            BuildActions(upper);
            AddKeyed(rig, upper, FighterAnimComponent.CastFirstPersonState, BattleAnimationLibrary.CastChargeTime, SettleRoll(rig, BattleAnimationLibrary.CastKeys()));
            AddKeyed(rig, upper, FighterAnimComponent.CastReleaseFirstPersonState, BattleAnimationLibrary.CastReleaseTime, SettleRoll(rig, BattleAnimationLibrary.CastReleaseKeys()));
            List<PoseKey> use = SettleRoll(rig, BattleAnimationLibrary.UseKeys());
            AddKeyed(rig, upper, FighterAnimComponent.UseFirstPersonState, BattleAnimationLibrary.DrinkTime, use);
            AddState(upper, FighterAnimComponent.HoldState, Record(rig, FighterAnimComponent.HoldState, 1f, true, _ => use[0].Pose));
            AddState(upper, FighterAnimComponent.BandageFirstPersonState, Record(rig, FighterAnimComponent.BandageFirstPersonState, BandageCycle, true,
                time => BattleAnimationLibrary.Bandage(time / BandageCycle)));
            BuildHitReactions(controller.layers[2].stateMachine);
            BakeEdits();

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            ReloadUsers();
            Debug.Log($"[{nameof(BattleAnimationBuilder)}] Animations built in {BattleEditorUtility.AnimationsFolder}");
        }

        /// A loaded prefab goes on pointing at the controller that was deleted. Those no builder saves anew are loaded again.
        private static void ReloadUsers()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (Array.IndexOf(AssetDatabase.GetDependencies(path, false), BattleEditorUtility.ControllerPath) >= 0)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        /// Hand edits made in the animation test scene go back onto the freshly generated clips.
        private static void BakeEdits()
        {
            AnimationEditConfig edits = AssetDatabase.LoadAssetAtPath<AnimationEditConfig>(AnimationEditBaker.ConfigPath);

            if (edits == null || edits.Clips.Count == 0)
                return;

            GameObject model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath));
            Animator animator = model.GetComponent<Animator>();
            BattlePoseRig.CreateSockets(animator);
            AnimationEditBaker.BakeAll(edits, animator);
            UnityEngine.Object.DestroyImmediate(model);
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

            AnimationClip idle = RecordLegs(rig, "Idle", "Idle_Loop", IdleDrop);
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
            AnimationClip jump = RecordLegs(rig, "Jump", "Jump_Rise", 0f);
            AnimationClip land = RecordLegs(rig, "Land", "Jump_Land", 0f, anchor: 1f);
            AnimationClip rest = RecordLegs(rig, "Rest", "Kneel_Loop", crouch);

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
            stateMachine.AddState(FighterAnimComponent.JumpState).motion = jump;
            stateMachine.AddState(FighterAnimComponent.LandState).motion = land;
            stateMachine.AddState(FighterAnimComponent.RestState).motion = rest;
            stateMachine.AddState(FighterAnimComponent.DeathState).motion = BattleEditorUtility.LoadLibraryClip("Death01");
        }

        /// Puts the legs of a library clip on the simulation body. The feet keep their place relative to the hips, while the
        /// hips stay where the combat model has them (only the vertical bob is taken over): the upper body, the hitboxes and
        /// the weapon traces are the same whatever the legs do. A non-zero speed retimes the cycle so the stride covers that
        /// ground speed (negative = played backwards). The bob is measured from the average height of the hips, or from
        /// their height at the anchor moment (a share of the clip) for clips that start or end standing.
        private static AnimationClip RecordLegs(BattlePoseRig rig, string name, string library, float drop, float speed = 0f, float lean = 0f,
            float anchor = -1f)
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

            if (anchor >= 0f)
                height = rig.SampleLegs(source, source.length * anchor).Hips.y;

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

        /// The feet under the swing of a fighter who stands still, from the strike of the library that goes the same way.
        /// Its stride is retimed to land at the peak and shortened to what the legs reach from hips that stay where the
        /// combat model has them; of its crouch only a share is taken over.
        private static AnimationClip RecordAttackLegs(BattlePoseRig rig, string name, WeaponDefinition weapon, AttackDefinition attack, int index)
        {
            const int samples = 24;
            (string strike, string recovery, float blow) = Footwork(weapon, attack, index);
            AnimationClip first = BattleEditorUtility.LoadLibraryClip(strike);
            AnimationClip second = recovery == null ? first : BattleEditorUtility.LoadLibraryClip(recovery);
            float length = recovery == null ? first.length : first.length + second.length;
            float peak = BattleAnimationLibrary.PeakTime(attack);
            float height = float.MinValue;

            BodyPose Legs(float time) => recovery == null || time < first.length ? rig.SampleLegs(first, time) : rig.SampleLegs(second, time - first.length);

            for (int i = 0; i <= samples; i++)
                height = Mathf.Max(height, Legs(length * i / samples).Hips.y);

            return Record(rig, name, attack.Duration, false, time =>
            {
                BodyPose pose = Legs(time < peak ? blow * time / peak : Mathf.Lerp(blow, length, (time - peak) / (attack.Duration - peak)));
                pose.Hips = new Vector3(0f, (pose.Hips.y - height) * FootworkBob - IdleDrop, 0f);
                pose.LeftFoot *= FootworkStride;
                pose.RightFoot *= FootworkStride;

                return pose;
            });
        }

        /// The strike of the library whose feet go with an attack, the take it recovers with when that is a clip of its
        /// own, and the moment its feet are set for the blow.
        private static (string strike, string recovery, float blow) Footwork(WeaponDefinition weapon, AttackDefinition attack, int index)
        {
            float side = attack.WindupPose.Main.Position.x - attack.EndPose.Main.Position.x;

            if (weapon.IsUnarmed)
                return index % 2 == 0 ? ("Punch_Jab", null, 5f * LibraryFrame) : ("Punch_Cross", null, 7f * LibraryFrame);

            if (!BattleAnimationLibrary.IsCut(weapon, attack))
                return ("Melee_Hook", "Melee_Hook_Rec", 8f * LibraryFrame);

            if (Mathf.Abs(side) < OverheadSide)
                return ("Sword_Attack", null, 14f * LibraryFrame);

            return side > 0f ? ("Sword_Regular_A", "Sword_Regular_A_Rec", 8f * LibraryFrame) : ("Sword_Regular_B", "Sword_Regular_B_Rec", 8f * LibraryFrame);
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

            AddState(stateMachine, FighterAnimComponent.CastReleaseState, BattleEditorUtility.LoadLibraryClip("Spell_Simple_Shoot"));
            AddState(stateMachine, FighterAnimComponent.BandageState, BattleEditorUtility.LoadLibraryClip("Bandage_Loop"));
            AddState(stateMachine, FighterAnimComponent.InteractState, BattleEditorUtility.LoadLibraryClip("Interact_Loop"));
            AddState(stateMachine, FighterAnimComponent.OpenState, BattleEditorUtility.LoadLibraryClip("Chest_Open"));
            AddState(stateMachine, FighterAnimComponent.ThrowState, BattleEditorUtility.LoadLibraryClip("Throw"));
            AddState(stateMachine, FighterAnimComponent.PickUpState, BattleEditorUtility.LoadLibraryClip("PickUp_Table"));

            // The belt item sits in the right hand while the library drinks with the left: the state is flipped for good.
            // Mirroring a looped clip starts it half a cycle later, which the offset takes back.
            AnimatorState use = AddState(stateMachine, FighterAnimComponent.UseState, BattleEditorUtility.LoadLibraryClip("Consume_Loop"));
            Flip(use);
            use.cycleOffset = 0.5f;
        }

        private static void BuildHitReactions(AnimatorStateMachine stateMachine)
        {
            stateMachine.AddState(FighterAnimComponent.HitChestState).motion = BattleEditorUtility.LoadLibraryClip("Hit_Chest");
            stateMachine.AddState(FighterAnimComponent.HitHeadState).motion = BattleEditorUtility.LoadLibraryClip("Hit_Head");
            stateMachine.AddState(FighterAnimComponent.HitStaggerState).motion = BattleEditorUtility.LoadLibraryClip("Idle_Shield_Break");
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

        private static void BuildWeapon(BattlePoseRig rig, AnimatorStateMachine stateMachine, AnimatorStateMachine legs, WeaponDefinition weapon)
        {
            string prefix = weapon.Prefix;

            Settle(rig, weapon);

            List<PoseKey> idle = BattleAnimationLibrary.IdleKeys(weapon);
            AddState(stateMachine, prefix + FighterAnimComponent.IdleSuffix,
                Record(rig, prefix + FighterAnimComponent.IdleSuffix, idle[^1].Time, true, time => BattleAnimationLibrary.Sample(idle, time)));

            for (int i = 0; i < weapon.Attacks.Length; i++)
                BuildSwing(rig, stateMachine, legs, weapon, weapon.Attacks[i], i,
                    prefix + FighterAnimComponent.AttackSuffix + i, prefix + FighterAnimComponent.AttackLegsSuffix + i);

            if (weapon.Riposte != null)
                BuildSwing(rig, stateMachine, legs, weapon, weapon.Riposte, weapon.Attacks.Length,
                    prefix + FighterAnimComponent.RiposteSuffix, prefix + FighterAnimComponent.RiposteLegsSuffix);

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

        private static void BuildSwing(BattlePoseRig rig, AnimatorStateMachine stateMachine, AnimatorStateMachine legs, WeaponDefinition weapon,
            AttackDefinition attack, int index, string name, string legsName)
        {
            AnimatorState swing = AddKeyed(rig, stateMachine, name, attack.Duration, BattleAnimationLibrary.AttackKeys(weapon, attack));
            AnimatorState footwork = AddState(legs, legsName, RecordAttackLegs(rig, legsName, weapon, attack, index));

            if (!attack.IsOffHand)
                return;

            Flip(swing);
            Flip(footwork);
        }

        /// The authored poses take the roll the arm solve gives them, and between them the weapon turns from key to key:
        /// a roll solved anew on every frame flips over whenever the blade passes the line of the forearm. So do the
        /// poses a swing passes on its way up and back.
        public static void Settle(BattlePoseRig rig, WeaponDefinition weapon)
        {
            SettleRoll(rig, ref weapon.Idle);
            SettleRoll(rig, ref weapon.IdleBreath);
            SettleRoll(rig, ref weapon.Block);
            SettleRoll(rig, ref weapon.BlockHit);
            SettleRoll(rig, ref weapon.BlockLowered);
            SettleRoll(rig, ref weapon.DeflectPose);
            SettleRoll(rig, ref weapon.DrawPose);
            SettleRoll(rig, ref weapon.ReleasePose);

            foreach (AttackDefinition attack in weapon.Riposte == null ? weapon.Attacks : weapon.Attacks.Append(weapon.Riposte))
            {
                SettleRoll(rig, attack.Raise);
                SettleRoll(rig, attack.Return);
            }
        }

        private static void SettleRoll(BattlePoseRig rig, ref BodyPose pose)
        {
            if (pose.HasHands)
                rig.SolveEdge(ref pose);
        }

        private static List<PoseKey> SettleRoll(BattlePoseRig rig, List<PoseKey> keys)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                PoseKey key = keys[i];
                SettleRoll(rig, ref key.Pose);
                keys[i] = key;
            }

            return keys;
        }

        private static AnimatorState AddKeyed(BattlePoseRig rig, AnimatorStateMachine stateMachine, string name, float duration, List<PoseKey> keys)
        {
            return AddState(stateMachine, name, Record(rig, name, duration, false, time => BattleAnimationLibrary.Sample(keys, time)));
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

        /// How far the fingers of a hand are curled over a clip: a single value while the hand keeps its grip.
        private static AnimationCurve Curl(BodyPose[] poses, float duration, bool isOff)
        {
            AnimationCurve curve = new AnimationCurve();

            for (int frame = 0; frame < poses.Length; frame++)
                curve.AddKey(Mathf.Min(frame / FrameRate, duration), Mathf.Lerp(FingerCurl, FingerOpen, isOff ? poses[frame].OffOpen : poses[frame].MainOpen));

            return Array.TrueForAll(curve.keys, key => key.value == curve[0].value) ? AnimationCurve.Constant(0f, duration, curve[0].value) : curve;
        }

        /// The state plays its clip mirrored whichever hand the weapon is in.
        private static void Flip(AnimatorState state)
        {
            state.mirrorParameterActive = false;
            state.mirror = true;
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
            BodyPose[] poses = new BodyPose[frames + 1];

            // Keys sit on the simulation's 60 Hz grid, so the trace sampler and tick-aligned playback read authored poses, not blends.
            for (int frame = 0; frame <= frames; frame++)
                poses[frame] = evaluate(isLoop && frame == frames ? 0f : Mathf.Min(frame / FrameRate, duration));

            rig.Plan(poses, isLoop);

            for (int frame = 0; frame <= frames; frame++)
            {
                float time = Mathf.Min(frame / FrameRate, duration);
                rig.Apply(poses[frame], frame);
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

            // Hands that hold something close into a grip, free ones open as far as the poses say; the spread muscles stay neutral.
            AnimationCurve[] curls = { Curl(poses, duration, false), Curl(poses, duration, true) };

            for (int i = BodyMuscleCount; i < HumanTrait.MuscleCount && poses[0].HasHands; i++)
            {
                string[] muscle = HumanTrait.MuscleName[i].Split(' ');

                if (muscle[^1] != "Stretched")
                    continue;

                // The curve of a finger is named after its hand: 'Left Index 1 Stretched' is 'LeftHand.Index.1 Stretched'.
                SetCurve(clip, $"{muscle[0]}Hand.{muscle[1]}.{muscle[2]} {muscle[3]}", curls[muscle[0] == "Left" ? 1 : 0]);
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
