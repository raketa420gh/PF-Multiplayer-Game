using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Simulation model of the upper body: a spine chain that bends with look pitch and drops with crouch.
    /// All points are given in character root space for the standing bind pose.
    [CreateAssetMenu(menuName = "Game/Battle/Body Config")]
    public sealed class BodyConfig : ScriptableObject
    {
        public Vector3 EyePoint => _eyePoint;
        public float CrouchDrop => _crouchDrop;

        [SerializeField]
        private Vector3[] _spinePivots = { new(0f, 1.1f, 0f), new(0f, 1.24f, 0f), new(0f, 1.39f, 0f) };

        [SerializeField]
        private Vector3 _eyePoint = new(0f, 1.75f, 0.08f);

        [SerializeField]
        private float _crouchDrop = 0.45f;

        public Vector3 TransformPoint(Vector3 bindPoint, float pitch, float crouch)
        {
            int count = _spinePivots.Length;
            Quaternion step = Quaternion.AngleAxis(pitch / count, Vector3.right);

            for (int i = count - 1; i >= 0; i--)
                bindPoint = _spinePivots[i] + step * (bindPoint - _spinePivots[i]);

            bindPoint.y -= _crouchDrop * crouch;

            return bindPoint;
        }

        public Vector3 TransformLower(Vector3 bindPoint, float crouch)
        {
            bindPoint.y -= _crouchDrop * crouch;

            return bindPoint;
        }

        public Quaternion TransformRotation(Quaternion bindRotation, float pitch)
        {
            return Quaternion.AngleAxis(pitch, Vector3.right) * bindRotation;
        }
    }
}
