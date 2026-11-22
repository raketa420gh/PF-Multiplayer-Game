using System.Globalization;
using System.IO;
using System.Text;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Numbers of built swing clips, read off the clips themselves: how far the elbows are open, how long the right arm is
    /// and how fast the strike point goes, frame by frame. What a solver report does not show (an arm bent through the
    /// active window, a snap or a stall of the weapon at the peak) shows here.
    internal static class ClipMeasure
    {
        /// Clip names without the folder ("Club_Attack0"), the strike point's distance from the grip; the table goes to the file.
        public static string Run(string[] names, float strike, string path)
        {
            StringBuilder table = new StringBuilder();
            GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), Vector3.zero, Quaternion.identity);
            model.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                Animator animator = model.GetComponent<Animator>();
                Transform socket = BattlePoseRig.CreateSockets(animator)[(int)WeaponSocket.RightHand];
                Transform[] right = Arm(animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
                Transform[] left = Arm(animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
                float length = (right[1].position - right[0].position).magnitude + (right[2].position - right[1].position).magnitude;

                foreach (string name in names)
                {
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{BattleEditorUtility.AnimationsFolder}/{name}.anim");

                    if (clip == null)
                    {
                        table.AppendLine($"# {name}: no clip");

                        continue;
                    }

                    int frames = Mathf.RoundToInt(clip.length * BattleAnimationBuilder.FrameRate);
                    table.AppendLine($"# {name} frames {frames}");
                    table.AppendLine("f\tRelb\tLelb\treach\tm/s\tdeg/f\troll\tfist x y z\tweapon x y z");
                    Vector3 point = Vector3.zero;
                    Quaternion turn = Quaternion.identity;

                    for (int frame = 0; frame <= frames; frame++)
                    {
                        clip.SampleAnimation(model, frame / BattleAnimationBuilder.FrameRate);
                        Vector3 next = socket.TransformPoint(0f, 0f, strike);
                        // The root rides with the centre of mass under a bare clip: positions are given from the chest.
                        Vector3 fist = right[2].position - (right[0].position + left[0].position) * 0.5f;
                        float speed = frame == 0 ? 0f : (next - point).magnitude * BattleAnimationBuilder.FrameRate;
                        float swing = frame == 0 ? 0f : Vector3.Angle(turn * Vector3.forward, socket.forward);
                        // What is left of the frame's turn once the weapon's axis is brought along: the spin about itself.
                        float roll = frame == 0 ? 0f : Quaternion.Angle(Quaternion.FromToRotation(turn * Vector3.forward, socket.forward) * turn, socket.rotation);
                        point = next;
                        turn = socket.rotation;
                        table.AppendLine(string.Join("\t", frame, Elbow(right).ToString("0"), Elbow(left).ToString("0"), Number((right[2].position - right[0].position).magnitude / length),
                            speed.ToString("0.0", CultureInfo.InvariantCulture), swing.ToString("0", CultureInfo.InvariantCulture), roll.ToString("0", CultureInfo.InvariantCulture),
                            Vector(fist), Vector(socket.forward)));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(model);
            }

            File.WriteAllText(path, table.ToString());

            return table.ToString();
        }

        private static Transform[] Arm(Animator animator, params HumanBodyBones[] bones) => System.Array.ConvertAll(bones, animator.GetBoneTransform);

        private static float Elbow(Transform[] arm) => Vector3.Angle(arm[0].position - arm[1].position, arm[2].position - arm[1].position);

        private static string Number(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string Vector(Vector3 value) => $"{Number(value.x)} {Number(value.y)} {Number(value.z)}";
    }
}
