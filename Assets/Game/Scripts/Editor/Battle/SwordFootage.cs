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
        /// Two-handed: the left hand is off the hilt, at this point.
        public bool IsLeftFree;
        public Vector3 LeftHand;
        public Vector3 LeftElbow;

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

    /// A weapon whose motions are authored from footage.
    internal sealed class SwordSpec
    {
        public string Prefix;
        /// The weapon whose idle clip gives the fingers, the fist's hold of the hilt and the stance.
        public string Base;
        public float Strike;
        public float BladeBase;
        public float BladeTip;
        /// Two-handed: how far behind the right fist the left one closes on the hilt, metres. 0 = one-handed.
        public float OffHand;
        /// How far the hilt may lie over in the palm toward the forearm's line, degrees: a long hilt swung at arm's length lies further.
        public float MaxLean = 24f;
        public string Keys;
        /// A weapon prefab (its name in the battle prefabs folder) shown until the weapon has a config of its own.
        public string Model;
        /// Boxes (centre, size, colour; the socket's space) standing in for the weapon until it has a prefab.
        public (Vector3 Center, Vector3 Size, Color Color)[] Mock;
        /// Seconds of clip per second of the keys: motions keyed in footage frames play at the game's pace (DungeonWeaponLibrary.Footage).
        public float Pace = 1f;
    }

    /// The body under the upper-body solve at one moment: the hips off the stance (metres), the pelvis turned right and
    /// pitched forward (degrees), each foot off its stance (x, lift, z), turned right and its heel raised (degrees).
    internal struct BodyFrame
    {
        public Vector3 Shift, Left, Right;
        public float Yaw, Pitch, LeftYaw, RightYaw, LeftHeel, RightHeel;
        /// How far the eyes go with the body: the first-person view and everything keyed in it ride along.
        public Vector3 Eye;
    }

    /// A "body" line: the parts it names; the others stay as the key before has them.
    internal struct BodyKey
    {
        public int Frame;
        public BodyFrame Body;
        public bool HasHips, HasLeft, HasRight;
    }

    internal sealed class SwordMotion
    {
        /// The hips, the pelvis and the feet under the swing at key frames; none = the stance throughout.
        public List<BodyKey> BodyKeys = new List<BodyKey>();
        /// The body per frame; null while the motion and the one it is chained from stand in the stance.
        public BodyFrame[] Lower;
        public string Id;
        public string Text = string.Empty;
        public string AfterId;
        public bool HasTiming;
        /// The cut is led by the edge on the heel of the right hand, not by the one its knuckles face: the way back of a
        /// swing that does not turn the blade over.
        public bool IsBackEdge;
        /// The weapon is in the hand from this frame on (taking it out).
        public int Shown;
        /// 1 = the left hand holds the hilt, per frame.
        public float[] Left;
        public string Name;
        public int Index;
        public int Frames;
        public int Peak;
        public float Duration;
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

    /// Weapon motions authored from footage: the weapon is rigid in the right fist (one grip point, the hilt may turn a
    /// little in the hand) and the arms are solved in humanoid muscles inside the avatar's ranges, so no joint bends the way a
    /// body cannot and the fist never leaves the hilt. A two-handed weapon has the left fist on the hilt behind the right
    /// one, or off it where a key says so. The review pages (Docs/Tasks/*/mockup) show the same poses.
    internal static class SwordFootage
    {
        private const float FrameRate = 60f;
        private const int Body = BattleAnimationBuilder.BodyMuscleCount;
        private const int Layer = 31;
        // How far the hilt may lie back in the palm and turn about itself in the fist, degrees.
        private const float MinLean = -8f;
        private const float MaxRoll = 25f;
        private const float SoftLimit = 0.85f;
        private const int Arm = 9;
        private const int OneHanded = Arm + 2;

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
            public readonly Transform Socket, Hips, Chest, Head, Elbow, Hand, LeftElbow, LeftHand, LeftGrip;
            /// The body of the frame being posed; null = the stance.
            public BodyFrame? Lower;
            public readonly Vector3 PalmNormal;
            public Vector3 GripPosition;
            public Quaternion GripRotation;

            private readonly Transform[] _leftLeg, _rightLeg;
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
                Chest = Animator.GetBoneTransform(HumanBodyBones.UpperChest);
                Head = Animator.GetBoneTransform(HumanBodyBones.Head);
                Elbow = Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                Hand = Animator.GetBoneTransform(HumanBodyBones.RightHand);
                LeftElbow = Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                LeftHand = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
                LeftGrip = new GameObject("LeftGrip") { hideFlags = HideFlags.HideAndDontSave }.transform;
                LeftGrip.SetParent(LeftHand, false);
                _leftLeg = new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot }.Select(Animator.GetBoneTransform).ToArray();
                _rightLeg = new[] { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot }.Select(Animator.GetBoneTransform).ToArray();
                _handler = new HumanPoseHandler(Animator.avatar, Animator.transform);
            }

            /// The left fist closes on a hilt as the right one does, mirrored: taken from the avatar's rest pose, which is symmetric.
            public void PlaceLeftGrip()
            {
                Array.Clear(_pose.muscles, 0, _pose.muscles.Length);
                _pose.bodyPosition = Vector3.up;
                _pose.bodyRotation = Quaternion.identity;
                _handler.SetHumanPose(ref _pose);
                Socket.SetLocalPositionAndRotation(GripPosition, GripRotation);
                Transform root = Animator.transform;
                Vector3 position = root.InverseTransformPoint(Socket.position) - root.InverseTransformPoint(Hips.position);
                Quaternion rotation = Quaternion.Inverse(root.rotation) * Socket.rotation;
                LeftGrip.SetPositionAndRotation(root.TransformPoint(root.InverseTransformPoint(Hips.position) + new Vector3(-position.x, position.y, position.z)),
                    root.rotation * new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w));
            }

            public void Dispose()
            {
                _handler.Dispose();
                Source.Dispose();
            }

            public void Set(float[] muscles, Vector3 position, Quaternion rotation, float lean, float roll)
            {
                Array.Copy(muscles, _pose.muscles, muscles.Length);
                _pose.bodyPosition = position;
                _pose.bodyRotation = rotation;
                _handler.SetHumanPose(ref _pose);
                // A humanoid pose hangs on the body's centre of mass, so a reaching arm would push the hips back. The controller
                // does not do that: the legs layer sets the hips and the upper-body clip builds on them.
                Hips.SetPositionAndRotation(s_hips.position, s_hips.rotation);

                if (Lower is BodyFrame body)
                {
                    Hips.SetPositionAndRotation(s_hips.position + body.Shift, Quaternion.Euler(0f, body.Yaw, 0f) * Quaternion.Euler(body.Pitch, 0f, 0f) * s_hips.rotation);
                    Plant(_leftLeg, s_leftFoot, body.Left, body.LeftYaw, body.LeftHeel, body.Yaw);
                    Plant(_rightLeg, s_rightFoot, body.Right, body.RightYaw, body.RightHeel, body.Yaw);
                }

                Socket.SetLocalPositionAndRotation(GripPosition, Grip(lean, roll));
            }

            /// A leg (thigh, shin, foot) bent to keep its foot where the body has it: off its stance, turned, the heel raised
            /// on the ball. The knee points between where the pelvis and the foot do.
            private static void Plant(Transform[] leg, Pose stance, Vector3 offset, float yaw, float heel, float pelvisYaw)
            {
                const float footLength = 0.16f;
                Transform thigh = leg[0], shin = leg[1], foot = leg[2];
                Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
                float raised = heel * Mathf.Deg2Rad;
                Vector3 hip = thigh.position;
                float upper = Vector3.Distance(hip, shin.position), lower = Vector3.Distance(shin.position, foot.position);
                Vector3 reach = stance.position + offset + new Vector3(0f, Mathf.Sin(raised), 0f) * footLength + turn * Vector3.forward * ((1f - Mathf.Cos(raised)) * footLength) - hip;
                reach = Vector3.ClampMagnitude(reach, (upper + lower) * 0.999f);
                float length = reach.magnitude, along = (upper * upper - lower * lower + length * length) / (2f * length);
                Vector3 axis = reach / length;
                float kneeYaw = Mathf.Lerp(pelvisYaw, yaw, 0.6f);
                Vector3 bend = Vector3.ProjectOnPlane(Quaternion.Euler(0f, kneeYaw, 0f) * Vector3.forward, axis).normalized;
                Vector3 knee = hip + axis * along + bend * Mathf.Sqrt(Mathf.Max(upper * upper - along * along, 0f));
                // The thigh came round with the pelvis; the rest of the way to the knee's own heading it turns about itself.
                thigh.rotation = Quaternion.AngleAxis(kneeYaw - pelvisYaw, Vector3.up) * thigh.rotation;
                thigh.rotation = Quaternion.FromToRotation(shin.position - hip, knee - hip) * thigh.rotation;
                shin.rotation = Quaternion.FromToRotation(foot.position - shin.position, hip + reach - shin.position) * shin.rotation;
                foot.rotation = turn * Quaternion.AngleAxis(heel, Vector3.right) * stance.rotation;
            }

            public Quaternion Grip(float lean, float roll)
            {
                return Quaternion.AngleAxis(-lean, PalmNormal) * GripRotation * Quaternion.AngleAxis(roll, Vector3.forward);
            }

        }

        /// A clip's frames on the 60 Hz grid: the body's muscles and the turn of the weapon in the fist.
        private struct Track
        {
            public float[][] Muscles;
            public Quaternion[] Grips;
            public float Duration;
        }

        private struct Target
        {
            public Vector3 Grip, Blade, Edge, Elbow, Torso, LeftHand, LeftElbow;
            public float EdgeWeight, ElbowWeight, Hard, Aim, Idle, Left, LeftElbowWeight;
        }

        private sealed class ClipPose
        {
            public AnimationCurve[] Muscles;
            public AnimationCurve[] Socket;

            public float[] Read(float time)
            {
                float[] muscles = new float[HumanTrait.MuscleCount];

                for (int i = 0; i < muscles.Length; i++)
                    muscles[i] = Muscles[i]?.Evaluate(time) ?? 0f;

                return muscles;
            }

        }

        public static string[] Tables = Array.Empty<string>();

        private static readonly Dictionary<string, SwordMotion> s_solved = new Dictionary<string, SwordMotion>();
        private static readonly Dictionary<Color, Material> s_mockMaterials = new Dictionary<Color, Material>();
        private static SwordSpec s_spec;
        private static int[] s_right, s_left, s_legs, s_torso, s_neck, s_rightFingers, s_leftFingers;
        private static Vector3 s_torsoScale, s_neckScale, s_idleTorso;
        private static float[] s_idle, s_leftOpen;
        private static float s_idleLean, s_idleRoll;
        private static bool s_hasIdleMotion, s_isIdleLeftFree, s_isStanding;

        private static float Strike => s_spec.Strike;

        private static float MaxLean => s_spec.MaxLean;

        private static bool IsTwoHanded => s_spec.OffHand > 0f;
        private static ClipPose s_idleClip;
        private static HumanPose s_stance;
        private static Pose s_hips, s_leftFoot, s_rightFoot;
        private static WeaponDefinition s_weapon;

        /// Writes ArmingSword_Attack0-2 from the keys: body muscles, the fingers as the idle holds them and the sword's socket
        /// in the fist. Hand edits stored for these clips are baked back on top (a grip that holds the weapon keeps its path,
        /// so the hands can be re-posed without moving the blade). With traces, the weapon config's blade traces are sampled anew through the
        /// controller (the content build does that itself). Returns the numbers.
        /// A weapon with every motion from footage gets its rest, taking out and putting away (the taking out backwards), the
        /// block up and down, the hit on the block and the bounce off a block written too. The game's legs and eyes stay in
        /// the stance under an upper-body clip, so the body keys are left out: the chest gives the whole turn the pelvis
        /// shares in the mock-up, and the hits are where they are seen.
        public static string Bake(SwordSpec spec, bool withTraces)
        {
            s_spec = spec;
            StringBuilder report = new StringBuilder();
            s_isStanding = true;
            SwordMotion[] motions;

            try
            {
                motions = Solve(string.Empty, report);
            }
            finally
            {
                s_isStanding = false;
            }

            SwordMotion Find(string id) => Array.Find(motions, motion => motion.Id == id);
            List<(string Name, Track Track)> clips = motions.Select(motion => (ClipName(motion), motion.Id == "idle" ? Still(Sample(motion, 0f), 1f) : Retime(motion))).ToList();

            if (Find("equip") is SwordMotion equip)
                clips.Add((s_spec.Prefix + FighterAnimComponent.UnequipSuffix, Reverse(Retime(equip))));

            if (Find("block") is SwordMotion block && Find("blockLower") is SwordMotion lower)
            {
                (float[], Quaternion) held = Sample(block, block.Frames);
                clips.Add((s_spec.Prefix + FighterAnimComponent.BlockImpactSuffix, Blend(s_weapon.BlockImpact + s_weapon.BlockRecovery,
                    (0f, held), (0.08f, Sample(lower, 4f)), (s_weapon.BlockImpact, Sample(lower, 2f)), (s_weapon.BlockImpact + s_weapon.BlockRecovery, held))));
            }

            if (Find("attack1") is SwordMotion first && Find("idle") is SwordMotion idle)
            {
                clips.Add((s_spec.Prefix + FighterAnimComponent.DeflectSuffix, Blend(s_weapon.DeflectDuration,
                    (0f, Sample(first, first.Windup + 2f)), (0.12f, Sample(first, Mathf.Max(first.Windup - 12f, 0f))), (s_weapon.DeflectDuration, Sample(idle, 0f)))));
            }

            using (Stage rig = CreateStage())
            {
                foreach ((string name, Track track) in clips)
                    Write(name, track, rig, name.EndsWith(FighterAnimComponent.IdleSuffix));
            }

            BakeEdits(clips.Select(clip => clip.Name));
            AssetDatabase.SaveAssets();

            if (withTraces)
                Trace(motions, report);

            return report.ToString();
        }

        /// The motions as keyed, unsolved: what the weapon's definition takes its timings and rough poses from.
        public static SwordMotion[] Read(SwordSpec spec)
        {
            SwordSpec solving = s_spec;
            s_spec = spec;

            try
            {
                return Parse(string.Empty);
            }
            finally
            {
                s_spec = solving;
            }
        }

        /// Solves the attacks (keys: a text file while tuning, empty = the keys in the code) and writes data.js of the review
        /// page into the folder. Returns the numbers.
        public static string Build(SwordSpec spec, string keys, string folder)
        {
            s_spec = spec;
            Directory.CreateDirectory(folder);
            StringBuilder report = new StringBuilder();
            Export(folder, Solve(keys, report));

            return report.ToString();
        }

        /// Solves the attacks and renders frames of them: shots "0:12,16|1:20", g_{attack}_{view}_{frame}.png and table{attack}.txt
        /// in the folder. Views: fp, front, side, left, back, top, ref, hand, c_x_y_z_tx_ty_tz_fov.
        public static string Run(SwordSpec spec, string keys, string folder, string shots, string views, int size)
        {
            s_spec = spec;
            Directory.CreateDirectory(folder);
            StringBuilder report = new StringBuilder();
            SwordMotion[] motions = Solve(keys, report);

            for (int i = 0; i < motions.Length; i++)
                File.WriteAllText($"{folder}/table{i}.txt", Tables[i]);

            if (shots.Length == 0)
                return report.ToString();

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
                    string id = shot.Split(':')[0];
                    SwordMotion motion = int.TryParse(id, out int number) ? motions[number] : motions.First(item => item.Id == id);

                    foreach (int frame in Array.ConvertAll(shot.Split(':')[1].Split(','), int.Parse))
                    {
                        // Skinning is evaluated once per editor frame, so every moment gets a rig of its own.
                        using Stage rig = CreateStage();
                        int index = Mathf.Clamp(frame, 0, motion.Frames);
                        Enter(rig, motion, index);
                        rig.Set(motion.Muscles[index], motion.BodyPositions[index], motion.BodyRotations[index], motion.Lean[index], motion.Roll[index]);

                        if (index >= motion.Shown)
                            CreateSword(rig.Socket);

                        foreach (Transform child in rig.Animator.GetComponentsInChildren<Transform>(true))
                            child.gameObject.layer = Layer;

                        foreach (string view in views.Split(','))
                        {
                            rig.Head.localScale = view.StartsWith("fp") ? Vector3.zero : Vector3.one;
                            Place(camera, view, rig.Hand.position);

                            if (view.StartsWith("fp"))
                                camera.transform.position += Eye(motion, index);

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
                case "fpd":
                    // fpd: the view of the Dark and Darker footage, 90 degrees across.
                    camera.fieldOfView = view == "fp" ? 75f : 58.7f;
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

            if (s_idleClip.Socket[0] != null)
            {
                stage.GripPosition = new Vector3(s_idleClip.Socket[0].Evaluate(0f), s_idleClip.Socket[1].Evaluate(0f), s_idleClip.Socket[2].Evaluate(0f));
                stage.GripRotation = new Quaternion(s_idleClip.Socket[3].Evaluate(0f), s_idleClip.Socket[4].Evaluate(0f), s_idleClip.Socket[5].Evaluate(0f),
                    s_idleClip.Socket[6].Evaluate(0f)).normalized;
            }

            if (IsTwoHanded)
                stage.PlaceLeftGrip();

            return stage;
        }

        /// The weapon's prefab in the socket, or the boxes that stand in for one it does not have yet.
        private static GameObject CreateSword(Transform socket)
        {
            WeaponConfig config = AssetDatabase.LoadAssetAtPath<WeaponConfig>($"{BattleEditorUtility.ConfigsFolder}/{s_spec.Prefix}.asset");
            GameObject prefab = config != null ? config.Attachments[0].Prefab
                : s_spec.Model != null ? AssetDatabase.LoadAssetAtPath<GameObject>($"{BattleEditorUtility.PrefabsFolder}/{s_spec.Model}.prefab") : null;
            GameObject sword = prefab != null ? Object.Instantiate(prefab, socket, false) : new GameObject(s_spec.Prefix);
            sword.hideFlags = HideFlags.HideAndDontSave;

            if (prefab != null)
                return sword;

            sword.transform.SetParent(socket, false);

            foreach ((Vector3 center, Vector3 size, Color color) in s_spec.Mock)
            {
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.hideFlags = HideFlags.HideAndDontSave;
                box.transform.SetParent(sword.transform, false);
                box.transform.localPosition = center;
                box.transform.localScale = size;

                if (!s_mockMaterials.TryGetValue(color, out Material material) || material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.HideAndDontSave };
                    material.SetColor("_BaseColor", color);
                    s_mockMaterials[color] = material;
                }

                box.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            return sword;
        }

        private static SwordMotion[] Solve(string keys, StringBuilder report)
        {
            s_right = Array.ConvertAll(s_arm, name => Muscle("Right " + name));
            s_left = Array.ConvertAll(s_arm, name => Muscle("Left " + name));
            s_rightFingers = Enumerable.Range(Body, HumanTrait.MuscleCount - Body).Where(i => HumanTrait.MuscleName[i].StartsWith("Right")).ToArray();
            s_leftFingers = Array.ConvertAll(s_rightFingers, i => Muscle("Left" + HumanTrait.MuscleName[i].Substring(5)));
            s_legs = Enumerable.Range(0, Body).Where(i => HumanTrait.MuscleName[i].Contains("Leg") || HumanTrait.MuscleName[i].Contains("Foot") || HumanTrait.MuscleName[i].Contains("Toes")).ToArray();
            s_torso = new[] { "Spine", "Chest", "UpperChest" }.SelectMany(bone => new[] { " Front-Back", " Left-Right", " Twist Left-Right" }.Select(axis => Muscle(bone + axis))).ToArray();
            s_neck = new[] { "Neck", "Head" }.SelectMany(bone => new[] { " Nod Down-Up", " Tilt Left-Right", " Turn Left-Right" }.Select(axis => Muscle(bone + axis))).ToArray();
            string socketPath;

            using (Stage probe = new Stage())
                socketPath = AnimationUtility.CalculateTransformPath(probe.Socket, probe.Animator.transform);

            s_idleClip = ReadClip(s_spec.Base + FighterAnimComponent.IdleSuffix, socketPath);
            s_idle = s_idleClip.Read(0f);
            s_leftOpen = Array.ConvertAll(s_leftFingers, i => s_idle[i]);
            s_idleTorso = Vector3.zero;
            s_idleLean = s_idleRoll = 0f;
            s_hasIdleMotion = s_isIdleLeftFree = false;

            // Both fists hold the hilt alike.
            for (int i = 0; i < s_leftFingers.Length && IsTwoHanded; i++)
                s_idle[s_leftFingers[i]] = s_idle[s_rightFingers[i]];

            s_stance = ReadStance();
            s_weapon = Dungeon.DungeonWeaponLibrary.CreateAll().FirstOrDefault(weapon => weapon.Prefix == s_spec.Prefix);

            using Stage rig = CreateStage();
            Calibrate(rig);
            SwordMotion[] motions = Parse(keys);
            Tables = new string[motions.Length];

            for (int i = 0; i < motions.Length; i++)
            {
                SwordMotion motion = motions[i];
                motion.Index = i;

                // The timing is the combat model's: the swing is fitted into the windup, the active window and the recovery it
                // has. A motion the model does not know yet carries its frames in the keys.
                if (!motion.HasTiming)
                {
                    AttackDefinition attack = s_weapon.Attacks[i];
                    motion.Duration = attack.Duration;
                    motion.Windup = attack.Windup * FrameRate;
                    motion.ActiveEnd = (attack.Windup + attack.Active) * FrameRate;
                    motion.Peak = Mathf.RoundToInt(BattleAnimationLibrary.PeakTime(attack) * FrameRate);
                    motion.Frames = Mathf.CeilToInt(attack.Duration * FrameRate - 1e-3f);
                }

                if (motion.AfterId != null)
                    motion.After = int.TryParse(motion.AfterId, out int index) ? index : Array.FindIndex(motions, item => item.Id == motion.AfterId);

                if (motion.After >= 0 && motion.AfterFrame < 0)
                    motion.AfterFrame = motions[motion.After].Frames;

                // A motion whose keys, the keys it is chained from and the idle are as they were last time is not solved again.
                motion.Text = $"{s_spec.Prefix}|{(s_hasIdleMotion ? motions[0].Text : string.Empty)}|{(motion.After >= 0 ? motions[motion.After].Text : string.Empty)}|{motion.Text}|{s_spec.Strike},{s_spec.OffHand},{motion.Frames},{motion.Windup},{motion.ActiveEnd},{motion.Peak},{motion.AfterFrame}";

                if (s_solved.TryGetValue(motion.Text, out SwordMotion solved))
                {
                    motion.Muscles = solved.Muscles;
                    motion.BodyPositions = solved.BodyPositions;
                    motion.BodyRotations = solved.BodyRotations;
                    motion.Grips = solved.Grips;
                    motion.Torso = solved.Torso;
                    motion.Lean = solved.Lean;
                    motion.Roll = solved.Roll;
                    motion.Strain = solved.Strain;
                    motion.Left = solved.Left;
                    motion.Lower = solved.Lower;
                    Tables[i] = solved.Name;
                    report.AppendLine($"{motion.Name}: as solved before");
                }
                else
                {
                    Solve(rig, motion, motions, report);
                    s_solved[motion.Text] = new SwordMotion
                    {
                        Muscles = motion.Muscles, BodyPositions = motion.BodyPositions, BodyRotations = motion.BodyRotations, Grips = motion.Grips, Torso = motion.Torso,
                        Lean = motion.Lean, Roll = motion.Roll, Strain = motion.Strain, Left = motion.Left, Lower = motion.Lower, Name = Tables[i]
                    };
                }

                if (i > 0 || motion.Id != "idle")
                    continue;

                // The rest pose of a weapon that has no idle clip yet: every motion after it leaves and meets this one.
                s_hasIdleMotion = true;
                // A weapon the right hand holds alone rests with the left hand off it: every idle key has it so.
                s_isIdleLeftFree = IsTwoHanded && motion.Left[0] < 0.5f;
                s_idleTorso = motion.Torso[0];
                s_idleLean = motion.Lean[0];
                s_idleRoll = motion.Roll[0];

                foreach (int muscle in s_right.Concat(s_left))
                    s_idle[muscle] = motion.Muscles[0][muscle];
            }

            return motions;
        }

        /// The body the controller plays under an upper-body clip while the fighter stands: the legs and the root every frame
        /// of the swing is solved on, the same the blade traces are sampled with.
        private static HumanPose ReadStance()
        {
            GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), Vector3.zero, Quaternion.identity);
            model.hideFlags = HideFlags.HideAndDontSave;
            Animator animator = Play(model, s_spec.Base + FighterAnimComponent.IdleSuffix, 0f);
            HumanPose pose = new HumanPose();

            using (HumanPoseHandler handler = new HumanPoseHandler(animator.avatar, animator.transform))
                handler.GetHumanPose(ref pose);

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            s_hips = new Pose(hips.position, hips.rotation);
            Transform left = animator.GetBoneTransform(HumanBodyBones.LeftFoot), right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            s_leftFoot = new Pose(left.position, left.rotation);
            s_rightFoot = new Pose(right.position, right.rotation);

            Object.DestroyImmediate(model);

            return pose;
        }

        private static Animator Play(GameObject model, string state, float normalizedTime)
        {
            Animator animator = model.GetComponent<Animator>();

            if (animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BattleEditorUtility.ControllerPath);
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
            }

            animator.SetFloat(FighterAnimComponent.ActionSpeedParam, 1f);

            // Twice: the first evaluation after switching states still shows the state before.
            for (int pass = 0; pass < 2; pass++)
            {
                animator.Play(state, 1, Mathf.Clamp(normalizedTime, 0f, 0.999f));
                animator.Update(0f);
            }

            return animator;
        }

        private static string ClipPath(string name)
        {
            return $"{BattleEditorUtility.AnimationsFolder}/{name}.anim";
        }

        /// The game clip a motion is: the named ones by their id, attackN as Attack(N-1), any other as the attack of its place.
        private static string ClipName(SwordMotion motion)
        {
            string suffix = motion.Id switch
            {
                "idle" => FighterAnimComponent.IdleSuffix,
                "equip" => FighterAnimComponent.EquipSuffix,
                "block" => FighterAnimComponent.BlockSuffix,
                "blockLower" => FighterAnimComponent.BlockLowerSuffix,
                "riposte" => FighterAnimComponent.RiposteSuffix,
                _ when motion.Id.StartsWith("attack") && int.TryParse(motion.Id.Substring(6), out int number) => FighterAnimComponent.AttackSuffix + (number - 1),
                _ => FighterAnimComponent.AttackSuffix + motion.Index
            };

            return s_spec.Prefix + suffix;
        }

        /// The motion at the game's pace: keyed in footage frames it plays Pace times as long, and the peak of a swing lands on
        /// the frame the combat model has it at (its windup and half its active time, rounded to the frame).
        private static Track Retime(SwordMotion motion)
        {
            float duration = motion.Duration * s_spec.Pace, end = motion.Frames * s_spec.Pace;
            float peak = motion.ActiveEnd > motion.Windup ? Mathf.Round(motion.Peak * s_spec.Pace) : 0f;
            int count = Mathf.CeilToInt(duration * FrameRate - 1e-3f) + 1;
            Track track = new Track { Muscles = new float[count][], Grips = new Quaternion[count], Duration = duration };

            for (int frame = 0; frame < count; frame++)
            {
                float at = Mathf.Min(frame, end);
                float source = peak <= 0f ? at / s_spec.Pace
                    : at <= peak ? at * motion.Peak / peak : motion.Peak + (at - peak) * (motion.Frames - motion.Peak) / Mathf.Max(end - peak, 1e-3f);
                (track.Muscles[frame], track.Grips[frame]) = Sample(motion, source);
            }

            return track;
        }

        /// The solved pose at a frame of the motion, between two frames by a share of each.
        private static (float[] Muscles, Quaternion Grip) Sample(SwordMotion motion, float frame)
        {
            frame = Mathf.Clamp(frame, 0f, motion.Frames);
            int from = Mathf.Min((int)frame, motion.Frames), to = Mathf.Min(from + 1, motion.Frames);
            float t = frame - from;

            if (t <= 0f)
                return ((float[])motion.Muscles[from].Clone(), motion.Grips[from]);

            float[] muscles = new float[motion.Muscles[from].Length];

            for (int i = 0; i < muscles.Length; i++)
                muscles[i] = Mathf.Lerp(motion.Muscles[from][i], motion.Muscles[to][i], t);

            return (muscles, Quaternion.Slerp(motion.Grips[from], motion.Grips[to], t));
        }

        private static Track Still((float[] Muscles, Quaternion Grip) pose, float duration)
        {
            return Blend(duration, (0f, pose), (duration, pose));
        }

        private static Track Reverse(Track track)
        {
            return new Track { Muscles = track.Muscles.Reverse().ToArray(), Grips = track.Grips.Reverse().ToArray(), Duration = track.Duration };
        }

        /// Poses already solved, run into one another (easing in and out) at the times given: a short clip made of the motions.
        private static Track Blend(float duration, params (float Time, (float[] Muscles, Quaternion Grip) Pose)[] keys)
        {
            int count = Mathf.CeilToInt(duration * FrameRate - 1e-3f) + 1;
            Track track = new Track { Muscles = new float[count][], Grips = new Quaternion[count], Duration = duration };

            for (int frame = 0; frame < count; frame++)
            {
                float time = Mathf.Min(frame / FrameRate, duration);
                int next = Mathf.Clamp(Array.FindIndex(keys, key => key.Time > time), 1, keys.Length - 1);
                (float start, (float[] Muscles, Quaternion Grip) a) = keys[next - 1];
                (float stop, (float[] Muscles, Quaternion Grip) b) = keys[next];
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time - start) / Mathf.Max(stop - start, 1e-4f)));
                track.Muscles[frame] = new float[a.Muscles.Length];

                for (int i = 0; i < a.Muscles.Length; i++)
                    track.Muscles[frame][i] = Mathf.Lerp(a.Muscles[i], b.Muscles[i], t);

                track.Grips[frame] = Quaternion.Slerp(a.Grip, b.Grip, t);
            }

            return track;
        }

        private static string MuscleProperty(int muscle)
        {
            string[] parts = HumanTrait.MuscleName[muscle].Split(' ');

            // The curve of a finger is named after its hand: 'Left Index 1 Stretched' is 'LeftHand.Index.1 Stretched'.
            return muscle < Body ? HumanTrait.MuscleName[muscle] : $"{parts[0]}Hand.{parts[1]}.{string.Join(" ", parts.Skip(2))}";
        }

        private static void Write(string name, Track track, Stage rig, bool isLoop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(name)) ?? throw new ArgumentException($"No clip '{ClipPath(name)}'");
            AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(s_spec.Prefix + FighterAnimComponent.IdleSuffix));
            int count = track.Muscles.Length;
            List<EditorCurveBinding> bindings = new List<EditorCurveBinding>();
            List<AnimationCurve> curves = new List<AnimationCurve>();

            // Keys sit on the simulation's 60 Hz grid, as the generated clips have them.
            void Add(EditorCurveBinding binding, Func<int, float> value)
            {
                AnimationCurve curve = new AnimationCurve();

                for (int frame = 0; frame < count; frame++)
                    curve.AddKey(Mathf.Min(frame / FrameRate, track.Duration), value(frame));

                for (int i = 0; i < curve.length; i++)
                    curve.SmoothTangents(i, 0f);

                bindings.Add(binding);
                curves.Add(curve);
            }

            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                int muscle = i;

                // The fingers hold the hilt as the idle has them; a finger the idle does not key stays as the avatar rests it.
                if (muscle < Body || s_idleClip.Muscles[muscle] != null)
                    Add(EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), MuscleProperty(muscle)), frame => track.Muscles[frame][muscle]);
            }

            for (int i = 0; i < s_rootCurves.Length; i++)
            {
                int axis = i;
                Add(EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), s_rootCurves[i]), frame =>
                    axis < 3 ? s_stance.bodyPosition[axis] : s_stance.bodyRotation[axis - 3]);
            }

            // The sword's socket turns in the fist as the swing was solved; the other sockets stay as the idle carries them.
            Quaternion[] grips = new Quaternion[count];

            for (int frame = 0; frame < count; frame++)
            {
                Quaternion grip = track.Grips[frame];
                grips[frame] = frame > 0 && Quaternion.Dot(grips[frame - 1], grip) < 0f ? new Quaternion(-grip.x, -grip.y, -grip.z, -grip.w) : grip;
            }

            for (int s = 0; s < rig.Source.Sockets.Length; s++)
            {
                Transform socket = rig.Source.Sockets[s];
                string path = AnimationUtility.CalculateTransformPath(socket, rig.Animator.transform);
                bool isSword = socket == rig.Socket;
                float[] rest = { socket.localPosition.x, socket.localPosition.y, socket.localPosition.z, socket.localRotation.x, socket.localRotation.y, socket.localRotation.z, socket.localRotation.w };

                for (int i = 0; i < s_socketCurves.Length; i++)
                {
                    int axis = i;
                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), s_socketCurves[i]);
                    AnimationCurve carried = idle == null ? null : AnimationUtility.GetEditorCurve(idle, binding);
                    float still = isSword && axis < 3 ? rig.GripPosition[axis] : carried?.Evaluate(0f) ?? rest[axis];
                    Add(binding, frame => isSword && axis >= 3 ? grips[frame][axis - 3] : still);
                }
            }

            clip.ClearCurves();
            clip.frameRate = FrameRate;
            AnimationUtility.SetEditorCurves(clip, bindings.ToArray(), curves.ToArray());
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = isLoop;
            settings.loopBlendOrientation = true;
            settings.loopBlendPositionY = true;
            settings.loopBlendPositionXZ = true;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            settings.stopTime = track.Duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        /// Hand edits of the written clips (made in the animation test scene on these footage clips) go back on top of them.
        private static void BakeEdits(IEnumerable<string> names)
        {
            AnimationEditConfig edits = AssetDatabase.LoadAssetAtPath<AnimationEditConfig>(AnimationEditBaker.ConfigPath);

            if (edits == null)
                return;

            HashSet<AnimationClip> clips = new HashSet<AnimationClip>(names.Select(name => AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(name))));

            if (!edits.Clips.Any(edit => clips.Contains(edit.Clip)))
                return;

            GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath));
            BattlePoseRig.CreateSockets(model.GetComponent<Animator>());
            AnimationEditBaker.BakeAll(edits, model.GetComponent<Animator>(), clips.Contains);
            Object.DestroyImmediate(model);
        }

        /// The blade through each swing as the controller plays the clip, the way the content build samples it into the weapon
        /// config: what hits is what is seen.
        private static void Trace(SwordMotion[] motions, StringBuilder report)
        {
            WeaponConfig config = AssetDatabase.LoadAssetAtPath<WeaponConfig>($"{BattleEditorUtility.ConfigsFolder}/{s_spec.Prefix}.asset");
            SerializedObject serialized = new SerializedObject(config);
            GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), Vector3.zero, Quaternion.identity);
            model.hideFlags = HideFlags.HideAndDontSave;
            Transform socket = BattlePoseRig.CreateSockets(model.GetComponent<Animator>())[(int)WeaponSocket.RightHand];

            foreach (SwordMotion motion in motions)
            {
                SerializedProperty attack = serialized.FindProperty("_attacks").GetArrayElementAtIndex(motion.Index);
                SerializedProperty bases = attack.FindPropertyRelative("_traceBase"), tips = attack.FindPropertyRelative("_traceTip");
                int count = motion.Frames + 1;
                bases.arraySize = tips.arraySize = count;

                for (int frame = 0; frame < count; frame++)
                {
                    Play(model, s_spec.Prefix + FighterAnimComponent.AttackSuffix + motion.Index, Mathf.Min(frame / FrameRate, motion.Duration) / motion.Duration);
                    bases.GetArrayElementAtIndex(frame).vector3Value = socket.TransformPoint(0f, 0f, s_weapon.BladeBase);
                    tips.GetArrayElementAtIndex(frame).vector3Value = socket.TransformPoint(0f, 0f, s_weapon.BladeTip);

                    if (frame != motion.Peak)
                        continue;

                    Vector3 strike = socket.TransformPoint(0f, 0f, Strike);
                    report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}: through the controller the strike point is {1:F1} cm off the crosshair at the peak",
                        motion.Name, new Vector2(strike.x - s_eye.x, strike.y - s_eye.y).magnitude * 100f));
                }
            }

            Object.DestroyImmediate(model);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
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
                pose.Muscles[i] = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), MuscleProperty(i)));
            }

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

        /// The chest turned against the way ahead and the head kept on what the eyes look at. What the pelvis has turned
        /// already the spine does not turn again; what it pitches the neck gives back.
        private static void SetTorso(float[] muscles, Vector3 torso, float pelvisYaw = 0f, float pelvisPitch = 0f)
        {
            Vector3 chest = new Vector3(torso.x / s_torsoScale.x, torso.z / s_torsoScale.z, (torso.y - pelvisYaw) / s_torsoScale.y);
            Vector3 head = new Vector3(-(torso.x + pelvisPitch) / s_neckScale.x, -torso.z / s_neckScale.z, -torso.y / s_neckScale.y) * 0.8f;

            for (int i = 0; i < s_torso.Length; i++)
                muscles[s_torso[i]] = Mathf.Clamp(s_idle[s_torso[i]] + chest[i % 3], -1f, 1f);

            for (int i = 0; i < s_neck.Length; i++)
                muscles[s_neck[i]] = Mathf.Clamp(s_idle[s_neck[i]] + head[i % 3], -1f, 1f);
        }

        private static void Solve(Stage rig, SwordMotion motion, SwordMotion[] motions, StringBuilder report)
        {
            int count = motion.Frames + 1;
            motion.Muscles = new float[count][];
            motion.BodyPositions = new Vector3[count];
            motion.BodyRotations = new Quaternion[count];
            motion.Grips = new Quaternion[count];
            motion.Torso = new Vector3[count];
            motion.Lean = new float[count];
            motion.Roll = new float[count];
            motion.Strain = new float[count];
            motion.Left = new float[count];
            motion.Lower = BuildLower(motion, motions);

            for (int frame = 0; frame < count; frame++)
            {
                float[] muscles = (float[])s_idle.Clone();

                foreach (int i in s_legs)
                    muscles[i] = s_stance.muscles[i];

                motion.Muscles[frame] = muscles;
                motion.BodyPositions[frame] = s_stance.bodyPosition;
                motion.BodyRotations[frame] = s_stance.bodyRotation;
            }

            // The idle as the game shows it, and the pose the attack before leaves: what the first key starts from.
            float[] rest = (float[])motion.Muscles[0].Clone();

            if (s_hasIdleMotion)
                SetTorso(rest, s_idleTorso);

            rig.Lower = null;
            rig.Set(rest, motion.BodyPositions[0], motion.BodyRotations[0], s_idleLean, s_idleRoll);
            SwordKey idle = new SwordKey
            {
                Grip = rig.Socket.position, Blade = rig.Socket.forward, Edge = rig.Socket.up, Elbow = rig.Elbow.position, Torso = s_idleTorso, Flow = 0f, Hard = 2f, IsIdle = true,
                IsLeftFree = s_isIdleLeftFree, LeftHand = rig.LeftHand.position
            };
            float[] seed = Seed(s_idle, s_idleLean, s_idleRoll);
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
                Enter(rig, before, from);
                rig.Set(before.Muscles[from], motion.BodyPositions[0], motion.BodyRotations[0], before.Lean[from], before.Roll[from]);
                // Keys are given as the eyes see them: what the body adds comes back on every frame's target.
                Vector3 eye = Eye(before, from);
                keys[0] = new SwordKey
                {
                    Grip = rig.Socket.position - eye, Blade = rig.Socket.forward, Elbow = rig.Elbow.position - eye, Torso = before.Torso[from], Flow = 0f,
                    IsLeftFree = IsTwoHanded && before.Left[from] < 0.5f, LeftHand = rig.LeftHand.position - eye
                };
                seed = Seed(before.Muscles[from], before.Lean[from], before.Roll[from]);
            }

            // First the aimed moments find where the arm is at ease with the strike point on the crosshair; their keys take
            // those poses, so the motion runs through them instead of hopping onto them for a frame.
            Target[] rough = Interpolate(keys, count, motion);
            float[] found = seed;

            for (int frame = 0; frame < count; frame++)
            {
                found = SolveArm(rig, motion, frame, rough[frame], found, frame == 0 ? 0f : 1f, null);
                Apply(motion, frame, found, rough[frame].Left);
                Enter(rig, motion, frame);
                rig.Set(motion.Muscles[frame], motion.BodyPositions[frame], motion.BodyRotations[frame], motion.Lean[frame], motion.Roll[frame]);

                for (int i = 0; i < keys.Count; i++)
                {
                    if (!keys[i].IsAim || Mathf.RoundToInt(keys[i].Frame) != frame)
                        continue;

                    SwordKey key = keys[i];
                    key.Grip = rig.Socket.position - Eye(motion, frame);
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
                        : Mathf.Clamp01(Mathf.Max((motion.Windup - 4f - frame) / 4f, (frame - motion.ActiveEnd) / 4f));
                    float[] earlier = before[Mathf.Max(frame - 1, 0)], later = before[Mathf.Min(frame + 1, count - 1)];
                    solved[frame] = new float[earlier.Length];

                    for (int i = 0; i < earlier.Length; i++)
                        solved[frame][i] = Mathf.Lerp(before[frame][i], earlier[i] * 0.25f + before[frame][i] * 0.5f + later[i] * 0.25f, free);
                }
            }

            // The left fist is free to turn about the hilt, and where its forearm has to turn over to keep hold (palm down
            // before a cut round, palm up after it), it would do so in a frame. Its muscles are eased over the frames around
            // that instead, and with the forearm's turn kept as eased the fist is brought back onto the hilt: the hand turns
            // round the hilt in the course of the swing.
            for (int pass = 0; pass < 24 && IsTwoHanded; pass++)
            {
                float[][] before = solved;
                solved = new float[count][];

                for (int frame = 0; frame < count; frame++)
                {
                    solved[frame] = (float[])before[frame].Clone();

                    for (int i = OneHanded; i < solved[frame].Length && frame > 0 && frame < count - 1; i++)
                        solved[frame][i] = before[frame - 1][i] * 0.25f + before[frame][i] * 0.5f + before[frame + 1][i] * 0.25f;
                }
            }

            for (int frame = 1; frame < count - 1 && IsTwoHanded; frame++)
            {
                // The left arm goes on from where the frame before left it, so it keeps to one way of holding on.
                float[] from = (float[])solved[frame - 1].Clone();
                Array.Copy(solved[frame], from, OneHanded);
                solved[frame] = SolveArm(rig, motion, frame, targets[frame], from, 1f, solved[frame], OneHanded + 6);
            }

            float worstGrip = 0f, worstBlade = 0f, worstStep = 0f, worstGap = 0f;
            int gripFrame = 0, bladeFrame = 0, stepFrame = 0, gapFrame = 0;
            int[] joints = Enumerable.Range(0, seed.Length).Where(i => i < Arm || i >= OneHanded).ToArray();
            StringBuilder limits = new StringBuilder();
            StringBuilder table = new StringBuilder("frame | grip-off blade-off edge | ShDU ShFB ArDU ArFB ArTw FoSt FoTw HaDU HaIO\n");

            for (int frame = 0; frame < count; frame++)
            {
                float[] previous = solved[Mathf.Max(frame - 1, 0)];
                values = solved[frame];
                Apply(motion, frame, values, targets[frame].Left);
                Enter(rig, motion, frame);
                rig.Set(motion.Muscles[frame], motion.BodyPositions[frame], motion.BodyRotations[frame], motion.Lean[frame], motion.Roll[frame]);
                motion.Grips[frame] = rig.Grip(motion.Lean[frame], motion.Roll[frame]);
                motion.Torso[frame] = targets[frame].Torso;
                motion.Left[frame] = targets[frame].Left;
                float gap = IsTwoHanded ? Vector3.Distance(rig.LeftGrip.position, rig.Socket.position - rig.Socket.forward * s_spec.OffHand) * targets[frame].Left : 0f;
                float grip = Vector3.Distance(rig.Socket.position, targets[frame].Grip);
                float blade = Vector3.Angle(rig.Socket.forward, targets[frame].Blade);
                float step = joints.Max(i => Mathf.Abs(values[i] - previous[i]));
                float strain = joints.Max(i => Mathf.Abs(values[i]));
                motion.Strain[frame] = strain;

                if (grip > worstGrip) { worstGrip = grip; gripFrame = frame; }
                if (blade > worstBlade) { worstBlade = blade; bladeFrame = frame; }
                if (step > worstStep) { worstStep = step; stepFrame = frame; }
                if (gap > worstGap) { worstGap = gap; gapFrame = frame; }

                if (strain > 0.97f)
                    limits.Append(' ').Append(frame);

                table.Append(frame).Append(string.Format(CultureInfo.InvariantCulture, " | {0,5:F1}cm {1,4:F0}deg e{2:F1} |", grip * 100f, blade, targets[frame].EdgeWeight));

                for (int i = 0; i < 9; i++)
                    table.Append(string.Format(CultureInfo.InvariantCulture, " {0,5:F2}", values[i]));

                table.AppendLine(string.Format(CultureInfo.InvariantCulture, " | lean {0,4:F0} roll {1,4:F0} | fore-blade {2,4:F0} | grip {3:F2} {4:F2} {5:F2} elbow {6:F2} {7:F2} {8:F2}",
                    motion.Lean[frame], motion.Roll[frame], Vector3.Angle(rig.Hand.position - rig.Elbow.position, rig.Socket.forward),
                    rig.Socket.position.x, rig.Socket.position.y, rig.Socket.position.z, rig.Elbow.position.x, rig.Elbow.position.y, rig.Elbow.position.z));

                if (!IsTwoHanded)
                    continue;

                table.Length -= Environment.NewLine.Length;
                table.Append(string.Format(CultureInfo.InvariantCulture, " | left {0:F1} gap {1,4:F1}cm |", targets[frame].Left, gap * 100f));

                for (int i = OneHanded; i < values.Length; i++)
                    table.Append(string.Format(CultureInfo.InvariantCulture, " {0,5:F2}", values[i]));

                table.AppendLine(string.Format(CultureInfo.InvariantCulture, " | hand {0:F2} {1:F2} {2:F2} elbow {3:F2} {4:F2} {5:F2}",
                    rig.LeftHand.position.x, rig.LeftHand.position.y, rig.LeftHand.position.z, rig.LeftElbow.position.x, rig.LeftElbow.position.y, rig.LeftElbow.position.z));
            }

            Enter(rig, motion, motion.Peak);
            rig.Set(motion.Muscles[motion.Peak], motion.BodyPositions[motion.Peak], motion.BodyRotations[motion.Peak], motion.Lean[motion.Peak], motion.Roll[motion.Peak]);
            Vector3 strike = rig.Socket.position + rig.Socket.forward * Strike - Eye(motion, motion.Peak);
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0}: frames {1} peak {2} | peak miss {3:F1} cm (strike z {4:F2}) | grip off target max {5:F1} cm @{6} | blade off max {7:F0} deg @{8} | muscle step max {9:F2} @{10}",
                motion.Name, motion.Frames, motion.Peak, new Vector2(strike.x - s_eye.x, strike.y - s_eye.y).magnitude * 100f, strike.z, worstGrip * 100f, gripFrame,
                worstBlade, bladeFrame, worstStep, stepFrame));
            report.AppendLine("  muscle past 0.97 on frames:" + (limits.Length > 0 ? limits.ToString() : " none"));

            if (IsTwoHanded)
                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "  left fist off the hilt max {0:F1} cm @{1}", worstGap * 100f, gapFrame));

            Tables[motion.Index] = table.ToString();
        }

        private static void Enter(Stage rig, SwordMotion motion, int frame)
        {
            rig.Lower = motion.Lower != null ? motion.Lower[frame] : (BodyFrame?)null;
        }

        private static Vector3 Eye(SwordMotion motion, int frame)
        {
            return motion.Lower != null ? motion.Lower[frame].Eye : Vector3.zero;
        }

        /// The body per frame from the "body" keys: the hips run through them, a foot stands where a key has it and steps
        /// between two keys that have it elsewhere. A motion chained from another starts as that one stands; one that ends in
        /// the idle ends in the stance, any other stays as its last key has it.
        private static BodyFrame[] BuildLower(SwordMotion motion, SwordMotion[] motions)
        {
            BodyFrame[] before = motion.After >= 0 ? motions[motion.After].Lower : null;

            if (motion.BodyKeys.Count == 0 && before == null)
                return null;

            List<BodyKey> keys = motion.BodyKeys.OrderBy(key => key.Frame).ToList();
            BodyKey first = new BodyKey { Body = before != null ? before[motion.AfterFrame] : default, HasHips = true, HasLeft = true, HasRight = true };

            if (before != null)
                keys.RemoveAll(key => key.Frame <= 0);

            if (keys.Count == 0 || keys[0].Frame > 0)
                keys.Insert(0, first);

            for (int i = 0; i < keys.Count; i++)
            {
                BodyKey key = keys[i];
                BodyFrame last = i > 0 ? keys[i - 1].Body : default, body = key.Body;

                if (!key.HasHips)
                    (body.Shift, body.Yaw, body.Pitch) = (last.Shift, last.Yaw, last.Pitch);

                if (!key.HasLeft)
                    (body.Left, body.LeftYaw, body.LeftHeel) = (last.Left, last.LeftYaw, last.LeftHeel);

                if (!key.HasRight)
                    (body.Right, body.RightYaw, body.RightHeel) = (last.Right, last.RightYaw, last.RightHeel);

                key.Body = body;
                keys[i] = key;
            }

            if (keys[^1].Frame < motion.Frames)
                keys.Add(new BodyKey { Frame = motion.Frames, Body = motion.Keys[^1].IsIdle ? default : keys[^1].Body });

            Vector3 Slope(Func<BodyFrame, Vector3> value, int i)
            {
                return i == 0 || i == keys.Count - 1 ? Vector3.zero : (value(keys[i + 1].Body) - value(keys[i - 1].Body)) / (keys[i + 1].Frame - keys[i - 1].Frame);
            }

            Vector3 Step(Vector3 from, Vector3 to, float t)
            {
                float stride = Vector3.Distance(from, to);

                return Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)) + Vector3.up * (stride > 0.04f ? Mathf.Min(0.09f, stride * 0.3f) * Mathf.Sin(t * Mathf.PI) : 0f);
            }

            Vector3 toEye = s_eye - s_hips.position;
            BodyFrame[] frames = new BodyFrame[motion.Frames + 1];

            for (int frame = 0; frame < frames.Length; frame++)
            {
                int i = Mathf.Clamp(keys.FindLastIndex(key => key.Frame <= frame), 0, keys.Count - 2);
                float span = keys[i + 1].Frame - keys[i].Frame, t = Mathf.Clamp01((frame - keys[i].Frame) / span), ease = Mathf.SmoothStep(0f, 1f, t);
                BodyFrame a = keys[i].Body, b = keys[i + 1].Body;
                Vector3 Along(Func<BodyFrame, Vector3> value) => Hermite(value(a), Slope(value, i) * span, value(b), Slope(value, i + 1) * span, t);
                Vector3 turn = Along(body => new Vector3(body.Yaw, body.Pitch, 0f));
                BodyFrame at = new BodyFrame
                {
                    Shift = Along(body => body.Shift), Yaw = turn.x, Pitch = turn.y,
                    Left = Step(a.Left, b.Left, t), Right = Step(a.Right, b.Right, t),
                    LeftYaw = Mathf.Lerp(a.LeftYaw, b.LeftYaw, ease), RightYaw = Mathf.Lerp(a.RightYaw, b.RightYaw, ease),
                    LeftHeel = Mathf.Lerp(a.LeftHeel, b.LeftHeel, ease), RightHeel = Mathf.Lerp(a.RightHeel, b.RightHeel, ease)
                };
                // The eyes go with the hips, and forward and down as the pelvis pitches the body over them.
                at.Eye = at.Shift + Quaternion.Euler(0f, at.Yaw, 0f) * (Quaternion.Euler(at.Pitch, 0f, 0f) * (Quaternion.Euler(0f, -at.Yaw, 0f) * toEye)) - toEye;
                frames[frame] = at;
            }

            return frames;
        }

        private static float[] Seed(float[] muscles, float lean, float roll)
        {
            IEnumerable<float> seed = Array.ConvertAll(s_right, i => muscles[i]).Concat(new[] { lean / MaxLean, roll / MaxRoll });

            return (IsTwoHanded ? seed.Concat(Array.ConvertAll(s_left, i => muscles[i])) : seed).ToArray();
        }

        private static void Apply(SwordMotion motion, int frame, float[] values, float left)
        {
            for (int i = 0; i < s_right.Length; i++)
                motion.Muscles[frame][s_right[i]] = values[i];

            motion.Lean[frame] = values[9] * MaxLean;
            motion.Roll[frame] = values[10] * MaxRoll;

            for (int i = OneHanded; i < values.Length; i++)
                motion.Muscles[frame][s_left[i - OneHanded]] = values[i];

            // A fist on the hilt, an open hand off it.
            for (int i = 0; i < s_leftFingers.Length && IsTwoHanded; i++)
                motion.Muscles[frame][s_leftFingers[i]] = Mathf.Lerp(s_leftOpen[i], s_idle[s_leftFingers[i]], left);
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
                float freeA = a.IsLeftFree ? 1f : 0f, freeB = b.IsLeftFree ? 1f : 0f;
                target.Left = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Lerp(freeA, freeB, t));
                target.LeftHand = freeA + freeB > 1.5f ? Vector3.Lerp(a.LeftHand, b.LeftHand, Mathf.SmoothStep(0f, 1f, t)) : freeA > 0f ? a.LeftHand : b.LeftHand;
                float leftElbowA = a.LeftElbow == Vector3.zero ? 0f : 1f, leftElbowB = b.LeftElbow == Vector3.zero ? 0f : 1f;
                target.LeftElbowWeight = Mathf.Lerp(leftElbowA, leftElbowB, t);
                target.LeftElbow = leftElbowA + leftElbowB > 1.5f ? Vector3.Lerp(a.LeftElbow, b.LeftElbow, Mathf.SmoothStep(0f, 1f, t)) : leftElbowA > 0f ? a.LeftElbow : b.LeftElbow;
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
                targets[frame].Edge = Vector3.ProjectOnPlane(side, targets[frame].Blade).normalized * (motion.IsBackEdge ? -1f : 1f);
                targets[frame].EdgeWeight = swing;
            }

            // Keyed as the eyes see it, everything goes where the body takes the eyes.
            for (int frame = 0; frame < count && motion.Lower != null; frame++)
            {
                Vector3 eye = motion.Lower[frame].Eye;
                targets[frame].Grip += eye;
                targets[frame].Elbow += eye;
                targets[frame].LeftHand += eye;
                targets[frame].LeftElbow += eye;
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
        private static float[] SolveArm(Stage rig, SwordMotion motion, int frame, Target target, float[] start, float inertia, float[] between, int pinned = -1)
        {
            const float step = 2e-3f;
            int n = start.Length;
            float[] muscles = (float[])motion.Muscles[frame].Clone();
            Enter(rig, motion, frame);
            BodyFrame body = rig.Lower ?? default;
            SetTorso(muscles, target.Torso, body.Yaw, body.Pitch);
            Array.Copy(muscles, motion.Muscles[frame], muscles.Length);
            float hard = 1f + target.Hard;

            float[] Residuals(float[] x)
            {
                for (int i = 0; i < 9; i++)
                    muscles[s_right[i]] = x[i];

                for (int i = OneHanded; i < x.Length; i++)
                    muscles[s_left[i - OneHanded]] = x[i];

                rig.Set(muscles, motion.BodyPositions[frame], motion.BodyRotations[frame], x[9] * MaxLean, x[10] * MaxRoll);
                List<float> r = new List<float>(48);
                Add(r, (rig.Socket.position - target.Grip) * (35f * hard * (1f - 0.4f * target.Aim)));
                Add(r, (rig.Socket.forward - target.Blade) * (14f * hard * (1f - 0.5f * target.Aim)));
                Vector3 strike = rig.Socket.position + rig.Socket.forward * Strike;
                r.Add((strike.x - s_eye.x - body.Eye.x) * 300f * target.Aim);
                r.Add((strike.y - s_eye.y - body.Eye.y) * 300f * target.Aim);
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

                if (x.Length == OneHanded)
                    return r.ToArray();

                // The left fist on the hilt behind the right one; off the hilt, the hand where the key has it. Each forearm comes
                // to the hilt from its own side, so the left fist is free to turn about the hilt, and its wrist gives: the hold
                // of the hilt weighs more than its line through the fist.
                Vector3 hilt = rig.Socket.position - rig.Socket.forward * s_spec.OffHand;
                Add(r, (rig.LeftGrip.position - hilt) * (30f * target.Left));
                float askew = Vector3.Angle(rig.LeftGrip.forward, rig.Socket.forward);
                r.Add(target.Left * (0.012f * askew + 0.15f * Mathf.Max(0f, askew - MaxLean)));
                Add(r, (rig.LeftHand.position - target.LeftHand) * (14f * (1f - target.Left)));
                Add(r, (rig.LeftElbow.position - target.LeftElbow) * (10f * target.LeftElbowWeight));
                r.Add(4f * target.Idle * (x[9] - s_idleLean / MaxLean));
                r.Add(4f * target.Idle * (x[10] - s_idleRoll / MaxRoll));
                r.Add(pinned < 0 ? 0f : 25f * (x[pinned] - between[pinned]));

                for (int i = 0; i < Arm; i++)
                {
                    int at = OneHanded + i;
                    r.Add(12f * Mathf.Max(0f, Mathf.Abs(x[at]) - SoftLimit));
                    r.Add(s_ease[i] * (x[at] - target.Idle * s_idle[s_left[i]]));
                    r.Add(4f * target.Idle * (x[at] - s_idle[s_left[i]]));
                    r.Add(inertia * (i < 2 ? 2.5f : 0.8f) * (x[at] - start[at]));
                    r.Add(between == null ? 0f : (i < 2 ? 3f : 1.5f) * (x[at] - between[at]));
                }

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

        /// Keys of the attacks: the ones in the code, or a text file of the same lines while poses are being tuned. Lines:
        /// "motion <name> | after <attack> <frame>", "idle <frame>", "key <frame> x y z bx by bz", "aim <frame> x y z",
        /// each followed by any of "to <bow> <turn> <lean>", "el x y z", "ed x y z", "hard <w>", "stop".
        private static SwordMotion[] Parse(string path)
        {
            List<SwordMotion> motions = new List<SwordMotion>();

            foreach (string raw in path.Length > 0 ? File.ReadAllLines(path) : s_spec.Keys.Split('\n'))
            {
                string line = raw.Split('#')[0].Trim();

                if (line.Length == 0)
                    continue;

                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int at = 1;
                float Next() => float.Parse(words[at++], CultureInfo.InvariantCulture);

                if (words[0] == "motion")
                {
                    SwordMotion motion = new SwordMotion { Id = words[1], Text = line, Name = "Attack " + (motions.Count + 1), Keys = new List<SwordKey>() };
                    int after = Array.IndexOf(words, "after"), title = Array.IndexOf(words, "title", 2);
                    float Word(string name, float fallback) => Array.IndexOf(words, name, 2) is int found and > 0 ? float.Parse(words[found + 1], CultureInfo.InvariantCulture) : fallback;

                    motion.IsCut = Array.IndexOf(words, "thrust", 2) < 0;
                    motion.IsBackEdge = Array.IndexOf(words, "back", 2) > 0;
                    motion.Shown = (int)Word("shown", 0f);

                    if (after > 0)
                    {
                        motion.AfterId = words[after + 1];
                        motion.AfterFrame = words[after + 2] == "end" ? -1 : int.Parse(words[after + 2]);
                    }

                    if (title > 0)
                        motion.Name = words[title + 1].Replace('_', ' ');

                    // "frames <n> windup <f> active <f> peak <f>": the timing of a motion the combat model does not have yet.
                    if (Array.IndexOf(words, "frames", 2) > 0)
                    {
                        motion.HasTiming = true;
                        motion.Frames = (int)Word("frames", 0f);
                        motion.Duration = motion.Frames / FrameRate;
                        motion.Windup = Word("windup", 0f);
                        motion.ActiveEnd = motion.Windup + Word("active", 0f);
                        motion.Peak = (int)Word("peak", motion.Windup);
                    }

                    motions.Add(motion);

                    continue;
                }

                // "body <frame> [h dx dy dz yaw [pitch]] [lf dx dz [yaw [heel]]] [rf dx dz [yaw [heel]]]": the hips off the stance
                // and the pelvis turned right / pitched forward, each foot off its stance, turned right, its heel raised.
                if (words[0] == "body" && s_isStanding)
                    continue;

                if (words[0] == "body")
                {
                    BodyKey body = new BodyKey { Frame = (int)Next() };

                    float Maybe()
                    {
                        return at < words.Length && float.TryParse(words[at], NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && at++ >= 0 ? value : 0f;
                    }

                    while (at < words.Length)
                    {
                        switch (words[at++])
                        {
                            case "h":
                                body.HasHips = true;
                                body.Body.Shift = new Vector3(Next(), Next(), Next());
                                body.Body.Yaw = Next();
                                body.Body.Pitch = Maybe();
                                break;
                            case "lf":
                                body.HasLeft = true;
                                body.Body.Left = new Vector3(Next(), 0f, Next());
                                body.Body.LeftYaw = Maybe();
                                body.Body.LeftHeel = Maybe();
                                break;
                            case "rf":
                                body.HasRight = true;
                                body.Body.Right = new Vector3(Next(), 0f, Next());
                                body.Body.RightYaw = Maybe();
                                body.Body.RightHeel = Maybe();
                                break;
                            default:
                                throw new ArgumentException($"Unknown word in '{line}'");
                        }
                    }

                    motions[^1].BodyKeys.Add(body);
                    motions[^1].Text += "\n" + line;

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
                        case "lh":
                            key.IsLeftFree = true;
                            key.LeftHand = new Vector3(Next(), Next(), Next());
                            break;
                        case "le":
                            key.LeftElbow = new Vector3(Next(), Next(), Next());
                            break;
                        case "stop":
                            key = key.Stop();
                            break;
                        default:
                            throw new ArgumentException($"Unknown word in '{line}'");
                    }
                }

                motions[^1].Keys.Add(key);
                motions[^1].Text += "\n" + line;
            }

            return motions.ToArray();
        }

        private static void Export(string folder, SwordMotion[] motions)
        {
            using Stage rig = CreateStage();
            GameObject sword = CreateSword(rig.Socket);
            SkinnedMeshRenderer[] skins = rig.Animator.GetComponentsInChildren<SkinnedMeshRenderer>(false).Where(skin => skin.enabled && skin.sharedMesh != null).ToArray();
            List<Transform> bones = skins.SelectMany(skin => skin.bones).Where(bone => bone != null).Distinct().ToList();
            List<Texture> textures = new List<Texture>();
            StringBuilder json = new StringBuilder("window.MOCKUP=");
            json.Append("{\"eye\":").Append(Json(new[] { s_eye.x, s_eye.y, s_eye.z })).Append(",\"fov\":75,\"strike\":").Append(Json(Strike));
            json.Append(",\"bladeBase\":").Append(Json(s_spec.BladeBase)).Append(",\"bladeTip\":").Append(Json(s_spec.BladeTip));

            if (IsTwoHanded)
                json.Append(",\"offHand\":").Append(Json(s_spec.OffHand));

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
                    Enter(rig, motion, frame);
                    rig.Set(motion.Muscles[frame], motion.BodyPositions[frame], motion.BodyRotations[frame], motion.Lean[frame], motion.Roll[frame]);

                    for (int b = 0; b < bones.Count; b++)
                        Write(poses, (frame * bones.Count + b) * 10, bones[b].position, bones[b].rotation, bones[b].lossyScale);

                    Write(sockets, frame * 7, rig.Socket.position, rig.Socket.rotation, null);
                }

                json.Append(c > 0 ? "," : string.Empty).Append("{\"name\":\"").Append(motion.Name).Append("\",\"index\":").Append(motion.Index);
                json.Append(",\"frames\":").Append(count).Append(",\"peak\":").Append(motion.Peak);
                json.Append(",\"windup\":").Append(Json(motion.Windup)).Append(",\"activeEnd\":").Append(Json(motion.ActiveEnd));
                json.Append(",\"after\":").Append(motion.After).Append(",\"afterFrame\":").Append(motion.AfterFrame);

                if (IsTwoHanded)
                    json.Append(",\"id\":\"").Append(motion.Id).Append("\",\"shown\":").Append(motion.Shown).Append(",\"left\":").Append(Json(motion.Left));

                if (motion.Lower != null)
                    json.Append(",\"eyes\":").Append(Json(motion.Lower.SelectMany(body => new[] { body.Eye.x, body.Eye.y, body.Eye.z }).ToArray()));

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
