namespace Game.Scripts.Dungeon
{
    /// Where a player turns up in the hub: at home (the host in the cell, a guest at the road gate), at the portal anchor
    /// after an escape or in the infirmary after a death.
    public enum HubArrival : byte
    {
        Home,
        Anchor,
        Infirmary
    }
}
