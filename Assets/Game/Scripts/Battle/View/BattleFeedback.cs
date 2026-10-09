using System;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class BattleFeedback : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem _hitVfx;

        [SerializeField]
        private ParticleSystem _blockVfx;

        [SerializeField]
        private TextMeshPro _popupPrefab;

        [SerializeField]
        private int _popupCount = 12;

        [SerializeField]
        private float _popupLifetime = 1f;

        [SerializeField]
        private float _popupRiseSpeed = 0.8f;

        [SerializeField]
        private Color _hitColor = Color.white;

        [SerializeField]
        private Color _headColor = new(1f, 0.8f, 0.2f);

        [SerializeField]
        private Color _blockColor = new(0.5f, 0.8f, 1f);

        [Header("Audio")]
        [SerializeField, Tooltip("Template of the voices: its settings are copied to every voice of the pool")]
        private AudioSource _audioSource;

        [SerializeField]
        private AudioClip[] _hitClips;

        [SerializeField]
        private AudioClip[] _blockClips;

        [SerializeField]
        private AudioClip[] _worldClips;

        [SerializeField]
        private AudioClip[] _swingClips;

        [SerializeField]
        private AudioClip[] _shotClips;

        [SerializeField, Tooltip("Recorded weapon sets by id; a blow without a set falls back to the clips above")]
        private WeaponSoundConfig[] _weaponSounds = Array.Empty<WeaponSoundConfig>();

        [SerializeField]
        private int _voiceCount = 8;

        [SerializeField]
        private float _pitchSpread = 0.06f;

        [SerializeField, Tooltip("Swing pitch of the shortest and of the longest weapon")]
        private Vector2 _swingPitch = new(1.25f, 0.8f);

        [SerializeField, Tooltip("Weapon reach that maps onto the swing pitch range")]
        private Vector2 _swingReach = new(1f, 2f);

        private TextMeshPro[] _popups;
        private float[] _popupTimes;
        private int _nextPopup;
        private AudioSource[] _voices;
        private int _nextVoice;

        private void Awake()
        {
            _popups = new TextMeshPro[_popupCount];
            _popupTimes = new float[_popupCount];

            for (int i = 0; i < _popupCount; i++)
            {
                _popups[i] = Instantiate(_popupPrefab, transform);
                _popups[i].gameObject.SetActive(false);
            }

            _voices = new AudioSource[_voiceCount];

            for (int i = 0; i < _voiceCount; i++)
            {
                AudioSource voice = new GameObject("Voice" + i).AddComponent<AudioSource>();
                voice.transform.SetParent(transform, false);
                voice.playOnAwake = false;
                voice.volume = _audioSource.volume;
                voice.spatialBlend = _audioSource.spatialBlend;
                voice.rolloffMode = _audioSource.rolloffMode;
                voice.minDistance = _audioSource.minDistance;
                voice.maxDistance = _audioSource.maxDistance;
                voice.dopplerLevel = 0f;
                _voices[i] = voice;
            }
        }

        private void LateUpdate()
        {
            Quaternion rotation = BattleContext.Instance.Camera.transform.rotation;

            for (int i = 0; i < _popupCount; i++)
            {
                TextMeshPro popup = _popups[i];

                if (!popup.gameObject.activeSelf)
                    continue;

                _popupTimes[i] += Time.deltaTime;
                popup.transform.position += Vector3.up * (_popupRiseSpeed * Time.deltaTime);
                popup.transform.rotation = rotation;
                popup.alpha = 1f - Mathf.Clamp01(_popupTimes[i] / _popupLifetime);

                if (_popupTimes[i] >= _popupLifetime)
                    popup.gameObject.SetActive(false);
            }
        }

        public void PlayHit(HitEventData hit, ImpactSurface surface)
        {
            ParticleSystem vfx = hit.Result == HitResult.Hit ? _hitVfx : _blockVfx;

            vfx.transform.SetPositionAndRotation(hit.Point,
                hit.Normal == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(hit.Normal));
            vfx.Play();

            WeaponSoundConfig sounds = hit.Sound > 0 && hit.Sound <= _weaponSounds.Length ? _weaponSounds[hit.Sound - 1] : null;
            PlayClip(GetClips(sounds, surface) ?? (hit.Result == HitResult.Hit ? _hitClips : _blockClips), hit.Point);
        }

        public void PlayWorldHit(Vector3 point, Vector3 normal, WeaponSoundConfig sounds, ImpactSurface surface)
        {
            _blockVfx.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
            _blockVfx.Play();
            PlayClip(GetClips(sounds, surface) ?? _worldClips, point);
        }

        /// Recorded swings carry their own weight; the synthesized fallback drops in pitch with reach, as long weapons move more air.
        public void PlaySwing(Vector3 position, float reach, WeaponSoundConfig sounds)
        {
            if (sounds != null && sounds.Swing.Length > 0)
            {
                PlayClip(sounds.Swing, position);

                return;
            }

            PlayClip(_swingClips, position, Mathf.Lerp(_swingPitch.x, _swingPitch.y, Mathf.InverseLerp(_swingReach.x, _swingReach.y, reach)));
        }

        public void PlayShot(Vector3 position)
        {
            PlayClip(_shotClips, position);
        }

        private static AudioClip[] GetClips(WeaponSoundConfig sounds, ImpactSurface surface)
        {
            AudioClip[] clips = sounds != null ? sounds.Get(surface) : null;

            return clips != null && clips.Length > 0 ? clips : null;
        }

        private static string GetText(HitEventData hit)
        {
            return hit.Result switch
            {
                HitResult.Blocked => hit.Damage > 0 ? $"BLOCK {hit.Damage}" : "BLOCK",
                HitResult.PartialBlock => $"PARTIAL {hit.Damage}",
                _ => hit.Zone == HitZone.Head ? $"HEAD {hit.Damage}" : hit.Damage.ToString()
            };
        }

        private void ShowPopup(Vector3 position, string text, Color color)
        {
            TextMeshPro popup = _popups[_nextPopup];
            _popupTimes[_nextPopup] = 0f;
            _nextPopup = (_nextPopup + 1) % _popupCount;

            popup.transform.position = position + Vector3.up * 0.25f;
            popup.text = text;
            popup.color = color;
            popup.gameObject.SetActive(true);
        }

        private void PlayClip(AudioClip[] clips, Vector3 position, float pitch = 1f)
        {
            if (clips.Length == 0)
                return;

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.transform.position = position;
            voice.pitch = pitch * UnityEngine.Random.Range(1f - _pitchSpread, 1f + _pitchSpread);
            voice.clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            voice.Play();
        }
    }
}
