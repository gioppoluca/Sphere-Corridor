using System.Collections.Generic;
using System.IO;
using System.Linq;
using SphereCorridor.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SphereCorridor.Editor
{
    /// <summary>
    /// Creates and validates the M0 scene foundation through supported Unity APIs.
    /// The operation is explicit and idempotent so it cannot silently overwrite
    /// later scene-authoring work.
    /// </summary>
    public static class M0ProjectSetup
    {
        private const string LogSubsystem = "M0Setup";
        private const string ScenesDirectory = "Assets/_Project/Scenes";
        private const string InputActionsPath = "Assets/_Project/Input/SphereCorridorInputActions.inputactions";
        private const string VersionControlSettingsPath = "ProjectSettings/VersionControlSettings.asset";

        /// <summary>
        /// Creates missing foundation scenes and establishes their standalone build order.
        /// Existing scenes are deliberately preserved.
        /// </summary>
        [MenuItem("Tools/Sphere Corridor/M0/Apply Foundation Setup")]
        public static void ApplyFoundationSetup()
        {
            // Scene creation changes the open scene. Give the developer a chance to
            // save unrelated work before the setup tool proceeds.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                AppLog.Warning(LogSubsystem, "Foundation setup cancelled because the open scene was not saved.");
                return;
            }

            Directory.CreateDirectory(ScenesDirectory);
            AssetDatabase.Refresh();

            CreateSceneIfMissing(SceneIds.BootstrapPath, CreateBootstrapScene);
            CreateSceneIfMissing(SceneIds.MainMenuPath, CreateMainMenuScene);
            CreateSceneIfMissing(SceneIds.GameplayPath, CreateGameplayScene);
            ConfigureBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool valid = ValidateFoundation(logSuccess: true);
            if (valid)
            {
                EditorSceneManager.OpenScene(SceneIds.BootstrapPath, OpenSceneMode.Single);
                AppLog.Info(LogSubsystem, "M0 foundation applied; Bootstrap is ready for a Play-mode smoke test.");
            }
        }

        /// <summary>
        /// Runs the same structural checks used by automated EditMode tests.
        /// </summary>
        [MenuItem("Tools/Sphere Corridor/M0/Validate Foundation")]
        public static void ValidateFoundationFromMenu()
        {
            ValidateFoundation(logSuccess: true);
        }

        /// <summary>
        /// Checks required assets and settings without changing the project.
        /// </summary>
        /// <param name="logSuccess">Whether to emit a summary when every check passes.</param>
        /// <returns>True only when all M0 structural checks pass.</returns>
        public static bool ValidateFoundation(bool logSuccess)
        {
            List<string> failures = new List<string>();

            foreach (string scenePath in SceneIds.GetBuildScenePaths())
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    failures.Add($"Required scene is missing: {scenePath}");
                }
            }

            if (AssetDatabase.LoadMainAssetAtPath(InputActionsPath) == null)
            {
                failures.Add($"Project input actions are missing: {InputActionsPath}");
            }

            string[] expectedBuildScenes = SceneIds.GetBuildScenePaths();
            string[] actualBuildScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (actualBuildScenes.Length < expectedBuildScenes.Length ||
                !expectedBuildScenes.SequenceEqual(actualBuildScenes.Take(expectedBuildScenes.Length)))
            {
                failures.Add("Enabled Build Settings scenes must begin with Bootstrap, MainMenu, Gameplay.");
            }

            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                failures.Add("Asset Serialization Mode must be Force Text.");
            }

            if (!File.Exists(VersionControlSettingsPath) ||
                !File.ReadAllText(VersionControlSettingsPath).Contains("m_Mode: Visible Meta Files"))
            {
                failures.Add("Version Control Mode must be Visible Meta Files.");
            }

            foreach (string failure in failures)
            {
                AppLog.Error(LogSubsystem, failure);
            }

            if (failures.Count == 0 && logSuccess)
            {
                AppLog.Info(LogSubsystem, "Foundation validation passed with no structural errors.");
            }

            return failures.Count == 0;
        }

        /// <summary>
        /// Invokes a scene factory only when the target asset does not already exist.
        /// </summary>
        private static void CreateSceneIfMissing(string scenePath, System.Action<string> createScene)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                AppLog.Development(LogSubsystem, $"Preserved existing scene '{scenePath}'.");
                return;
            }

            createScene(scenePath);
            AppLog.Info(LogSubsystem, $"Created scene '{scenePath}'.");
        }

        /// <summary>
        /// Creates the minimal first scene responsible for application initialization.
        /// </summary>
        private static void CreateBootstrapScene(string scenePath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrapObject = new GameObject("[Bootstrap]");
            bootstrapObject.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        /// <summary>
        /// Creates a visible temporary menu that proves Bootstrap-to-menu flow.
        /// </summary>
        private static void CreateMainMenuScene(string scenePath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera("Main Camera", new Vector3(0f, 0f, -10f), Quaternion.identity, new Color(0.015f, 0.025f, 0.05f));

            GameObject presenter = new GameObject("[M0 Main Menu]");
            presenter.AddComponent<FoundationMainMenu>();
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        /// <summary>
        /// Creates a geometric sandbox with no player behavior; M1 will own movement.
        /// </summary>
        private static void CreateGameplayScene(string scenePath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(
                "Main Camera",
                new Vector3(0f, 3.5f, -12f),
                Quaternion.Euler(10f, 0f, 0f),
                new Color(0.015f, 0.025f, 0.05f));

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Light sceneLight = lightObject.AddComponent<Light>();
            sceneLight.type = LightType.Directional;
            sceneLight.intensity = 1.2f;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "M0 Floor Placeholder";
            floor.transform.position = new Vector3(0f, -1f, 0f);
            floor.transform.localScale = new Vector3(24f, 1f, 4f);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "M0 Sphere Placeholder";
            sphere.transform.position = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 1.5f;

            GameObject presenter = new GameObject("[M0 Gameplay View]");
            presenter.AddComponent<FoundationGameplayView>();
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        /// <summary>
        /// Creates one tagged perspective camera with a deterministic diagnostic background.
        /// </summary>
        private static void CreateCamera(string name, Vector3 position, Quaternion rotation, Color background)
        {
            GameObject cameraObject = new GameObject(name);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(position, rotation);

            Camera sceneCamera = cameraObject.AddComponent<Camera>();
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = background;
            sceneCamera.fieldOfView = 60f;
        }

        /// <summary>
        /// Makes Bootstrap build index zero and enables only the three M0 scenes.
        /// </summary>
        private static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = SceneIds.GetBuildScenePaths()
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();

            AppLog.Info(LogSubsystem, "Configured Build Settings scene order: Bootstrap, MainMenu, Gameplay.");
        }
    }
}
