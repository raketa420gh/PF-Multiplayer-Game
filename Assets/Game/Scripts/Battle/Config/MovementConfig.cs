using UnityEngine;

namespace Game.Scripts.Battle
{
    [CreateAssetMenu(menuName = "Game/Battle/Movement Config")]
    public sealed class MovementConfig : ScriptableObject
    {
        public float WalkSpeed => _walkSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float CrouchSpeed => _crouchSpeed;
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
        private float _walkSpeed = 3.2f;

        [SerializeField]
        private float _sprintSpeed = 5.4f;

        [SerializeField]
        private float _crouchSpeed = 1.7f;

        [SerializeField]
        private float _backpedalMultiplier = 0.7f;

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
