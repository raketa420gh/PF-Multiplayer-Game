using System.IO;
using System.Linq;
using Game.Scripts.Dungeon;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Multiplayer tooling: the dedicated server build that LocalServer starts for every new dungeon.
    internal static class NetworkBuildMenu
    {
        private const string PlaytestFolder = "Builds/Playtest";

        [MenuItem("Tools/Game/Network/Build Dedicated Server")]
        public static void BuildDedicatedServer()
        {
            Build(Path.GetFullPath(Path.Combine(Application.dataPath, "..", LocalServer.Folder, LocalServer.Executable)), BuildOptions.None);
        }

        /// A client to send to testers: the dedicated server sits inside it where LocalServer looks in a player, all zipped next to the folder.
        [MenuItem("Tools/Game/Network/Build Playtest")]
        public static void BuildPlaytest()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", PlaytestFolder));

            if (Directory.Exists(root))
                Directory.Delete(root, true);

            if (!Build(Path.Combine(root, LocalServer.Folder, LocalServer.Executable), BuildOptions.None)
                || !Build(Path.Combine(root, $"{PlayerSettings.productName}.exe"), BuildOptions.Development))
                return;

            string zip = $"{root}.zip";
            File.Delete(zip);
            System.IO.Compression.ZipFile.CreateFromDirectory(root, zip);
            Debug.Log($"[{nameof(NetworkBuildMenu)}] Playtest ready: {zip}");
            EditorUtility.RevealInFinder(zip);
        }

        [MenuItem("Tools/Game/Network/Open Server Logs")]
        public static void OpenServerLogs()
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(Path.Combine(Application.dataPath, "..", LocalServer.Folder)));
        }

        private static bool Build(string path, BuildOptions options)
        {
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = options
            });
            Debug.Log($"[{nameof(NetworkBuildMenu)}] Build {report.summary.result}: {path} ({report.summary.totalTime})");

            return report.summary.result == BuildResult.Succeeded;
        }
    }
}
