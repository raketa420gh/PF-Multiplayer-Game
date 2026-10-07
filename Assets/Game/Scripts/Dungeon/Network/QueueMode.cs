namespace Game.Scripts.Dungeon
{
    /// Dungeon queues: solo adventurers only, or teams of up to three (two may register together). Any = an idle dedicated
    /// server that takes the queue of its first player.
    public enum QueueMode : byte
    {
        Solo,
        Trio,
        Any = 255
    }
}
