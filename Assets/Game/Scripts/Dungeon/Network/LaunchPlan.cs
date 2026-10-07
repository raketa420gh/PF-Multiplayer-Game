using System.Threading.Tasks;
using Fusion;

namespace Game.Scripts.Dungeon
{
    /// How the runner of a scene starts its session: set before travelling, taken by the scene's bootstrapper (NetworkLaunch).
    public abstract class LaunchPlan
    {
        public abstract string Scene { get; }

        /// Fills in or replaces the scene's default arguments and starts the runner; null on success, otherwise what went wrong.
        public abstract Task<string> Start(NetworkRunner runner, NetworkEvents events, StartGameArgs args);

        /// The session could not be started: back to the tavern with a notice.
        public virtual void Fail(NetworkRunner runner, string error)
        {
            NetworkLaunch.Notice ??= $"Connection failed: {error}";
            SceneTravel.Load(runner, SceneTravel.LobbyScene, SceneTravel.LobbyTitle);
        }

        protected static async Task<string> Run(NetworkRunner runner, StartGameArgs args)
        {
            StartGameResult result = await runner.StartGame(args);

            return result.Ok ? null : result.ShutdownReason.ToString();
        }
    }
}
