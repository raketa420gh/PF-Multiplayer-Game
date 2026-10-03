using System;
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

        /// Zero Up = the rig derives the hand roll from the solved forearm, keeping the wrist straight.
        public bool IsAutoRoll => Up == Vector3.zero;
        public Quaternion Rotation => Quaternion.LookRotation(Forward, Up);

        public HandPose(Vector3 position, Vector3 forward, Vector3 up)
        {
            Position = position;
            Forward = forward.normalized;
            Up = up.normalized;
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
        private const int SwivelSamples = 36;

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
        private HumanPose _humanPose;

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
                SolveArm(_rightArm, _sockets[(int)WeaponSocket.RightHand], pose.Main);
                SolveArm(_leftArm, _sockets[(int)pose.OffSocket], pose.Off);
            }
            else
            {
                DropArm(_rightArm, pose.ArmDrop);
                DropArm(_leftArm, pose.ArmDrop);
            }
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
            return new Arm { Shoulder = Bone(shoulder), Upper = Bone(upper), Lower = Bone(lower), Hand = Bone(hand), Side = side };
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

        private void SolveArm(in Arm arm, Transform socket, in HandPose pose)
        {
            Quaternion socketInverse = Quaternion.Inverse(socket.localRotation);
            Vector3 shoulderPosition = arm.Shoulder.position;
            Quaternion assist = Quaternion.FromToRotation(arm.Upper.position - shoulderPosition, pose.Position - shoulderPosition);
            arm.Shoulder.rotation = Quaternion.Slerp(Quaternion.identity, assist, ShoulderAssist) * arm.Shoulder.rotation;

            Vector3 upperPosition = arm.Upper.position;
            Vector3 defaultHint = upperPosition + new Vector3(arm.Side * 0.2f, -0.5f, -0.3f);
            Vector3 forearm = pose.IsAutoRoll ? pose.Position - upperPosition : pose.Up;
            Quaternion handRotation = Quaternion.identity;
            Vector3 target = pose.Position;
            Vector3 hint = defaultHint;

            // Hand roll, wrist position and elbow swivel depend on each other; a few passes converge.
            for (int i = 0; i < (pose.IsAutoRoll ? 3 : 1); i++)
            {
                handRotation = Quaternion.LookRotation(pose.Forward, forearm) * socketInverse;
                target = pose.Position - handRotation * socket.localPosition;

                if (!pose.IsAutoRoll)
                    break;

                hint = FindElbow(arm, upperPosition, target, defaultHint, pose.Forward);
                forearm = target - hint;
            }

            SolveTwoBone(arm.Upper, arm.Lower, arm.Hand, target, hint);
            arm.Hand.rotation = handRotation;
        }

        /// Picks the elbow on the IK swivel circle that keeps the forearm near perpendicular to the held weapon
        /// (natural grip, wrist deviation within limits) while staying close to the relaxed down-and-out elbow.
        private static Vector3 FindElbow(in Arm arm, Vector3 upper, Vector3 wrist, Vector3 defaultHint, Vector3 weaponAxis)
        {
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
            Vector3 best = center + relaxed * radius;
            float bestCost = float.MaxValue;

            for (int i = 0; i < SwivelSamples; i++)
            {
                float angle = i * Mathf.PI * 2f / SwivelSamples;
                Vector3 elbow = center + (relaxed * Mathf.Cos(angle) + side * Mathf.Sin(angle)) * radius;
                float tilt = Mathf.Asin(Mathf.Clamp(Vector3.Dot((wrist - elbow).normalized, weaponAxis), -1f, 1f));
                float excess = Mathf.Max(0f, Mathf.Abs(tilt - GripTilt) - MaxWristDeviation);
                float cost = excess * excess * 6f + (1f - Mathf.Cos(angle)) * 1.5f;

                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = elbow;
                }
            }

            return best;
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

            if (bendProjected.sqrMagnitude > 1e-6f && hintProjected.sqrMagnitude > 1e-6f)
                upper.rotation = Quaternion.FromToRotation(bendProjected, hintProjected) * upper.rotation;
        }

        private static float TriangleAngle(float opposite, float sideA, float sideB)
        {
            float cos = Mathf.Clamp((sideA * sideA + sideB * sideB - opposite * opposite) / (2f * sideA * sideB), -1f, 1f);

            return Mathf.Acos(cos);
        }
    }
}
