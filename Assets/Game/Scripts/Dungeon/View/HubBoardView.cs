using System;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Two see-through windows over the hub: the party board (three full-height slots, you in the middle, an empty side slot
    /// invites a player) and the book of descents (map page on the left; queue, party, last run and Start on the right).
    /// The leader registers the party for the dungeon queue; everyone sees who is in the party.
    public sealed class HubBoardView : DisplayableView
    {
        [Serializable]
        private sealed class Destination
        {
            public string Title;
            public string Info;
            [Tooltip("Blue line under the info: when the portals open")]
            public string Portals;
            public Texture Picture;
            [Tooltip("Scene the Start button travels to")]
            public string Scene;
            [Tooltip("Dungeon Test: one random room without monsters, monsters called in from the F2 panel")]
            public bool IsTest;
        }

        /// A full-height slot of the party board: the member's doll with the name band, or a big "+" while it is open.
        [Serializable]
        private sealed class MemberSlot
        {
            public Image Back;
            public Image[] Border;
            public CharacterPreviewView Preview;
            public GameObject Band;
            public TMP_Text Name;
            public GameObject Leader;
            [Tooltip("Side slots only: the big + of an open slot")]
            public Button Invite;
        }

        /// A member chip of the book's party row.
        [Serializable]
        private sealed class MemberChip
        {
            public Image Back;
            public TMP_Text Text;
        }

        [Serializable]
        private sealed class QueueButton
        {
            public Button Button;
            public TMP_Text Label;
            public GameObject Mark;
        }

        private const int MeSlot = 1;

        [SerializeField]
        private DungeonContext _context;

        [SerializeField, Tooltip("Party board window: the slots, leave or join by code")]
        private GameObject _partyWindow;

        [SerializeField, Tooltip("Book of descents window: map, queue, start and the dev tools")]
        private GameObject _descentWindow;

        [SerializeField, Tooltip("Title of the party window, then of the descent one")]
        private TMP_Text[] _titleTexts;

        [SerializeField]
        private TMP_Text[] _rankTexts;

        [SerializeField, Tooltip("Left, you (middle), right")]
        private MemberSlot[] _slots;

        [SerializeField, Tooltip("You, left, right")]
        private MemberChip[] _chips;

        [SerializeField]
        private TMP_Text _partyLabelText;

        [SerializeField]
        private Destination[] _destinations;

        [SerializeField]
        private Button _prevMapButton;

        [SerializeField]
        private Button _nextMapButton;

        [SerializeField]
        private TMP_Text _mapTitleText;

        [SerializeField]
        private TMP_Text _mapInfoText;

        [SerializeField]
        private TMP_Text _mapPortalsText;

        [SerializeField]
        private RawImage _mapImage;

        [SerializeField, Tooltip("Solo, Duo, Trio")]
        private QueueButton[] _queueButtons;

        [SerializeField]
        private TMP_Text _queueNoteText;

        [SerializeField]
        private Button _startButton;

        [SerializeField]
        private Button _joinPartyButton;

        [SerializeField]
        private TMP_InputField _partyCodeInput;

        [SerializeField]
        private Button _leavePartyButton;

        [SerializeField]
        private TMP_Text[] _noticeTexts;

        [SerializeField]
        private float _noticeTime = 10f;

        [SerializeField]
        private Button _resetKitButton;

        [SerializeField]
        private Button _wipeButton;

        [SerializeField, Tooltip("Dev: lifts the character to the level cap")]
        private Button _maxLevelButton;

        [SerializeField]
        private Button _charactersButton;

        [SerializeField]
        private TMP_Text _resultText;

        private static QueueMode s_queue = QueueMode.Solo;
        private readonly PlayerSessionComponent[] _members = new PlayerSessionComponent[3];
        private readonly PlayerSessionComponent[] _shownMembers = new PlayerSessionComponent[3];
        private PlayerSessionComponent _session;
        private string _notice;
        private string _title;
        private float _noticeUntil;
        private int _destination;

        private void Awake()
        {
            _prevMapButton.onClick.AddListener(() => { Click(); ShowDestination((_destination + _destinations.Length - 1) % _destinations.Length); });
            _nextMapButton.onClick.AddListener(() => { Click(); ShowDestination((_destination + 1) % _destinations.Length); });
            _startButton.onClick.AddListener(StartRun);
            _resetKitButton.onClick.AddListener(() => { Click(); _session?.RpcResetKit(); });
            _wipeButton.onClick.AddListener(Wipe);
            _maxLevelButton.onClick.AddListener(() => { Click(); _session?.RpcMaxLevel(); });
            _charactersButton.onClick.AddListener(ToCharacters);
            _queueButtons[0].Button.onClick.AddListener(() => { Click(); s_queue = QueueMode.Solo; });
            _queueButtons[2].Button.onClick.AddListener(() => { Click(); s_queue = QueueMode.Trio; });
            // No Duo queue on the servers yet.
            _queueButtons[1].Button.interactable = false;
            _joinPartyButton.onClick.AddListener(JoinParty);
            _leavePartyButton.onClick.AddListener(LeaveParty);

            foreach (MemberSlot slot in _slots)
                if (slot.Invite != null)
                    slot.Invite.onClick.AddListener(Invite);

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
            RefreshParty();
            _titleTexts[0].text = PartyService.IsInParty ? $"{_title}  <size=60%><color=#9a9283>#{PartyService.Code}</color></size>" : _title;
            _titleTexts[1].text = _title;

            foreach (TMP_Text rank in _rankTexts)
                rank.text = $"Lv\n<size=170%>{_session.Level}</size>";
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;
            Array.Clear(_shownMembers, 0, _shownMembers.Length);

            if (session == null)
                return;

            bool hasRun = session.LastRunValue > 0 || session.LastRunKills > 0 || session.LastRunExperience > 0;
            _resultText.text = hasRun ? $"Loot {session.LastRunValue}g  ·  kills {session.LastRunKills}  ·  XP +{session.LastRunExperience}" : "<color=#6f6a5f>No runs yet</color>";
        }

        public void SetMode(bool isParty, string title)
        {
            _title = title;
            _partyWindow.SetActive(isParty);
            _descentWindow.SetActive(!isParty);
        }

        /// You stand in the middle, the others fill the left slot, then the right one. The host leads the party; a client takes
        /// the earliest joined of the others for it.
        private void RefreshParty()
        {
            NetworkRunner runner = _session.Runner;
            Array.Clear(_members, 0, _members.Length);
            _members[MeSlot] = _session;
            PlayerSessionComponent leader = runner.IsServer ? _session : null;
            int side = 0;

            foreach (PlayerRef player in runner.ActivePlayers.OrderBy(player => player.PlayerId))
            {
                if (!runner.TryGetPlayerObject(player, out NetworkObject playerObject) || !playerObject.TryGetComponent(out PlayerSessionComponent member) || member == _session)
                    continue;

                leader ??= member;

                if (side < 2)
                    _members[side++ * 2] = member;
            }

            for (int i = 0; i < _slots.Length; i++)
                ShowSlot(i, _members[i], PartyService.IsInParty && _members[i] == leader);

            for (int i = 0; i < _chips.Length; i++)
            {
                PlayerSessionComponent member = _members[i == 0 ? MeSlot : i == 1 ? 0 : 2];
                _chips[i].Back.color = new Color(0.02f, 0.02f, 0.02f, member != null ? 0.7f : 0.3f);
                _chips[i].Text.text = member != null ? MemberText(member, false) : "<color=#6f6a5f>+  Open</color>";
            }

            _partyLabelText.text = PartyService.IsInParty ? $"PARTY  #{PartyService.Code}" : "NO PARTY";
        }

        private void ShowSlot(int index, PlayerSessionComponent member, bool isLeader)
        {
            MemberSlot slot = _slots[index];
            bool isTaken = member != null;
            slot.Back.color = new Color(0.02f, 0.02f, 0.02f, isTaken ? 0.75f : 0.35f);
            slot.Preview.gameObject.SetActive(isTaken);
            slot.Band.SetActive(isTaken);
            slot.Leader.SetActive(isLeader);

            if (slot.Invite != null)
            {
                slot.Invite.gameObject.SetActive(!isTaken);

                foreach (Image edge in slot.Border)
                    edge.color = new Color(edge.color.r, edge.color.g, edge.color.b, isTaken ? 1f : 0.5f);
            }

            if (!isTaken)
            {
                _shownMembers[index] = null;

                return;
            }

            slot.Name.text = MemberText(member, index == MeSlot);

            if (_shownMembers[index] == member)
                return;

            _shownMembers[index] = member;
            slot.Preview.Bind(member.Kit, member.Class);
        }

        private string MemberText(PlayerSessionComponent member, bool hasProgress)
        {
            string text = $"<color=#{(member == _session ? "ffd98c" : "f2ede0")}>{member.DisplayName}</color>\n<size=75%><color=#c8e632>Lv. {member.Level}</color> {member.Class.DisplayName}</size>";

            return hasProgress
                ? text + $"\n<size=62%><color=#b9b29a>XP {member.Experience}/{_context.Config.ExperienceForLevel(member.Level)}</color>  ·  <color=#7fd4c8>{member.Kit.TotalValue()} Gear Value</color></size>"
                : text;
        }

        /// Only the leader starts a dungeon run; the solo queue takes nobody along.
        private void RefreshQueue()
        {
            NetworkRunner runner = _session.Runner;
            Destination destination = _destinations[_destination];
            bool isDungeon = destination.Scene == SceneTravel.DungeonScene;
            bool hasQueue = isDungeon && !destination.IsTest;
            bool isLeader = runner.IsServer;
            bool isPartyTooBig = Queue() == QueueMode.Solo && runner.ActivePlayers.Count() > 1;
            bool isInParty = PartyService.IsInParty;
            _startButton.interactable &= !isDungeon || isLeader && !isPartyTooBig;

            for (int i = 0; i < _queueButtons.Length; i++)
            {
                QueueButton button = _queueButtons[i];
                bool isOn = i == (s_queue == QueueMode.Solo ? 0 : 2);
                button.Button.gameObject.SetActive(hasQueue);
                button.Button.image.color = isOn ? new Color(0.3f, 0.24f, 0.14f, 0.97f) : new Color(0.12f, 0.1f, 0.08f, 0.95f);
                button.Label.color = isOn ? new Color(0.85f, 0.72f, 0.45f) : button.Button.interactable ? new Color(0.95f, 0.93f, 0.88f) : new Color(0.43f, 0.41f, 0.37f);
                button.Mark.SetActive(isOn);
            }

            _queueNoteText.text = destination.IsTest ? "Dungeon Test has a queue of its own · party of up to 3"
                : !hasQueue ? string.Empty : s_queue == QueueMode.Solo ? "Alone · a party cannot queue Solo" : "Party of up to 3 · open slots stay empty";
            _joinPartyButton.gameObject.SetActive(!isInParty);
            _partyCodeInput.gameObject.SetActive(!isInParty);
            _leavePartyButton.gameObject.SetActive(isInParty);

            string hint = !isDungeon ? null : !isLeader ? "The party leader starts the run" : isPartyTooBig ? "The Solo queue takes no party: switch to Trio or leave the party" : null;
            string notice = Time.time < _noticeUntil ? _notice : hint ?? string.Empty;

            foreach (TMP_Text text in _noticeTexts)
                text.text = notice;
        }

        private void Notify(string notice)
        {
            _notice = notice;
            _noticeUntil = string.IsNullOrEmpty(notice) ? 0f : Time.time + _noticeTime;
        }

        /// An open slot: outside a party it creates one, inside it tells the code the others join with.
        private void Invite()
        {
            Click();

            if (_session == null)
                return;

            if (PartyService.IsInParty)
                Notify($"Friends join with the code #{PartyService.Code}");
            else
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
            _mapPortalsText.text = destination.Portals;
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
                _session.RpcQueue(Queue());
            else
                SceneTravel.Load(_session.Runner, destination.Scene, destination.Title);
        }

        /// Dungeon Test has a queue of its own; the other dungeon maps take the chosen one.
        private QueueMode Queue()
        {
            return _destinations[_destination].IsTest ? QueueMode.Test : s_queue;
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
