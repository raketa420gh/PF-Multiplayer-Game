using System;
using System.Linq;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Keeps edited shoulders, arms, legs and feet inside the avatar's muscle ranges (±1 = the human limit of the joint), so a
    /// bone key cannot wrench a joint the way a real body cannot bend. A muscle the clip itself already drives past its range
    /// may go as far as the clip does: generated swings stay as they are, edits only cannot push further.
    public sealed class JointLimits : IDisposable
    {
        private static readonly string[] s_limbs = { "Shoulder", "Arm", "Forearm", "Upper Leg", "Lower Leg", "Foot" };
        private static readonly int[] s_muscles = FindMuscles();

        private readonly Animator _animator;
        private readonly HumanPoseHandler _handler;
        private readonly float[] _reference = new float[HumanTrait.MuscleCount];
        private HumanPose _pose = new() { muscles = new float[HumanTrait.MuscleCount] };

        public JointLimits(Animator animator)
        {
            _animator = animator;
            _handler = new HumanPoseHandler(animator.avatar, animator.transform);
        }

        public void Dispose()
        {
            _handler.Dispose();
        }

        /// Takes the pose as the clip has it, before the edits.
        public void Capture()
        {
            ReadPose();
            Array.Copy(_pose.muscles, _reference, _reference.Length);
        }

        /// Pulls the edited pose back inside the limits of the captured one.
        public void Restrict()
        {
            ReadPose();

            if (Clamp(_pose.muscles, _reference))
                _handler.SetHumanPose(ref _pose);
        }

        /// True when a muscle had to be pulled back.
        public static bool Clamp(float[] muscles, float[] reference)
        {
            bool isChanged = false;

            foreach (int i in s_muscles)
            {
                float value = Mathf.Clamp(muscles[i], Mathf.Min(-1f, reference[i]), Mathf.Max(1f, reference[i]));
                isChanged |= value != muscles[i];
                muscles[i] = value;
            }

            return isChanged;
        }

        /// A first-person view hides the head by scaling it to zero, which would leave the neck without a pose to read.
        private void ReadPose()
        {
            Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);
            Vector3 scale = head.localScale;
            head.localScale = Vector3.one;
            _handler.GetHumanPose(ref _pose);
            head.localScale = scale;
        }

        private static int[] FindMuscles()
        {
            return Enumerable.Range(0, HumanTrait.MuscleCount)
                .Where(i => Array.Exists(s_limbs, limb => HumanTrait.MuscleName[i].Contains(" " + limb + " "))).ToArray();
        }
    }
}
