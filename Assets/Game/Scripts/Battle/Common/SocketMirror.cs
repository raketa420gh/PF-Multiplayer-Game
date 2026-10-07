using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Generated clips carry transform curves on the weapon sockets: the weapon keeps its path while the arm stays inside its
    /// joint limits. A mirrored humanoid state mirrors the muscles but not those curves, so after the animator has written
    /// them each socket takes its twin's pose reflected across the body's middle plane (the bones are world-aligned at bind).
    public static class SocketMirror
    {
        /// Sockets indexed by WeaponSocket.
        public static void Apply(Transform[] sockets)
        {
            Swap(sockets[(int)WeaponSocket.RightHand], sockets[(int)WeaponSocket.LeftHand]);
            Swap(sockets[(int)WeaponSocket.RightShield], sockets[(int)WeaponSocket.LeftShield]);
        }

        /// The same socket on the other side of the body.
        public static WeaponSocket Mirror(WeaponSocket socket)
        {
            return socket switch
            {
                WeaponSocket.RightHand => WeaponSocket.LeftHand,
                WeaponSocket.LeftHand => WeaponSocket.RightHand,
                WeaponSocket.RightShield => WeaponSocket.LeftShield,
                _ => WeaponSocket.RightShield
            };
        }

        private static void Swap(Transform right, Transform left)
        {
            if (right == null || left == null)
                return;

            right.GetLocalPositionAndRotation(out Vector3 rightPosition, out Quaternion rightRotation);
            left.GetLocalPositionAndRotation(out Vector3 leftPosition, out Quaternion leftRotation);
            right.SetLocalPositionAndRotation(Reflect(leftPosition), Reflect(leftRotation));
            left.SetLocalPositionAndRotation(Reflect(rightPosition), Reflect(rightRotation));
        }

        private static Vector3 Reflect(Vector3 position)
        {
            return new Vector3(-position.x, position.y, position.z);
        }

        private static Quaternion Reflect(Quaternion rotation)
        {
            return new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
        }
    }
}
