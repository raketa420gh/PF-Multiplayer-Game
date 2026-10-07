using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Dungeon
{
    /// Role of this process and the rules of the dungeon sessions. A dedicated server is a build started with -dedicatedServer
    /// (batch mode, see LocalServer) or, in the editor, a ParrelSync clone whose argument contains "server"; it goes straight to
    /// the dungeon scene and hosts it without a player of its own. Every other process is a game client.
    public static class GameServer
    {
        public const string DungeonLobby = "dungeons";
        public const string PartyLobby = "parties";
        public const string ModeProperty = "mode";
        public const string UntilProperty = "until";
        public const string PartiesProperty = "parties";
        public const string FloorProperty = "floor";
        public const string ServerTitle = "Dedicated Server";
        /// Seconds after the first player arrives during which later registrations still join the same dungeon.
        public const int LateJoinWindow = 180;
        public const int PartyCapacity = 3;
        public const int MaxCapacity = 12;

        public static bool IsDedicated => s_isDedicated;
        /// Session an orchestrator started this server for; such a server quits when its dungeon is over.
        public static string AllocatedSession => s_session;
        public static QueueMode Mode => s_mode;
        /// Floor the server was allocated for; 0 = the floor of its first player.
        public static byte Floor => s_floor;
        /// PlayerPrefs prefix of the account: ParrelSync clones and -account builds keep their own characters.
        public static string Account => s_account;
        public static int Now => (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private static readonly bool s_isDedicated;
        private static readonly string s_session = string.Empty;
        private static readonly QueueMode s_mode = QueueMode.Any;
        private static readonly byte s_floor;
        private static readonly string s_account = string.Empty;

        static GameServer()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                string next = i + 1 < args.Length ? args[i + 1] : string.Empty;

                switch (args[i])
                {
                    case "-dedicatedServer":
                        s_isDedicated = true;
                        break;
                    case "-session":
                        s_session = next;
                        break;
                    case "-mode" when byte.TryParse(next, out byte mode):
                        s_mode = (QueueMode)mode;
                        break;
                    case "-floor" when byte.TryParse(next, out byte floor):
                        s_floor = floor;
                        break;
                    case "-account":
                        s_account = next + ".";
                        break;
                }
            }

#if UNITY_EDITOR
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Match clone = Regex.Match(Path.GetFileName(root), @"_clone_(\d+)$");
            string argument = Path.Combine(root, ".parrelsyncarg");

            if (clone.Success)
                s_account = $"clone{clone.Groups[1].Value}.";

            if (File.Exists(argument))
            {
                string text = File.ReadAllText(argument).ToLowerInvariant();
                s_isDedicated |= text.Contains("server");
                s_mode = text.Contains("trio") ? QueueMode.Trio : text.Contains("solo") ? QueueMode.Solo : s_mode;
            }
#endif
        }

        public static int Capacity(QueueMode mode)
        {
            return mode == QueueMode.Solo ? 8 : MaxCapacity;
        }

        public static string Title(QueueMode mode)
        {
            return mode == QueueMode.Solo ? "Solo" : "Trio";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!s_isDedicated)
                return;

            Application.targetFrameRate = 60;
            Debug.Log($"[{nameof(GameServer)}] Dedicated server, session '{s_session}', queue {s_mode}, floor {s_floor}");

            if (SceneManager.GetActiveScene().name != SceneTravel.DungeonScene)
                SceneTravel.Load(null, SceneTravel.DungeonScene, ServerTitle);
        }
    }
}
