using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A monster's own recorded sounds, as Dark and Darker gives every monster kind: body steps and foley, voice lines, its swing and its fall.
    [CreateAssetMenu(menuName = "Game/Dungeon/Monster Sound Config")]
    public sealed class MonsterSoundConfig : ScriptableObject
    {
        public AudioClip[] Steps => _steps;
        public AudioClip[] Foley => _foley;
        public AudioClip[] Idle => _idle;
        public AudioClip[] Alert => _alert;
        public AudioClip[] AttackVoice => _attackVoice;
        public AudioClip[] AttackWhoosh => _attackWhoosh;
        public AudioClip[] Pain => _pain;
        public AudioClip[] Death => _death;
        public AudioClip[] BoneDrop => _boneDrop;
        public AudioClip[] ReloadVoice => _reloadVoice;

        [SerializeField]
        private AudioClip[] _steps = System.Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _foley = System.Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _idle = System.Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _alert = System.Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _attackVoice = System.Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _attackWhoosh = System.Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _pain = System.Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _death = System.Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _boneDrop = System.Array.Empty<AudioClip>();

        [SerializeField, Tooltip("Strained voice while cocking a crossbow by hand")]
        private AudioClip[] _reloadVoice = System.Array.Empty<AudioClip>();

        public static AudioClip Pick(AudioClip[] clips)
        {
            return clips == null || clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];
        }
    }
}
