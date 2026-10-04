using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Hand-made corrections on top of generated clips: per-bone keys of a local rotation (and a hips offset) turned after the
    /// clip has posed the body. Baked into the clip on save and again after every animation rebuild, so they outlive the generators.
    [CreateAssetMenu(menuName = "Game/Battle/Animation Edit Config")]
    public sealed class AnimationEditConfig : ScriptableObject
    {
        public IReadOnlyList<ClipEdit> Clips => _clips;

        [SerializeField]
        private List<ClipEdit> _clips = new();

        public ClipEdit Find(AnimationClip clip)
        {
            return _clips.Find(edit => edit.Clip == clip);
        }

        public void Store(ClipEdit edit)
        {
            _clips.RemoveAll(other => other.Clip == edit.Clip);
            _clips.Add(edit);
        }
    }

    [Serializable]
    public sealed class ClipEdit
    {
        public AnimationClip Clip => _clip;
        /// The clip as generated, before any edit; the edit is always baked onto it.
        public AnimationClip Source => _source;
        public IReadOnlyList<BoneTrack> Tracks => _tracks;
        public IReadOnlyList<HandGrip> Grips => _grips;

        [SerializeField]
        private AnimationClip _clip;

        [SerializeField]
        private AnimationClip _source;

        [SerializeField]
        private List<BoneTrack> _tracks = new();

        [SerializeField]
        private List<HandGrip> _grips = new();

        public ClipEdit(AnimationClip clip, AnimationClip source)
        {
            _clip = clip;
            _source = source;
        }

        public void SetSource(AnimationClip source) => _source = source;

        public BoneTrack Find(HumanBodyBones bone)
        {
            return _tracks.Find(track => track.Bone == bone);
        }

        public BoneTrack GetOrAdd(HumanBodyBones bone)
        {
            BoneTrack track = Find(bone);

            if (track != null)
                return track;

            track = new BoneTrack(bone);
            _tracks.Add(track);

            return track;
        }

        public HandGrip FindGrip(HumanBodyBones hand)
        {
            return _grips.Find(grip => grip.Hand == hand);
        }

        public void SetGrip(HandGrip grip)
        {
            RemoveGrip(grip.Hand);
            _grips.Add(grip);
        }

        public void RemoveGrip(HumanBodyBones hand)
        {
            _grips.RemoveAll(grip => grip.Hand == hand);
        }

        public void RemoveTrack(HumanBodyBones bone)
        {
            _tracks.RemoveAll(track => track.Bone == bone);
        }

        public void RemoveEmpty()
        {
            _tracks.RemoveAll(track => track.Keys.Count == 0);
        }

        public bool HasKey(int frame)
        {
            return _tracks.Exists(track => track.IndexOf(frame) >= 0);
        }

        public void Apply(Animator animator, float frame)
        {
            foreach (BoneTrack track in _tracks)
            {
                Transform bone = animator.GetBoneTransform(track.Bone);

                if (bone != null)
                    track.Apply(bone, frame);
            }

            // Grips go last, holds before pins: a pinned hand follows the weapon where the holding hand finally carries it.
            foreach (HandGrip grip in _grips)
            {
                if (grip.IsHolding)
                    grip.Apply(animator, frame);
            }

            foreach (HandGrip grip in _grips)
            {
                if (!grip.IsHolding)
                    grip.Apply(animator, frame);
            }
        }

        public ClipEdit Clone()
        {
            ClipEdit clone = new ClipEdit(_clip, _source);

            foreach (BoneTrack track in _tracks)
                clone._tracks.Add(track.Clone());

            foreach (HandGrip grip in _grips)
                clone._grips.Add(grip.Clone());

            return clone;
        }
    }

    [Serializable]
    public sealed class BoneTrack
    {
        public HumanBodyBones Bone => _bone;
        public IReadOnlyList<BoneKey> Keys => _keys;

        [SerializeField]
        private HumanBodyBones _bone;

        [SerializeField]
        private List<BoneKey> _keys = new();

        /// Quaternion x, y, z, w and position x, y, z.
        [NonSerialized]
        private AnimationCurve[] _curves;

        public BoneTrack(HumanBodyBones bone) => _bone = bone;

        public int IndexOf(int frame)
        {
            return _keys.FindIndex(key => key.Frame == frame);
        }

        public void SetKey(BoneKey key)
        {
            int index = IndexOf(key.Frame);

            if (index >= 0)
                _keys[index] = key;
            else
                _keys.Add(key);

            _keys.Sort((a, b) => a.Frame.CompareTo(b.Frame));
            _curves = null;
        }

        public bool RemoveKey(int frame)
        {
            int index = IndexOf(frame);

            if (index < 0)
                return false;

            _keys.RemoveAt(index);
            _curves = null;

            return true;
        }

        public bool MoveKey(int from, int to)
        {
            int index = IndexOf(from);

            if (index < 0 || IndexOf(to) >= 0)
                return false;

            BoneKey key = _keys[index];
            RemoveKey(from);
            SetKey(new BoneKey(to, key.Rotation, key.Position));

            return true;
        }

        /// Keys are joined by smooth curves; before the first and after the last key the offset holds.
        public BoneKey Evaluate(float frame)
        {
            if (_keys.Count == 0)
                return new BoneKey(Mathf.RoundToInt(frame), Quaternion.identity, Vector3.zero);

            _curves ??= CreateCurves();
            Quaternion rotation = new Quaternion(_curves[0].Evaluate(frame), _curves[1].Evaluate(frame), _curves[2].Evaluate(frame),
                _curves[3].Evaluate(frame));

            return new BoneKey(Mathf.RoundToInt(frame), rotation.normalized,
                new Vector3(_curves[4].Evaluate(frame), _curves[5].Evaluate(frame), _curves[6].Evaluate(frame)));
        }

        public void Apply(Transform bone, float frame)
        {
            if (_keys.Count == 0)
                return;

            BoneKey key = Evaluate(frame);
            bone.localRotation *= key.Rotation;
            bone.localPosition += key.Position;
        }

        public BoneTrack Clone()
        {
            BoneTrack clone = new BoneTrack(_bone);
            clone._keys.AddRange(_keys);

            return clone;
        }

        private AnimationCurve[] CreateCurves()
        {
            AnimationCurve[] curves = new AnimationCurve[7];

            for (int i = 0; i < curves.Length; i++)
                curves[i] = new AnimationCurve();

            Quaternion previous = Quaternion.identity;

            foreach (BoneKey key in _keys)
            {
                Quaternion q = key.Rotation;

                // Neighbouring keys must lie on the same hemisphere, otherwise the curves swing the long way round.
                if (Quaternion.Dot(previous, q) < 0f)
                    q = new Quaternion(-q.x, -q.y, -q.z, -q.w);

                previous = q;
                float[] values = { q.x, q.y, q.z, q.w, key.Position.x, key.Position.y, key.Position.z };

                for (int i = 0; i < curves.Length; i++)
                    curves[i].AddKey(key.Frame, values[i]);
            }

            foreach (AnimationCurve curve in curves)
            {
                for (int i = 0; i < curve.length; i++)
                    curve.SmoothTangents(i, 0f);
            }

            return curves;
        }
    }

    /// Hand on a weapon. A pin keeps the hand in one pose on the weapon held by the other hand. A hold lets the hand that carries
    /// the weapon slide over it: the weapon keeps its path, the hand is solved to the new grip and the socket takes the
    /// difference, keyed into the clip. Within the frame range the grip is full, around it it blends back to the clip.
    [Serializable]
    public sealed class HandGrip
    {
        public const float BlendFrames = 6f;

        public HumanBodyBones Hand => _hand;
        public HumanBodyBones Anchor => _anchor;
        public bool IsHolding => _isHolding;
        /// Pin: hand in the anchor socket's space. Hold: socket in the hand's space.
        public Vector3 Position => _position;
        public Quaternion Rotation => _rotation;
        public int Start => _start;
        public int End => _end;

        [SerializeField]
        private HumanBodyBones _hand;

        [SerializeField]
        private HumanBodyBones _anchor;

        [SerializeField]
        private bool _isHolding;

        [SerializeField]
        private Vector3 _position;

        [SerializeField]
        private Quaternion _rotation;

        [Tooltip("Socket pose the weapon path is measured from, as the character is built")]
        [SerializeField]
        private Vector3 _restPosition;

        [SerializeField]
        private Quaternion _restRotation;

        [SerializeField]
        private int _start;

        [SerializeField]
        private int _end;

        private HandGrip(HumanBodyBones hand, HumanBodyBones anchor, bool isHolding, int start, int end)
        {
            _hand = hand;
            _anchor = anchor;
            _isHolding = isHolding;
            SetRange(start, end);
        }

        public static HandGrip Pin(Animator animator, HumanBodyBones hand, int start, int end)
        {
            HandGrip grip = new HandGrip(hand, hand == HumanBodyBones.LeftHand ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand, false, start, end);
            grip.Capture(animator);

            return grip;
        }

        public static HandGrip Hold(HumanBodyBones hand, Pose rest, int start, int end)
        {
            HandGrip grip = new HandGrip(hand, hand, true, start, end) { _restPosition = rest.position, _restRotation = rest.rotation };
            grip.SetPose(rest.position, rest.rotation);

            return grip;
        }

        /// The weapon socket built under a hand by BattlePoseRig.CreateSockets.
        public static Transform GetSocket(Animator animator, HumanBodyBones hand)
        {
            return animator.GetBoneTransform(hand).Find(hand + "Socket");
        }

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            _position = position;
            _rotation = rotation;
        }

        /// Pin: takes the hand as it is posed now on the weapon. Hold: back to the grip the character is built with.
        public void Capture(Animator animator)
        {
            if (_isHolding)
            {
                SetPose(_restPosition, _restRotation);

                return;
            }

            Transform anchor = GetSocket(animator, _anchor);
            Transform hand = animator.GetBoneTransform(_hand);
            SetPose(anchor.InverseTransformPoint(hand.position), Quaternion.Inverse(anchor.rotation) * hand.rotation);
        }

        public void SetRange(int start, int end)
        {
            _start = start;
            _end = Mathf.Max(start, end);
        }

        public float GetWeight(float frame)
        {
            return Mathf.Clamp01(1f - Mathf.Max(_start - frame, frame - _end) / BlendFrames);
        }

        public void Apply(Animator animator, float frame)
        {
            float weight = GetWeight(frame);
            Transform hand = animator.GetBoneTransform(_hand);
            Transform socket = GetSocket(animator, _anchor);
            Vector3 target;
            Quaternion rotation;

            if (socket == null)
                return;

            if (_isHolding)
            {
                // The weapon where the clip carries it, the hand placed so the socket reaches it with the new grip.
                Vector3 position = Vector3.Lerp(_restPosition, _position, weight);
                Quaternion local = Quaternion.Slerp(_restRotation, _rotation, weight);
                Vector3 weapon = hand.TransformPoint(_restPosition);
                rotation = hand.rotation * _restRotation * Quaternion.Inverse(local);
                target = weapon - rotation * position;
                socket.SetLocalPositionAndRotation(position, local);
            }
            else
            {
                rotation = Quaternion.Slerp(hand.rotation, socket.rotation * _rotation, weight);
                target = Vector3.Lerp(hand.position, socket.TransformPoint(_position), weight);
            }

            if (weight <= 0f)
                return;

            bool isLeft = _hand == HumanBodyBones.LeftHand;
            TwoBoneIk.Solve(animator.GetBoneTransform(isLeft ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm),
                animator.GetBoneTransform(isLeft ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm), hand, target);
            hand.rotation = rotation;
        }

        public HandGrip Clone()
        {
            return (HandGrip)MemberwiseClone();
        }
    }

    public static class TwoBoneIk
    {
        /// Solves in the current bend plane of the limb: the knee or elbow opens to the target distance, then the upper bone aims.
        public static void Solve(Transform upper, Transform lower, Transform end, Vector3 target)
        {
            Vector3 a = upper.position;
            Vector3 b = lower.position;
            Vector3 c = end.position;
            float upperLength = Vector3.Distance(a, b);
            float lowerLength = Vector3.Distance(b, c);
            float distance = Mathf.Clamp(Vector3.Distance(a, target), Mathf.Abs(upperLength - lowerLength) + 0.001f, upperLength + lowerLength - 0.001f);
            Vector3 normal = Vector3.Cross(a - b, c - b);
            float bend = Mathf.Acos(Mathf.Clamp((upperLength * upperLength + lowerLength * lowerLength - distance * distance) /
                (2f * upperLength * lowerLength), -1f, 1f)) * Mathf.Rad2Deg;

            if (normal.sqrMagnitude < 1e-8f)
                normal = upper.rotation * Vector3.right;

            lower.rotation = Quaternion.AngleAxis(bend - Vector3.Angle(a - b, c - b), normal.normalized) * lower.rotation;
            upper.rotation = Quaternion.FromToRotation(end.position - a, target - a) * upper.rotation;
        }
    }

    [Serializable]
    public struct BoneKey
    {
        public int Frame => _frame;
        public Quaternion Rotation => _rotation;
        public Vector3 Position => _position;

        [SerializeField]
        private int _frame;

        [SerializeField]
        private Quaternion _rotation;

        [SerializeField]
        private Vector3 _position;

        public BoneKey(int frame, Quaternion rotation, Vector3 position)
        {
            _frame = frame;
            _rotation = rotation;
            _position = position;
        }
    }
}
