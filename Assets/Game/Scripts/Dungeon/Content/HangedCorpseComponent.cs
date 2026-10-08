using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Body hanging from the great tree. Some are not quite dead: the first try to loot one (or a blow) wakes it — it convulses
    /// on the rope and screams for the whole area to hear until it takes enough damage to die for good, then it can be searched.
    public sealed class HangedCorpseComponent : NetworkBehaviour
    {
        /// Searching is blocked only while the body is awake and alive.
        public bool IsLootable => !IsAwake || _health.IsDead;

        [Networked]
        public NetworkBool IsAwake { get; private set; }

        [SerializeField]
        private HealthComponent _health;

        [SerializeField, Tooltip("Pivot at the branch: the rope and the body swing round it")]
        private Transform _swing;

        [SerializeField, Tooltip("First is the limp pose, the rest are convulsion frames")]
        private GameObject[] _poses;

        [SerializeField]
        private AudioSource _voice;

        [SerializeField]
        private AudioClip[] _screams;

        [SerializeField, Range(0f, 1f)]
        private float _aliveChance = 0.2f;

        [SerializeField]
        private int _aliveHealth = 50;

        private bool IsConvulsing => IsAwake && _health.IsAlive;

        private float _seed;
        private int _pose;
        private int _shownPose = -1;
        private float _nextPose;
        private float _nextScream;

        public override void Spawned()
        {
            _seed = (Object.Id.Raw % 97) * 3.7f;
        }

        public override void FixedUpdateNetwork()
        {
            // A blow wakes a living body as surely as a hand reaching for its pockets.
            if (HasStateAuthority && !IsAwake && _health.IsAlive && _health.CurrentHealth < _health.MaxHealth)
                IsAwake = true;
        }

        public override void Render()
        {
            bool isConvulsing = IsConvulsing;
            float time = Time.time + _seed;

            if (isConvulsing && time >= _nextPose)
            {
                _pose = Random.Range(1, _poses.Length);
                _nextPose = time + Random.Range(0.07f, 0.22f);
            }

            ShowPose(isConvulsing ? _pose : 0);

            float jerk = isConvulsing ? 1f : 0f;
            _swing.localRotation = Quaternion.Euler(
                Mathf.Sin(time * 0.7f) * 2.5f + (Mathf.PerlinNoise(time * 6f, _seed) - 0.5f) * 28f * jerk,
                Mathf.Sin(time * 0.21f) * 30f + (Mathf.PerlinNoise(_seed, time * 4f) - 0.5f) * 50f * jerk,
                Mathf.Sin(time * 0.53f) * 2f + (Mathf.PerlinNoise(time * 7f, _seed + 5f) - 0.5f) * 28f * jerk);

            if (!isConvulsing)
            {
                if (_voice.isPlaying)
                    _voice.Stop();

                return;
            }

            if (_voice.isPlaying || time < _nextScream)
                return;

            _voice.clip = _screams[Random.Range(0, _screams.Length)];
            _voice.pitch = Random.Range(0.9f, 1.1f);
            _voice.Play();
            _nextScream = time + _voice.clip.length + Random.Range(0.15f, 0.9f);
        }

        /// State authority, at the start of a match: rolls whether this body is still alive.
        public void Arm(int seed)
        {
            IsAwake = false;

            if (new System.Random(seed ^ 0x5bd1e995).NextDouble() < _aliveChance)
                _health.SetMaxHealth(_aliveHealth, true);
            else
                _health.Kill();
        }

        /// State authority: a living body wakes instead of being looted.
        public bool TryWake()
        {
            if (IsAwake || _health.IsDead)
                return false;

            IsAwake = true;

            return true;
        }

        private void ShowPose(int pose)
        {
            if (pose == _shownPose)
                return;

            _shownPose = pose;

            for (int i = 0; i < _poses.Length; i++)
                _poses[i].SetActive(i == pose);
        }
    }
}
