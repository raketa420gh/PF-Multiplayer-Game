using System.Threading.Tasks;
using Fusion;

namespace Game.Scripts.Dungeon
{
    /// Tavern session of a party (party-<code>, hidden). Creating or coming back from the dungeon takes whichever role is free,
    /// so the first member back hosts it; joining by code needs the party to exist.
    public sealed class PartyLaunch : LaunchPlan
    {
        public override string Scene => SceneTravel.LobbyScene;

        private readonly bool _isJoining;

        public PartyLaunch(bool isJoining)
        {
            _isJoining = isJoining;
        }

        public override Task<string> Start(NetworkRunner runner, NetworkEvents events, StartGameArgs args)
        {
            args.GameMode = _isJoining ? GameMode.Client : GameMode.AutoHostOrClient;
            args.SessionName = PartyService.SessionName;
            args.PlayerCount = GameServer.PartyCapacity;
            args.CustomLobbyName = GameServer.PartyLobby;
            args.IsVisible = false;

            return Run(runner, args);
        }

        public override void Fail(NetworkRunner runner, string error)
        {
            NetworkLaunch.Notice = $"Party {PartyService.Code} is full or gone ({error})";
            PartyService.Forget();
            base.Fail(runner, error);
        }
    }
}
