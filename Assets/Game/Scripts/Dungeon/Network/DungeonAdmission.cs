using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Server side of a dungeon session: the queue and teams of the registered players and the late-join window. The window opens
    /// with the first player; until it closes the session stays listed for the queue, after it nobody else gets in. A dedicated
    /// server whose closed dungeon has emptied quits (allocated) or opens a fresh one (long-lived). The session holds one floor:
    /// the first or a deeper one, which adventurers reach from the floor above.
    public sealed class DungeonAdmission : MonoBehaviour
    {
        public int Floor => _floor > 0 ? _floor : MatchComponent.EntryFloor;

        [SerializeField]
        private NetworkEvents _networkEvents;

        [SerializeField, Tooltip("An allocated server nobody comes to quits after this many seconds")]
        private float _idleTimeout = 120f;

        private readonly Dictionary<PlayerRef, int> _teams = new();
        private NetworkRunner _runner;
        private QueueMode _mode = QueueMode.Any;
        private string _parties = ",";
        private byte _floor = GameServer.Floor;
        private int _until;
        private float _startedAt;
        private bool _isClosed;
        private bool _isEnding;

        private void OnEnable()
        {
            _networkEvents.PlayerJoined.AddListener(OnPlayerJoined);
            _networkEvents.PlayerLeft.AddListener(OnPlayerLeft);
        }

        private void OnDisable()
        {
            _networkEvents.PlayerJoined.RemoveListener(OnPlayerJoined);
            _networkEvents.PlayerLeft.RemoveListener(OnPlayerLeft);
        }

        private void Start()
        {
            _startedAt = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (_runner == null && TryGetComponent(out NetworkRunner runner) && runner.IsRunning)
                Bind(runner);

            if (_runner == null || !_runner.IsRunning || !_runner.IsServer || _isEnding)
                return;

            if (_until > 0 && GameServer.Now >= _until && !_isClosed)
            {
                _isClosed = true;
                _runner.SessionInfo.IsOpen = false;
                Debug.Log($"[{nameof(DungeonAdmission)}] Late-join window closed with {_teams.Count} players");
            }

            if (!GameServer.IsDedicated)
                return;

            bool isOver = _isClosed && _teams.Count == 0;
            bool isAbandoned = _until == 0 && !string.IsNullOrEmpty(GameServer.AllocatedSession) && Time.realtimeSinceStartup - _startedAt > _idleTimeout;

            if (isOver || isAbandoned)
                End();
        }

        /// Seconds of the late-join window left as the session advertises it; negative when it is closed or not open yet.
        public static int WindowLeft(NetworkRunner runner)
        {
            return runner != null && runner.SessionInfo != null && runner.SessionInfo.IsValid && runner.SessionInfo.IsOpen
                && runner.SessionInfo.Properties.TryGetValue(GameServer.UntilProperty, out SessionProperty until) && until.PropertyValue is int value && value > 0
                ? value - GameServer.Now
                : -1;
        }

        /// Players of one party share a team; everybody else, solo queue included, is a team of one.
        public int TeamOf(PlayerRef player)
        {
            return _teams.TryGetValue(player, out int team) ? team : -1 - player.PlayerId;
        }

        private void Bind(NetworkRunner runner)
        {
            _runner = runner;

            if (!runner.IsServer || runner.SessionInfo.Properties == null)
                return;

            if (runner.SessionInfo.Properties.TryGetValue(GameServer.ModeProperty, out SessionProperty mode) && mode.PropertyValue is int value)
                _mode = (QueueMode)value;

            if (runner.SessionInfo.Properties.TryGetValue(GameServer.FloorProperty, out SessionProperty floor) && floor.PropertyValue is int number && number > 0)
                _floor = (byte)number;
        }

        private void End()
        {
            _isEnding = true;

            if (!string.IsNullOrEmpty(GameServer.AllocatedSession))
            {
                Debug.Log($"[{nameof(DungeonAdmission)}] Dungeon over, server quits");
                Application.Quit();

                return;
            }

            Debug.Log($"[{nameof(DungeonAdmission)}] Dungeon over, opening a fresh one");
            SceneTravel.Load(_runner, SceneTravel.DungeonScene, GameServer.ServerTitle);
        }

        private void Publish()
        {
            _runner.SessionInfo.UpdateCustomProperties(ServerLaunch.Properties(_mode, _until, _parties, _floor));
        }

        private void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
                return;

            if (_runner == null)
                Bind(runner);

            (QueueMode mode, int party, int _, byte floor) = DungeonTicket.Read(player == runner.LocalPlayer ? NetworkLaunch.LocalToken : runner.GetPlayerConnectionToken(player));
            _teams[player] = mode == QueueMode.Trio && party != 0 ? party : -1 - player.PlayerId;

            if (_mode == QueueMode.Any)
                _mode = mode;

            if (_floor == 0)
                _floor = floor;

            if (_until == 0)
                _until = GameServer.Now + GameServer.LateJoinWindow;

            if (party != 0 && !_parties.Contains($",{party},"))
                _parties += party + ",";

            Publish();
            Debug.Log($"[{nameof(DungeonAdmission)}] {player} joined the {_mode} dungeon, floor {Floor}, team {_teams[player]}, window {_until - GameServer.Now} s");
        }

        private void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            _teams.Remove(player);
        }
    }
}
