using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Game.Scripts.Editor.Battle;
using Game.Scripts.Editor.Dungeon;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Game.Scripts.Editor
{
    [InitializeOnLoad]
    public static class RebuildVerify
    {
        private const int MaxErrors = 5;
        private const int MaxLength = 200;
        private static readonly string[] Scenes =
        {
            "Assets/Game/Scenes/BattleScene.unity", "Assets/Game/Scenes/DungeonScene.unity",
            "Assets/Game/Scenes/LobbyScene.unity", "Assets/Game/Scenes/CharacterSelectScene.unity"
        };
        private static readonly string[] PrefabFolders = { "Assets/Game/Prefabs" };
        private static readonly Regex GuidRegex = new Regex(@"guid: ([0-9a-f]{32})", RegexOptions.Compiled);
        private static readonly List<string> CompileErrors = new List<string>();

        static RebuildVerify()
        {
            CompilationPipeline.compilationStarted += _ => CompileErrors.Clear();
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;
        }

        [MenuItem("Tools/Rebuild & Verify/Verify Only")]
        public static void VerifyMenu() => Debug.Log(Run("Verify"));

        [MenuItem("Tools/Rebuild & Verify/Battle")]
        public static void BattleMenu() => Debug.Log(Run("Battle"));

        [MenuItem("Tools/Rebuild & Verify/Dungeon")]
        public static void DungeonMenu() => Debug.Log(Run("Dungeon"));

        [MenuItem("Tools/Rebuild & Verify/All")]
        public static void AllMenu() => Debug.Log(Run("All"));

        public static string Run(string mode = "Verify")
        {
            List<string> errors = new List<string>();
            Application.LogCallback onLog = (message, stack, type) =>
            {
                if (type != LogType.Log && type != LogType.Warning)
                    errors.Add($"{type}: {message}");
            };
            DateTime start = DateTime.Now;
            Application.logMessageReceived += onLog;

            try
            {
                Build(mode);
            }
            catch (Exception exception)
            {
                errors.Add($"Build threw {exception.GetType().Name}: {exception.Message}");
            }
            finally
            {
                Application.logMessageReceived -= onLog;
            }

            AssetDatabase.SaveAssets();
            List<string> missing = FindMissingReferences();
            bool compileFailed = EditorUtility.scriptCompilationFailed || CompileErrors.Count > 0;
            List<string> unique = CompileErrors.Concat(errors).Distinct().ToList();
            bool ok = !compileFailed && unique.Count == 0 && missing.Count == 0;
            StringBuilder report = new StringBuilder();
            report.AppendLine($"{(ok ? "OK" : "FAIL")} [{mode}] {(DateTime.Now - start).TotalSeconds:F1}s");
            report.AppendLine($"compile: {(compileFailed ? $"FAILED ({CompileErrors.Count})" : "ok")} | logged errors: {errors.Count} | missing refs: {missing.Count}");

            foreach (string error in unique.Take(MaxErrors))
                report.AppendLine("- " + Truncate(error));

            foreach (string file in missing.Take(MaxErrors))
                report.AppendLine("~ " + file);

            return report.ToString().TrimEnd();
        }

        private static void Build(string mode)
        {
            switch (mode)
            {
                case "Battle":
                    BattleBuildMenu.BuildAnimationsAndContent();
                    break;
                case "Dungeon":
                    DungeonBuildMenu.BuildContent();
                    break;
                case "All":
                    DungeonBuildMenu.BuildAll();
                    break;
                case "Verify":
                    break;
                default:
                    throw new ArgumentException($"Unknown mode '{mode}' (Verify/Battle/Dungeon/All)");
            }
        }

        private static List<string> FindMissingReferences()
        {
            IEnumerable<string> prefabs = AssetDatabase.FindAssets("t:Prefab", PrefabFolders).Select(AssetDatabase.GUIDToAssetPath);
            List<string> result = new List<string>();

            foreach (string path in Scenes.Concat(prefabs).Where(File.Exists))
            {
                int count = GuidRegex.Matches(File.ReadAllText(path)).Cast<Match>().Select(match => match.Groups[1].Value)
                    .Distinct().Count(guid => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)));

                if (count > 0)
                    result.Add($"{path} ({count} missing guid)");
            }

            return result;
        }

        private static string Truncate(string text)
        {
            string line = text.Replace('\n', ' ').Replace('\r', ' ');

            return line.Length <= MaxLength ? line : line.Substring(0, MaxLength) + "...";
        }

        private static void OnAssemblyCompiled(string assembly, CompilerMessage[] messages) =>
            CompileErrors.AddRange(messages.Where(message => message.type == CompilerMessageType.Error).Select(message => message.message));
    }
}
