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
        Wood,
        Head,
        Handle,
        Grass,
        Snow,
        WoodHard
    }

    /// Handling of the weapon as an item: drawn into the hands, dropped, picked up, moved in the inventory.
    public enum ItemSound : byte
    {
        Equip,
        Drop,
        Pickup,
        Drag,
        Place
    }

    /// Recorded takes of one weapon family: its swing and what it sounds like on every surface.
    [CreateAssetMenu(menuName = "Game/Battle/Weapon Sound Config")]
    public sealed class WeaponSoundConfig : ScriptableObject
    {
        /// Network id of the set: index in the feedback's table plus one; zero means no set.
        public byte Id => _id;
        public AudioClip[] Swing => _swing;
        /// Bows: the string taken back with the limbs creaking under it, then the hold at full draw.
        public AudioClip[] Draw => _draw;
        public AudioClip[] Creak => _creak;
        public AudioClip[] Hold => _hold;
        /// The shot as the shooter hears it and as everyone else does, each with the arrow's flight.
        public AudioClip[] Fire => _fire;
        public AudioClip[] FireFar => _fireFar;
        public AudioClip[] Flight => _flight;
        public AudioClip[] FlightFar => _flightFar;
        /// The next arrow put on the string after a shot; the hip quiver refilled from the back.
        public AudioClip[] Nock => _nock;
        public AudioClip[] Restock => _restock;
        /// Share of the blade trace, from the grip, that is the haft: body hits landing only there sound as Handle.
        public float HandleShare => _handleShare;

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

        [SerializeField]
        private AudioClip[] _head = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _handle = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _grass = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _snow = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _woodHard = Array.Empty<AudioClip>();

        [SerializeField, Range(0f, 1f)]
        private float _handleShare;

        [SerializeField]
        private AudioClip[] _equip = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _drop = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _pickup = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _drag = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _place = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _draw = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _creak = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _hold = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _fire = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _fireFar = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _flight = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _flightFar = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _nock = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _restock = Array.Empty<AudioClip>();

        /// A random take of the item sound, or null when the set has none.
        public static AudioClip Pick(WeaponSoundConfig sounds, ItemSound sound)
        {
            AudioClip[] clips = sounds != null ? sounds.Get(sound) : Array.Empty<AudioClip>();

            return clips.Length > 0 ? clips[UnityEngine.Random.Range(0, clips.Length)] : null;
        }

        public AudioClip[] Get(ItemSound sound)
        {
            return sound switch
            {
                ItemSound.Equip => _equip,
                ItemSound.Drop => _drop,
                ItemSound.Pickup => _pickup,
                ItemSound.Drag => _drag,
                ItemSound.Place => _place,
                _ => Array.Empty<AudioClip>()
            };
        }

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
                ImpactSurface.Head => Or(_head, _flesh),
                ImpactSurface.Handle => Or(_handle, _flesh),
                ImpactSurface.Grass => Or(_grass, _stone),
                ImpactSurface.Snow => Or(_snow, _stone),
                ImpactSurface.WoodHard => Or(_woodHard, _wood),
                _ => Array.Empty<AudioClip>()
            };
        }

        /// Surfaces added later fall back to the nearest older one where the set has no takes of its own.
        private static AudioClip[] Or(AudioClip[] clips, AudioClip[] fallback)
        {
            return clips.Length > 0 ? clips : fallback;
        }
    }
}
