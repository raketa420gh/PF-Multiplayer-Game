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
        [SerializeField]
        private AudioSource _audioSource;

        [SerializeField]
        private AudioClip _hitClip;

        [SerializeField]
        private AudioClip _blockClip;

        [SerializeField]
        private AudioClip _swingClip;

        [SerializeField]
        private AudioClip _shotClip;

        private TextMeshPro[] _popups;
        private float[] _popupTimes;
        private int _nextPopup;

        private void Awake()
        {
            _popups = new TextMeshPro[_popupCount];
            _popupTimes = new float[_popupCount];

            for (int i = 0; i < _popupCount; i++)
            {
                _popups[i] = Instantiate(_popupPrefab, transform);
                _popups[i].gameObject.SetActive(false);
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

        public void PlayHit(HitEventData hit)
        {
            bool isBlocked = hit.Result == HitResult.Blocked;
            ParticleSystem vfx = hit.Result == HitResult.Hit ? _hitVfx : _blockVfx;

            vfx.transform.SetPositionAndRotation(hit.Point,
                hit.Normal == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(hit.Normal));
            vfx.Play();

            PlayClip(hit.Result == HitResult.Hit ? _hitClip : _blockClip, hit.Point);
            ShowPopup(hit.Point, GetText(hit), isBlocked ? _blockColor : hit.Zone == HitZone.Head ? _headColor : _hitColor);
        }

        public void PlaySwing(Vector3 position)
        {
            PlayClip(_swingClip, position);
        }

        public void PlayShot(Vector3 position)
        {
            PlayClip(_shotClip, position);
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

        private void PlayClip(AudioClip clip, Vector3 position)
        {
            if (clip == null)
                return;

            _audioSource.transform.position = position;
            _audioSource.PlayOneShot(clip);
        }
    }
}
