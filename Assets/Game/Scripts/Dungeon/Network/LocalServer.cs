using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public static bool TryLaunch(string session, QueueMode mode, byte floor)
        {
            string path = ExecutablePath;

            if (!File.Exists(path) || IsStale(path))
                return false;

            string log = Path.Combine(Path.GetDirectoryName(path), $"{session}.log");
            Process.Start(new ProcessStartInfo(path, $"-batchmode -nographics -dedicatedServer -session {session} -mode {(int)mode} -floor {floor} -logFile \"{log}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            UnityEngine.Debug.Log($"[{nameof(LocalServer)}] Started a dungeon server for '{session}', log: {log}");

            return true;
        }

        /// A build older than the code, the scenes or the prefabs has other scene NetworkObjects (Fusion "Behaviour count mismatch")
        /// or other rooms than the clients walk through: host instead. A build may leave the exe as it was, its code is always new.
        private static bool IsStale(string path)
        {
#if UNITY_EDITOR
            DateTime built = File.GetLastWriteTime(Path.Combine(Path.ChangeExtension(path, null) + "_Data", "Managed", "Assembly-CSharp.dll"));
            string stale = UnityEditor.EditorBuildSettings.scenes.Select(scene => scene.path)
                .Append("Library/ScriptAssemblies/Assembly-CSharp.dll")
                .Concat(Directory.EnumerateFiles("Assets/Game/Prefabs/Dungeon", "*.prefab", SearchOption.AllDirectories))
                .FirstOrDefault(file => File.Exists(file) && File.GetLastWriteTime(file) > built);

            if (stale == null)
                return false;

            UnityEngine.Debug.LogWarning($"[{nameof(LocalServer)}] The dedicated server build is older than {stale}, hosting instead. Rebuild: Tools/Game/Network/Build Dedicated Server");

            return true;
#else
            return false;
#endif
        }
    }
}
