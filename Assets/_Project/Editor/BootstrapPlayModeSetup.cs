using System.Collections.Generic;
using SphereCorridor.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SphereCorridor.Editor
{
    /// <summary>
    /// Makes Editor Play behave like a standalone build: execution always enters
    /// through Bootstrap even when a developer is editing Gameplay or MainMenu.
    /// </summary>
    [InitializeOnLoad]
    public static class BootstrapPlayModeSetup
    {
        private const string LogSubsystem = "EditorPlay";

        /// <summary>Schedules configuration after the AssetDatabase finishes importing scenes.</summary>
        static BootstrapPlayModeSetup()
        {
            EditorApplication.delayCall += Configure;
        }

        /// <summary>Sets the Play Mode start scene and repairs canonical build-scene order.</summary>
        [MenuItem("Tools/Sphere Corridor/Foundation/Configure Bootstrap Play Mode")]
        public static void Configure()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Configure;
                return;
            }

            SceneAsset bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneIds.BootstrapPath);
            if (bootstrap == null)
            {
                AppLog.Error(LogSubsystem, $"Bootstrap scene is missing: {SceneIds.BootstrapPath}");
                return;
            }

            // This Editor-only setting does not change the scene being authored. It
            // changes only which scene Unity loads when the Play button is pressed.
            if (EditorSceneManager.playModeStartScene != bootstrap)
            {
                EditorSceneManager.playModeStartScene = bootstrap;
                AppLog.Info(LogSubsystem, "Editor Play Mode now starts from Bootstrap.", bootstrap);
            }

            EnsureCanonicalBuildOrder();
        }

        /// <summary>
        /// Places required application scenes first while preserving unrelated scenes
        /// that a developer may add later.
        /// </summary>
        private static void EnsureCanonicalBuildOrder()
        {
            string[] canonicalPaths = SceneIds.GetBuildScenePaths();
            List<EditorBuildSettingsScene> orderedScenes = new List<EditorBuildSettingsScene>();

            for (int index = 0; index < canonicalPaths.Length; index++)
            {
                orderedScenes.Add(new EditorBuildSettingsScene(canonicalPaths[index], true));
            }

            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            for (int index = 0; index < existingScenes.Length; index++)
            {
                if (!Contains(canonicalPaths, existingScenes[index].path))
                {
                    orderedScenes.Add(existingScenes[index]);
                }
            }

            if (!Matches(existingScenes, orderedScenes))
            {
                EditorBuildSettings.scenes = orderedScenes.ToArray();
                AppLog.Info(LogSubsystem, "Build Settings repaired: Bootstrap is enabled at index zero.");
            }
        }

        /// <summary>Performs an ordinal path lookup without LINQ allocation.</summary>
        private static bool Contains(string[] paths, string expected)
        {
            for (int index = 0; index < paths.Length; index++)
            {
                if (paths[index] == expected)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Checks whether assigning Build Settings would make a real change.</summary>
        private static bool Matches(
            EditorBuildSettingsScene[] existing,
            List<EditorBuildSettingsScene> expected)
        {
            if (existing.Length != expected.Count)
            {
                return false;
            }

            for (int index = 0; index < existing.Length; index++)
            {
                if (existing[index].path != expected[index].path ||
                    existing[index].enabled != expected[index].enabled)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
