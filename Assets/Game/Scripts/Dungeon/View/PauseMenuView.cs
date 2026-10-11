using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Escape menu of a gameplay scene: resume, or the settings page with the master volume. The match keeps running.
    public sealed class PauseMenuView : DisplayableView
    {
        public event Action OnResumeClicked;

        [SerializeField]
        private GameObject _mainPage;

        [SerializeField]
        private GameObject _settingsPage;

        [SerializeField]
        private Button _resumeButton;

        [SerializeField]
        private Button _settingsButton;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private Slider _volumeSlider;

        [SerializeField]
        private TMP_Text _volumeText;

        private void Awake()
        {
            _resumeButton.onClick.AddListener(OnResume);
            _settingsButton.onClick.AddListener(OnSettings);
            _backButton.onClick.AddListener(OnBack);
            _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        public override void Show()
        {
            base.Show();
            SetSettingsShown(false);
            _volumeSlider.SetValueWithoutNotify(VolumeSettings.Volume);
            ShowVolume();
        }

        public override void Hide()
        {
            base.Hide();
            PlayerPrefs.Save();
        }

        private void SetSettingsShown(bool isShown)
        {
            _mainPage.SetActive(!isShown);
            _settingsPage.SetActive(isShown);
        }

        private void ShowVolume()
        {
            _volumeText.text = $"Volume {Mathf.RoundToInt(VolumeSettings.Volume * 100f)}%";
        }

        private void OnResume()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
            OnResumeClicked?.Invoke();
        }

        private void OnSettings()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
            SetSettingsShown(true);
        }

        private void OnBack()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
            SetSettingsShown(false);
        }

        private void OnVolumeChanged(float volume)
        {
            VolumeSettings.SetVolume(volume);
            ShowVolume();
        }
    }
}
