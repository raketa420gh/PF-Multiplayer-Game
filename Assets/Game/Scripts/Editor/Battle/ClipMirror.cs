using System.Linq;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Bakes a generated clip into its mirror image, as a mirrored humanoid state plays it: the sides swap their muscles, the
    /// middle of the body tilts and turns the other way, and the weapon socket curves swap and reflect the way SocketMirror does.
    internal static class ClipMirror
    {
        private const string Left = "Left";
        private const string Right = "Right";

        /// Socket paths indexed by WeaponSocket.
        public static void Apply(AnimationClip clip, string[] sockets)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            AnimationCurve[] curves = bindings.Select(binding => Reflect(binding, AnimationUtility.GetEditorCurve(clip, binding))).ToArray();

            clip.ClearCurves();
            AnimationUtility.SetEditorCurves(clip, bindings.Select(binding => Swap(binding, sockets)).ToArray(), curves);
            EditorUtility.SetDirty(clip);
        }

        private static EditorCurveBinding Swap(EditorCurveBinding binding, string[] sockets)
        {
            if (binding.type == typeof(Animator))
            {
                if (binding.propertyName.StartsWith(Left))
                    binding.propertyName = Right + binding.propertyName[Left.Length..];
                else if (binding.propertyName.StartsWith(Right))
                    binding.propertyName = Left + binding.propertyName[Right.Length..];

                return binding;
            }

            int socket = System.Array.IndexOf(sockets, binding.path);

            if (socket >= 0)
                binding.path = sockets[(int)SocketMirror.Mirror((WeaponSocket)socket)];

            return binding;
        }

        /// Root and IK goals reflect across the middle plane (x and the rotation about y and z); spine, neck, head and jaw
        /// muscles that tilt or turn to a side take the other side.
        private static AnimationCurve Reflect(EditorCurveBinding binding, AnimationCurve curve)
        {
            string name = binding.propertyName;
            bool isFlipped = binding.type == typeof(Animator)
                ? name.EndsWith("T.x") || name.EndsWith("Q.y") || name.EndsWith("Q.z") || name.Contains("Left-Right")
                : name is "m_LocalPosition.x" or "m_LocalRotation.y" or "m_LocalRotation.z";

            if (!isFlipped)
                return curve;

            Keyframe[] keys = curve.keys;

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].value = -keys[i].value;
                keys[i].inTangent = -keys[i].inTangent;
                keys[i].outTangent = -keys[i].outTangent;
            }

            curve.keys = keys;

            return curve;
        }
    }
}
