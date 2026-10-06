using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Dark and Darker style locomotion: running is the default, Shift toggles a slow quiet walk.
    [CreateAssetMenu(menuName = "Game/Battle/Movement Config")]
    public sealed class MovementConfig : ScriptableObject
    {
        public float RunSpeed => _runSpeed;
        public float WalkMultiplier => _walkMultiplier;
        public float CrouchMultiplier => _crouchMultiplier;
        public float BackpedalMultiplier => _backpedalMultiplier;
        public float StrafeMultiplier => _strafeMultiplier;
        public float Acceleration => _acceleration;
        public float Braking => _braking;
        public float JumpImpulse => _jumpImpulse;
        public float Gravity => _gravity;
        public float CrouchTransitionTime => _crouchTransitionTime;
        public float StandHeight => _standHeight;
        public float CrouchHeight => _crouchHeight;

        [SerializeField]
        private float _runSpeed = 3.36f;

        [SerializeField]
        private float _walkMultiplier = 0.4f;

        [SerializeField]
        private float _crouchMultiplier = 0.65f;

        [SerializeField]
        private float _backpedalMultiplier = 0.6f;

        [SerializeField]
        private float _strafeMultiplier = 0.85f;

        [SerializeField]
        private float _acceleration = 60f;

        [SerializeField]
        private float _braking = 14f;

        [SerializeField]
        private float _jumpImpulse = 6.5f;

        [SerializeField]
        private float _gravity = -20f;

        [SerializeField]
        private float _crouchTransitionTime = 0.18f;

        [SerializeField]
        private float _standHeight = 1.85f;

        [SerializeField]
        private float _crouchHeight = 1.35f;
    }
}
