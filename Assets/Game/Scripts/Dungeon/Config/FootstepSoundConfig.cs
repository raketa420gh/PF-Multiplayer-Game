using System;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum Footwear : byte
    {
        Bare,
        Light,
        Heavy,
        Plate
    }

    /// Recorded steps per kind of footwear.
    [CreateAssetMenu(menuName = "Game/Dungeon/Footstep Sound Config")]
    public sealed class FootstepSoundConfig : ScriptableObject
    {
        [SerializeField]
        private AudioClip[] _bare = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _light = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _heavy = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _plate = Array.Empty<AudioClip>();

        public AudioClip Pick(Footwear footwear)
        {
            AudioClip[] clips = footwear switch
            {
                Footwear.Bare => _bare,
                Footwear.Heavy => _heavy,
                Footwear.Plate => _plate,
                _ => _light
            };

            return clips.Length == 0 ? null : clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        /// Boots decide the step: none is barefoot, cloth is soft leather, leather is heavy, plate clanks.
        public static Footwear FromArmor(ArmorItemConfig boots)
        {
            if (boots == null)
                return Footwear.Bare;

            return boots.ArmorType switch
            {
                ArmorType.Plate => Footwear.Plate,
                ArmorType.Leather => Footwear.Heavy,
                _ => Footwear.Light
            };
        }
    }
}
