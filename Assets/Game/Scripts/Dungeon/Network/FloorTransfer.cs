using Fusion;

namespace Game.Scripts.Dungeon
{
    /// The way down: every floor below the first is a dungeon session of its own, matched like a new run, so adventurers from
    /// different upper dungeons may meet there or not. The kit leaves the upper session saved like after an escape, the rest
    /// of the adventurer (health, spell charges, the run's kills and experience) rides along and is restored on arrival.
    public static class FloorTransfer
    {
        public sealed class Carry
        {
            public readonly byte Floor;
            public readonly int Health;
            public readonly int Kills;
            public readonly int Experience;
            public readonly byte[] Charges;

            public Carry(byte floor, int health, int kills, int experience, byte[] charges)
            {
                Floor = floor;
                Health = health;
                Kills = kills;
                Experience = experience;
                Charges = charges;
            }
        }

        private static Carry s_pending;

        public static void Descend(NetworkRunner runner, Carry carry)
        {
            s_pending = carry;
            NetworkLaunch.Set(DungeonTicket.Deeper(carry.Floor));
            SceneTravel.Load(runner, SceneTravel.DungeonScene, $"Floor {carry.Floor}");
        }

        /// The state to restore in the session just joined; taken once.
        public static Carry Take()
        {
            Carry carry = s_pending;
            s_pending = null;

            return carry;
        }

        /// No deeper session was reached: the adventurer is out with the saved kit, as if escaped.
        public static void Forget()
        {
            s_pending = null;
        }
    }
}
