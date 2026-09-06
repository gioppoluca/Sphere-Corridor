using System;
using System.IO;
using System.Linq;
using SphereCorridor.Foundation;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SphereCorridor.Editor.Build
{
    /// <summary>
    /// Produces the repeatable local Windows development build required by M0.
    /// This is intentionally usable without Jenkins or another CI server.
    /// </summary>
    public static class WindowsDevelopmentBuild
    {
        private const string LogSubsystem = "Build";
        private const string OutputDirectory = "Builds/WindowsDevelopment";
        private const string ExecutablePath = OutputDirectory + "/SphereCorridor.exe";

        /// <summary>
        /// Validates the foundation and creates a debuggable Windows x64 player.
        /// </summary>
        [MenuItem("Tools/Sphere Corridor/M0/Build Windows Development")]
        public static void Build()
        {
            if (!M0ProjectSetup.ValidateFoundation(logSuccess: false))
            {
                AppLog.Error(LogSubsystem, "Build cancelled because M0 validation failed.");
                return;
            }

            Directory.CreateDirectory(OutputDirectory);

            string[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = ExecutablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            AppLog.Info(LogSubsystem, $"Starting Windows x64 development build at '{ExecutablePath}'.");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                AppLog.Error(
                    LogSubsystem,
                    $"Build failed with result {summary.result}, {summary.totalErrors} errors, " +
                    $"and {summary.totalWarnings} warnings.");
                throw new InvalidOperationException("Sphere Corridor Windows development build failed.");
            }

            AppLog.Info(
                LogSubsystem,
                $"Build succeeded in {summary.totalTime.TotalSeconds:F1}s; size {summary.totalSize / (1024f * 1024f):F1} MiB.");
        }
    }
}
