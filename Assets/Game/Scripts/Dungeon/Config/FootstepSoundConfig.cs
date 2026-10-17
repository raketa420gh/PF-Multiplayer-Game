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

    /// What the foot lands on; untagged ground is stone. Order is serialized: only append.
    public enum FootstepSurface : byte
    {
        Stone,
        Wood,
        WoodStair,
        Jail,
        Rubble,
        Water,
        Grass
    }

    /// Recorded steps per surface and kind of footwear; cloth and leather share the leather takes as in Dark and Darker,
    /// heavy leather plays them lower.
    [CreateAssetMenu(menuName = "Game/Dungeon/Footstep Sound Config")]
    public sealed class FootstepSoundConfig : ScriptableObject
    {
        [SerializeField]
        private SurfaceSounds[] _surfaces = Array.Empty<SurfaceSounds>();

        [SerializeField]
        private float _heavyPitch = 0.9f;

        public AudioClip Pick(Footwear footwear, FootstepSurface surface)
        {
            AudioClip[] clips = Find(surface)?.Get(footwear);

            if (clips == null || clips.Length == 0)
                clips = Find(FootstepSurface.Stone)?.Get(footwear);

            return clips == null || clips.Length == 0 ? null : clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        public float GetPitch(Footwear footwear)
        {
            return footwear == Footwear.Heavy ? _heavyPitch : 1f;
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

        private SurfaceSounds Find(FootstepSurface surface)
        {
            return Array.Find(_surfaces, sounds => sounds.Surface == surface);
        }

        [Serializable]
        private sealed class SurfaceSounds
        {
            [SerializeField]
            private FootstepSurface _surface;

            [SerializeField]
            private AudioClip[] _bare = Array.Empty<AudioClip>();

            [SerializeField]
            private AudioClip[] _leather = Array.Empty<AudioClip>();

            [SerializeField]
            private AudioClip[] _plate = Array.Empty<AudioClip>();

            public FootstepSurface Surface => _surface;

            public AudioClip[] Get(Footwear footwear)
            {
                return footwear switch
                {
                    Footwear.Bare => _bare,
                    Footwear.Plate => _plate,
                    _ => _leather
                };
            }
        }
    }
}
