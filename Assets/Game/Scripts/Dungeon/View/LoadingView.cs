using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Full-screen cover of a scene change: shown from the start of the trip until the arrival scene is ready to play.
    public sealed class LoadingView : DisplayableView
    {
        [SerializeField, Tooltip("Name of the scene this view belongs to, shown while it starts up")]
        private string _sceneTitle;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _statusText;

        [SerializeField]
        private Image _progressFill;

        [SerializeField, Tooltip("Beam of the loading scales, turned about its middle")]
        private RectTransform _beam;

        [SerializeField, Tooltip("Pans hanging from the beam ends: the bone on the left, the coins on the right")]
        private RectTransform[] _pans;

        [SerializeField]
        private float _tilt = 16f;

        [SerializeField, Tooltip("Half the beam length: the pans hang at its ends")]
        private float _arm = 40f;

        [SerializeField]
        private float _period = 2.6f;

        private void Awake()
        {
            SceneTravel.OnProgress += OnProgress;
            _titleText.text = _sceneTitle;
        }

        private void OnDestroy()
        {
            SceneTravel.OnProgress -= OnProgress;
        }

        private void Update()
        {
            // Holds a side for 12% of the period, then tips over smoothly to the other one.
            float t = Time.unscaledTime / _period % 1f;
            float swing = t < 0.5f ? Mathf.SmoothStep(0f, 1f, (t - 0.12f) / 0.38f) : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.62f) / 0.38f);
            float angle = Mathf.Lerp(_tilt, -_tilt, swing);
            Vector2 end = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * _arm;
            _beam.localEulerAngles = new Vector3(0f, 0f, angle);
            _pans[0].anchoredPosition = _beam.anchoredPosition - end;
            _pans[1].anchoredPosition = _beam.anchoredPosition + end;
        }

        public void SetStatus(string status, float progress)
        {
            _statusText.text = status;
            _progressFill.fillAmount = progress;
        }

        private void OnProgress(string title, string status, float progress)
        {
            Show();
            _titleText.text = title;
            SetStatus(status, progress);
        }
    }
}
