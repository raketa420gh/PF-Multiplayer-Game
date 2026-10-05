#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Writes clip edits into their clips: every 60 Hz frame of the untouched source is posed on a character, the edited bones
    /// are turned, and the human pose read back replaces the body muscles and the root of the clip. Finger curves stay as generated.
    /// The character must come with its hand sockets at rest.
    public static class AnimationEditBaker
    {
        public const string ConfigPath = "Assets/Game/Configs/Battle/AnimationEdits.asset";

        private const string SourceFolder = "Assets/Game/Animations/BattleSource";
        private const float FrameRate = 60f;
        private const int BodyMuscleCount = 55;

        private static readonly string[] s_rootCurves = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };

        private static readonly string[] s_socketCurves =
        {
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z",
            "m_LocalRotation.w"
        };

        /// Only generated clips can be edited; imported library takes live inside read-only FBX files.
        public static bool IsEditable(AnimationClip clip)
        {
            return clip != null && AssetDatabase.GetAssetPath(clip).EndsWith(".anim") && !AssetDatabase.GetAssetPath(clip).StartsWith(SourceFolder);
        }

        public static void Save(AnimationEditConfig config, ClipEdit edit, Animator animator)
        {
            // An undo step taken before the first save still lacks the source the config already knows.
            if (edit.Source == null)
                edit.SetSource(config.Find(edit.Clip)?.Source ?? CaptureSource(edit.Clip));

            edit.RemoveEmpty();
            config.Store(edit.Clone());
            Bake(edit, animator);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        public static void SaveConfig(AnimationEditConfig config)
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        /// After a rebuild every edited clip is fresh from the generator: it becomes the new source and gets its edit again.
        public static void BakeAll(AnimationEditConfig config, Animator animator)
        {
            foreach (ClipEdit edit in config.Clips)
            {
                if (edit.Clip == null)
                    continue;

                edit.SetSource(CaptureSource(edit.Clip));
                Bake(edit, animator);
            }

            EditorUtility.SetDirty(config);
        }

        private static AnimationClip CaptureSource(AnimationClip clip)
        {
            string path = $"{SourceFolder}/{clip.name}.anim";
            AnimationClip source = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (!AssetDatabase.IsValidFolder(SourceFolder))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(SourceFolder), System.IO.Path.GetFileName(SourceFolder));

            if (source == null)
                AssetDatabase.CreateAsset(source = new AnimationClip { name = clip.name }, path);

            Copy(clip, source);

            return source;
        }

        /// Curve by curve rather than CopySerialized, which leaves the clip without its binding constant.
        private static void Copy(AnimationClip from, AnimationClip to)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(from);
            AnimationCurve[] curves = System.Array.ConvertAll(bindings, binding => AnimationUtility.GetEditorCurve(from, binding));

            to.ClearCurves();
            to.frameRate = from.frameRate;
            AnimationUtility.SetEditorCurves(to, bindings, curves);
            AnimationUtility.SetAnimationClipSettings(to, AnimationUtility.GetAnimationClipSettings(from));
            EditorUtility.SetDirty(to);
        }

        private static void Bake(ClipEdit edit, Animator animator)
        {
            AnimationClip source = edit.Source;
            AnimationClip clip = edit.Clip;
            Copy(source, clip);

            float length = source.length;
            int frames = Mathf.Max(1, Mathf.RoundToInt(length * FrameRate));
            bool isLoop = AnimationUtility.GetAnimationClipSettings(source).loopTime;
            AnimationCurve[] muscles = new AnimationCurve[HumanTrait.MuscleCount];
            AnimationCurve[] root = new AnimationCurve[s_rootCurves.Length];
            AnimationCurve[] bakedMuscles = new AnimationCurve[BodyMuscleCount];
            AnimationCurve[] bakedRoot = new AnimationCurve[s_rootCurves.Length];

            for (int i = 0; i < muscles.Length; i++)
                muscles[i] = AnimationUtility.GetEditorCurve(source, Binding(HumanTrait.MuscleName[i]));

            for (int i = 0; i < root.Length; i++)
            {
                root[i] = AnimationUtility.GetEditorCurve(source, Binding(s_rootCurves[i]));
                bakedRoot[i] = new AnimationCurve();
            }

            for (int i = 0; i < bakedMuscles.Length; i++)
                bakedMuscles[i] = new AnimationCurve();

            // A first-person view hides the head by scaling it to zero, which would leave the neck without a pose to read.
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Vector3 headScale = head.localScale;
            head.localScale = Vector3.one;

            // Socket tracks and holding grips move the weapon socket under the hand; its local pose goes into the clip as a
            // transform curve. Both start from the socket at rest, so it is put back before every frame.
            List<Transform> moved = new List<Transform>();
            List<Pose> rests = new List<Pose>();

            foreach (HumanBodyBones hand in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                Transform socket = HandGrip.GetSocket(animator, hand);

                if (socket == null || (edit.Find(hand, true) == null && edit.FindGrip(hand) is not { IsHolding: true }))
                    continue;

                moved.Add(socket);
                rests.Add(new Pose(socket.localPosition, socket.localRotation));
            }

            AnimationCurve[,] sockets = new AnimationCurve[moved.Count, s_socketCurves.Length];

            for (int i = 0; i < moved.Count; i++)
            {
                for (int j = 0; j < s_socketCurves.Length; j++)
                    sockets[i, j] = new AnimationCurve();
            }

            using HumanPoseHandler handler = new HumanPoseHandler(animator.avatar, animator.transform);
            HumanPose pose = new HumanPose { muscles = new float[HumanTrait.MuscleCount] };
            float[] reference = new float[HumanTrait.MuscleCount];
            Quaternion previous = Quaternion.identity;
            Quaternion[] previousSockets = new Quaternion[moved.Count];

            for (int frame = 0; frame <= frames; frame++)
            {
                float time = Mathf.Min(frame / FrameRate, length);

                for (int i = 0; i < muscles.Length; i++)
                    pose.muscles[i] = muscles[i]?.Evaluate(time) ?? 0f;

                pose.bodyPosition = new Vector3(root[0].Evaluate(time), root[1].Evaluate(time), root[2].Evaluate(time));
                pose.bodyRotation = new Quaternion(root[3].Evaluate(time), root[4].Evaluate(time), root[5].Evaluate(time), root[6].Evaluate(time)).normalized;
                pose.muscles.CopyTo(reference, 0);
                handler.SetHumanPose(ref pose);

                for (int i = 0; i < moved.Count; i++)
                    moved[i].SetLocalPositionAndRotation(rests[i].position, rests[i].rotation);

                edit.Apply(animator, isLoop && frame == frames ? 0f : frame);
                handler.GetHumanPose(ref pose);
                JointLimits.Clamp(pose.muscles, reference);

                Quaternion rotation = pose.bodyRotation;

                if (Quaternion.Dot(previous, rotation) < 0f)
                    rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);

                previous = rotation;

                for (int i = 0; i < bakedMuscles.Length; i++)
                    bakedMuscles[i].AddKey(time, pose.muscles[i]);

                float[] values = { pose.bodyPosition.x, pose.bodyPosition.y, pose.bodyPosition.z, rotation.x, rotation.y, rotation.z, rotation.w };

                for (int i = 0; i < bakedRoot.Length; i++)
                    bakedRoot[i].AddKey(time, values[i]);

                for (int i = 0; i < moved.Count; i++)
                {
                    Vector3 p = moved[i].localPosition;
                    Quaternion q = moved[i].localRotation;

                    // Same hemisphere as the frame before: a flipped sign is the same pose on a key, but the curves between
                    // the keys would spin the weapon the long way round once the clip plays at the game's frame rate.
                    if (frame > 0 && Quaternion.Dot(previousSockets[i], q) < 0f)
                        q = new Quaternion(-q.x, -q.y, -q.z, -q.w);

                    previousSockets[i] = q;
                    float[] local = { p.x, p.y, p.z, q.x, q.y, q.z, q.w };

                    for (int j = 0; j < local.Length; j++)
                        sockets[i, j].AddKey(time, local[j]);
                }
            }

            head.localScale = headScale;

            for (int i = 0; i < bakedMuscles.Length; i++)
                SetCurve(clip, HumanTrait.MuscleName[i], bakedMuscles[i]);

            for (int i = 0; i < bakedRoot.Length; i++)
                SetCurve(clip, s_rootCurves[i], bakedRoot[i]);

            for (int i = 0; i < moved.Count; i++)
            {
                moved[i].SetLocalPositionAndRotation(rests[i].position, rests[i].rotation);
                string path = AnimationUtility.CalculateTransformPath(moved[i], animator.transform);

                for (int j = 0; j < s_socketCurves.Length; j++)
                    SetCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), s_socketCurves[j]), sockets[i, j]);
            }

            EditorUtility.SetDirty(clip);
        }

        private static EditorCurveBinding Binding(string property)
        {
            return EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), property);
        }

        private static void SetCurve(AnimationClip clip, string property, AnimationCurve curve)
        {
            SetCurve(clip, Binding(property), curve);
        }

        private static void SetCurve(AnimationClip clip, EditorCurveBinding binding, AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0f);

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }
}
#endif
