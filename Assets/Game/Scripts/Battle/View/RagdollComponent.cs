using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Local, non-networked ragdoll of a humanoid model (built by BattleRagdollBuilder): starts from another model's pose, falls with
    /// the given velocity and freezes for good once it comes to rest. The frozen bone colliders stay as triggers for the look ray.
    public sealed class RagdollComponent : MonoBehaviour
    {
        /// Raised once when the body stops; true when it was lost (fell through the floor or too far) and has to be posed otherwise.
        public event Action<bool> OnSettled;

        public bool IsSettled => _isSettled;
        public Transform Hips => _skeleton[0];

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private Rigidbody[] _bodies;

        [SerializeField]
        private Collider[] _colliders;

        [SerializeField, Tooltip("Every transform of the skeleton, the hips first")]
        private Transform[] _skeleton;

        [SerializeField, Tooltip("Layer of the frozen bone colliders")]
        private int _frozenLayer;

        [SerializeField]
        private float _restSpeed = 0.05f;

        [SerializeField]
        private float _restTime = 0.5f;

        [SerializeField]
        private float _timeout = 6f;

        [SerializeField]
        private float _maxDrift = 8f;

        [SerializeField, Tooltip("Share of the push the whole body gets; the bone nearest the hit gets all of it")]
        private float _bodyPush = 0.4f;

        private static readonly HumanBodyBones[] s_hands = { HumanBodyBones.LeftHand, HumanBodyBones.RightHand };
        private static readonly Dictionary<string, Transform> s_bones = new();

        private Vector3 _origin;
        private float _time;
        private float _restLeft;
        private bool _isSimulating;
        private bool _isSettled;

        private void FixedUpdate()
        {
            if (!_isSimulating)
                return;

            _time += Time.fixedDeltaTime;

            if ((Hips.position - _origin).sqrMagnitude > _maxDrift * _maxDrift)
            {
                Settle(true);

                return;
            }

            _restLeft = IsResting() ? _restLeft - Time.fixedDeltaTime : _restTime;

            if (_restLeft <= 0f || _time >= _timeout)
                Settle(false);
        }

        /// Takes over the source model's pose and what its hands hold, then lets go with the velocity and the push at the hit point.
        public void Begin(Animator source, Vector3 velocity, Vector3 hitPoint, Vector3 push)
        {
            _animator.enabled = false;
            CopyPose(source);
            CopyHeldItems(source);
            Physics.SyncTransforms();

            _origin = Hips.position;
            _restLeft = _restTime;
            _isSimulating = true;
            Rigidbody struck = _bodies[0];

            foreach (Collider collider in _colliders)
                collider.enabled = true;

            foreach (Rigidbody body in _bodies)
            {
                body.isKinematic = false;
                body.linearVelocity = velocity + push * _bodyPush;

                if ((body.worldCenterOfMass - hitPoint).sqrMagnitude < (struck.worldCenterOfMass - hitPoint).sqrMagnitude)
                    struck = body;
            }

            if (push != Vector3.zero)
                struck.AddForceAtPosition(push * (1f - _bodyPush), hitPoint, ForceMode.VelocityChange);
        }

        /// Stays in the pose it has now; the bones are only looked at from here on.
        public void Freeze()
        {
            Settle(false);
        }

        private bool IsResting()
        {
            foreach (Rigidbody body in _bodies)
            {
                if (!body.IsSleeping() && body.linearVelocity.sqrMagnitude > _restSpeed * _restSpeed)
                    return false;
            }

            return true;
        }

        private void Settle(bool isLost)
        {
            _isSimulating = false;
            _isSettled = true;

            foreach (Rigidbody body in _bodies)
                body.isKinematic = true;

            foreach (Collider collider in _colliders)
            {
                collider.isTrigger = true;
                collider.gameObject.layer = _frozenLayer;
                collider.enabled = true;
            }

            enabled = false;
            OnSettled?.Invoke(isLost);
        }

        private void CopyPose(Animator source)
        {
            Transform hips = source.GetBoneTransform(HumanBodyBones.Hips);

            foreach (Transform bone in hips.GetComponentsInChildren<Transform>())
                s_bones.TryAdd(bone.name, bone);

            _skeleton[0].SetPositionAndRotation(hips.position, hips.rotation);

            for (int i = 1; i < _skeleton.Length; i++)
            {
                if (s_bones.TryGetValue(_skeleton[i].name, out Transform from))
                    _skeleton[i].SetLocalPositionAndRotation(from.localPosition, from.localRotation);
            }

            s_bones.Clear();
        }

        /// Weapons, shields and torches go down with the body, riding on its hand bones.
        private void CopyHeldItems(Animator source)
        {
            foreach (HumanBodyBones hand in s_hands)
            {
                Transform from = source.GetBoneTransform(hand);
                Transform to = _animator.GetBoneTransform(hand);

                if (from == null || to == null)
                    continue;

                foreach (MeshRenderer renderer in from.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!renderer.enabled || !renderer.TryGetComponent(out MeshFilter filter))
                        continue;

                    GameObject copy = new GameObject(renderer.name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = gameObject.layer };
                    Transform item = renderer.transform;
                    copy.transform.SetPositionAndRotation(item.position, item.rotation);
                    copy.transform.localScale = item.lossyScale;
                    copy.transform.SetParent(to, true);
                    copy.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    copy.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                }
            }
        }
    }
}
