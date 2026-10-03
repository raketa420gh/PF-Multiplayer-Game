using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum DungeonSound : byte
    {
        Footstep,
        FootstepB,
        DoorCreak,
        ChestOpen,
        Drink,
        Bandage,
        Portal,
        PortalOpen,
        SwarmTick,
        LevelUp,
        Click,
        Growl,
        Rattle,
        Cast,
        Lever,
        Death,
        Extract
    }

    /// One-shot sounds at world positions plus the tavern theme / dungeon ambience.
    public sealed class DungeonAudioComponent : MonoBehaviour
    {
        public static DungeonAudioComponent Instance => s_instance;

        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private AudioClip[] _clips;

        [SerializeField]
        private AudioClip _menuMusic;

        [SerializeField]
        private AudioClip _ambient;

        [SerializeField]
        private AudioSource _music;

        [SerializeField]
        private AudioSource _ambientSource;

        [SerializeField]
        private int _voices = 10;

        [SerializeField]
        private float _volume = 0.7f;

        private static DungeonAudioComponent s_instance;
        private AudioSource[] _sources;
        private int _next;
        private bool _isInDungeon;

        private void Awake()
        {
            s_instance = this;
            _sources = new AudioSource[_voices];

            for (int i = 0; i < _voices; i++)
            {
                AudioSource source = new GameObject("Voice" + i).AddComponent<AudioSource>();
                source.transform.SetParent(transform, false);
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1.5f;
                source.maxDistance = 24f;
                _sources[i] = source;
            }

            _music.clip = _menuMusic;
            _music.loop = true;
            _music.volume = 0.35f;
            _ambientSource.clip = _ambient;
            _ambientSource.loop = true;
            _ambientSource.volume = 0.45f;
            _music.Play();
        }

        private void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        private void Update()
        {
            PlayerSessionComponent session = _context.LocalSession;
            bool isInDungeon = session != null && session.Object != null && session.Object.IsValid && session.State == SessionState.InDungeon;

            if (isInDungeon == _isInDungeon)
                return;

            _isInDungeon = isInDungeon;

            if (isInDungeon)
            {
                _music.Stop();
                _ambientSource.Play();
            }
            else
            {
                _ambientSource.Stop();
                _music.Play();
            }
        }

        public static void Play(DungeonSound sound, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (s_instance != null)
                s_instance.PlayAt(sound, position, volume, pitch);
        }

        public static void PlayUi(DungeonSound sound, float volume = 1f)
        {
            if (s_instance != null)
                s_instance.PlayFlat(sound, volume);
        }

        private void PlayAt(DungeonSound sound, Vector3 position, float volume, float pitch)
        {
            AudioClip clip = GetClip(sound);

            if (clip == null)
                return;

            AudioSource source = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            source.transform.position = position;
            source.spatialBlend = 1f;
            source.pitch = pitch;
            source.volume = _volume * volume;
            source.clip = clip;
            source.Play();
        }

        private void PlayFlat(DungeonSound sound, float volume)
        {
            AudioClip clip = GetClip(sound);

            if (clip == null)
                return;

            AudioSource source = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            source.spatialBlend = 0f;
            source.pitch = 1f;
            source.volume = _volume * volume;
            source.clip = clip;
            source.Play();
        }

        private AudioClip GetClip(DungeonSound sound)
        {
            int index = (int)sound;

            return index < _clips.Length ? _clips[index] : null;
        }
    }
}
