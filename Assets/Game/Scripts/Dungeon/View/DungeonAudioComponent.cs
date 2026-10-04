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
        Extract,
        Screech
    }

    /// Takes of one sound; a random one is played each time.
    [System.Serializable]
    public sealed class DungeonSoundTakes
    {
        public AudioClip[] Clips => _clips;

        [SerializeField]
        private AudioClip[] _clips;
    }

    /// One-shot sounds at world positions plus the tavern theme / dungeon ambience.
    public sealed class DungeonAudioComponent : MonoBehaviour
    {
        public static DungeonAudioComponent Instance => s_instance;

        [SerializeField]
        private DungeonContext _context;

        [SerializeField, Tooltip("Indexed by DungeonSound")]
        private DungeonSoundTakes[] _sounds;

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
            _music.reverbZoneMix = 0f;
            _ambientSource.clip = _ambient;
            _ambientSource.loop = true;
            _ambientSource.volume = 0.45f;
            _ambientSource.reverbZoneMix = 0f;
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
            source.reverbZoneMix = 1f;
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
            source.reverbZoneMix = 0f;
            source.pitch = 1f;
            source.volume = _volume * volume;
            source.clip = clip;
            source.Play();
        }

        private AudioClip GetClip(DungeonSound sound)
        {
            int index = (int)sound;

            if (index >= _sounds.Length || _sounds[index].Clips.Length == 0)
                return null;

            AudioClip[] clips = _sounds[index].Clips;

            return clips[Random.Range(0, clips.Length)];
        }
    }
}
