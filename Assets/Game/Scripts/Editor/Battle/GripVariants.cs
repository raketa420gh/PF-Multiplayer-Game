using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Battle
{
    /// Temporary: ways to fix the grip of a clip posed in the editor with a free hand, rendered side by side to choose from.
    /// free — as posed; keys — the grip of every key, blended between them; fixed — one grip (the calm keys), the hand on the
    /// weapon's path; point — one point in the palm, the turn on the hilt from the keys; follow — one grip, the weapon follows the hand.
    internal static class GripVariants
    {
        private const int Layer = 31;
        private const float FrameRate = 60f;
        private const float TipDistance = 0.8f;
        private const int BodyMuscleCount = 55;

        private static readonly int[] s_calm = { 4, 17, 42, 50, 59 };
        private static readonly string[] s_views = { "fp", "side", "hand" };
        private static readonly string[] s_rootCurves = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };

        private static readonly string[] s_socketCurves =
        {
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z",
            "m_LocalRotation.w"
        };

        public static string Run(string clipName, string weaponName, string variant, string folder, bool isShooting)
        {
            AnimationEditConfig config = AssetDatabase.LoadAssetAtPath<AnimationEditConfig>(AnimationEditBaker.ConfigPath);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{BattleEditorUtility.AnimationsFolder}/{clipName}.anim");
            ClipEdit edit = config.Find(clip).Clone();
            WeaponConfig weapon = AssetDatabase.LoadAssetAtPath<WeaponConfig>($"{BattleEditorUtility.ConfigsFolder}/{weaponName}.asset");
            int frames = Mathf.RoundToInt(edit.Source.length * FrameRate);
            int[] keys = edit.Find(HumanBodyBones.RightHand).Keys.Select(key => key.Frame).ToArray();
            Directory.CreateDirectory($"{folder}/{variant}");

            using Rig rig = new Rig(edit, weapon);
            BoneTrack grips = new BoneTrack(HumanBodyBones.RightHand);

            foreach (int key in keys)
            {
                rig.PoseFree(key);
                Pose grip = rig.Grip;
                grips.SetKey(new BoneKey(key, grip.rotation, grip.position));
            }

            Pose calm = Average(s_calm.Select(frame => { BoneKey key = grips.Evaluate(frame); return new Pose(key.Position, key.Rotation); }).ToArray());
            StringBuilder data = new StringBuilder($"window.GRIP = window.GRIP || {{}};\nwindow.GRIP['{variant}'] = [\n");
            GameObject camera = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
            List<GameObject> lights = new List<GameObject> { SwingPreview.CreateLight(1.3f, 160f), SwingPreview.CreateLight(0.7f, -40f) };

            try
            {
                Camera view = camera.AddComponent<Camera>();
                view.clearFlags = CameraClearFlags.SolidColor;
                view.backgroundColor = new Color(0.32f, 0.36f, 0.42f);
                view.cullingMask = 1 << Layer;

                foreach (GameObject light in lights)
                    light.hideFlags = HideFlags.HideAndDontSave;

                for (int frame = 0; frame <= frames; frame++)
                {
                    rig.PoseFree(frame);
                    Pose path = rig.Weapon;
                    BoneKey blended = grips.Evaluate(frame);
                    Pose target = variant switch
                    {
                        "keys" => new Pose(blended.Position, blended.Rotation),
                        "point" => new Pose(calm.position, blended.Rotation),
                        _ => calm
                    };

                    if (variant is "keys" or "fixed" or "point")
                        rig.Reach(target);
                    else if (variant == "follow")
                        rig.Follow(calm);

                    Pose grip = rig.Grip;
                    float[] muscles = rig.Muscles;
                    data.AppendLine(FormattableString.Invariant(
                        $"  {{f:{frame},key:{(keys.Contains(frame) ? 1 : 0)},miss:{(variant == "free" ? 0f : Vector3.Distance(grip.position, target.position) * 100f):0.0},turn:{(variant == "free" ? 0f : Quaternion.Angle(grip.rotation, target.rotation)):0},hiltTurn:{Quaternion.Angle(grip.rotation, calm.rotation):0},hiltSlide:{Vector3.Distance(grip.position, calm.position) * 100f:0.0},path:{Vector3.Distance(rig.Socket.TransformPoint(0f, 0f, TipDistance), path.position + path.rotation * new Vector3(0f, 0f, TipDistance)) * 100f:0.0},du:{muscles[Muscle("Right Hand Down-Up")]:0.00},io:{muscles[Muscle("Right Hand In-Out")]:0.00},ftw:{muscles[Muscle("Right Forearm Twist In-Out")]:0.00},atw:{muscles[Muscle("Right Arm Twist In-Out")]:0.00}}},"));

                    if (isShooting)
                        Shoot(rig, view, $"{folder}/{variant}", frame);
                }
            }
            finally
            {
                Object.DestroyImmediate(camera);

                foreach (GameObject light in lights)
                    Object.DestroyImmediate(light);
            }

            data.AppendLine("];");
            File.WriteAllText($"{folder}/data_{variant}.js", data.ToString());

            return $"{variant}: {frames + 1} frames, keys {string.Join(",", keys)}, calm grip {calm.position.ToString("F3")} {calm.rotation.eulerAngles.ToString("F0")}";
        }

        private static int Muscle(string name)
        {
            return Array.IndexOf(HumanTrait.MuscleName, name);
        }

        private static Pose Average(Pose[] poses)
        {
            Vector3 position = Vector3.zero;
            Vector4 sum = Vector4.zero;

            foreach (Pose pose in poses)
            {
                Quaternion q = pose.rotation;
                float sign = Vector4.Dot(sum, new Vector4(q.x, q.y, q.z, q.w)) < 0f ? -1f : 1f;
                sum += new Vector4(q.x, q.y, q.z, q.w) * sign;
                position += pose.position / poses.Length;
            }

            sum.Normalize();

            return new Pose(position, new Quaternion(sum.x, sum.y, sum.z, sum.w));
        }

        private static void Shoot(Rig rig, Camera camera, string folder, int frame)
        {
            Transform head = rig.Animator.GetBoneTransform(HumanBodyBones.Head);

            foreach (string view in s_views)
            {
                head.localScale = view == "fp" ? Vector3.zero : Vector3.one;
                Vector3 fist = rig.Socket.position;
                camera.nearClipPlane = view == "fp" ? 0.1f : 0.04f;

                switch (view)
                {
                    case "fp":
                        camera.fieldOfView = 75f;
                        camera.transform.SetPositionAndRotation(BattleAnimationLibrary.Eye, Quaternion.identity);
                        break;
                    case "side":
                        camera.fieldOfView = 35f;
                        camera.transform.position = new Vector3(4.4f, 2.1f, 1.3f);
                        camera.transform.LookAt(new Vector3(0f, 1.15f, 0.2f));
                        break;
                    default:
                        camera.fieldOfView = 30f;
                        camera.transform.position = fist + new Vector3(0.3f, 0.12f, 0.38f);
                        camera.transform.LookAt(fist);
                        break;
                }

                List<GameObject> baked = Bake(rig.Model);
                SwingPreview.Render(camera, view == "fp" ? 640 : 400, view == "fp" ? 360 : 400, $"{folder}/{view}_{frame}.png");

                foreach (GameObject item in baked)
                {
                    Object.DestroyImmediate(item.GetComponent<MeshFilter>().sharedMesh);
                    Object.DestroyImmediate(item);
                }
            }

            head.localScale = Vector3.one;
        }

        private static List<GameObject> Bake(GameObject model)
        {
            List<GameObject> baked = new List<GameObject>();

            foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Mesh mesh = new Mesh();
                skin.BakeMesh(mesh);
                GameObject item = new GameObject("Baked") { hideFlags = HideFlags.HideAndDontSave, layer = Layer };
                item.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                item.AddComponent<MeshFilter>().sharedMesh = mesh;
                item.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                baked.Add(item);
            }

            return baked;
        }

        /// The character posed as baking poses it: the source clip, the edit's keys, the joint limits, the weapon socket.
        private sealed class Rig : IDisposable
        {
            public GameObject Model => _model;
            public Animator Animator => _animator;
            public Transform Socket => _socket;
            /// The weapon where the clip carries it on the frame posed last.
            public Pose Weapon => _weapon;
            /// The weapon socket in the hand's space.
            public Pose Grip => new(_hand.InverseTransformPoint(_socket.position), Quaternion.Inverse(_hand.rotation) * _socket.rotation);

            public float[] Muscles
            {
                get
                {
                    _handler.GetHumanPose(ref _pose);

                    return _pose.muscles;
                }
            }

            private readonly GameObject _model;
            private readonly Animator _animator;
            private readonly HumanPoseHandler _handler;
            private readonly ClipEdit _edit;
            private readonly Transform _hand;
            private readonly Transform _upper;
            private readonly Transform _lower;
            private readonly Transform _socket;
            private readonly AnimationCurve[] _muscles = new AnimationCurve[HumanTrait.MuscleCount];
            private readonly AnimationCurve[] _root = new AnimationCurve[s_rootCurves.Length];
            private readonly AnimationCurve[] _socketCurves = new AnimationCurve[s_socketCurves.Length];
            private readonly float[] _reference = new float[HumanTrait.MuscleCount];
            private HumanPose _pose = new() { muscles = new float[HumanTrait.MuscleCount] };
            private Pose _weapon;

            public Rig(ClipEdit edit, WeaponConfig weapon)
            {
                _edit = edit;
                _model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), Vector3.zero, Quaternion.identity);
                _model.hideFlags = HideFlags.HideAndDontSave;
                _animator = _model.GetComponentInChildren<Animator>();
                BattlePoseRig.CreateSockets(_animator);
                _handler = new HumanPoseHandler(_animator.avatar, _animator.transform);
                _hand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                _upper = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                _lower = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                _socket = HandGrip.GetSocket(_animator, HumanBodyBones.RightHand);
                AnimationClip source = edit.Source;
                string path = AnimationUtility.CalculateTransformPath(_socket, _animator.transform);

                for (int i = 0; i < _muscles.Length; i++)
                {
                    string name = i < BodyMuscleCount ? HumanTrait.MuscleName[i] : Regex.Replace(HumanTrait.MuscleName[i], @"^(Left|Right) (\w+) ", "$1Hand.$2.");
                    _muscles[i] = AnimationUtility.GetEditorCurve(source, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), name));
                }

                for (int i = 0; i < _root.Length; i++)
                    _root[i] = AnimationUtility.GetEditorCurve(source, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), s_rootCurves[i]));

                for (int i = 0; i < _socketCurves.Length; i++)
                    _socketCurves[i] = AnimationUtility.GetEditorCurve(source, EditorCurveBinding.FloatCurve(path, typeof(Transform), s_socketCurves[i]));

                foreach (WeaponAttachment attachment in weapon.Attachments)
                {
                    if (attachment.Socket != WeaponSocket.RightHand)
                        continue;

                    foreach (Transform part in Object.Instantiate(attachment.Prefab, _socket, false).GetComponentsInChildren<Transform>(true))
                        part.gameObject.layer = Layer;
                }
            }

            public void Dispose()
            {
                _handler.Dispose();
                Object.DestroyImmediate(_model);
            }

            /// The clip with the edit's keys, the hand where they put it, the weapon on the clip's path.
            public void PoseFree(int frame)
            {
                float time = frame / FrameRate;

                for (int i = 0; i < _muscles.Length; i++)
                    _pose.muscles[i] = _muscles[i]?.Evaluate(time) ?? 0f;

                _pose.bodyPosition = new Vector3(_root[0].Evaluate(time), _root[1].Evaluate(time), _root[2].Evaluate(time));
                _pose.bodyRotation = new Quaternion(_root[3].Evaluate(time), _root[4].Evaluate(time), _root[5].Evaluate(time), _root[6].Evaluate(time)).normalized;
                Array.Copy(_pose.muscles, _reference, _reference.Length);
                _handler.SetHumanPose(ref _pose);
                _socket.SetLocalPositionAndRotation(
                    new Vector3(_socketCurves[0].Evaluate(time), _socketCurves[1].Evaluate(time), _socketCurves[2].Evaluate(time)),
                    new Quaternion(_socketCurves[3].Evaluate(time), _socketCurves[4].Evaluate(time), _socketCurves[5].Evaluate(time), _socketCurves[6].Evaluate(time)).normalized);
                _edit.Apply(_animator, frame);
                _weapon = new Pose(_socket.position, _socket.rotation);
                Clamp();
                _socket.SetPositionAndRotation(_weapon.position, _weapon.rotation);
            }

            /// The hand onto the weapon in the grip, the elbow in the bend plane the keys give; the weapon keeps its path.
            public void Reach(Pose grip)
            {
                Quaternion rotation = _weapon.rotation * Quaternion.Inverse(grip.rotation);
                TwoBoneIk.Solve(_upper, _lower, _hand, _weapon.position - rotation * grip.position);
                _hand.rotation = rotation;
                Clamp();
                _socket.SetPositionAndRotation(_weapon.position, _weapon.rotation);
            }

            /// The weapon in the grip wherever the hand is.
            public void Follow(Pose grip)
            {
                _socket.SetPositionAndRotation(_hand.TransformPoint(grip.position), _hand.rotation * grip.rotation);
            }

            private void Clamp()
            {
                _handler.GetHumanPose(ref _pose);

                if (Game.Scripts.Battle.JointLimits.Clamp(_pose.muscles, _reference))
                    _handler.SetHumanPose(ref _pose);
            }
        }
    }
}
