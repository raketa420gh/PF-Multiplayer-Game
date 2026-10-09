using System;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// What a blow lands on; picks the impact set of the striking weapon.
    public enum ImpactSurface : byte
    {
        None,
        Flesh,
        Bone,
        Plate,
        Shield,
        Clash,
        Stone,
        Wood
    }

    /// Recorded takes of one weapon family: its swing and what it sounds like on every surface.
    [CreateAssetMenu(menuName = "Game/Battle/Weapon Sound Config")]
    public sealed class WeaponSoundConfig : ScriptableObject
    {
        /// Network id of the set: index in the feedback's table plus one; zero means no set.
        public byte Id => _id;
        public AudioClip[] Swing => _swing;

        [SerializeField]
        private byte _id;

        [SerializeField]
        private AudioClip[] _swing = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _flesh = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _bone = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _plate = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _shield = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _clash = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _stone = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _wood = Array.Empty<AudioClip>();

        public AudioClip[] Get(ImpactSurface surface)
        {
            return surface switch
            {
                ImpactSurface.Flesh => _flesh,
                ImpactSurface.Bone => _bone,
                ImpactSurface.Plate => _plate,
                ImpactSurface.Shield => _shield,
                ImpactSurface.Clash => _clash,
                ImpactSurface.Stone => _stone,
                ImpactSurface.Wood => _wood,
                _ => Array.Empty<AudioClip>()
            };
        }
    }
}
