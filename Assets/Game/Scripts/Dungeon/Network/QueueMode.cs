namespace Game.Scripts.Dungeon
{
    /// Dungeon queues: solo adventurers only, or teams of up to three (two may register together). Any = an idle dedicated
    /// server that takes the queue of its first player. Test plays by the Trio rules on a floor of one random room without monsters.
    public enum QueueMode : byte
    {
        Solo,
        Trio,
        Test,
        Any = 255
    }
}
