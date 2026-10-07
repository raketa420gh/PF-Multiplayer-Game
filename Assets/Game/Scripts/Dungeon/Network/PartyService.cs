using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// The local player's party between scenes. A party is a small hidden session of the tavern known by its four-digit code;
    /// its host is the leader who registers everyone for a dungeon queue, the members follow with the same ticket.
    public static class PartyService
    {
        public static int Code => s_code;
        public static bool IsInParty => s_code != 0;
        public static string SessionName => "party-" + s_code;

        private static int s_code;

        public static void Create(NetworkRunner runner)
        {
            s_code = Random.Range(1000, 10000);
            Travel(runner, new PartyLaunch(false));
        }

        public static void Join(NetworkRunner runner, int code)
        {
            s_code = code;
            Travel(runner, new PartyLaunch(true));
        }

        public static void Leave(NetworkRunner runner)
        {
            s_code = 0;
            Travel(runner, null);
        }

        public static void Forget()
        {
            s_code = 0;
        }

        /// Everyone of the party goes to the dungeon scene with the same ticket; the leader waits a moment so the order
        /// reaches the members before its party session closes.
        public static async void Depart(NetworkRunner runner, QueueMode mode, int size, bool isLeader)
        {
            DungeonContext.Instance?.LocalSession?.SaveLocal();
            int party = mode == QueueMode.Solo ? 0 : s_code;
            NetworkLaunch.Set(new DungeonTicket(mode, party, size, isLeader || party == 0));

            if (isLeader && size > 1)
                await Task.Delay(1000);

            SceneTravel.Load(runner, SceneTravel.DungeonScene, SceneTravel.DungeonTitle);
        }

        private static void Travel(NetworkRunner runner, LaunchPlan plan)
        {
            DungeonContext.Instance?.LocalSession?.SaveLocal();
            NetworkLaunch.Set(plan);
            SceneTravel.Load(runner, SceneTravel.LobbyScene, SceneTravel.LobbyTitle);
        }
    }
}
