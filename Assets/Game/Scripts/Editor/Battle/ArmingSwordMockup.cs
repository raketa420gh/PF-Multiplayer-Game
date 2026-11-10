using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Battle
{
    /// The sword and the body at a clip frame (60 Hz), in the model's space: +Z ahead, +X right, metres.
    internal struct SwordKey
    {
        public float Frame;
        public Vector3 Grip;
        public Vector3 Blade;
        /// Where the leading edge faces; zero = led by the cut, or wherever the arm is at ease.
        public Vector3 Edge;
        /// Where the elbow is drawn to; zero = wherever the arm is at ease.
        public Vector3 Elbow;
        /// The chest against the hips, degrees: bowed forward, turned right, leant right.
        public Vector3 Torso;
        /// 1 = the motion passes through the key, 0 = it stops there.
        public float Flow;
        /// How much more the grip point weighs than on an ordinary key.
        public float Hard;
        public bool IsIdle;
        /// The strike point lies on the crosshair; the grip and the blade given are only where to look for the pose.
        public bool IsAim;

        public SwordKey El(float x, float y, float z)
        {
            Elbow = new Vector3(x, y, z);

            return this;
        }

        public SwordKey To(float bow, float turn, float lean = 0f)
        {
            Torso = new Vector3(bow, turn, lean);

            return this;
        }

        public SwordKey Ed(float x, float y, float z)
        {
            Edge = new Vector3(x, y, z).normalized;

            return this;
        }

        public SwordKey Stop()
        {
            Flow = 0f;

            return this;
        }
    }

    internal sealed class SwordMotion
    {
        public string Name;
        public int Index;
        public int Frames;
        public int Peak;
        public float Windup;
        public float ActiveEnd;
        /// The attack it is chained from and the frame of that attack it starts at; -1 = from the idle.
        public int After = -1;
        public int AfterFrame;
        /// A cut turns its edge into the swing; a thrust keeps the roll the arm is at ease with.
        public bool IsCut = true;
        public List<SwordKey> Keys;
        public float[][] Muscles;
        public Vector3[] BodyPositions;
        public Quaternion[] BodyRotations;
        public Quaternion[] Grips;
        public Vector3[] Torso;
        public float[] Lean;
        public float[] Roll;
        public float[] Strain;
    }

    /// Arming sword attacks authored from footage as a mock-up: the sword is rigid in the fist (one grip point, the hilt may
    /// turn a little in the hand), the arm is solved in humanoid muscles inside the avatar's ranges, so no joint bends the
    /// way a body cannot and the fist never leaves the hilt. Writes the data of the review page; the game is not touched.
    internal static class ArmingSwordMockup
    {
        private const string Prefix = "ArmingSword";
        private const float FrameRate = 60f;
        private const int Body = BattleAnimationBuilder.BodyMuscleCount;
        private const int Layer = 31;
        private const float Strike = 0.627f;
        private const float BladeBase = 0.105f;
        private const float BladeTip = 0.79f;
        // How far the hilt may lie over in the palm toward the forearm's line (a hammer grip sliding to a handshake grip)
        // and turn about itself in the fist, degrees.
        private const float MaxLean = 24f;
        private const float MinLean = -8f;
        private const float MaxRoll = 25f;
        private const float SoftLimit = 0.85f;

        private static readonly Vector3 s_eye = BattleAnimationLibrary.Eye;
        private static readonly string[] s_arm = { "Shoulder Down-Up", "Shoulder Front-Back", "Arm Down-Up", "Arm Front-Back", "Arm Twist In-Out", "Forearm Stretch", "Forearm Twist In-Out", "Hand Down-Up", "Hand In-Out" };
        // What it costs a muscle to leave its rest: shoulders and wrists give way last.
        private static readonly float[] s_ease = { 1f, 1f, 0.05f, 0.05f, 0.12f, 0.03f, 0.2f, 0.3f, 0.35f };
        private static readonly string[] s_rootCurves = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };
        private static readonly string[] s_socketCurves = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };

        private sealed class Stage : IDisposable
        {
            public readonly BattlePoseRig Source = new BattlePoseRig();
            public readonly Animator Animator;
            public readonly Transform Socket, Hips, Spine, Chest, Head, Upper, Elbow, Hand;
            public readonly Transform[] Legs;
            public readonly float SpineHeight;
            public readonly Vector3 PalmNormal;
            public Vector3 GripPosition;
            public Quaternion GripRotation;

            private readonly HumanPoseHandler _handler;
            private HumanPose _pose = new HumanPose { muscles = new float[HumanTrait.MuscleCount] };

            public Stage()
            {
                Animator = Source.Animator;
                Socket = Source.Sockets[(int)WeaponSocket.RightHand];
                GripPosition = Socket.localPosition;
                GripRotation = Socket.localRotation;
                PalmNormal = Socket.localRotation * Vector3.right;
                Hips = Animator.GetBoneTransform(HumanBodyBones.Hips);
                Spine = Animator.GetBoneTransform(HumanBodyBones.Spine);
                Chest = Animator.GetBoneTransform(HumanBodyBones.UpperChest);
                Head = Animator.GetBoneTransform(HumanBodyBones.Head);
                Upper = Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                Elbow = Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                Hand = Animator.GetBoneTransform(HumanBodyBones.RightHand);
                Legs = new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot }
                    .Select(Animator.GetBoneTransform).ToArray();
                SpineHeight = Spine.position.y - FighterAnimComponent.IdleDrop;
                _handler = new HumanPoseHandler(Animator.avatar, Animator.transform);
            }

            public void Dispose()
            {
                _handler.Dispose();
                Source.Dispose();
            }

            /// The pose as the game shows it: the upper body held at the model's height whatever the legs do (HoldUpperBody).
            public void Set(float[] muscles, Vector3 position, Quaternion rotation, float lean, float roll, bool plantsFeet = false)
            {
                Array.Copy(muscles, _pose.muscles, muscles.Length);
                _pose.bodyPosition = position;
                _pose.bodyRotation = rotation;
                _handler.SetHumanPose(ref _pose);
                Socket.SetLocalPositionAndRotation(GripPosition, Grip(lean, roll));
                Vector3 offset = Vector3.up * (SpineHeight - Spine.position.y);

                if (!plantsFeet)
                {
                    Hips.position += offset;

                    return;
                }

                Vector3 left = Legs[2].position, right = Legs[5].position;
                Quaternion leftRotation = Legs[2].rotation, rightRotation = Legs[5].rotation;
                Hips.position += offset;
                TwoBoneIk.Solve(Legs[0], Legs[1], Legs[2], left);
                TwoBoneIk.Solve(Legs[3], Legs[4], Legs[5], right);
                Legs[2].rotation = leftRotation;
                Legs[5].rotation = rightRotation;
            }

            public Quaternion Grip(float lean, float roll)
            {
                return Quaternion.AngleAxis(-lean, PalmNormal) * GripRotation * Quaternion.AngleAxis(roll, Vector3.forward);
            }

            public HumanPose Read()
            {
                HumanPose pose = new HumanPose();
                _handler.GetHumanPose(ref pose);

                return pose;
            }
        }

        private struct Target
        {
            public Vector3 Grip, Blade, Edge, Elbow, Torso;
            public float EdgeWeight, ElbowWeight, Hard, Aim, Idle;
        }

        private sealed class ClipPose
        {
            public AnimationCurve[] Muscles;
            public AnimationCurve[] Root;
            public AnimationCurve[] Socket;

            public float[] Read(float time)
            {
                float[] muscles = new float[HumanTrait.MuscleCount];

                for (int i = 0; i < muscles.Length; i++)
                    muscles[i] = Muscles[i]?.Evaluate(time) ?? 0f;

                return muscles;
            }

            public Vector3 Position(float time) => new Vector3(Root[0].Evaluate(time), Root[1].Evaluate(time), Root[2].Evaluate(time));

            public Quaternion Rotation(float time) =>
                new Quaternion(Root[3].Evaluate(time), Root[4].Evaluate(time), Root[5].Evaluate(time), Root[6].Evaluate(time)).normalized;
        }

        public static readonly string[] Tables = new string[3];

        private static int[] s_right, s_left, s_legs, s_torso, s_neck;
        private static Vector3 s_torsoScale, s_neckScale;
        private static float[] s_idle;
        private static ClipPose s_idleClip;

        /// Solves the attacks of the keys file and writes data.js of the review page into the folder. Returns the numbers.
        public static string Build(string keys, string folder)
        {
            Directory.CreateDirectory(folder);
            StringBuilder report = new StringBuilder();
            Export(folder, Solve(keys, report));

            return report.ToString();
        }

        /// Solves the attacks and renders frames of them: shots "0:12,16|1:20", g_{attack}_{view}_{frame}.png and table{attack}.txt
        /// in the folder. Views: fp, front, side, left, back, top, ref, hand, c_x_y_z_tx_ty_tz_fov.
        public static string Run(string keys, string folder, string shots, string views, int size)
        {
            Directory.CreateDirectory(folder);
            StringBuilder report = new StringBuilder();
            SwordMotion[] motions = Solve(keys, report);

            for (int i = 0; i < motions.Length; i++)
                File.WriteAllText($"{folder}/table{i}.txt", Tables[i]);

            if (shots.Length == 0)
                return report.ToString();

            GameObject sword = AssetDatabase.LoadAssetAtPath<WeaponConfig>($"{BattleEditorUtility.ConfigsFolder}/{Prefix}.asset").Attachments[0].Prefab;
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, -0.02f, 0f);
            floor.transform.localScale = new Vector3(3f, 0.04f, 3f);
            List<GameObject> stage = new List<GameObject> { floor, new GameObject("Camera"), SwingPreview.CreateLight(1.3f, 160f), SwingPreview.CreateLight(0.7f, -40f) };
            bool asyncCompile = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;

            try
            {
                foreach (GameObject item in stage)
                {
                    item.hideFlags = HideFlags.HideAndDontSave;
                    item.layer = Layer;
                }

                Camera camera = stage[1].AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.32f, 0.36f, 0.42f);
                camera.cullingMask = 1 << Layer;
                bool isFirst = true;

                foreach (string shot in shots.Split('|'))
                {
                    SwordMotion motion = motions[int.Parse(shot.Split(':')[0])];

                    foreach (int frame in Array.ConvertAll(shot.Split(':')[1].Split(','), int.Parse))
                    {
                        // Skinning is evaluated once per editor frame, so every moment gets a rig of its own.
                        using Stage rig = CreateStage();
                        int index = Mathf.Clamp(frame, 0, motion.Frames);
                        rig.Set(motion.Muscles[index], motion.BodyPositions[index], motion.BodyRotations[index], motion.Lean[index], motion.Roll[index], true);
                        GameObject held = Object.Instantiate(sword, rig.Socket, false);
                        held.hideFlags = HideFlags.HideAndDontSave;

                        foreach (Transform child in rig.Animator.GetComponentsInChildren<Transform>(true))
                            child.gameObject.layer = Layer;

                        foreach (string view in views.Split(','))
                        {
                            rig.Head.localScale = view.StartsWith("fp") ? Vector3.zero : Vector3.one;
                            Place(camera, view, rig.Hand.position);

                            // The first render of an editor session comes out empty.
                            for (int pass = isFirst ? 0 : 1; pass < 2; pass++)
                                SwingPreview.Render(camera, view.StartsWith("fp") ? size * 16 / 9 : size, size, $"{folder}/g_{motion.Index}_{view}_{frame}.png");

                            isFirst = false;
                        }
                    }
                }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncCompile;

                foreach (GameObject item in stage)
                    Object.DestroyImmediate(item);
            }

            return report.ToString();
        }

        private static void Place(Camera camera, string view, Vector3 hand)
        {
            camera.fieldOfView = 35f;
            camera.nearClipPlane = view.StartsWith("fp") ? 0.1f : 0.04f;

            if (view.StartsWith("c_"))
            {
                float[] v = Array.ConvertAll(view.Substring(2).Split('_'), s => float.Parse(s, CultureInfo.InvariantCulture));
                camera.fieldOfView = v[6];
                camera.transform.position = new Vector3(v[0], v[1], v[2]);
                camera.transform.LookAt(new Vector3(v[3], v[4], v[5]));

                return;
            }

            switch (view)
            {
                case "fp":
                    camera.fieldOfView = 75f;
                    camera.transform.SetPositionAndRotation(s_eye, Quaternion.identity);
                    break;
                case "hand":
                    camera.transform.position = hand + new Vector3(0.45f, 0.2f, 0.55f);
                    camera.transform.LookAt(hand + new Vector3(0f, 0.03f, 0.05f));
                    break;
                case "front":
                    camera.transform.position = new Vector3(0f, 1.5f, 4.4f);
                    camera.transform.LookAt(new Vector3(0f, 1.2f, 0f));
                    break;
                case "side":
                    camera.transform.position = new Vector3(4.4f, 1.5f, 0.3f);
                    camera.transform.LookAt(new Vector3(0f, 1.2f, 0.3f));
                    break;
                case "left":
                    camera.transform.position = new Vector3(-4.4f, 1.5f, 0.3f);
                    camera.transform.LookAt(new Vector3(0f, 1.2f, 0.3f));
                    break;
                case "back":
                    camera.transform.position = new Vector3(0.8f, 2f, -4f);
                    camera.transform.LookAt(new Vector3(0f, 1.25f, 0.3f));
                    break;
                case "ref":
                    camera.transform.position = new Vector3(3f, 1.7f, 3.3f);
                    camera.transform.LookAt(new Vector3(0f, 1.2f, 0.2f));
                    break;
                default:
                    camera.transform.SetPositionAndRotation(new Vector3(0f, 6.5f, 0.3f), Quaternion.LookRotation(Vector3.down, Vector3.forward));
                    break;
            }
        }

        private static Stage CreateStage()
        {
            Stage stage = new Stage();

            if (s_idleClip.Socket[0] == null)
                return stage;

            stage.GripPosition = new Vector3(s_idleClip.Socket[0].Evaluate(0f), s_idleClip.Socket[1].Evaluate(0f), s_idleClip.Socket[2].Evaluate(0f));
            stage.GripRotation = new Quaternion(s_idleClip.Socket[3].Evaluate(0f), s_idleClip.Socket[4].Evaluate(0f), s_idleClip.Socket[5].Evaluate(0f),
                s_idleClip.Socket[6].Evaluate(0f)).normalized;

            return stage;
        }

        private static SwordMotion[] Solve(string keys, StringBuilder report)
        {
            s_right = Array.ConvertAll(s_arm, name => Muscle("Right " + name));
            s_left = Array.ConvertAll(s_arm, name => Muscle("Left " + name));
            s_legs = Enumerable.Range(0, Body).Where(i => HumanTrait.MuscleName[i].Contains("Leg") || HumanTrait.MuscleName[i].Contains("Foot") || HumanTrait.MuscleName[i].Contains("Toes")).ToArray();
            s_torso = new[] { "Spine", "Chest", "UpperChest" }.SelectMany(bone => new[] { " Front-Back", " Left-Right", " Twist Left-Right" }.Select(axis => Muscle(bone + axis))).ToArray();
            s_neck = new[] { "Neck", "Head" }.SelectMany(bone => new[] { " Nod Down-Up", " Tilt Left-Right", " Turn Left-Right" }.Select(axis => Muscle(bone + axis))).ToArray();
            string socketPath;

            using (Stage probe = new Stage())
                socketPath = AnimationUtility.CalculateTransformPath(probe.Socket, probe.Animator.transform);

            s_idleClip = ReadClip(Prefix + FighterAnimComponent.IdleSuffix, socketPath);
            s_idle = s_idleClip.Read(0f);

            using Stage rig = CreateStage();
            Calibrate(rig);
            SerializedObject config = new SerializedObject(AssetDatabase.LoadAssetAtPath<WeaponConfig>($"{BattleEditorUtility.ConfigsFolder}/{Prefix}.asset"));
            SwordMotion[] motions = Parse(keys);

            for (int i = 0; i < motions.Length; i++)
            {
                SwordMotion motion = motions[i];
                SerializedProperty attack = config.FindProperty("_attacks").GetArrayElementAtIndex(i);
                float windup = attack.FindPropertyRelative("_windupTime").floatValue;
                float active = attack.FindPropertyRelative("_activeTime").floatValue;
                motion.Index = i;
                motion.Windup = windup * FrameRate;
                motion.ActiveEnd = (windup + active) * FrameRate;
                motion.Peak = Mathf.RoundToInt((windup + active * 0.5f) * FrameRate);
                motion.Frames = Mathf.CeilToInt((windup + active + attack.FindPropertyRelative("_recoveryTime").floatValue) * FrameRate - 1e-3f);
                Solve(rig, motion, motions, report);
            }

            return motions;
        }

        private static int Muscle(string name)
        {
            int index = Array.IndexOf(HumanTrait.MuscleName, name);

            return index >= 0 ? index : throw new ArgumentException($"No muscle '{name}'");
        }

        private static ClipPose ReadClip(string name, string socketPath)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{BattleEditorUtility.AnimationsFolder}/{name}.anim")
                                 ?? throw new ArgumentException($"No clip '{name}'");
            ClipPose pose = new ClipPose { Muscles = new AnimationCurve[HumanTrait.MuscleCount] };

            for (int i = 0; i < pose.Muscles.Length; i++)
            {
                string[] parts = HumanTrait.MuscleName[i].Split(' ');
                string property = i < Body ? HumanTrait.MuscleName[i] : $"{parts[0]}Hand.{parts[1]}.{string.Join(" ", parts.Skip(2))}";
                pose.Muscles[i] = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), property));
            }

            pose.Root = Array.ConvertAll(s_rootCurves, property => AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), property)));
            pose.Socket = Array.ConvertAll(s_socketCurves, property => AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(socketPath, typeof(Transform), property)));

            return pose;
        }

        /// How many degrees the chest and the head turn per unit of their muscles on this avatar, signed.
        private static void Calibrate(Stage rig)
        {
            Vector3 Measure(int[] muscles, Transform bone, int axis)
            {
                float[] pose = (float[])s_idle.Clone();
                rig.Set(pose, new Vector3(0f, 1f, 0f), Quaternion.identity, 0f, 0f);
                Quaternion rest = bone.rotation;

                for (int i = axis; i < muscles.Length; i += 3)
                    pose[muscles[i]] += 0.5f;

                rig.Set(pose, new Vector3(0f, 1f, 0f), Quaternion.identity, 0f, 0f);
                Quaternion turn = bone.rotation * Quaternion.Inverse(rest);
                Vector3 ahead = turn * Vector3.forward, up = turn * Vector3.up;

                return new Vector3(Mathf.Atan2(-ahead.y, ahead.z), Mathf.Atan2(ahead.x, ahead.z), Mathf.Atan2(up.x, up.y)) * (Mathf.Rad2Deg / 0.5f);
            }

            s_torsoScale = new Vector3(Measure(s_torso, rig.Chest, 0).x, Measure(s_torso, rig.Chest, 2).y, Measure(s_torso, rig.Chest, 1).z);
            s_neckScale = new Vector3(Measure(s_neck, rig.Head, 0).x, Measure(s_neck, rig.Head, 2).y, Measure(s_neck, rig.Head, 1).z);
        }

        /// The chest turned against the hips and the head kept on what the eyes look at.
        private static void SetTorso(float[] muscles, Vector3 torso)
        {
            Vector3 chest = new Vector3(torso.x / s_torsoScale.x, torso.z / s_torsoScale.z, torso.y / s_torsoScale.y);
            Vector3 head = new Vector3(-torso.x / s_neckScale.x, -torso.z / s_neckScale.z, -torso.y / s_neckScale.y) * 0.8f;

            for (int i = 0; i < s_torso.Length; i++)
                muscles[s_torso[i]] = Mathf.Clamp(s_idle[s_torso[i]] + chest[i % 3], -1f, 1f);

            for (int i = 0; i < s_neck.Length; i++)
                muscles[s_neck[i]] = Mathf.Clamp(s_idle[s_neck[i]] + head[i % 3], -1f, 1f);
        }

        private static void Solve(Stage rig, SwordMotion motion, SwordMotion[] motions, StringBuilder report)
        {
            int count = motion.Frames + 1;
            ClipPose legs = ReadClip(Prefix + FighterAnimComponent.AttackLegsSuffix + motion.Index, string.Empty);
            motion.Muscles = new float[count][];
            motion.BodyPositions = new Vector3[count];
            motion.BodyRotations = new Quaternion[count];
            motion.Grips = new Quaternion[count];
            motion.Torso = new Vector3[count];
            motion.Lean = new float[count];
            motion.Roll = new float[count];
            motion.Strain = new float[count];

            for (int frame = 0; frame < count; frame++)
            {
                float time = frame / FrameRate;
                float[] muscles = (float[])s_idle.Clone();
                float[] feet = legs.Read(time);

                foreach (int i in s_legs)
                    muscles[i] = feet[i];

                motion.Muscles[frame] = muscles;
                motion.BodyPositions[frame] = legs.Position(time);
                motion.BodyRotations[frame] = legs.Rotation(time);
            }

            // The idle as the game shows it, and the pose the attack before leaves: what the first key starts from.
            rig.Set(motion.Muscles[0], motion.BodyPositions[0], motion.BodyRotations[0], 0f, 0f);
            SwordKey idle = new SwordKey { Grip = rig.Socket.position, Blade = rig.Socket.forward, Edge = rig.Socket.up, Elbow = rig.Elbow.position, Flow = 0f, Hard = 2f, IsIdle = true };
            float[] seed = Array.ConvertAll(s_right, i => s_idle[i]).Concat(new[] { 0f, 0f }).ToArray();
            List<SwordKey> keys = new List<SwordKey>(motion.Keys);

            for (int i = 0; i < keys.Count; i++)
            {
                if (!keys[i].IsIdle)
                    continue;

                idle.Frame = keys[i].Frame;
                keys[i] = idle;
            }

            if (motion.After >= 0)
            {
                SwordMotion before = motions[motion.After];
                int from = motion.AfterFrame;
                rig.Set(before.Muscles[from], motion.BodyPositions[0], motion.BodyRotations[0], before.Lean[from], before.Roll[from]);
                keys[0] = new SwordKey { Grip = rig.Socket.position, Blade = rig.Socket.forward, Elbow = rig.Elbow.position, Torso = before.Torso[from], Flow = 0f };
                seed = Array.ConvertAll(s_right, i => before.Muscles[from][i]).Concat(new[] { before.Lean[from] / MaxLean, before.Roll[from] / MaxRoll }).ToArray();
            }

            // First the aimed moments find where the arm is at ease with the strike point on the crosshair; their keys take
            // those poses, so the motion runs through them instead of hopping onto them for a frame.
            Target[] rough = Interpolate(keys, count, motion);
            float[] found = seed;

            for (int frame = 0; frame < count; frame++)
            {
                found = SolveArm(rig, motion, frame, rough[frame], found, frame == 0 ? 0f : 1f, null);
                Apply(motion, frame, found);
                rig.Set(motion.Muscles[frame], motion.BodyPositions[frame], motion.BodyRotations[frame], motion.Lean[frame], motion.Roll[frame]);

                for (int i = 0; i < keys.Count; i++)
                {
                    if (!keys[i].IsAim || Mathf.RoundToInt(keys[i].Frame) != frame)
                        continue;

                    SwordKey key = keys[i];
                    key.Grip = rig.Socket.position;
                    key.Blade = rig.Socket.forward;
                    keys[i] = key;
                }
            }

            Target[] targets = Interpolate(keys, count, motion);
            float[][] solved = new float[count][];
            float[] values = seed;

            for (int frame = 0; frame < count; frame++)
                solved[frame] = values = SolveArm(rig, motion, frame, targets[frame], values, frame == 0 ? 0f : 1f, null);

            // Each frame once more between its neighbours: what the arm is free to choose (the shoulder, the elbow, the turn of
            // the hilt) settles into one run instead of a choice per frame, while the sword stays on its targets.
            for (int pass = 0; pass < 4; pass++)
            {
                float[][] before = solved;
                solved = new float[count][];

                for (int frame = 0; frame < count; frame++)
                {
                    float[] earlier = before[Mathf.Max(frame - 1, 0)], later = before[Mathf.Min(frame + 1, count - 1)];
                    float[] between = new float[earlier.Length];

                    for (int i = 0; i < between.Length; i++)
                        between[i] = (earlier[i] + later[i]) * 0.5f;

                    solved[frame] = SolveArm(rig, motion, frame, targets[frame], before[frame], 0f, between);
                }
            }

            // Away from the swing a jump between two ways to hold the same pose (the twist shared between the upper arm and the
            // forearm) is spread over the frames around it: the fist keeps the hilt, the sword barely leaves its path.
            for (int pass = 0; pass < 6; pass++)
            {
                float[][] before = solved;
                solved = new float[count][];

                for (int frame = 0; frame < count; frame++)
                {
                    float free = frame == 0 || frame == count - 1 ? 0f
                        : Mathf.Clamp01(Mathf.Max((motion.Windup - 4f - frame) / 4f, (frame - motion.ActiveEnd - 4f) / 4f));
                    float[] earlier = before[Mathf.Max(frame - 1, 0)], later = before[Mathf.Min(frame + 1, count - 1)];
                    solved[frame] = new float[earlier.Length];

                    for (int i = 0; i < earlier.Length; i++)
                        solved[frame][i] = Mathf.Lerp(before[frame][i], earlier[i] * 0.25f + before[frame][i] * 0.5f + later[i] * 0.25f, free);
                }
            }

            float worstGrip = 0f, worstBlade = 0f, worstStep = 0f;
            int gripFrame = 0, bladeFrame = 0, stepFrame = 0;
            StringBuilder limits = new StringBuilder();
            StringBuilder table = new StringBuilder("frame | grip-off blade-off edge | ShDU ShFB ArDU ArFB ArTw FoSt FoTw HaDU HaIO\n");

            for (int frame = 0; frame < count; frame++)
            {
                float[] previous = solved[Mathf.Max(frame - 1, 0)];
                values = solved[frame];
                Apply(motion, frame, values);
                rig.Set(motion.Muscles[frame], motion.BodyPositions[frame], motion.BodyRotations[frame], motion.Lean[frame], motion.Roll[frame]);
                motion.Grips[frame] = rig.Grip(motion.Lean[frame], motion.Roll[frame]);
                motion.Torso[frame] = targets[frame].Torso;
                float grip = Vector3.Distance(rig.Socket.position, targets[frame].Grip);
                float blade = Vector3.Angle(rig.Socket.forward, targets[frame].Blade);
                float step = Enumerable.Range(0, 9).Max(i => Mathf.Abs(values[i] - previous[i]));
                float strain = Enumerable.Range(0, 9).Max(i => Mathf.Abs(values[i]));
                motion.Strain[frame] = strain;

                if (grip > worstGrip) { worstGrip = grip; gripFrame = frame; }
                if (blade > worstBlade) { worstBlade = blade; bladeFrame = frame; }
                if (step > worstStep) { worstStep = step; stepFrame = frame; }

                if (strain > 0.97f)
                    limits.Append(' ').Append(frame);

                table.Append(frame).Append(string.Format(CultureInfo.InvariantCulture, " | {0,5:F1}cm {1,4:F0}deg e{2:F1} |", grip * 100f, blade, targets[frame].EdgeWeight));

                for (int i = 0; i < 9; i++)
                    table.Append(string.Format(CultureInfo.InvariantCulture, " {0,5:F2}", values[i]));

                table.AppendLine(string.Format(CultureInfo.InvariantCulture, " | lean {0,4:F0} roll {1,4:F0} | fore-blade {2,4:F0} | grip {3:F2} {4:F2} {5:F2} elbow {6:F2} {7:F2} {8:F2}",
                    motion.Lean[frame], motion.Roll[frame], Vector3.Angle(rig.Hand.position - rig.Elbow.position, rig.Socket.forward),
                    rig.Socket.position.x, rig.Socket.position.y, rig.Socket.position.z, rig.Elbow.position.x, rig.Elbow.position.y, rig.Elbow.position.z));
            }

            rig.Set(motion.Muscles[motion.Peak], motion.BodyPositions[motion.Peak], motion.BodyRotations[motion.Peak], motion.Lean[motion.Peak], motion.Roll[motion.Peak]);
            Vector3 strike = rig.Socket.position + rig.Socket.forward * Strike;
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0}: frames {1} peak {2} | peak miss {3:F1} cm (strike z {4:F2}) | grip off target max {5:F1} cm @{6} | blade off max {7:F0} deg @{8} | muscle step max {9:F2} @{10}",
                motion.Name, motion.Frames, motion.Peak, new Vector2(strike.x - s_eye.x, strike.y - s_eye.y).magnitude * 100f, strike.z, worstGrip * 100f, gripFrame,
                worstBlade, bladeFrame, worstStep, stepFrame));
            report.AppendLine("  muscle past 0.97 on frames:" + (limits.Length > 0 ? limits.ToString() : " none"));
            Tables[motion.Index] = table.ToString();
        }

        private static void Apply(SwordMotion motion, int frame, float[] values)
        {
            for (int i = 0; i < s_right.Length; i++)
                motion.Muscles[frame][s_right[i]] = values[i];

            motion.Lean[frame] = values[9] * MaxLean;
            motion.Roll[frame] = values[10] * MaxRoll;
        }

        /// Keys to a target per frame: positions and directions run through the keys on a Hermite spline in time.
        private static Target[] Interpolate(List<SwordKey> keys, int count, SwordMotion motion)
        {
            int peak = motion.Peak;
            Target[] targets = new Target[count];
            float[] times = keys.Select(key => key.Frame).ToArray();
            Vector3[] SlopesOf(Func<SwordKey, Vector3> value) => Slopes(keys, times, value);
            Vector3[] grip = SlopesOf(key => key.Grip), blade = SlopesOf(key => key.Blade.normalized), torso = SlopesOf(key => key.Torso);

            for (int frame = 0; frame < count; frame++)
            {
                int i = Mathf.Clamp(Array.FindLastIndex(times, time => time <= frame), 0, keys.Count - 2);
                float span = times[i + 1] - times[i], t = Mathf.Clamp01((frame - times[i]) / span);
                Vector3 Along(Func<SwordKey, Vector3> value, Vector3[] slopes) => Hermite(value(keys[i]), slopes[i] * span, value(keys[i + 1]), slopes[i + 1] * span, t);
                SwordKey a = keys[i], b = keys[i + 1];
                Target target = new Target
                {
                    Grip = Along(key => key.Grip, grip),
                    Blade = Along(key => key.Blade.normalized, blade).normalized,
                    Torso = Along(key => key.Torso, torso),
                    Hard = Mathf.Lerp(a.Hard, b.Hard, t),
                    Idle = Mathf.Pow(Mathf.Lerp(a.IsIdle ? 1f : 0f, b.IsIdle ? 1f : 0f, t), 2f),
                    Aim = Mathf.Pow(Mathf.Lerp(a.IsAim ? 1f : 0f, b.IsAim ? 1f : 0f, t), 8f)
                };
                float elbowA = a.Elbow == Vector3.zero ? 0f : 1f, elbowB = b.Elbow == Vector3.zero ? 0f : 1f;
                target.ElbowWeight = Mathf.Lerp(elbowA, elbowB, t);
                target.Elbow = elbowA + elbowB > 1.5f ? Vector3.Lerp(a.Elbow, b.Elbow, Mathf.SmoothStep(0f, 1f, t)) : elbowA > 0f ? a.Elbow : b.Elbow;
                float edgeA = a.Edge == Vector3.zero ? 0f : 1f, edgeB = b.Edge == Vector3.zero ? 0f : 1f;
                target.EdgeWeight = Mathf.Lerp(edgeA, edgeB, t);
                target.Edge = edgeA + edgeB > 1.5f ? Vector3.Slerp(a.Edge, b.Edge, t) : edgeA > 0f ? a.Edge : b.Edge;
                targets[frame] = target;
            }

            // A cut leads with its edge: where the strike point travels across the blade, the edge faces that way. On the way
            // up the same edge trails (the fist does not turn the sword over to carry it back), so the side is carried from the
            // peak outward. Through the turn at the top of the windup the blade hardly moves: there the edge keeps turning from
            // the side it had to the side it will have, and the hand is already set when the swing starts.
            Vector3[] lead = new Vector3[count];
            bool[] isMoving = new bool[count];

            for (int frame = 0; frame < count; frame++)
            {
                int before = Mathf.Max(frame - 1, 0), after = Mathf.Min(frame + 1, count - 1);
                Vector3 velocity = (targets[after].Grip + targets[after].Blade * Strike - targets[before].Grip - targets[before].Blade * Strike) * (FrameRate / (after - before));
                Vector3 across = Vector3.ProjectOnPlane(velocity, targets[frame].Blade);
                lead[frame] = across.normalized;
                isMoving[frame] = across.magnitude > 3f;
            }

            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector3 last = lead[peak];

                for (int frame = peak + direction; frame >= 0 && frame < count; frame += direction)
                {
                    // The blade turns between frames: the side is compared in the blade's own frame, carried along with it.
                    last = Vector3.ProjectOnPlane(last, targets[frame].Blade).normalized;

                    if (Vector3.Dot(lead[frame], last) < 0f)
                        lead[frame] = -lead[frame];

                    if (isMoving[frame])
                        last = lead[frame];
                }
            }

            for (int frame = 0; frame < count && motion.IsCut; frame++)
            {
                // Only the swing itself: from the last of the windup until the blade has run out.
                float swing = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(motion.Windup - 13f, motion.Windup - 6f, frame)) *
                              (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(motion.ActiveEnd + 3f, motion.ActiveEnd + 9f, frame)));

                if (swing <= targets[frame].EdgeWeight)
                    continue;

                int before = frame, after = frame;

                while (before > 0 && !isMoving[before])
                    before--;

                while (after < count - 1 && !isMoving[after])
                    after++;

                Vector3 from = isMoving[before] ? lead[before] : lead[after], to = isMoving[after] ? lead[after] : lead[before];
                Vector3 side = after > before ? Vector3.Slerp(from, to, (frame - before) / (float)(after - before)) : from;
                targets[frame].Edge = Vector3.ProjectOnPlane(side, targets[frame].Blade).normalized;
                targets[frame].EdgeWeight = swing;
            }

            return targets;
        }

        private static Vector3[] Slopes(List<SwordKey> keys, float[] times, Func<SwordKey, Vector3> value)
        {
            Vector3[] slopes = new Vector3[keys.Count];

            for (int i = 1; i < keys.Count - 1; i++)
                slopes[i] = (value(keys[i + 1]) - value(keys[i - 1])) / (times[i + 1] - times[i - 1]) * keys[i].Flow;

            return slopes;
        }

        private static Vector3 Hermite(Vector3 a, Vector3 slopeA, Vector3 b, Vector3 slopeB, float t)
        {
            float t2 = t * t, t3 = t2 * t;

            return (2f * t3 - 3f * t2 + 1f) * a + (t3 - 2f * t2 + t) * slopeA + (-2f * t3 + 3f * t2) * b + (t3 - t2) * slopeB;
        }

        /// The right arm (nine muscles) and the turn of the hilt in the fist (lean, roll) that bring the sword onto the target
        /// with every joint inside its range: damped least squares over what the sword misses and what the joints strain.
        private static float[] SolveArm(Stage rig, SwordMotion motion, int frame, Target target, float[] start, float inertia, float[] between)
        {
            const int n = 11;
            const float step = 2e-3f;
            float[] muscles = (float[])motion.Muscles[frame].Clone();
            SetTorso(muscles, target.Torso);
            Array.Copy(muscles, motion.Muscles[frame], muscles.Length);
            float hard = 1f + target.Hard;

            float[] Residuals(float[] x)
            {
                for (int i = 0; i < 9; i++)
                    muscles[s_right[i]] = x[i];

                rig.Set(muscles, motion.BodyPositions[frame], motion.BodyRotations[frame], x[9] * MaxLean, x[10] * MaxRoll);
                List<float> r = new List<float>(48);
                Add(r, (rig.Socket.position - target.Grip) * (35f * hard * (1f - 0.4f * target.Aim)));
                Add(r, (rig.Socket.forward - target.Blade) * (14f * hard * (1f - 0.5f * target.Aim)));
                Vector3 strike = rig.Socket.position + rig.Socket.forward * Strike;
                r.Add((strike.x - s_eye.x) * 300f * target.Aim);
                r.Add((strike.y - s_eye.y) * 300f * target.Aim);
                Add(r, (rig.Socket.up - target.Edge) * (3.5f * target.EdgeWeight));
                Add(r, (rig.Elbow.position - target.Elbow) * (14f * target.ElbowWeight));

                for (int i = 0; i < 9; i++)
                {
                    r.Add(12f * Mathf.Max(0f, Mathf.Abs(x[i]) - SoftLimit));
                    // By the idle the arm rests as the idle clip has it, so the attack leaves and meets it without a step.
                    r.Add(s_ease[i] * (x[i] - target.Idle * s_idle[s_right[i]]));
                    r.Add(4f * target.Idle * (x[i] - s_idle[s_right[i]]));
                    r.Add(inertia * (i < 2 ? 2.5f : 0.8f) * (x[i] - start[i]));
                    r.Add(between == null ? 0f : (i < 2 ? 3f : 1.5f) * (x[i] - between[i]));
                }

                // The hilt lies square in the fist unless the wrist has nothing left to give.
                r.Add(1.8f * x[9]);
                r.Add(12f * Mathf.Max(0f, MinLean / MaxLean - x[9]));
                r.Add(1.2f * x[10]);
                r.Add(inertia * 1.5f * (x[9] - start[9]));
                r.Add(inertia * 1.5f * (x[10] - start[10]));
                r.Add(between == null ? 0f : 2f * (x[9] - between[9]));
                r.Add(between == null ? 0f : 2f * (x[10] - between[10]));

                return r.ToArray();
            }

            float[] best = (float[])start.Clone();
            float[] residuals = Residuals(best);
            float cost = residuals.Sum(v => v * v), damping = 1e-2f;
            int m = residuals.Length;
            float[,] jacobian = new float[m, n];

            for (int iteration = 0; iteration < 40; iteration++)
            {
                for (int j = 0; j < n; j++)
                {
                    float[] probe = (float[])best.Clone();
                    float h = probe[j] + step > 1f ? -step : step;
                    probe[j] += h;
                    float[] moved = Residuals(probe);

                    for (int i = 0; i < m; i++)
                        jacobian[i, j] = (moved[i] - residuals[i]) / h;
                }

                float[,] normal = new float[n, n];
                float[] gradient = new float[n];

                for (int j = 0; j < n; j++)
                {
                    for (int i = 0; i < m; i++)
                        gradient[j] += jacobian[i, j] * residuals[i];

                    for (int k = j; k < n; k++)
                    {
                        float sum = 0f;

                        for (int i = 0; i < m; i++)
                            sum += jacobian[i, j] * jacobian[i, k];

                        normal[j, k] = normal[k, j] = sum;
                    }
                }

                bool isBetter = false;

                for (int attempt = 0; attempt < 8 && !isBetter; attempt++)
                {
                    float[] delta = SolveLinear(normal, gradient, damping);
                    float[] next = new float[n];

                    for (int j = 0; j < n; j++)
                        next[j] = Mathf.Clamp(best[j] - delta[j], -1f, 1f);

                    float[] moved = Residuals(next);
                    float movedCost = moved.Sum(v => v * v);

                    if (movedCost < cost)
                    {
                        isBetter = cost - movedCost > 1e-7f;
                        best = next;
                        residuals = moved;
                        cost = movedCost;
                        damping = Mathf.Max(damping * 0.3f, 1e-5f);

                        break;
                    }

                    damping *= 5f;
                }

                if (!isBetter)
                    break;
            }

            return best;
        }

        private static void Add(List<float> residuals, Vector3 value)
        {
            residuals.Add(value.x);
            residuals.Add(value.y);
            residuals.Add(value.z);
        }

        /// (A + damping * diag(A)) x = b by Gaussian elimination with partial pivoting.
        private static float[] SolveLinear(float[,] a, float[] b, float damping)
        {
            int n = b.Length;
            double[,] m = new double[n, n + 1];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    m[i, j] = a[i, j];

                m[i, i] += damping * a[i, i] + 1e-6;
                m[i, n] = b[i];
            }

            for (int column = 0; column < n; column++)
            {
                int pivot = column;

                for (int row = column + 1; row < n; row++)
                    if (Math.Abs(m[row, column]) > Math.Abs(m[pivot, column]))
                        pivot = row;

                for (int k = column; k <= n; k++)
                    (m[column, k], m[pivot, k]) = (m[pivot, k], m[column, k]);

                for (int row = column + 1; row < n; row++)
                {
                    double factor = m[row, column] / m[column, column];

                    for (int k = column; k <= n; k++)
                        m[row, k] -= factor * m[column, k];
                }
            }

            float[] x = new float[n];

            for (int row = n - 1; row >= 0; row--)
            {
                double sum = m[row, n];

                for (int k = row + 1; k < n; k++)
                    sum -= m[row, k] * x[k];

                x[row] = (float)(sum / m[row, row]);
            }

            return x;
        }

        private static SwordKey Key(float frame, float x, float y, float z, float bladeX, float bladeY, float bladeZ)
        {
            return new SwordKey { Frame = frame, Grip = new Vector3(x, y, z), Blade = new Vector3(bladeX, bladeY, bladeZ).normalized, Flow = 1f };
        }

        /// The moment the player aims with: the strike point of the blade lies on the crosshair.
        private static SwordKey Aim(float frame, float x, float y, float z)
        {
            Vector3 grip = new Vector3(x, y, z);
            Vector2 offset = new Vector2(s_eye.x - x, s_eye.y - y);
            Vector3 blade = new Vector3(offset.x, offset.y, Mathf.Sqrt(Strike * Strike - offset.sqrMagnitude)) / Strike;

            return new SwordKey { Frame = frame, Grip = grip, Blade = blade, Flow = 1f, IsAim = true };
        }

        private static SwordKey Idle(float frame)
        {
            return new SwordKey { Frame = frame, IsIdle = true };
        }

        /// Keys of the attacks from a text file, so poses are tuned without recompiling. Lines:
        /// "motion <name> | after <attack> <frame>", "idle <frame>", "key <frame> x y z bx by bz", "aim <frame> x y z",
        /// each followed by any of "to <bow> <turn> <lean>", "el x y z", "ed x y z", "hard <w>", "stop".
        private static SwordMotion[] Parse(string path)
        {
            List<SwordMotion> motions = new List<SwordMotion>();

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Split('#')[0].Trim();

                if (line.Length == 0)
                    continue;

                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int at = 1;
                float Next() => float.Parse(words[at++], CultureInfo.InvariantCulture);

                if (words[0] == "motion")
                {
                    SwordMotion motion = new SwordMotion { Name = "Attack " + (motions.Count + 1), Keys = new List<SwordKey>() };
                    int after = Array.IndexOf(words, "after");

                    motion.IsCut = Array.IndexOf(words, "thrust", 2) < 0;

                    if (after > 0)
                    {
                        motion.After = int.Parse(words[after + 1]);
                        motion.AfterFrame = int.Parse(words[after + 2]);
                    }

                    motions.Add(motion);

                    continue;
                }

                SwordKey key = words[0] switch
                {
                    "idle" => Idle(Next()),
                    "aim" => Aim(Next(), Next(), Next(), Next()),
                    _ => Key(Next(), Next(), Next(), Next(), Next(), Next(), Next())
                };

                while (at < words.Length)
                {
                    switch (words[at++])
                    {
                        case "to":
                            key = key.To(Next(), Next(), Next());
                            break;
                        case "el":
                            key = key.El(Next(), Next(), Next());
                            break;
                        case "ed":
                            key = key.Ed(Next(), Next(), Next());
                            break;
                        case "hard":
                            key.Hard = Next();
                            break;
                        case "stop":
                            key = key.Stop();
                            break;
                        default:
                            throw new ArgumentException($"Unknown word in '{line}'");
                    }
                }

                motions[^1].Keys.Add(key);
            }

            return motions.ToArray();
        }

        private static void Export(string folder, SwordMotion[] motions)
        {
            using Stage rig = CreateStage();
            GameObject sword = Object.Instantiate(AssetDatabase.LoadAssetAtPath<WeaponConfig>($"{BattleEditorUtility.ConfigsFolder}/{Prefix}.asset").Attachments[0].Prefab, rig.Socket, false);
            sword.hideFlags = HideFlags.HideAndDontSave;
            SkinnedMeshRenderer[] skins = rig.Animator.GetComponentsInChildren<SkinnedMeshRenderer>(false).Where(skin => skin.enabled && skin.sharedMesh != null).ToArray();
            List<Transform> bones = skins.SelectMany(skin => skin.bones).Where(bone => bone != null).Distinct().ToList();
            List<Texture> textures = new List<Texture>();
            StringBuilder json = new StringBuilder("window.MOCKUP=");
            json.Append("{\"eye\":").Append(Json(new[] { s_eye.x, s_eye.y, s_eye.z })).Append(",\"fov\":75,\"strike\":").Append(Json(Strike));
            json.Append(",\"bladeBase\":").Append(Json(BladeBase)).Append(",\"bladeTip\":").Append(Json(BladeTip));
            json.Append(",\"head\":[").Append(string.Join(",", bones.Select((bone, i) => bone.IsChildOf(rig.Head) ? i : -1).Where(i => i >= 0))).Append("]");
            json.Append(",\"headBone\":").Append(bones.IndexOf(rig.Head));
            json.Append(",\"meshes\":[");

            for (int s = 0; s < skins.Length; s++)
            {
                SkinnedMeshRenderer skin = skins[s];
                Mesh mesh = skin.sharedMesh;
                BoneWeight[] weights = mesh.boneWeights;
                int[] map = Array.ConvertAll(skin.bones, bone => bones.IndexOf(bone));
                ushort[] indices = new ushort[weights.Length * 4];
                float[] amounts = new float[weights.Length * 4];

                for (int i = 0; i < weights.Length; i++)
                {
                    BoneWeight w = weights[i];
                    int[] bone = { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 };
                    float[] amount = { w.weight0, w.weight1, w.weight2, w.weight3 };

                    for (int k = 0; k < 4; k++)
                    {
                        indices[i * 4 + k] = (ushort)Mathf.Max(amount[k] > 0f ? map[bone[k]] : 0, 0);
                        amounts[i * 4 + k] = amount[k];
                    }
                }

                Matrix4x4[] bind = new Matrix4x4[bones.Count];

                for (int i = 0; i < bind.Length; i++)
                    bind[i] = Matrix4x4.identity;

                for (int i = 0; i < map.Length; i++)
                    if (map[i] >= 0)
                        bind[map[i]] = mesh.bindposes[i];

                json.Append(s > 0 ? "," : string.Empty).Append("{\"name\":\"").Append(skin.name).Append("\"");
                json.Append(",\"v\":\"").Append(Base64(mesh.vertices)).Append("\",\"n\":\"").Append(Base64(mesh.normals)).Append("\"");
                json.Append(",\"uv\":\"").Append(Base64(mesh.uv)).Append("\",\"si\":\"").Append(Base64(indices)).Append("\",\"sw\":\"").Append(Base64(amounts)).Append("\"");
                json.Append(",\"bind\":\"").Append(Base64(bind.SelectMany(Columns).ToArray())).Append("\"");
                AppendParts(json, mesh, skin.sharedMaterials, textures);
                json.Append("}");
            }

            json.Append("],\"sword\":[");
            MeshFilter[] filters = sword.GetComponentsInChildren<MeshFilter>(false).Where(filter => filter.sharedMesh != null && filter.GetComponent<MeshRenderer>() is { enabled: true }).ToArray();

            for (int f = 0; f < filters.Length; f++)
            {
                Mesh mesh = filters[f].sharedMesh;
                Matrix4x4 local = rig.Socket.worldToLocalMatrix * filters[f].transform.localToWorldMatrix;
                json.Append(f > 0 ? "," : string.Empty).Append("{\"name\":\"").Append(filters[f].name).Append("\"");
                json.Append(",\"v\":\"").Append(Base64(Array.ConvertAll(mesh.vertices, local.MultiplyPoint3x4))).Append("\"");
                json.Append(",\"n\":\"").Append(Base64(Array.ConvertAll(mesh.normals, normal => local.MultiplyVector(normal).normalized))).Append("\"");
                json.Append(",\"uv\":\"").Append(Base64(mesh.uv)).Append("\"");
                AppendParts(json, mesh, filters[f].GetComponent<MeshRenderer>().sharedMaterials, textures);
                json.Append("}");
            }

            json.Append("],\"textures\":[").Append(string.Join(",", textures.Select(texture => "\"data:image/jpeg;base64," + Jpeg(texture) + "\""))).Append("]");
            json.Append(",\"clips\":[");

            for (int c = 0; c < motions.Length; c++)
            {
                SwordMotion motion = motions[c];
                int count = motion.Frames + 1;
                float[] poses = new float[count * bones.Count * 10];
                float[] sockets = new float[count * 7];

                for (int frame = 0; frame < count; frame++)
                {
                    rig.Set(motion.Muscles[frame], motion.BodyPositions[frame], motion.BodyRotations[frame], motion.Lean[frame], motion.Roll[frame], true);

                    for (int b = 0; b < bones.Count; b++)
                        Write(poses, (frame * bones.Count + b) * 10, bones[b].position, bones[b].rotation, bones[b].lossyScale);

                    Write(sockets, frame * 7, rig.Socket.position, rig.Socket.rotation, null);
                }

                json.Append(c > 0 ? "," : string.Empty).Append("{\"name\":\"").Append(motion.Name).Append("\",\"index\":").Append(motion.Index);
                json.Append(",\"frames\":").Append(count).Append(",\"peak\":").Append(motion.Peak);
                json.Append(",\"windup\":").Append(Json(motion.Windup)).Append(",\"activeEnd\":").Append(Json(motion.ActiveEnd));
                json.Append(",\"after\":").Append(motion.After).Append(",\"afterFrame\":").Append(motion.AfterFrame);
                json.Append(",\"strain\":").Append(Json(motion.Strain)).Append(",\"lean\":").Append(Json(motion.Lean)).Append(",\"roll\":").Append(Json(motion.Roll));
                json.Append(",\"poses\":\"").Append(Base64(poses)).Append("\",\"socket\":\"").Append(Base64(sockets)).Append("\"}");
            }

            json.Append("],\"bones\":").Append(bones.Count).Append("};");
            File.WriteAllText($"{folder}/data.js", json.ToString());
        }

        private static void AppendParts(StringBuilder json, Mesh mesh, Material[] materials, List<Texture> textures)
        {
            json.Append(",\"parts\":[");

            for (int part = 0; part < mesh.subMeshCount; part++)
            {
                Material material = materials[Mathf.Min(part, materials.Length - 1)];
                Color color = material != null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.gray;
                Texture texture = material != null && material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
                int index = texture == null ? -1 : textures.IndexOf(texture);

                if (texture != null && index < 0)
                {
                    index = textures.Count;
                    textures.Add(texture);
                }

                json.Append(part > 0 ? "," : string.Empty).Append("{\"i\":\"").Append(Base64(Array.ConvertAll(mesh.GetTriangles(part), i => (uint)i))).Append("\"");
                json.Append(",\"color\":").Append(Json(new[] { color.r, color.g, color.b })).Append(",\"tex\":").Append(index).Append("}");
            }

            json.Append("]");
        }

        private static void Write(float[] target, int at, Vector3 position, Quaternion rotation, Vector3? scale)
        {
            target[at] = position.x;
            target[at + 1] = position.y;
            target[at + 2] = position.z;
            target[at + 3] = rotation.x;
            target[at + 4] = rotation.y;
            target[at + 5] = rotation.z;
            target[at + 6] = rotation.w;

            if (scale == null)
                return;

            target[at + 7] = scale.Value.x;
            target[at + 8] = scale.Value.y;
            target[at + 9] = scale.Value.z;
        }

        private static IEnumerable<float> Columns(Matrix4x4 matrix)
        {
            for (int i = 0; i < 16; i++)
                yield return matrix[i];
        }

        private static string Jpeg(Texture texture)
        {
            int size = Mathf.Min(512, Mathf.Max(texture.width, texture.height));
            RenderTexture target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            Texture2D image = new Texture2D(size, size, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            string data = Convert.ToBase64String(image.EncodeToJPG(82));
            Object.DestroyImmediate(image);

            return data;
        }

        private static string Json(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        private static string Json(float[] values) => "[" + string.Join(",", values.Select(value => Json(value))) + "]";

        private static string Base64(Vector3[] values) => Base64(values.SelectMany(v => new[] { v.x, v.y, v.z }).ToArray());

        private static string Base64(Vector2[] values) => Base64(values.SelectMany(v => new[] { v.x, v.y }).ToArray());

        private static string Base64<T>(T[] values) where T : struct
        {
            byte[] bytes = new byte[Buffer.ByteLength(values)];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);

            return Convert.ToBase64String(bytes);
        }
    }
}
