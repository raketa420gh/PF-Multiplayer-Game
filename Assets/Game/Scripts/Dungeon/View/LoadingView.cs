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

        [SerializeField]
        private RectTransform _spinner;

        [SerializeField]
        private float _spinSpeed = 200f;

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
            _spinner.Rotate(0f, 0f, -_spinSpeed * Time.unscaledDeltaTime);
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
