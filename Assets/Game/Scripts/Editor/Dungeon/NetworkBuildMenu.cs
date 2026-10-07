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
        [MenuItem("Tools/Game/Network/Build Dedicated Server")]
        public static void BuildDedicatedServer()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", LocalServer.Folder, LocalServer.Executable));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            });
            Debug.Log($"[{nameof(NetworkBuildMenu)}] Dedicated server build {report.summary.result}: {path} ({report.summary.totalTime})");
        }

        [MenuItem("Tools/Game/Network/Open Server Logs")]
        public static void OpenServerLogs()
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(Path.Combine(Application.dataPath, "..", LocalServer.Folder)));
        }
    }
}
