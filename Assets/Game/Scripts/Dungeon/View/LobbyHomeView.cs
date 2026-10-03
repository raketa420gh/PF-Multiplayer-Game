using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Front page of the tavern: the character at the table, the party, the map choice and the start button.
    public sealed class LobbyHomeView : DisplayableView
    {
        [Serializable]
        private sealed class Destination
        {
            public string Title;
            public string Info;
            public Texture Picture;
            [Tooltip("Scene the Start button travels to")]
            public string Scene;
        }

        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private CharacterPreviewView _preview;

        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private TMP_Text _partyTitleText;

        [SerializeField]
        private TMP_Text[] _partySlots;

        [SerializeField]
        private Destination[] _destinations;

        [SerializeField]
        private Button _mapButton;

        [SerializeField]
        private TMP_Text _mapTitleText;

        [SerializeField]
        private TMP_Text _mapInfoText;

        [SerializeField]
        private RawImage _mapImage;

        [SerializeField]
        private Button _startButton;

        [SerializeField]
        private Button _resetKitButton;

        [SerializeField]
        private Button _wipeButton;

        [SerializeField]
        private Button _classButton;

        [SerializeField]
        private GameObject _resultRoot;

        [SerializeField]
        private TMP_Text _resultText;

        private PlayerSessionComponent _session;
        private int _destination;
        private int _shownClass = -1;

        private void Awake()
        {
            _mapButton.onClick.AddListener(() => { Click(); ShowDestination((_destination + 1) % _destinations.Length); });
            _startButton.onClick.AddListener(StartRun);
            _resetKitButton.onClick.AddListener(() => { Click(); _session?.RpcResetKit(); });
            _wipeButton.onClick.AddListener(Wipe);
            _classButton.onClick.AddListener(NextClass);
            ShowDestination(0);
        }

        private void Update()
        {
            bool hasSession = _session != null && _session.Object != null && _session.Object.IsValid;
            _startButton.interactable = hasSession && _session.HasLoadedKit;

            if (!hasSession)
                return;

            ClassConfig config = _session.Class;

            if (_shownClass != config.Id)
            {
                _shownClass = config.Id;
                _preview.Bind(_session.Kit, config);
            }

            int needed = _context.Config.ExperienceForLevel(_session.Level);
            _nameText.text = $"<size=65%><color=#b9b29a>• XP {_session.Experience}/{needed}</color>\n<color=#7fd4c8>• {_session.Kit.TotalValue()} Gear Value</color></size>\n" +
                $"<color=#c8e632>Lv. {_session.Level}</color> {_session.DisplayName}";
            RefreshParty();
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;
            _shownClass = -1;

            if (session == null)
                return;

            _resultRoot.SetActive(session.LastRunValue > 0 || session.LastRunKills > 0 || session.LastRunExperience > 0);
            _resultText.text = $"Last run:  loot {session.LastRunValue}g  ·  kills {session.LastRunKills}  ·  XP +{session.LastRunExperience}";
        }

        private void RefreshParty()
        {
            NetworkRunner runner = _session.Runner;
            int slot = 0;

            foreach (PlayerRef player in runner.ActivePlayers)
            {
                if (slot >= _partySlots.Length)
                    break;

                if (!runner.TryGetPlayerObject(player, out NetworkObject playerObject) || !playerObject.TryGetComponent(out PlayerSessionComponent member))
                    continue;

                string color = member == _session ? "ffd98c" : "f2ede0";
                _partySlots[slot++].text = $"<color=#{color}>{member.DisplayName}</color>\n<size=80%>{member.Level} {member.Class.DisplayName}</size>";
            }

            _partyTitleText.text = slot > 1 ? "Party" : "No Party";

            for (; slot < _partySlots.Length; slot++)
                _partySlots[slot].text = "<color=#6f6a5f><size=75%>Party Slot</size>\n- Open -</color>";
        }

        private void ShowDestination(int index)
        {
            Destination destination = _destinations[index];
            _destination = index;
            _mapTitleText.text = destination.Title;
            _mapInfoText.text = destination.Info;
            _mapImage.texture = destination.Picture;
        }

        private void StartRun()
        {
            Click();

            if (_session == null)
                return;

            Destination destination = _destinations[_destination];
            _session.SaveLocal();
            SceneTravel.Load(_session.Runner, destination.Scene, destination.Title);
        }

        private void NextClass()
        {
            Click();

            if (_session == null)
                return;

            ClassConfig[] classes = _context.Classes;
            int index = Array.IndexOf(classes, _session.Class);
            _session.RpcSelectClass(classes[(index + 1) % classes.Length].Id);
        }

        private void Wipe()
        {
            Click();
            StashService.Clear();
            _session?.RpcResetKit();
        }

        private static void Click()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
        }
    }
}
