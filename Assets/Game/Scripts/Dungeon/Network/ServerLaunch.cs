using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;

namespace Game.Scripts.Dungeon
{
    /// Dedicated server: hosts one dungeon session in the dungeon lobby, idle (queue Any) until its first player claims it.
    public sealed class ServerLaunch : LaunchPlan
    {
        public override string Scene => SceneTravel.DungeonScene;

        public override Task<string> Start(NetworkRunner runner, NetworkEvents events, StartGameArgs args)
        {
            args.GameMode = GameMode.Server;
            args.SessionName = string.IsNullOrEmpty(GameServer.AllocatedSession) ? NewSessionName() : GameServer.AllocatedSession;
            args.PlayerCount = GameServer.MaxCapacity;
            args.CustomLobbyName = GameServer.DungeonLobby;
            args.SessionProperties = Properties(GameServer.Mode, 0, ",");

            return Run(runner, args);
        }

        public override void Fail(NetworkRunner runner, string error)
        {
            UnityEngine.Debug.LogError($"[{nameof(ServerLaunch)}] Server failed to start: {error}");
            UnityEngine.Application.Quit(1);
        }

        public static string NewSessionName()
        {
            return "dungeon-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public static Dictionary<string, SessionProperty> Properties(QueueMode mode, int until, string parties)
        {
            return new Dictionary<string, SessionProperty>
            {
                [GameServer.ModeProperty] = (int)mode,
                [GameServer.UntilProperty] = until,
                [GameServer.PartiesProperty] = parties
            };
        }
    }
}
