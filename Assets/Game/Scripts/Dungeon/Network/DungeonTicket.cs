using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Dungeon
{
    /// Registration for a dungeon queue and the matchmaking of the dungeon scene. The leader (or a solo player) joins the fullest
    /// open session of the queue that still has room for the whole party and whose late-join window is not over; without one
    /// a new dungeon server is allocated. Members wait until the server lists their party, then join the same session.
    /// Deeper floors are sessions of their own: whoever comes down joins the session of their party on that floor if there
    /// is one, otherwise the fullest open one of the floor.
    public sealed class DungeonTicket : LaunchPlan
    {
        public const int TokenSize = 7;

        public override string Scene => SceneTravel.DungeonScene;
        public QueueMode Mode => _mode;
        public int Party => _party;
        public byte Floor => _floor;

        private const float ListTimeout = 4f;
        private const float FollowTimeout = 90f;
        private const float ServerBootTimeout = 90f;
        private const int MaxAttempts = 3;
        /// A session whose window closes sooner than this is not worth the trip.
        private const int MinWindowLeft = 10;

        private static DungeonTicket s_current;

        private readonly QueueMode _mode;
        private readonly int _party;
        private readonly int _size;
        private readonly bool _isLeader;
        private readonly byte _floor;
        private readonly int _attempt;
        private List<SessionInfo> _sessions;
        private bool _isRetryable;

        public DungeonTicket(QueueMode mode, int party, int size, bool isLeader, byte floor = 1, int attempt = 0)
        {
            _mode = mode;
            _party = party;
            _size = Mathf.Max(1, size);
            _isLeader = isLeader;
            _floor = (byte)Mathf.Max(1, (int)floor);
            _attempt = attempt;
        }

        /// Ticket of one adventurer going down from the dungeon the last ticket led to: same queue and party, one more floor.
        public static DungeonTicket Deeper(byte floor)
        {
            return s_current != null ? new DungeonTicket(s_current._mode, s_current._party, 1, true, floor) : new DungeonTicket(QueueMode.Solo, 0, 1, true, floor);
        }

        public static (QueueMode mode, int party, int size, byte floor) Read(byte[] token)
        {
            return token == null || token.Length < TokenSize ? (QueueMode.Solo, 0, 1, (byte)1) : ((QueueMode)token[0], BitConverter.ToInt32(token, 2), token[1], token[6]);
        }

        public override async Task<string> Start(NetworkRunner runner, NetworkEvents events, StartGameArgs args)
        {
            UnityAction<NetworkRunner, List<SessionInfo>> listener = (_, sessions) => _sessions = sessions;
            s_current = this;
            events.OnSessionListUpdate.AddListener(listener);

            try
            {
                SceneTravel.Report(_floor > 1 ? $"Descending to floor {_floor}..." : $"Registering for the {GameServer.Title(_mode)} queue...", 0.2f);
                StartGameResult lobby = await runner.JoinSessionLobby(SessionLobby.Custom, GameServer.DungeonLobby);

                if (!lobby.Ok)
                    return lobby.ShutdownReason.ToString();

                args.ConnectionToken = Token();
                args.CustomLobbyName = GameServer.DungeonLobby;
                NetworkLaunch.LocalToken = args.ConnectionToken;
                SessionInfo session = _isLeader ? await Wait(FindOpen, ListTimeout, "Searching for a dungeon...") : await Wait(FindParty, FollowTimeout, "Waiting for the party leader...");

                if (session == null && !_isLeader)
                    return "no session of the party";

                if (session == null)
                {
                    string name = ServerLaunch.NewSessionName();

                    if (LocalServer.TryLaunch(name, _mode, _floor))
                        session = await Wait(list => list.Find(s => s.Name == name), ServerBootTimeout, "Starting a dungeon server...");
                    else
                        return await Host(runner, args, name);

                    if (session == null)
                        return "the dungeon server did not start";
                }

                SceneTravel.Report(WindowStatus(session), 0.7f);
                args.GameMode = GameMode.Client;
                args.SessionName = session.Name;

                StartGameResult result = await runner.StartGame(args);
                _isRetryable = result.ShutdownReason is ShutdownReason.GameIsFull or ShutdownReason.GameClosed;

                return result.Ok ? null : result.ShutdownReason.ToString();
            }
            finally
            {
                events.OnSessionListUpdate.RemoveListener(listener);
            }
        }

        /// A full or just closed session: look again, the queue may have moved on; after a few tries back to the tavern.
        public override void Fail(NetworkRunner runner, string error)
        {
            if (_attempt + 1 < MaxAttempts && _isRetryable)
            {
                NetworkLaunch.Set(new DungeonTicket(_mode, _party, _size, _isLeader, _floor, _attempt + 1));
                SceneTravel.Load(runner, SceneTravel.DungeonScene, SceneTravel.DungeonTitle);

                return;
            }

            if (_floor > 1)
            {
                FloorTransfer.Forget();
                NetworkLaunch.Notice = $"The way down to floor {_floor} collapsed ({error}). You made it out with everything you carried.";
            }

            NetworkLaunch.Notice ??= _isLeader ? $"No dungeon found: {error}" : "The party leader did not reach a dungeon";
            base.Fail(runner, error);
        }

        /// No dungeon server to allocate on this machine: the player hosts the dungeon itself, the same server code with a local player.
        private Task<string> Host(NetworkRunner runner, StartGameArgs args, string name)
        {
            SceneTravel.Report("Opening a new dungeon...", 0.7f);
            args.GameMode = GameMode.Host;
            args.SessionName = name;
            args.PlayerCount = GameServer.Capacity(_mode);
            args.SessionProperties = ServerLaunch.Properties(_mode, 0, ",", _floor);

            return Run(runner, args);
        }

        private SessionInfo FindOpen(List<SessionInfo> sessions)
        {
            SessionInfo best = null;

            foreach (SessionInfo session in sessions)
            {
                QueueMode mode = (QueueMode)Property(session, GameServer.ModeProperty);
                int until = Property(session, GameServer.UntilProperty);
                int floor = Property(session, GameServer.FloorProperty);
                // An idle server (queue Any, floor 0) takes the queue and the floor of its first player.
                bool isIdle = session.PlayerCount == 0 && (mode == QueueMode.Any || floor == 0);
                bool isQueue = (mode == _mode || mode == QueueMode.Any) && (floor == _floor || floor == 0) && (isIdle || mode == _mode && floor == _floor);
                bool isOpen = session.IsOpen && (until == 0 || until - GameServer.Now >= MinWindowLeft);

                if (!isQueue || !isOpen || session.PlayerCount + _size > GameServer.Capacity(_mode))
                    continue;

                // Down below, teammates who came first hold the place for the rest of the party.
                if (_floor > 1 && _party != 0 && IsListed(session, _party))
                    return session;

                if (best == null || session.PlayerCount > best.PlayerCount)
                    best = session;
            }

            return best;
        }

        private SessionInfo FindParty(List<SessionInfo> sessions)
        {
            return sessions.Find(s => Property(s, GameServer.FloorProperty) <= 1 && IsListed(s, _party));
        }

        private static bool IsListed(SessionInfo session, int party)
        {
            return session.Properties.TryGetValue(GameServer.PartiesProperty, out SessionProperty parties) && parties.PropertyValue is string list
                && list.Contains($",{party},");
        }

        private async Task<SessionInfo> Wait(Func<List<SessionInfo>, SessionInfo> find, float timeout, string status)
        {
            SceneTravel.Report(status, 0.4f);
            float end = Time.realtimeSinceStartup + timeout;

            while (Time.realtimeSinceStartup < end)
            {
                SessionInfo session = _sessions != null ? find(_sessions) : null;

                if (session != null)
                    return session;

                await Task.Delay(250);
            }

            return null;
        }

        private byte[] Token()
        {
            byte[] token = new byte[TokenSize];
            token[0] = (byte)_mode;
            token[1] = (byte)_size;
            BitConverter.GetBytes(_party).CopyTo(token, 2);
            token[6] = _floor;

            return token;
        }

        private static string WindowStatus(SessionInfo session)
        {
            int until = Property(session, GameServer.UntilProperty);

            return until == 0 ? "Entering a new dungeon..." : $"Joining a dungeon in progress ({session.PlayerCount} inside, late entry closes in {until - GameServer.Now} s)...";
        }

        private static int Property(SessionInfo session, string key)
        {
            return session.Properties.TryGetValue(key, out SessionProperty property) && property.PropertyValue is int value ? value : 0;
        }
    }
}
