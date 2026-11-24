namespace Game.Scripts.Battle
{
    /// A solid thing of the world that takes the melee blows landing on its colliders: a door that can be broken down.
    public interface IWorldHitReceiver
    {
        /// State authority only, once per blow. isHandle: it landed with the haft, not with the head or the blade.
        void OnWorldHit(WeaponConfig weapon, bool isHandle);
    }
}
