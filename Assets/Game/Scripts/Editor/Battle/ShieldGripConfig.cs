using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// How each shield sits in the off hand (AnimationEditConfig, set in the animation editor's Shield grip): every clip with the
    /// shield is solved for it, so the board keeps its authored path and the hand takes the grip.
    internal static class ShieldGripConfig
    {
        public const string RoundShield = "Shield";
        public const string Ecu = "Ecu";

        public static Pose Get(string shield)
        {
            AnimationEditConfig config = shield == null ? null : AssetDatabase.LoadAssetAtPath<AnimationEditConfig>(AnimationEditBaker.ConfigPath);

            return config != null ? config.GetShieldGrip(shield) : Pose.identity;
        }
    }
}
