using System.Collections.Generic;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Ragdoll Wizard by code: 11 bodies on the humanoid bones of a model in its bind pose, anatomical joint limits,
    /// hands and feet riding on the forearms and shins. Everything starts kinematic with the colliders off.
    internal static class BattleRagdollBuilder
    {
        public const string RagdollLayer = "Ragdoll";

        private const float TotalMass = 70f;
        private const string MaterialPath = BattleEditorUtility.ConfigsFolder + "/RagdollPhysics.asset";

        private sealed class Rig
        {
            public readonly List<Rigidbody> Bodies = new();
            public readonly List<Collider> Colliders = new();
            public Animator Animator;
            public Vector3 Up;
            public Vector3 Forward;
            public int Layer;
            public PhysicsMaterial Material;

            public Transform Bone(HumanBodyBones bone) => Animator.GetBoneTransform(bone);
        }

        /// Layer of the falling bones: they lie on the level and on each other, never on characters, hitboxes or triggers.
        public static int EnsureLayer()
        {
            int layer = BattleEditorUtility.EnsureLayer(RagdollLayer);

            for (int i = 0; i < 32; i++)
                Physics.IgnoreLayerCollision(layer, i, i != 0 && i != layer);

            return layer;
        }

        public static RagdollComponent Build(Animator animator, int frozenLayer)
        {
            Transform root = animator.transform;
            Rig rig = new Rig { Animator = animator, Up = root.up, Forward = root.forward, Layer = EnsureLayer(), Material = LoadMaterial() };

            Transform hips = rig.Bone(HumanBodyBones.Hips);
            Transform spine = rig.Bone(HumanBodyBones.Spine);
            Transform chest = rig.Bone(HumanBodyBones.Chest) ?? spine;
            Transform neck = rig.Bone(HumanBodyBones.Neck) ?? rig.Bone(HumanBodyBones.Head);
            Transform head = rig.Bone(HumanBodyBones.Head);
            Vector3 down = -rig.Up;

            Rigidbody pelvis = AddBody(rig, hips, 0.15f);
            AddBox(rig, hips, 0.22f, 0.05f, rig.Bone(HumanBodyBones.LeftUpperLeg).position, rig.Bone(HumanBodyBones.RightUpperLeg).position, hips.position, spine.position);

            Rigidbody torso = AddBody(rig, chest, 0.31f);
            AddBox(rig, chest, 0.24f, 0.04f, spine.position, rig.Bone(HumanBodyBones.LeftUpperArm).position, rig.Bone(HumanBodyBones.RightUpperArm).position, neck.position);
            AddJoint(chest, pelvis, head.position, rig.Forward, -20f, 35f, rig.Forward, 15f, 15f);

            AddBody(rig, head, 0.08f);
            AddSphere(rig, head, head.position + rig.Up * 0.08f, 0.11f);
            AddJoint(head, torso, head.position + rig.Up, rig.Forward, -25f, 40f, rig.Forward, 25f, 30f);

            foreach (bool isLeft in new[] { true, false })
            {
                Transform upperArm = rig.Bone(isLeft ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
                Transform lowerArm = rig.Bone(isLeft ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
                Transform hand = rig.Bone(isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                Transform upperLeg = rig.Bone(isLeft ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
                Transform lowerLeg = rig.Bone(isLeft ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
                Transform foot = rig.Bone(isLeft ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
                Transform toes = rig.Bone(isLeft ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes);

                // Shoulder: forward 100, back 40, up and down 85, roll 40; the elbow bends forward only.
                Rigidbody arm = AddBody(rig, upperArm, 0.03f);
                AddCapsule(rig, upperArm, lowerArm.position, 0.05f);
                AddJoint(upperArm, torso, lowerArm.position, rig.Forward, -40f, 100f, rig.Forward, 85f, 40f);

                AddBody(rig, lowerArm, 0.02f);
                AddCapsule(rig, lowerArm, hand.position, 0.04f);
                AddSphere(rig, hand, hand.position + (hand.position - lowerArm.position).normalized * 0.07f, 0.045f);
                AddJoint(lowerArm, arm, hand.position, rig.Forward, -5f, 140f, hand.position - lowerArm.position, 60f, 5f);

                // Hip: forward 90, back 20, sideways 35, roll 20; the knee bends backward only.
                Rigidbody thigh = AddBody(rig, upperLeg, 0.12f);
                AddCapsule(rig, upperLeg, lowerLeg.position, 0.075f);
                AddJoint(upperLeg, pelvis, lowerLeg.position, rig.Forward, -20f, 90f, rig.Forward, 35f, 20f);

                AddBody(rig, lowerLeg, 0.06f);
                AddCapsule(rig, lowerLeg, foot.position, 0.055f);
                Vector3 tip = toes != null ? toes.position + rig.Forward * 0.04f : foot.position + rig.Forward * 0.18f;
                AddBox(rig, foot, 0.09f, 0.015f, foot.position - rig.Forward * 0.05f, tip, foot.position + down * Vector3.Dot(foot.position - root.position, rig.Up), tip + down * Vector3.Dot(tip - root.position, rig.Up), true);
                AddJoint(lowerLeg, thigh, foot.position, -rig.Forward, -2f, 130f, foot.position - lowerLeg.position, 10f, 5f);
            }

            RagdollComponent ragdoll = animator.gameObject.AddComponent<RagdollComponent>();
            SerializedObject so = new SerializedObject(ragdoll);
            BattleEditorUtility.Set(so, "_animator", animator);
            BattleEditorUtility.Set(so, "_bodies", rig.Bodies.ToArray());
            BattleEditorUtility.Set(so, "_colliders", rig.Colliders.ToArray());
            BattleEditorUtility.Set(so, "_skeleton", hips.GetComponentsInChildren<Transform>(true));
            BattleEditorUtility.Set(so, "_frozenLayer", frozenLayer);
            so.ApplyModifiedPropertiesWithoutUndo();

            return ragdoll;
        }

        private static PhysicsMaterial LoadMaterial()
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(MaterialPath);

            if (material == null)
            {
                material = new PhysicsMaterial("Ragdoll");
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.dynamicFriction = 0.7f;
            material.staticFriction = 0.9f;
            material.bounciness = 0f;
            material.frictionCombine = PhysicsMaterialCombine.Maximum;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);

            return material;
        }

        private static Rigidbody AddBody(Rig rig, Transform bone, float massShare)
        {
            Rigidbody body = bone.gameObject.AddComponent<Rigidbody>();
            body.mass = TotalMass * massShare;
            body.isKinematic = true;
            // Limp, not springy: a limb hanging over an edge stops swinging within a second or two.
            body.linearDamping = 0.2f;
            body.angularDamping = 3f;
            body.sleepThreshold = 0.02f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.maxDepenetrationVelocity = 2f;
            body.solverIterations = 12;
            body.solverVelocityIterations = 4;
            rig.Bodies.Add(body);

            return body;
        }

        /// The joint's twist bends the bone towards `bend` (positive up to `high`, the other way down to `low`); swing 1 turns it
        /// about `swingAxis`, swing 2 about the third axis. CharacterJoint twist runs against Quaternion.AngleAxis, hence the cross order.
        private static void AddJoint(Transform bone, Rigidbody parent, Vector3 end, Vector3 bend, float low, float high, Vector3 swingAxis,
            float swing1, float swing2)
        {
            Vector3 along = (end - bone.position).normalized;
            Vector3 axis = Vector3.Cross(Vector3.ProjectOnPlane(bend, along).normalized, along);
            CharacterJoint joint = bone.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = parent;
            joint.axis = bone.InverseTransformDirection(axis).normalized;
            joint.swingAxis = bone.InverseTransformDirection(Vector3.ProjectOnPlane(swingAxis, axis).normalized).normalized;
            joint.lowTwistLimit = new SoftJointLimit { limit = low };
            joint.highTwistLimit = new SoftJointLimit { limit = high };
            joint.swing1Limit = new SoftJointLimit { limit = swing1 };
            joint.swing2Limit = new SoftJointLimit { limit = swing2 };
            joint.enablePreprocessing = false;
            joint.enableProjection = true;
        }

        private static void AddCapsule(Rig rig, Transform bone, Vector3 end, float radius)
        {
            Vector3 local = bone.InverseTransformPoint(end);
            int direction = LargestAxis(local);
            CapsuleCollider capsule = bone.gameObject.AddComponent<CapsuleCollider>();
            capsule.direction = direction;
            capsule.center = local * 0.5f;
            capsule.radius = radius / Scale(bone);
            capsule.height = Mathf.Abs(local[direction]) + capsule.radius;
            Register(rig, capsule);
        }

        private static void AddSphere(Rig rig, Transform bone, Vector3 center, float radius)
        {
            SphereCollider sphere = bone.gameObject.AddComponent<SphereCollider>();
            sphere.center = bone.InverseTransformPoint(center);
            sphere.radius = radius / Scale(bone);
            Register(rig, sphere);
        }

        /// Box around the points in the bone's space: `thickness` front to back (side to side for a foot), `padding` on the other axes.
        private static void AddBox(Rig rig, Transform bone, float thickness, float padding, Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool isFoot = false)
        {
            float scale = Scale(bone);
            Bounds bounds = new Bounds(bone.InverseTransformPoint(a), Vector3.zero);
            bounds.Encapsulate(bone.InverseTransformPoint(b));
            bounds.Encapsulate(bone.InverseTransformPoint(c));
            bounds.Encapsulate(bone.InverseTransformPoint(d));
            Vector3 size = bounds.size + Vector3.one * (padding * 2f / scale);
            Vector3 thin = isFoot ? Vector3.Cross(rig.Up, rig.Forward) : rig.Forward;
            size[LargestAxis(bone.InverseTransformDirection(thin))] = thickness / scale;

            BoxCollider box = bone.gameObject.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = size;
            Register(rig, box);
        }

        private static void Register(Rig rig, Collider collider)
        {
            collider.sharedMaterial = rig.Material;
            collider.enabled = false;
            collider.gameObject.layer = rig.Layer;
            rig.Colliders.Add(collider);
        }

        private static float Scale(Transform bone)
        {
            return bone.lossyScale.x;
        }

        private static int LargestAxis(Vector3 v)
        {
            Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

            return a.x >= a.y && a.x >= a.z ? 0 : a.y >= a.z ? 1 : 2;
        }
    }
}
