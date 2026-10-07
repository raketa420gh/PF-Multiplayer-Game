namespace Game.Scripts.Dungeon
{
    /// Hand-over of the launch plan between scenes. Without an explicit plan a dedicated server hosts the dungeon and a party
    /// member re-enters the party session of the tavern; everything else starts with the scene's own defaults.
    public static class NetworkLaunch
    {
        /// Message for the tavern about the last failed trip, shown once.
        public static string Notice;
        /// Connection token of the local player, for a host that reads its own ticket.
        public static byte[] LocalToken;

        private static LaunchPlan s_next;

        public static void Set(LaunchPlan plan)
        {
            s_next = plan;
        }

        public static LaunchPlan Take(string scene)
        {
            LaunchPlan plan = s_next != null && s_next.Scene == scene ? s_next : null;

            if (plan != null)
                s_next = null;
            else if (scene == SceneTravel.DungeonScene && GameServer.IsDedicated)
                plan = new ServerLaunch();
            else if (scene == SceneTravel.LobbyScene && PartyService.IsInParty)
                plan = new PartyLaunch(false);

            return plan;
        }
    }
}
