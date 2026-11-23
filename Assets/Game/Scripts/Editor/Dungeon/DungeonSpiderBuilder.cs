using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// The Tripo bone spider (one static mesh: a skull body, a spine tail, six bone legs) rigged here: a body, a tail and three bones per leg,
    /// each vertex skinned to the leg segment it lies along. Its clips are solved in code: the feet are IK targets, so a walking foot stays planted
    /// while the body passes over it (alternating tripod gait), and the bite moves the body over planted feet.
    internal static class DungeonSpiderBuilder
    {
        /// Normalized time of the bite at full reach in the attack clip.
        public const float AttackImpact = 0.45f;
        /// Ground speed the walk clip is authored for, m/s.
        public const float WalkSpeed = 1.2f;
        /// Metres of travel between two tripod touchdowns.
        public const float StepStride = Stride * 0.5f;

        public const string ModelPath = ModelFolder + "/BoneSpider.fbx";
        private const string ModelFolder = "Assets/Game/Models/Monsters/BoneSpider";
        private const string MaterialPath = ModelFolder + "/BoneSpider.mat";
        private const string MeshPath = "Assets/Game/Meshes/Dungeon/BoneSpider.asset";
        private const string ControllerFolder = "Assets/Game/Animations/Dungeon";
        private const string ClipsFolder = ControllerFolder + "/BoneSpider";
        private const string ControllerPath = ControllerFolder + "/BoneSpider.controller";
        private const string MaskPath = ControllerFolder + "/BoneSpiderBody.mask";
        private const float FrameRate = 60f;
        private const float Stride = 0.3f;
        private const float StepHeight = 0.07f;
        private const float Blend = 0.03f;

        private static readonly Vector3 s_body = new(0f, 0.33f, 0.05f);
        private static readonly Vector3 s_tail = new(0f, 0.45f, -0.1f);

        /// Right legs front to back: hip, knee, ankle, claw tip, measured on the mesh (face +Z, feet on y = 0). Left legs mirror them.
        private static readonly Vector3[][] s_right =
        {
            new[] { new Vector3(0.13f, 0.33f, 0.22f), new Vector3(0.23f, 0.37f, 0.29f), new Vector3(0.32f, 0.22f, 0.39f), new Vector3(0.29f, 0.02f, 0.47f) },
            new[] { new Vector3(0.16f, 0.33f, 0.06f), new Vector3(0.32f, 0.43f, 0.01f), new Vector3(0.46f, 0.24f, -0.01f), new Vector3(0.47f, 0.02f, 0f) },
            new[] { new Vector3(0.13f, 0.35f, -0.07f), new Vector3(0.24f, 0.46f, -0.19f), new Vector3(0.31f, 0.27f, -0.39f), new Vector3(0.23f, 0.02f, -0.46f) }
        };

        private static readonly string[] s_segments = { "Femur", "Tibia", "Tarsus" };
        /// Degrees each segment folds at full Curl: the femur lifts, tibia and tarsus tuck under.
        private static readonly float[] s_curl = { -20f, 65f, 55f };

        public static string[] ClipNames => new[] { "Idle", "Walk", "Attack", "Death", "Hit" };

        private static Leg[] Legs => Enumerable.Range(0, 6).Select(i => new Leg(i)).ToArray();

        /// The rigged spider under parent (the fighter's model pivot), scaled; returns its animator.
        public static Animator Attach(Transform parent, int layer, float scale)
        {
            GameObject model = CreateModel();
            model.transform.SetParent(parent, false);
            model.transform.localScale = Vector3.one * scale;
            BattleEditorUtility.SetLayerRecursively(model, layer);
            Animator animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = BuildController(scale);

            return animator;
        }

        /// The skinned model alone, for previews: Animator without a controller, bones at rest.
        public static GameObject CreateModel()
        {
            GameObject model = new GameObject("BoneSpider");
            Animator animator = model.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            Transform root = BattleEditorUtility.CreateChild("Root", model.transform).transform;
            Transform body = BattleEditorUtility.CreateChild("Body", root, s_body).transform;
            List<Transform> bones = new List<Transform> { body, BattleEditorUtility.CreateChild("Tail", body, s_tail - s_body).transform };

            foreach (Leg leg in Legs)
            {
                Transform parent = body;

                for (int s = 0; s < 3; s++)
                {
                    parent = BattleEditorUtility.CreateChild($"{leg.Name}_{s_segments[s]}", parent, leg.Joints[s] - (s == 0 ? s_body : leg.Joints[s - 1])).transform;
                    bones.Add(parent);
                }
            }

            SkinnedMeshRenderer renderer = BattleEditorUtility.CreateChild("Mesh", model.transform).AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = BuildMesh(bones.Select(bone => bone.position).ToArray());
            renderer.bones = bones.ToArray();
            renderer.rootBone = body;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.updateWhenOffscreen = false;
            renderer.localBounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(1.3f, 1f, 1.3f));

            return model;
        }

        /// The imported mesh with bone weights: a vertex within a leg segment's capsule follows that segment, blended over the joints.
        private static Mesh BuildMesh(Vector3[] bonePositions)
        {
            Mesh source = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Mesh>().First();
            Mesh mesh = Object.Instantiate(source);
            mesh.name = "BoneSpider";
            Vector3[] vertices = mesh.vertices;
            BoneWeight[] weights = new BoneWeight[vertices.Length];
            Leg[] legs = Legs;

            for (int v = 0; v < vertices.Length; v++)
                weights[v] = Weigh(vertices[v], legs);

            mesh.boneWeights = weights;
            mesh.bindposes = bonePositions.Select(position => Matrix4x4.Translate(-position)).ToArray();
            BattleEditorUtility.EnsureFolder(System.IO.Path.GetDirectoryName(MeshPath).Replace('\\', '/'));
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, MeshPath);

                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);

            return existing;
        }

        /// Bone indices: 0 body, 1 tail, 2 + leg * 3 + segment.
        private static BoneWeight Weigh(Vector3 vertex, Leg[] legs)
        {
            float best = float.MaxValue;
            int bestLeg = -1;
            int bestSegment = 0;
            float along = 0f;

            for (int l = 0; l < legs.Length; l++)
            {
                for (int s = 0; s < 3; s++)
                {
                    Vector3 a = legs[l].Joints[s];
                    Vector3 ab = legs[l].Joints[s + 1] - a;
                    float t = Mathf.Clamp01(Vector3.Dot(vertex - a, ab) / ab.sqrMagnitude);
                    float distance = Vector3.Distance(vertex, a + ab * t);
                    bool isInside = distance < (s == 0 ? 0.055f : 0.06f) && (s > 0 || Vector3.Dot(vertex - a, ab) > -0.005f * ab.magnitude);

                    if (!isInside || distance >= best)
                        continue;

                    best = distance;
                    bestLeg = l;
                    bestSegment = s;
                    along = t * ab.magnitude;
                }
            }

            if (bestLeg < 0)
            {
                // The spine: the tail bone takes the vertices behind the skull, blended across its root.
                float tail = vertex.y > 0.3f ? Mathf.InverseLerp(-0.06f, -0.14f, vertex.z) : 0f;

                return Pair(0, 1, 1f - tail);
            }

            int self = 2 + bestLeg * 3 + bestSegment;
            int parent = bestSegment == 0 ? 0 : self - 1;
            float length = Vector3.Distance(legs[bestLeg].Joints[bestSegment], legs[bestLeg].Joints[bestSegment + 1]);

            if (along < Blend)
                return Pair(self, parent, 0.5f + 0.5f * along / Blend);

            if (bestSegment < 2 && length - along < Blend)
                return Pair(self, self + 1, 0.5f + 0.5f * (length - along) / Blend);

            return Pair(self, self, 1f);
        }

        private static BoneWeight Pair(int a, int b, float weightA)
        {
            return a == b || weightA >= 0.999f
                ? new BoneWeight { boneIndex0 = a, weight0 = 1f }
                : new BoneWeight { boneIndex0 = a, weight0 = weightA, boneIndex1 = b, weight1 = 1f - weightA };
        }

        private static AnimatorController BuildController(float scale)
        {
            Dictionary<string, AnimationClip> clips = BuildClips();
            BattleEditorUtility.EnsureFolder(ControllerFolder);
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("AttackTime", AnimatorControllerParameterType.Float);

            // A faster spider runs the same walk faster, so its feet stay planted.
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips["Idle"], 0f);
            tree.AddChild(clips["Walk"], WalkSpeed * scale);
            tree.AddChild(clips["Walk"], WalkSpeed * scale * 3f);
            ChildMotion[] children = tree.children;
            children[2].timeScale = 3f;
            tree.children = children;

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            machine.defaultState = locomotion;
            machine.AddState("Fall").motion = clips["Idle"];
            AnimatorState attack = machine.AddState("Attack");
            attack.motion = clips["Attack"];
            attack.timeParameterActive = true;
            attack.timeParameter = "AttackTime";
            machine.AddState("Death").motion = clips["Death"];

            controller.AddLayer("Hit");
            AnimatorControllerLayer[] layers = controller.layers;
            layers[1].defaultWeight = 1f;
            layers[1].avatarMask = BuildBodyMask();
            controller.layers = layers;
            AnimatorStateMachine hitMachine = layers[1].stateMachine;
            AnimatorState empty = hitMachine.AddState("Empty");
            AnimatorState hit = hitMachine.AddState("Hit");
            hit.motion = clips["Hit"];
            hit.writeDefaultValues = false;
            empty.writeDefaultValues = false;
            hitMachine.defaultState = empty;
            AnimatorStateTransition back = hit.AddTransition(empty);
            back.hasExitTime = true;
            back.exitTime = 0.8f;
            back.duration = 0.1f;
            EditorUtility.SetDirty(controller);

            return controller;
        }

        /// The flinch moves only the body and the tail: the legs keep walking.
        private static AvatarMask BuildBodyMask()
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);

            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, MaskPath);
            }

            mask.transformCount = 2;
            mask.SetTransformPath(0, "Root/Body");
            mask.SetTransformActive(0, true);
            mask.SetTransformPath(1, "Root/Body/Tail");
            mask.SetTransformActive(1, true);
            EditorUtility.SetDirty(mask);

            return mask;
        }

        public static Dictionary<string, AnimationClip> BuildClips()
        {
            BattleEditorUtility.EnsureFolder(ClipsFolder);

            return new Dictionary<string, AnimationClip>
            {
                ["Idle"] = Bake("Idle", 2f, true, Idle),
                ["Walk"] = Bake("Walk", Stride * 2f / WalkSpeed, true, Walk),
                ["Attack"] = Bake("Attack", 1.2f, false, Attack),
                ["Death"] = Bake("Death", 1f, false, Death),
                ["Hit"] = Bake("Hit", 0.3f, false, Hit, true)
            };
        }

        /// One pose of the spider at normalized time t: the body's offset and rotation from rest, the tail's rotation, and each claw tip's target.
        private delegate void PoseAt(float t, Pose pose);

        private sealed class Pose
        {
            public Vector3 Body;
            public Quaternion BodyRotation = Quaternion.identity;
            public Quaternion Tail = Quaternion.identity;
            /// 0..1: every joint folds toward the body on top of the solve, as a dead spider's legs do.
            public float Curl;
            public readonly Vector3[] Tips = Legs.Select(leg => leg.Joints[3]).ToArray();
        }

        private static AnimationClip Bake(string name, float length, bool isLoop, PoseAt poseAt, bool isBodyOnly = false)
        {
            string path = $"{ClipsFolder}/{name}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            clip.frameRate = FrameRate;
            int frames = Mathf.RoundToInt(length * FrameRate);
            Dictionary<string, AnimationCurve[]> rotations = new Dictionary<string, AnimationCurve[]>();
            AnimationCurve[] bodyPosition = Curves(3);
            Leg[] legs = Legs;

            for (int f = 0; f <= frames; f++)
            {
                float time = f / FrameRate;
                Pose pose = new Pose();
                poseAt(isLoop ? (f % frames) / (float)frames : f / (float)frames, pose);
                Key(bodyPosition, time, s_body + pose.Body);
                Key(rotations, "Root/Body", time, pose.BodyRotation);
                Key(rotations, "Root/Body/Tail", time, pose.Tail);

                if (isBodyOnly)
                    continue;

                foreach (Leg leg in legs)
                {
                    // The target in the body's rest frame: the solve below never sees the body move.
                    Vector3 tip = Quaternion.Inverse(pose.BodyRotation) * (pose.Tips[leg.Index] - s_body - pose.Body) + s_body;
                    Quaternion[] local = leg.Solve(tip);

                    for (int s = 0; s < 3 && pose.Curl > 0f; s++)
                        local[s] *= Quaternion.AngleAxis(s_curl[s] * pose.Curl, leg.BendAxis);

                    for (int s = 0; s < 3; s++)
                        Key(rotations, leg.Path(s), time, local[s]);
                }
            }

            for (int a = 0; a < 3; a++)
                clip.SetCurve("Root/Body", typeof(Transform), "m_LocalPosition." + "xyz"[a], bodyPosition[a]);

            foreach (KeyValuePair<string, AnimationCurve[]> pair in rotations)
            {
                for (int a = 0; a < 4; a++)
                    clip.SetCurve(pair.Key, typeof(Transform), "m_LocalRotation." + "xyzw"[a], pair.Value[a]);
            }

            clip.EnsureQuaternionContinuity();
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = isLoop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);

            return clip;
        }

        private static AnimationCurve[] Curves(int count)
        {
            return Enumerable.Range(0, count).Select(_ => new AnimationCurve()).ToArray();
        }

        private static void Key(AnimationCurve[] curves, float time, Vector3 value)
        {
            for (int a = 0; a < 3; a++)
                curves[a].AddKey(new Keyframe(time, value[a]));
        }

        private static void Key(Dictionary<string, AnimationCurve[]> rotations, string path, float time, Quaternion value)
        {
            if (!rotations.TryGetValue(path, out AnimationCurve[] curves))
                rotations[path] = curves = Curves(4);

            Quaternion previous = curves[0].length > 0 ? new Quaternion(curves[0].keys[^1].value, curves[1].keys[^1].value, curves[2].keys[^1].value, curves[3].keys[^1].value) : value;

            if (Quaternion.Dot(previous, value) < 0f)
                value = new Quaternion(-value.x, -value.y, -value.z, -value.w);

            for (int a = 0; a < 4; a++)
                curves[a].AddKey(new Keyframe(time, value[a]));
        }

        /// Breathing body, a slow tail, a front claw tapping the floor now and then.
        private static void Idle(float t, Pose pose)
        {
            float wave = Mathf.Sin(t * Mathf.PI * 2f);
            pose.Body = new Vector3(0f, 0.006f * wave, 0f);
            pose.BodyRotation = Quaternion.Euler(1.5f * wave, 0f, 0f);
            pose.Tail = Quaternion.Euler(3f * Mathf.Sin(t * Mathf.PI * 2f + 1f), 5f * Mathf.Sin(t * Mathf.PI * 4f), 0f);
            Tap(pose, 0, Mathf.InverseLerp(0.25f, 0.45f, t));
            Tap(pose, 3, Mathf.InverseLerp(0.7f, 0.88f, t));
        }

        private static void Tap(Pose pose, int leg, float t)
        {
            if (t <= 0f || t >= 1f)
                return;

            pose.Tips[leg] += new Vector3(0f, 0.045f * Mathf.Sin(t * Mathf.PI), 0.02f * Mathf.Sin(t * Mathf.PI));
        }

        /// Alternating tripod: front and back legs of one side with the middle leg of the other plant while the other three swing.
        private static void Walk(float t, Pose pose)
        {
            pose.Body = new Vector3(0f, -0.012f * Mathf.Cos(t * Mathf.PI * 4f), 0f);
            pose.BodyRotation = Quaternion.Euler(0f, 2f * Mathf.Sin(t * Mathf.PI * 2f), 2.5f * Mathf.Sin(t * Mathf.PI * 2f));
            pose.Tail = Quaternion.Euler(4f * Mathf.Sin(t * Mathf.PI * 4f), -8f * Mathf.Sin(t * Mathf.PI * 2f + 0.6f), 0f);

            foreach (Leg leg in Legs)
            {
                float phase = Mathf.Repeat(t + (leg.IsTripodA ? 0f : 0.5f), 1f);
                Vector3 offset;

                if (phase < 0.5f)
                {
                    offset = new Vector3(0f, 0f, Stride * (0.5f - phase * 2f));
                }
                else
                {
                    float swing = (phase - 0.5f) * 2f;
                    float smooth = swing * swing * (3f - 2f * swing);
                    offset = new Vector3(0f, StepHeight * Mathf.Sin(swing * Mathf.PI), Stride * (smooth - 0.5f));
                }

                // Front and back legs are near full stretch at rest: their strokes sit a little closer to the hips.
                pose.Tips[leg.Index] += offset + new Vector3(0f, 0f, (leg.Index % 3 - 1) * 0.06f);
            }
        }

        /// Windup: the body draws back and up and the front claws rise; the bite: the skull darts forward and down and snaps twice; back to rest.
        private static void Attack(float t, Pose pose)
        {
            Vector3 back = new Vector3(0f, 0.05f, -0.07f);
            Vector3 lunge = new Vector3(0f, -0.06f, 0.16f);
            float rear = Ease(Mathf.InverseLerp(0f, 0.38f, t));
            float strike = Ease(Mathf.InverseLerp(0.38f, 0.46f, t));
            float settle = Ease(Mathf.InverseLerp(0.62f, 1f, t));
            float chomp = Mathf.InverseLerp(0.46f, 0.6f, t);
            Vector3 body = Vector3.Lerp(Vector3.Lerp(Vector3.zero, back, rear), lunge, strike);
            pose.Body = Vector3.Lerp(body, Vector3.zero, settle);
            float pitch = Mathf.Lerp(Mathf.Lerp(0f, -14f, rear), 18f, strike) - (chomp > 0f && chomp < 1f ? 9f * Mathf.Sin(chomp * Mathf.PI * 2f) : 0f);
            pose.BodyRotation = Quaternion.Euler(Mathf.Lerp(pitch, 0f, settle), 0f, 0f);
            pose.Tail = Quaternion.Euler(Mathf.Lerp(Mathf.Lerp(0f, -10f, rear), 8f, strike) * (1f - settle), 0f, 0f);

            foreach (int front in new[] { 0, 3 })
            {
                Vector3 raised = new Vector3(0f, 0.1f, 0.03f);
                Vector3 slammed = new Vector3(0f, 0f, 0.1f);
                Vector3 claw = Vector3.Lerp(Vector3.Lerp(Vector3.zero, raised, rear), slammed, strike);
                pose.Tips[front] += Vector3.Lerp(claw, Vector3.zero, settle);
            }
        }

        /// The legs curl in under the sinking body, the way a dead spider folds up.
        private static void Death(float t, Pose pose)
        {
            float fall = Ease(Mathf.InverseLerp(0f, 0.45f, t));
            float curl = Ease(Mathf.InverseLerp(0.1f, 0.8f, t));
            float twitch = t > 0.5f && t < 0.9f ? Mathf.Sin(Mathf.InverseLerp(0.5f, 0.9f, t) * Mathf.PI * 4f) * (0.9f - t) : 0f;
            pose.Body = new Vector3(0f, -0.17f * fall, 0f);
            pose.BodyRotation = Quaternion.Euler(6f * fall, 0f, 9f * fall);
            pose.Tail = Quaternion.Euler(20f * fall, 0f, 0f);

            pose.Curl = curl + 0.15f * twitch;
        }

        private static void Hit(float t, Pose pose)
        {
            float jolt = Mathf.Sin(Mathf.Min(1f, t * 2.2f) * Mathf.PI) * (1f - t * 0.5f);
            pose.Body = new Vector3(0f, 0.02f, -0.04f) * jolt;
            pose.BodyRotation = Quaternion.Euler(-8f * jolt, 0f, 3f * jolt);
            pose.Tail = Quaternion.Euler(-12f * jolt, 0f, 0f);
        }

        private static float Ease(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private readonly struct Leg
        {
            public readonly int Index;
            public readonly string Name;
            public readonly Vector3[] Joints;
            public readonly bool IsTripodA;

            public Leg(int index)
            {
                bool isLeft = index >= 3;
                int row = index % 3;
                Index = index;
                Name = $"{(isLeft ? 'L' : 'R')}{row + 1}";
                Joints = s_right[row].Select(joint => isLeft ? new Vector3(-joint.x, joint.y, joint.z) : joint).ToArray();
                // R1, L2, R3 step together; L1, R2, L3 together.
                IsTripodA = isLeft == (row == 1);
            }

            /// Normal of the rest leg plane, turned so a positive angle folds the leg toward the body.
            public Vector3 BendAxis => Vector3.Cross(Joints[1] - Joints[0], Joints[2] - Joints[1]).normalized;

            public string Path(int segment)
            {
                string path = "Root/Body";

                for (int s = 0; s <= segment; s++)
                    path += $"/{Name}_{s_segments[s]}";

                return path;
            }

            /// Local rotations of femur, tibia and tarsus (rest = identity) that put the claw tip on target, in the body's rest frame.
            /// The leg turns about the vertical at the hip toward the target, the tarsus keeps its rest direction in that turned frame,
            /// and femur and tibia reach the ankle bending to the side the knee bends at rest.
            public Quaternion[] Solve(Vector3 tip)
            {
                Vector3 hip = Joints[0];
                Vector3 femur = Joints[1] - Joints[0];
                Vector3 tibia = Joints[2] - Joints[1];
                Vector3 tarsus = Joints[3] - Joints[2];
                Vector3 restReach = Flat(Joints[3] - hip);
                Vector3 reach = Flat(tip - hip);
                Quaternion yaw = reach.sqrMagnitude > 1e-6f ? Quaternion.FromToRotation(restReach, reach) : Quaternion.identity;
                Vector3 ankle = tip - yaw * tarsus;
                Vector3 restLine = (Joints[2] - hip).normalized;
                Vector3 pole = yaw * (femur - restLine * Vector3.Dot(femur, restLine));
                float a = femur.magnitude;
                float b = tibia.magnitude;
                Vector3 line = ankle - hip;
                float d = Mathf.Clamp(line.magnitude, Mathf.Abs(a - b) + 1e-3f, a + b - 1e-3f);
                Vector3 u = line.normalized;
                float angle = Mathf.Acos(Mathf.Clamp((a * a + d * d - b * b) / (2f * a * d), -1f, 1f)) * Mathf.Rad2Deg;
                Vector3 axis = Vector3.Cross(u, pole).normalized;
                Vector3 femurDirection = Quaternion.AngleAxis(angle, axis) * u;
                Vector3 knee = hip + femurDirection * a;
                Vector3 tibiaDirection = (hip + u * d - knee).normalized;
                Quaternion femurRotation = Quaternion.FromToRotation(yaw * femur, femurDirection) * yaw;
                Quaternion tibiaRotation = Quaternion.FromToRotation(yaw * tibia, tibiaDirection * b) * yaw;

                return new[] { femurRotation, Quaternion.Inverse(femurRotation) * tibiaRotation, Quaternion.Inverse(tibiaRotation) * yaw };
            }

            private static Vector3 Flat(Vector3 vector)
            {
                return new Vector3(vector.x, 0f, vector.z);
            }
        }
    }
}
