using System;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Front page of the tavern: the character at the table, the party, the map and queue choice and the start button. The
    /// leader registers the party for the dungeon queue; a test ground run is everyone's own trip.
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
        private Button _queueButton;

        [SerializeField]
        private TMP_Text _queueText;

        [SerializeField]
        private Button _createPartyButton;

        [SerializeField]
        private Button _joinPartyButton;

        [SerializeField]
        private TMP_InputField _partyCodeInput;

        [SerializeField]
        private Button _leavePartyButton;

        [SerializeField]
        private TMP_Text _noticeText;

        [SerializeField]
        private float _noticeTime = 10f;

        [SerializeField]
        private Button _resetKitButton;

        [SerializeField]
        private Button _wipeButton;

        [SerializeField]
        private Button _charactersButton;

        [SerializeField]
        private GameObject _resultRoot;

        [SerializeField]
        private TMP_Text _resultText;

        private static QueueMode s_queue = QueueMode.Solo;
        private PlayerSessionComponent _session;
        private string _notice;
        private float _noticeUntil;
        private int _destination;
        private int _shownClass = -1;

        private void Awake()
        {
            _mapButton.onClick.AddListener(() => { Click(); ShowDestination((_destination + 1) % _destinations.Length); });
            _startButton.onClick.AddListener(StartRun);
            _resetKitButton.onClick.AddListener(() => { Click(); _session?.RpcResetKit(); });
            _wipeButton.onClick.AddListener(Wipe);
            _charactersButton.onClick.AddListener(ToCharacters);
            _queueButton.onClick.AddListener(() => { Click(); s_queue = s_queue == QueueMode.Solo ? QueueMode.Trio : QueueMode.Solo; });
            _createPartyButton.onClick.AddListener(CreateParty);
            _joinPartyButton.onClick.AddListener(JoinParty);
            _leavePartyButton.onClick.AddListener(LeaveParty);
            ShowDestination(0);
            Notify(NetworkLaunch.Notice);
            NetworkLaunch.Notice = null;
        }

        private void Update()
        {
            bool hasSession = _session != null && _session.Object != null && _session.Object.IsValid;
            _startButton.interactable = hasSession && _session.HasLoadedKit;

            if (!hasSession)
                return;

            RefreshQueue();

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

            _partyTitleText.text = PartyService.IsInParty ? $"Party  <color=#ffd98c>#{PartyService.Code}</color>" : "No Party";

            for (; slot < _partySlots.Length; slot++)
                _partySlots[slot].text = "<color=#6f6a5f><size=75%>Party Slot</size>\n- Open -</color>";
        }

        /// Only the leader starts a dungeon run; the solo queue takes nobody along.
        private void RefreshQueue()
        {
            NetworkRunner runner = _session.Runner;
            bool isDungeon = _destinations[_destination].Scene == SceneTravel.DungeonScene;
            bool isLeader = runner.IsServer;
            bool isPartyTooBig = s_queue == QueueMode.Solo && runner.ActivePlayers.Count() > 1;
            bool isInParty = PartyService.IsInParty;
            _startButton.interactable &= !isDungeon || isLeader && !isPartyTooBig;
            _queueButton.gameObject.SetActive(isDungeon);
            _queueText.text = $"Queue: <color=#ffd98c>{GameServer.Title(s_queue)}</color>  <size=75%><color=#9a9283>({(s_queue == QueueMode.Solo ? "alone" : "party of up to 3")})</color></size>";
            _createPartyButton.gameObject.SetActive(!isInParty);
            _joinPartyButton.gameObject.SetActive(!isInParty);
            _partyCodeInput.gameObject.SetActive(!isInParty);
            _leavePartyButton.gameObject.SetActive(isInParty);

            string hint = !isDungeon ? null : !isLeader ? "The party leader starts the run" : isPartyTooBig ? "The Solo queue takes no party: switch to Trio or leave the party" : null;
            _noticeText.text = Time.time < _noticeUntil ? _notice : hint ?? string.Empty;
        }

        private void Notify(string notice)
        {
            _notice = notice;
            _noticeUntil = string.IsNullOrEmpty(notice) ? 0f : Time.time + _noticeTime;
        }

        private void CreateParty()
        {
            Click();

            if (_session != null)
                PartyService.Create(_session.Runner);
        }

        private void JoinParty()
        {
            Click();

            if (!int.TryParse(_partyCodeInput.text, out int code) || code < 1000 || code > 9999)
            {
                Notify("Enter the four-digit code of the party");

                return;
            }

            if (_session != null)
                PartyService.Join(_session.Runner, code);
        }

        private void LeaveParty()
        {
            Click();

            if (_session != null)
                PartyService.Leave(_session.Runner);
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

            if (destination.Scene == SceneTravel.DungeonScene)
                _session.RpcQueue(s_queue);
            else
                SceneTravel.Load(_session.Runner, destination.Scene, destination.Title);
        }

        /// Back to the character select; the class of a character is fixed when it is created.
        private void ToCharacters()
        {
            Click();

            if (_session == null)
                return;

            _session.SaveLocal();
            SceneTravel.Load(_session.Runner, SceneTravel.CharacterSelectScene, SceneTravel.CharacterSelectTitle);
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
