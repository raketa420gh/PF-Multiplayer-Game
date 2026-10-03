using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class FighterMoveComponent : NetworkBehaviour
    {
        public const float MaxPitch = 90f;

        public MovementConfig Config => _config;
        public Vector3 Velocity => _controller.Velocity;
        public bool IsGrounded => _controller.Grounded;

        [Networked]
        public float Pitch { get; private set; }

        [Networked]
        public float CrouchAmount { get; private set; }

        [SerializeField]
        private MovementConfig _config;

        [SerializeField]
        private NetworkCharacterController _controller;

        [SerializeField]
        private CharacterController _collider;

        [SerializeField]
        private LayerMask _groundMask = 1;

        private const float SnapDistance = 0.35f;

        public override void Spawned()
        {
            _controller.acceleration = _config.Acceleration;
            _controller.braking = _config.Braking;
            _controller.gravity = _config.Gravity;
            _controller.jumpImpulse = _config.JumpImpulse;
            _controller.rotationSpeed = 0f;
        }

        public void Simulate(Vector2 move, Vector2 look, bool isWalk, bool isCrouch, bool isJump, float speedMultiplier)
        {
            Pitch = Mathf.Clamp(look.x, -MaxPitch, MaxPitch);
            transform.rotation = Quaternion.Euler(0f, look.y, 0f);

            float crouchTarget = isCrouch ? 1f : 0f;
            CrouchAmount = Mathf.MoveTowards(CrouchAmount, crouchTarget, Runner.DeltaTime / _config.CrouchTransitionTime);
            UpdateCollider();

            if (isJump && CrouchAmount < 0.5f)
                _controller.Jump();

            bool wasGrounded = _controller.Grounded;
            float riseSpeed = _controller.Velocity.y;
            _controller.maxSpeed = GetSpeed(move, isWalk) * speedMultiplier;
            _controller.Move(transform.rotation * new Vector3(move.x, 0f, move.y));

            // The controller derives velocity from displacement: a step-up or a push out of another capsule would
            // become a launch speed, so upward motion that was not a jump or an impulse is dropped.
            if (wasGrounded && riseSpeed <= 0f && _controller.Velocity.y > 0f)
                _controller.Velocity = Vector3.Scale(_controller.Velocity, new Vector3(1f, 0f, 1f));

            if (wasGrounded && riseSpeed <= 0f && !_controller.Grounded)
                SnapToGround();
        }

        public void Teleport(Vector3 position, float yaw)
        {
            _controller.Velocity = Vector3.zero;
            _controller.Teleport(position, Quaternion.Euler(0f, yaw, 0f));
        }

        public void AddImpulse(Vector3 impulse)
        {
            _controller.Velocity += impulse;
        }

        /// Walking down stairs and ramps the capsule would leave the ground on every tick; this keeps the feet on it.
        private void SnapToGround()
        {
            float radius = _collider.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (_collider.radius + 0.05f);

            if (!Runner.GetPhysicsScene().SphereCast(origin, radius, Vector3.down, out RaycastHit hit, SnapDistance, _groundMask, QueryTriggerInteraction.Ignore))
                return;

            _collider.Move(Vector3.down * hit.distance);
            _controller.Grounded = true;
        }

        private float GetSpeed(Vector2 move, bool isWalk)
        {
            float speed = _config.RunSpeed;

            if (isWalk)
                speed *= _config.WalkMultiplier;

            speed = Mathf.Lerp(speed, speed * _config.CrouchMultiplier, CrouchAmount);

            if (move.y < -0.1f)
                speed *= _config.BackpedalMultiplier;
            else if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
                speed *= _config.StrafeMultiplier;

            return speed;
        }

        private void UpdateCollider()
        {
            float height = Mathf.Lerp(_config.StandHeight, _config.CrouchHeight, CrouchAmount);
            _collider.height = height;
            _collider.center = new Vector3(0f, height * 0.5f, 0f);
        }
    }
}
