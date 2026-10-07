using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Development stand-in for a server orchestrator: starts the dedicated server build (Tools/Game/Network/Build Dedicated Server)
    /// as a windowless batch-mode process for one dungeon session. Without the build the player hosts the dungeon itself.
    public static class LocalServer
    {
        public const string Folder = "Builds/DedicatedServer";
        public const string Executable = "DungeonServer.exe";

        /// The build next to the project; ParrelSync clones look next to the original project.
        public static string ExecutablePath
        {
            get
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
#if UNITY_EDITOR
                root = Regex.Replace(root, @"_clone_\d+$", string.Empty);
#endif

                return Path.Combine(root, Folder, Executable);
            }
        }

        public static bool TryLaunch(string session, QueueMode mode)
        {
            string path = ExecutablePath;

            if (!File.Exists(path))
                return false;

            string log = Path.Combine(Path.GetDirectoryName(path), $"{session}.log");
            Process.Start(new ProcessStartInfo(path, $"-batchmode -nographics -dedicatedServer -session {session} -mode {(int)mode} -logFile \"{log}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            UnityEngine.Debug.Log($"[{nameof(LocalServer)}] Started a dungeon server for '{session}', log: {log}");

            return true;
        }
    }
}
