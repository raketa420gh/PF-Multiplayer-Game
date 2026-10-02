using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class FighterBodyComponent : MonoBehaviour
    {
        public BodyConfig Config => _config;
        public Vector3 EyePosition => UpperToWorld(_config.EyePoint);
        public Vector3 ChestPosition => UpperToWorld(_chestPoint);
        public Quaternion AimRotation => transform.rotation * Quaternion.Euler(_move.Pitch, 0f, 0f);
        public Vector3 AimDirection => AimRotation * Vector3.forward;

        [SerializeField]
        private BodyConfig _config;

        [SerializeField]
        private FighterMoveComponent _move;

        [SerializeField]
        private Vector3 _chestPoint = new(0f, 1.35f, 0f);

        [SerializeField]
        private Transform[] _upperHitboxes;

        [SerializeField]
        private Transform[] _lowerHitboxes;

        private Pose[] _upperBind;
        private Pose[] _lowerBind;

        private void Awake()
        {
            _upperBind = CaptureBind(_upperHitboxes);
            _lowerBind = CaptureBind(_lowerHitboxes);
        }

        public Vector3 UpperToWorld(Vector3 bindPoint)
        {
            return transform.TransformPoint(_config.TransformPoint(bindPoint, _move.Pitch, _move.CrouchAmount));
        }

        public Vector3 GetEyePosition(Vector3 rootPosition, float yaw, float pitch, float crouch)
        {
            return rootPosition + Quaternion.Euler(0f, yaw, 0f) * _config.TransformPoint(_config.EyePoint, pitch, crouch);
        }

        public void UpdateHitboxes()
        {
            float pitch = _move.Pitch;
            float crouch = _move.CrouchAmount;

            for (int i = 0; i < _upperHitboxes.Length; i++)
            {
                Pose bind = _upperBind[i];
                _upperHitboxes[i].SetLocalPositionAndRotation(
                    _config.TransformPoint(bind.position, pitch, crouch),
                    _config.TransformRotation(bind.rotation, pitch));
            }

            for (int i = 0; i < _lowerHitboxes.Length; i++)
                _lowerHitboxes[i].localPosition = _config.TransformLower(_lowerBind[i].position, crouch);
        }

        private static Pose[] CaptureBind(Transform[] transforms)
        {
            Pose[] poses = new Pose[transforms.Length];

            for (int i = 0; i < transforms.Length; i++)
                poses[i] = new Pose(transforms[i].localPosition, transforms[i].localRotation);

            return poses;
        }
    }
}
