using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Shown after death or extraction until the player returns to the tavern.
    public sealed class ResultView : DisplayableView
    {
        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _detailsText;

        [SerializeField]
        private Button _returnButton;

        private PlayerSessionComponent _session;

        private void Awake()
        {
            _returnButton.onClick.AddListener(Return);
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;

            if (session == null)
                return;

            bool isExtracted = session.State == SessionState.Extracted;
            _titleText.text = isExtracted ? "ESCAPED" : "YOU DIED";
            _titleText.color = isExtracted ? new Color(0.4f, 0.8f, 1f) : new Color(0.9f, 0.2f, 0.15f);
            _detailsText.text = isExtracted
                ? $"Loot value {session.LastRunValue}g\nKills {session.LastRunKills}\nExperience +{session.LastRunExperience}"
                : $"Your body and everything on it stays in the dungeon.\nKills {session.LastRunKills}\nExperience +{session.LastRunExperience}";
        }

        private void Return()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);

            if (_session == null)
                return;

            _session.SaveLocal();
            SceneTravel.Load(_session.Runner, SceneTravel.LobbyScene, SceneTravel.LobbyTitle);
        }
    }
}
