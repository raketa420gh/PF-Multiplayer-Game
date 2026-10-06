using System;
using System.Collections.Generic;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Battle
{
    internal struct HandPose
    {
        public Vector3 Position;
        public Vector3 Forward;
        public Vector3 Up;
        /// Where the elbow is, in root space, as the footage shows it; the arm solve pulls it there as hard as ElbowWeight
        /// says (0 = wherever the arm is most at ease, 1 = authored).
        public Vector3 Elbow;
        public float ElbowWeight;

        /// Zero Up = the rig derives the hand roll from the solved forearm, keeping the wrist straight.
        public bool IsAutoRoll => Up == Vector3.zero;
        public Quaternion Rotation => Quaternion.LookRotation(Forward, Up);

        public HandPose(Vector3 position, Vector3 forward, Vector3 up)
        {
            Position = position;
            Forward = forward.normalized;
            Up = up.normalized;
            Elbow = Vector3.zero;
            ElbowWeight = 0f;
        }

        /// Gripping hand: weapon axis is given, the forearm direction comes from the arm solve.
        public HandPose(Vector3 position, Vector3 forward) : this(position, forward, Vector3.zero)
        {
        }
    }

    internal struct BodyPose
    {
        public Vector3 Hips;
        public Vector3 HipsEuler;
        public Vector3 Spine;
        public Vector3 Head;
        public Vector3 LeftFoot;
        public Vector3 RightFoot;
        public Vector3 LeftFootEuler;
        public Vector3 RightFootEuler;
        public float ArmDrop;
        public bool HasHands;
        public HandPose Main;
        public HandPose Off;
        public WeaponSocket OffSocket;
        /// Where the leading edge of the held weapon faces. Zero = the roll comes from the arm solve, like a free hand's.
        public Vector3 Edge;
        /// How far an off hand on the same weapon is turned about it from the main hand, in degrees: hands keep their grip.
        public float OffRoll;
        /// How far the fingers of the main and the off hand are open, 0..1: a hand that holds nothing is not always a fist.
        public float MainOpen;
        public float OffOpen;
    }

    /// Poses a model instance with FK/IK in root space and converts the result to humanoid muscle curves.
    internal sealed class BattlePoseRig : IDisposable
    {
        public const float PalmOffset = 0.14f;

        public Animator Animator => _animator;
        public Transform[] Sockets => _sockets;

        private struct Arm
        {
            public Transform Shoulder;
            public Transform Upper;
            public Transform Lower;
            public Transform Hand;
            public float Side;
            // The humanoid muscles that turn the upper arm and the forearm about themselves.
            public int ArmTwist;
            public int ForearmTwist;
        }

        /// What a frame of a motion being planned knows about an elbow: the cost of every place on its swivel circle,
        /// the axis of the circle and the direction the places are counted from.
        private sealed class SwivelCircle
        {
            public float[] Costs;
            public Vector3 Axis;
            public Vector3 Zero;
        }

        private struct Leg
        {
            public Transform Upper;
            public Transform Lower;
            public Transform Foot;
            public Vector3 FootPosition;
            public Quaternion FootRotation;
        }

        private const float ShoulderAssist = 0.35f;
        // A fist holds the handle diagonally: with a neutral wrist the weapon leans ~15 deg past perpendicular toward
        // where the forearm points; the wrist adds about +-30 deg of radial/ulnar deviation on top.
        private const float GripTilt = 15f * Mathf.Deg2Rad;
        private const float MaxWristDeviation = 30f * Mathf.Deg2Rad;
        // With the roll of the weapon given, the wrist also bends about the handle to bring the knuckles to the edge.
        private const float MaxWristFlexion = 40f * Mathf.Deg2Rad;
        private const float WristStiffness = 15f;
        /// Past this the wrist reads as broken whatever the footage says about the elbow: the cost outweighs any authored one.
        private const float MaxWristBend = 85f * Mathf.Deg2Rad;
        private const float BrokenWrist = 400f;
        private const float ElbowRest = 1.5f;
        /// How far the arm's twist muscles go before it costs; the avatar clamps them at 1 and the forearm reads as wrung.
        private const float MaxTwistMuscle = 0.8f;
        private const float TwistStiffness = 100f;
        /// The twist is read off the humanoid pose on every this many places of the swivel circle, and interpolated between.
        private const int TwistStep = 4;
        /// What it costs an elbow to stand off the authored one, per (1 - cos) of the angle between them on the circle.
        private const float ElbowAuthored = 40f;
        private const int SwivelSamples = 72;
        private const float SwivelStep = Mathf.PI * 2f / SwivelSamples;
        /// What it costs an elbow of a planned motion to move along its swivel circle within a frame.
        private const float SwivelInertia = 10f;
        private const int SwivelSmoothing = 8;

        private static readonly float[] s_swivelCosts = new float[SwivelSamples];

        private readonly GameObject _root;
        private readonly Animator _animator;
        private readonly HumanPoseHandler _handler;
        private readonly Transform[] _bones;
        private readonly Pose[] _bind;
        private readonly Transform[] _sockets;
        private readonly Transform _hips;
        private readonly Transform[] _spine;
        private readonly Transform _neck;
        private readonly Transform _head;
        private readonly Vector3 _hipsPosition;
        private readonly Quaternion _hipsRotation;
        private readonly Arm _rightArm;
        private readonly Arm _leftArm;
        private readonly Leg _rightLeg;
        private readonly Leg _leftLeg;
        // The planned motion: where each elbow (right, left) is on its swivel circle on every frame, NaN = wherever is best.
        private readonly float[][] _swivels = new float[2][];
        private HumanPose _humanPose;
        private SwivelCircle[][] _circles;
        private int _frame = -1;

        public BattlePoseRig()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath);
            _root = Object.Instantiate(model, Vector3.zero, Quaternion.identity);
            _root.hideFlags = HideFlags.HideAndDontSave;
            _animator = _root.GetComponent<Animator>();
            _sockets = CreateSockets(_animator);
            _handler = new HumanPoseHandler(_animator.avatar, _root.transform);

            _bones = _root.GetComponentsInChildren<Transform>();
            _bind = new Pose[_bones.Length];

            for (int i = 0; i < _bones.Length; i++)
                _bind[i] = new Pose(_bones[i].localPosition, _bones[i].localRotation);

            _hips = Bone(HumanBodyBones.Hips);
            _hipsPosition = _hips.position;
            _hipsRotation = _hips.rotation;
            _spine = new[] { Bone(HumanBodyBones.Spine), Bone(HumanBodyBones.Chest), Bone(HumanBodyBones.UpperChest) };
            _neck = Bone(HumanBodyBones.Neck);
            _head = Bone(HumanBodyBones.Head);

            _rightArm = CreateArm(HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 1f);
            _leftArm = CreateArm(HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -1f);
            _rightLeg = CreateLeg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
            _leftLeg = CreateLeg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
        }

        public void Dispose()
        {
            _handler.Dispose();
            Object.DestroyImmediate(_root);
        }

        /// Must be called while the model is in its bind T-pose at identity. Result is indexed by WeaponSocket.
        public static Transform[] CreateSockets(Animator animator)
        {
            Transform[] sockets = new Transform[4];
            sockets[(int)WeaponSocket.RightHand] = CreateSocket(animator, HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm, false);
            sockets[(int)WeaponSocket.LeftHand] = CreateSocket(animator, HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm, false);
            sockets[(int)WeaponSocket.RightShield] = CreateSocket(animator, HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm, true);
            sockets[(int)WeaponSocket.LeftShield] = CreateSocket(animator, HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm, true);

            return sockets;
        }

        public Vector3 GetShoulderPosition(bool isRight)
        {
            return (isRight ? _rightArm : _leftArm).Upper.position;
        }

        public void Apply(in BodyPose pose)
        {
            for (int i = 0; i < _bones.Length; i++)
                _bones[i].SetLocalPositionAndRotation(_bind[i].position, _bind[i].rotation);

            _hips.SetPositionAndRotation(_hipsPosition + pose.Hips, Quaternion.Euler(pose.HipsEuler) * _hipsRotation);

            Quaternion spineStep = Quaternion.Euler(pose.Spine / _spine.Length);

            foreach (Transform bone in _spine)
                bone.rotation = spineStep * bone.rotation;

            Quaternion headStep = Quaternion.Euler(pose.Head * 0.5f);
            _neck.rotation = headStep * _neck.rotation;
            _head.rotation = headStep * _head.rotation;

            SolveLeg(_leftLeg, pose.LeftFoot, pose.LeftFootEuler);
            SolveLeg(_rightLeg, pose.RightFoot, pose.RightFootEuler);

            if (pose.HasHands)
            {
                // An off hand on the haft wraps it at whatever roll its forearm comes from: the haft turns in the lower hand, and
                // a roll tied to the edge bent that wrist back on itself whenever the edge turned through a cut.
                SolveArm(_rightArm, _sockets[(int)WeaponSocket.RightHand], pose.Main, pose.Edge, false);
                SolveArm(_leftArm, _sockets[(int)pose.OffSocket], pose.Off, Vector3.zero, IsShared(pose));
            }
            else
            {
                DropArm(_rightArm, pose.ArmDrop);
                DropArm(_leftArm, pose.ArmDrop);
            }
        }

        /// Reads the legs of a clip: the feet relative to the bind stance with the hips' ground drift removed, and the hips height.
        public BodyPose SampleLegs(AnimationClip clip, float time)
        {
            BattleEditorUtility.SampleClip(_animator, clip, time);
            Vector3 drift = Vector3.ProjectOnPlane(_hips.position - _hipsPosition, Vector3.up);

            return new BodyPose
            {
                Hips = Vector3.up * (_hips.position.y - _hipsPosition.y),
                LeftFoot = _leftLeg.Foot.position - drift - _leftLeg.FootPosition,
                RightFoot = _rightLeg.Foot.position - drift - _rightLeg.FootPosition,
                LeftFootEuler = (_leftLeg.Foot.rotation * Quaternion.Inverse(_leftLeg.FootRotation)).eulerAngles,
                RightFootEuler = (_rightLeg.Foot.rotation * Quaternion.Inverse(_rightLeg.FootRotation)).eulerAngles,
                ArmDrop = 70f
            };
        }

        /// Solves the elbows of a motion as a whole before its poses are applied frame by frame. A pose solved on its own
        /// puts each elbow at the best place of its swivel circle, and the elbow jumps across when another place becomes
        /// the better one; a planned elbow takes the path that is comfortable all the way. The first pose stays solved on
        /// its own, as other clips lead into the motion there, and so does the last one of a loop.
        public void Plan(IReadOnlyList<BodyPose> poses, bool isLoop)
        {
            _swivels[0] = null;
            _swivels[1] = null;
            _circles = new[] { new SwivelCircle[poses.Count], new SwivelCircle[poses.Count] };

            for (_frame = 0; _frame < poses.Count; _frame++)
                Apply(poses[_frame]);

            SwivelCircle[][] circles = _circles;
            _circles = null;
            _frame = -1;

            for (int side = 0; side < circles.Length; side++)
                _swivels[side] = PlanSwivels(circles[side], isLoop);
        }

        /// Applies a pose of the planned motion.
        public void Apply(in BodyPose pose, int frame)
        {
            _frame = frame;
            Apply(pose);
            _frame = -1;
        }

        /// Gives a pose the leading edge the arm solve finds for its main hand, unless it is authored with one, and the
        /// roll the solve finds for an off hand on the same weapon.
        public void SolveEdge(ref BodyPose pose)
        {
            Vector3 authored = pose.Edge;
            pose.Edge = Vector3.zero;
            Apply(pose);
            Vector3 solved = _sockets[(int)WeaponSocket.RightHand].up;
            pose.OffRoll = IsShared(pose) ? Vector3.SignedAngle(solved, _sockets[(int)pose.OffSocket].up, pose.Main.Forward) : 0f;
            pose.Edge = authored == Vector3.zero ? solved : Vector3.ProjectOnPlane(authored, pose.Main.Forward).normalized;
        }

        private static bool IsShared(in BodyPose pose)
        {
            return Vector3.Cross(pose.Off.Position - pose.Main.Position, pose.Main.Forward).sqrMagnitude < 1e-4f;
        }

        public HumanPose Capture()
        {
            _handler.GetHumanPose(ref _humanPose);

            return _humanPose;
        }

        private static Transform CreateSocket(Animator animator, HumanBodyBones handBone, HumanBodyBones lowerArmBone, bool isShield)
        {
            Transform hand = animator.GetBoneTransform(handBone);
            Vector3 armDirection = (hand.position - animator.GetBoneTransform(lowerArmBone).position).normalized;
            string name = handBone + (isShield ? "ShieldSocket" : "Socket");
            Transform socket = hand.Find(name);

            if (socket == null)
            {
                socket = new GameObject(name).transform;
                socket.SetParent(hand, false);
            }

            if (isShield)
                socket.SetPositionAndRotation(hand.position + armDirection * 0.02f + Vector3.up * 0.07f,
                    Quaternion.LookRotation(Vector3.up, Vector3.forward));
            else
                socket.SetPositionAndRotation(hand.position + armDirection * PalmOffset - Vector3.up * 0.02f,
                    Quaternion.LookRotation(Vector3.forward, armDirection));

            return socket;
        }

        private Transform Bone(HumanBodyBones bone)
        {
            return _animator.GetBoneTransform(bone);
        }

        private Arm CreateArm(HumanBodyBones shoulder, HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, float side)
        {
            string name = side > 0f ? "Right" : "Left";

            return new Arm
            {
                Shoulder = Bone(shoulder), Upper = Bone(upper), Lower = Bone(lower), Hand = Bone(hand), Side = side,
                ArmTwist = Array.IndexOf(HumanTrait.MuscleName, $"{name} Arm Twist In-Out"),
                ForearmTwist = Array.IndexOf(HumanTrait.MuscleName, $"{name} Forearm Twist In-Out")
            };
        }

        private Leg CreateLeg(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones foot)
        {
            Transform footBone = Bone(foot);

            return new Leg
            {
                Upper = Bone(upper),
                Lower = Bone(lower),
                Foot = footBone,
                FootPosition = footBone.position,
                FootRotation = footBone.rotation
            };
        }

        private void SolveLeg(in Leg leg, Vector3 offset, Vector3 euler)
        {
            Quaternion rotation = Quaternion.Euler(euler);
            Vector3 target = leg.FootPosition + offset;
            Vector3 hint = (leg.Upper.position + target) * 0.5f + rotation * Vector3.forward + Vector3.up * 0.2f;

            SolveTwoBone(leg.Upper, leg.Lower, leg.Foot, target, hint);
            leg.Foot.rotation = rotation * leg.FootRotation;
        }

        private void SolveArm(in Arm arm, Transform socket, in HandPose pose, Vector3 edge, bool isOnHaft)
        {
            Quaternion socketInverse = Quaternion.Inverse(socket.localRotation);
            Vector3 shoulderPosition = arm.Shoulder.position;
            Vector3 collar = arm.Upper.position - shoulderPosition;
            Vector3 reach = pose.Position - shoulderPosition;
            float turn = Vector3.Angle(collar, reach);

            // A hand across the body, behind the shoulder, gives it no side to turn to: the assist fades out toward there.
            arm.Shoulder.rotation = Quaternion.AngleAxis(turn * ShoulderAssist * Mathf.InverseLerp(180f, 120f, turn), Vector3.Cross(collar, reach)) *
                                    arm.Shoulder.rotation;

            Vector3 upperPosition = arm.Upper.position;

            // A hand on a haft turns its knuckles away from the shoulder: the wrist sits on the shoulder's side of the haft.
            // Solving that roll from the forearm instead fed back through the wrist and did not settle, which bent the wrist
            // back and threw the elbow about from frame to frame.
            if (isOnHaft && pose.IsAutoRoll)
                edge = Vector3.ProjectOnPlane(pose.Position - upperPosition, pose.Forward);

            // With an edge to face, the hand is given whole and only the elbow is searched; a free hand takes its roll from the forearm.
            bool isFree = pose.IsAutoRoll && edge == Vector3.zero;
            Vector3 defaultHint = upperPosition + new Vector3(arm.Side * 0.2f, -0.5f, -0.3f);
            Vector3 forearm = isFree ? pose.Position - upperPosition : pose.IsAutoRoll ? edge : pose.Up;
            Quaternion handRotation = Quaternion.identity;
            Vector3 target = pose.Position;
            Vector3 hint = pose.IsAutoRoll ? defaultHint : Vector3.Lerp(defaultHint, pose.Elbow, pose.ElbowWeight);

            // Hand roll, wrist position and elbow swivel depend on each other; a few passes converge.
            for (int i = 0; i < (isFree ? 3 : 1); i++)
            {
                handRotation = Quaternion.LookRotation(pose.Forward, forearm) * socketInverse;
                target = pose.Position - handRotation * socket.localPosition;

                if (!pose.IsAutoRoll)
                    break;

                hint = FindElbow(arm, upperPosition, target, defaultHint, pose, edge, handRotation);
                forearm = target - hint;
            }

            SolveTwoBone(arm.Upper, arm.Lower, arm.Hand, target, hint);
            arm.Hand.rotation = handRotation;
        }

        /// Picks the elbow on the IK swivel circle that keeps the forearm near perpendicular to the held weapon
        /// (natural grip, wrist deviation within limits) and behind the knuckles when the edge is given, while staying
        /// close to the relaxed down-and-out elbow, or to the authored one. In a planned motion the plan says where on the
        /// circle the elbow is.
        private Vector3 FindElbow(in Arm arm, Vector3 upper, Vector3 wrist, Vector3 defaultHint, in HandPose pose, Vector3 edge, Quaternion hand)
        {
            Vector3 weaponAxis = pose.Forward;
            float a = Vector3.Distance(upper, arm.Lower.position);
            float b = Vector3.Distance(arm.Lower.position, arm.Hand.position);
            Vector3 axis = wrist - upper;
            float d = Mathf.Clamp(axis.magnitude, Mathf.Abs(a - b) + 1e-3f, (a + b) * 0.999f);
            axis.Normalize();

            float along = (a * a - b * b + d * d) / (2f * d);
            float radius = Mathf.Sqrt(Mathf.Max(a * a - along * along, 0f));
            Vector3 center = upper + axis * along;
            Vector3 relaxed = Vector3.ProjectOnPlane(defaultHint - upper, axis).normalized;
            Vector3 side = Vector3.Cross(axis, relaxed);
            Vector3 authored = Vector3.ProjectOnPlane(pose.Elbow - center, axis);
            float[] twists = TwistCosts(arm, upper, wrist, center, relaxed, side, radius, hand);
            // An authored elbow near the line from the shoulder to the wrist says little about which side it is on.
            float authoredWeight = pose.ElbowWeight * Mathf.InverseLerp(0.03f, 0.1f, authored.magnitude);
            int best = 0;

            for (int i = 0; i < SwivelSamples; i++)
            {
                float angle = i * SwivelStep;
                Vector3 place = relaxed * Mathf.Cos(angle) + side * Mathf.Sin(angle);
                Vector3 elbow = center + place * radius;
                Vector3 forearm = (wrist - elbow).normalized;
                float tilt = Mathf.Asin(Mathf.Clamp(Vector3.Dot(forearm, weaponAxis), -1f, 1f));
                float excess = Mathf.Max(0f, Mathf.Abs(tilt - GripTilt) - MaxWristDeviation);
                float rest = (1f - Mathf.Cos(angle)) * ElbowRest * (1f - authoredWeight);
                // An elbow the footage shows is worth the wrist bend it takes.
                float stiffness = WristStiffness * (1f - 0.5f * authoredWeight);
                float cost = excess * excess * stiffness + rest + (1f - Vector3.Dot(place, authored.normalized)) * ElbowAuthored * authoredWeight;
                cost += twists[i];

                if (edge != Vector3.zero)
                {
                    float flexion = Vector3.Angle(Vector3.ProjectOnPlane(forearm, weaponAxis), edge) * Mathf.Deg2Rad;
                    excess = Mathf.Max(0f, flexion - MaxWristFlexion);
                    float broken = Mathf.Max(0f, flexion - MaxWristBend);
                    cost += excess * excess * stiffness + broken * broken * BrokenWrist;
                }

                s_swivelCosts[i] = cost;

                if (cost < s_swivelCosts[best])
                    best = i;
            }

            int index = arm.Side > 0f ? 0 : 1;
            float swivel = _frame >= 0 && _circles == null && _swivels[index] != null ? _swivels[index][_frame] : float.NaN;

            if (_frame >= 0 && _circles != null)
                _circles[index][_frame] = new SwivelCircle { Costs = (float[])s_swivelCosts.Clone(), Axis = axis, Zero = relaxed };

            // On its own the elbow goes to the lowest cost. That lies between the samples: a parabola through the best
            // one and its neighbours finds it.
            if (float.IsNaN(swivel))
            {
                float previous = s_swivelCosts[(best + SwivelSamples - 1) % SwivelSamples];
                float next = s_swivelCosts[(best + 1) % SwivelSamples];
                float curvature = previous - 2f * s_swivelCosts[best] + next;
                swivel = (best + (curvature > 1e-6f ? Mathf.Clamp((previous - next) / (2f * curvature), -0.5f, 0.5f) : 0f)) * SwivelStep;
            }

            return center + (relaxed * Mathf.Cos(swivel) + side * Mathf.Sin(swivel)) * radius;
        }

        /// What it costs every place of the swivel circle to turn the upper arm and the forearm about themselves past
        /// the reach of their muscles, read off the humanoid pose the arm takes there: the clip stores muscles, and a roll
        /// past their end is clamped and wrings the forearm.
        private float[] TwistCosts(in Arm arm, Vector3 upper, Vector3 wrist, Vector3 center, Vector3 relaxed, Vector3 side, float radius, Quaternion hand)
        {
            float[] costs = new float[SwivelSamples];

            for (int i = 0; i < SwivelSamples; i += TwistStep)
            {
                float angle = i * SwivelStep;
                SolveTwoBone(arm.Upper, arm.Lower, arm.Hand, wrist, center + (relaxed * Mathf.Cos(angle) + side * Mathf.Sin(angle)) * radius);
                arm.Hand.rotation = hand;
                _handler.GetHumanPose(ref _humanPose);
                float upperExcess = Mathf.Max(0f, Mathf.Abs(_humanPose.muscles[arm.ArmTwist]) - MaxTwistMuscle);
                float foreExcess = Mathf.Max(0f, Mathf.Abs(_humanPose.muscles[arm.ForearmTwist]) - MaxTwistMuscle);
                costs[i] = (upperExcess * upperExcess + foreExcess * foreExcess) * TwistStiffness;
            }

            for (int i = 0; i < SwivelSamples; i++)
            {
                int from = i / TwistStep * TwistStep;
                costs[i] = Mathf.Lerp(costs[from], costs[(from + TwistStep) % SwivelSamples], (float)(i - from) / TwistStep);
            }

            return costs;
        }

        /// The path around the swivel circle that costs the least over the whole motion when moving along the circle
        /// costs as well; frames without a circle have no elbow to place. The circle turns with the arm, so a move is
        /// counted from where the elbow would be if the arm carried it along without twisting. Smoothed at the end, as
        /// the path runs from sample to sample.
        private static float[] PlanSwivels(SwivelCircle[] circles, bool isLoop)
        {
            int frames = circles.Length;
            float[] total = new float[SwivelSamples];
            float[] next = new float[SwivelSamples];
            float[] carried = new float[frames];
            int[][] from = new int[frames][];

            if (circles[0] != null)
            {
                Array.Fill(total, float.MaxValue);
                total[Lowest(circles[0].Costs)] = 0f;
            }

            for (int i = 1; i < frames; i++)
            {
                from[i] = new int[SwivelSamples];
                float turn = 0f;

                // How far the direction the places are counted from has turned about the arm since the frame before.
                if (circles[i] != null && circles[i - 1] != null)
                {
                    Vector3 zero = Quaternion.FromToRotation(circles[i - 1].Axis, circles[i].Axis) * circles[i - 1].Zero;
                    turn = Mathf.Atan2(Vector3.Dot(zero, Vector3.Cross(circles[i].Axis, circles[i].Zero)), Vector3.Dot(zero, circles[i].Zero));
                }

                carried[i] = carried[i - 1] + turn;

                for (int s = 0; s < SwivelSamples; s++)
                {
                    next[s] = float.MaxValue;

                    for (int p = 0; p < SwivelSamples; p++)
                    {
                        float cost = total[p] + (1f - Mathf.Cos((s - p) * SwivelStep - turn)) * SwivelInertia;

                        if (cost >= next[s])
                            continue;

                        next[s] = cost;
                        from[i][s] = p;
                    }

                    if (circles[i] != null)
                        next[s] += circles[i].Costs[s];
                }

                (total, next) = (next, total);
            }

            float[] swivels = new float[frames];
            int state = Lowest(isLoop && circles[frames - 1] != null ? circles[frames - 1].Costs : total);

            for (int i = frames - 1; i >= 0; i--)
            {
                swivels[i] = state * SwivelStep - carried[i];
                state = i > 0 ? from[i][state] : state;
            }

            for (int i = 1; i < frames; i++)
                swivels[i] = swivels[i - 1] + Mathf.DeltaAngle(swivels[i - 1] * Mathf.Rad2Deg, swivels[i] * Mathf.Rad2Deg) * Mathf.Deg2Rad;

            for (int pass = 0; pass < SwivelSmoothing; pass++)
            {
                float previous = swivels[0];

                for (int i = 1; i < frames - 1; i++)
                {
                    float current = swivels[i];
                    swivels[i] = (previous + current * 2f + swivels[i + 1]) * 0.25f;
                    previous = current;
                }
            }

            for (int i = 0; i < frames; i++)
                swivels[i] += carried[i];

            swivels[0] = float.NaN;

            if (isLoop)
                swivels[frames - 1] = float.NaN;

            return swivels;
        }

        private static int Lowest(float[] costs)
        {
            int lowest = 0;

            for (int i = 1; i < costs.Length; i++)
            {
                if (costs[i] < costs[lowest])
                    lowest = i;
            }

            return lowest;
        }

        private static void DropArm(in Arm arm, float angle)
        {
            arm.Upper.rotation = Quaternion.AngleAxis(-arm.Side * angle, Vector3.forward) * arm.Upper.rotation;
        }

        private static void SolveTwoBone(Transform upper, Transform lower, Transform tip, Vector3 target, Vector3 hint)
        {
            Vector3 upperPosition = upper.position;
            Vector3 toLower = lower.position - upperPosition;
            Vector3 lowerToTip = tip.position - lower.position;
            Vector3 toTip = tip.position - upperPosition;
            Vector3 toTarget = target - upperPosition;
            Vector3 toHint = hint - upperPosition;

            float upperLength = toLower.magnitude;
            float lowerLength = lowerToTip.magnitude;
            float targetLength = Mathf.Min(toTarget.magnitude, (upperLength + lowerLength) * 0.999f);

            float currentAngle = TriangleAngle(toTip.magnitude, upperLength, lowerLength);
            float targetAngle = TriangleAngle(targetLength, upperLength, lowerLength);

            Vector3 axis = Vector3.Cross(toLower, lowerToTip);

            if (axis.sqrMagnitude < 1e-8f)
                axis = Vector3.Cross(toHint, lowerToTip);

            if (axis.sqrMagnitude < 1e-8f)
                axis = Vector3.Cross(toTarget, lowerToTip);

            if (axis.sqrMagnitude < 1e-8f)
                axis = Vector3.up;

            lower.rotation = Quaternion.AngleAxis((currentAngle - targetAngle) * Mathf.Rad2Deg, axis.normalized) * lower.rotation;
            upper.rotation = Quaternion.FromToRotation(tip.position - upperPosition, toTarget) * upper.rotation;

            Vector3 chain = tip.position - upperPosition;
            float chainSqr = chain.sqrMagnitude;

            if (chainSqr < 1e-8f)
                return;

            Vector3 bend = lower.position - upperPosition;
            Vector3 bendProjected = bend - chain * (Vector3.Dot(bend, chain) / chainSqr);
            Vector3 hintProjected = toHint - chain * (Vector3.Dot(toHint, chain) / chainSqr);

            // Turned about the chain itself: the shortest turn between opposite directions would take the tip off the target.
            if (bendProjected.sqrMagnitude > 1e-6f && hintProjected.sqrMagnitude > 1e-6f)
                upper.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(bendProjected, hintProjected, chain), chain) * upper.rotation;
        }

        private static float TriangleAngle(float opposite, float sideA, float sideB)
        {
            float cos = Mathf.Clamp((sideA * sideA + sideB * sideB - opposite * opposite) / (2f * sideA * sideB), -1f, 1f);

            return Mathf.Acos(cos);
        }
    }
}
