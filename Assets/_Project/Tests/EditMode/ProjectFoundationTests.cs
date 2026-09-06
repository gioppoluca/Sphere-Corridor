using System.IO;
using System.Linq;
using NUnit.Framework;
using SphereCorridor.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;

namespace SphereCorridor.Tests.EditMode
{
    /// <summary>
    /// Protects the structural assumptions required before gameplay development begins.
    /// These fast EditMode tests identify missing assets before a player build is attempted.
    /// </summary>
    public sealed class ProjectFoundationTests
    {
        private const string InputActionsPath = "Assets/_Project/Input/SphereCorridorInputActions.inputactions";
        private const string VersionControlSettingsPath = "ProjectSettings/VersionControlSettings.asset";

        /// <summary>
        /// Verifies that each application-flow scene exists as a Unity scene asset.
        /// </summary>
        [Test]
        public void RequiredScenesExist()
        {
            foreach (string scenePath in SceneIds.GetBuildScenePaths())
            {
                SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                Assert.That(scene, Is.Not.Null, $"Required scene is missing: {scenePath}");
            }

            AppLog.Development("Tests", "Required scene assets exist.");
        }

        /// <summary>
        /// Verifies the exact enabled scene order because standalone players start at index zero.
        /// </summary>
        [Test]
        public void BuildScenesUseCanonicalOrder()
        {
            string[] actualPaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            string[] canonicalPaths = SceneIds.GetBuildScenePaths();
            Assert.That(actualPaths.Take(canonicalPaths.Length), Is.EqualTo(canonicalPaths));
            AppLog.Development("Tests", "Build scene order is canonical.");
        }

        /// <summary>
        /// Verifies that pressing Play while authoring Gameplay still enters through
        /// the application Bootstrap and therefore displays the Main Menu first.
        /// </summary>
        [Test]
        public void EditorPlayModeStartsFromBootstrap()
        {
            string playStartPath = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);

            Assert.That(playStartPath, Is.EqualTo(SceneIds.BootstrapPath));
            AppLog.Development("Tests", "Editor Play Mode start scene is Bootstrap.");
        }

        /// <summary>
        /// Verifies the project input contract needed by M1 movement and later UI work.
        /// </summary>
        [Test]
        public void InputActionsContainRequiredMapsAndActions()
        {
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Assert.That(inputAsset, Is.Not.Null, $"Input action asset is missing: {InputActionsPath}");

            Assert.That(inputAsset.FindAction("Player/MoveHorizontal"), Is.Not.Null);
            Assert.That(inputAsset.FindAction("Player/MoveLateral"), Is.Not.Null);
            Assert.That(inputAsset.FindAction("Player/Jump"), Is.Not.Null);
            Assert.That(inputAsset.FindAction("Player/Fire"), Is.Not.Null);
            Assert.That(inputAsset.FindAction("Player/Interact"), Is.Not.Null);
            Assert.That(inputAsset.FindAction("Player/Pause"), Is.Not.Null);
            Assert.That(inputAsset.FindActionMap("UI"), Is.Not.Null);

            AppLog.Development("Tests", "Required input maps and actions exist.");
        }

        /// <summary>
        /// Protects the intended keyboard and Xbox-style controller layout. Testing
        /// binding paths here catches accidental Input Actions editor changes without
        /// coupling gameplay code to a particular device.
        /// </summary>
        [Test]
        public void PlayerActionsContainKeyboardAndGamepadBindings()
        {
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Assert.That(inputAsset, Is.Not.Null, $"Input action asset is missing: {InputActionsPath}");

            AssertBinding(inputAsset, "Player/MoveHorizontal", "<Keyboard>/upArrow");
            AssertBinding(inputAsset, "Player/MoveHorizontal", "<Keyboard>/downArrow");
            AssertBinding(inputAsset, "Player/MoveHorizontal", "<Gamepad>/leftStick/y");
            AssertBinding(inputAsset, "Player/MoveLateral", "<Keyboard>/leftArrow");
            AssertBinding(inputAsset, "Player/MoveLateral", "<Keyboard>/rightArrow");
            AssertBinding(inputAsset, "Player/MoveLateral", "<Gamepad>/leftStick/x");
            AssertBinding(inputAsset, "Player/Jump", "<Gamepad>/buttonSouth");
            AssertBinding(inputAsset, "Player/Fire", "<Gamepad>/buttonWest");
            AssertBinding(inputAsset, "Player/Fire", "<Gamepad>/rightShoulder");

            AppLog.Development("Tests", "Keyboard and gamepad bindings are present.");
        }

        /// <summary>
        /// Gives binding-test failures the missing action and control path instead of
        /// a generic collection mismatch, shortening diagnosis in Unity's Test Runner.
        /// </summary>
        private static void AssertBinding(InputActionAsset inputAsset, string actionPath, string controlPath)
        {
            InputAction action = inputAsset.FindAction(actionPath, throwIfNotFound: false);
            Assert.That(action, Is.Not.Null, $"Input action is missing: {actionPath}");
            Assert.That(
                action.bindings.Any(binding => binding.path == controlPath),
                Is.True,
                $"Action '{actionPath}' is missing binding '{controlPath}'.");
        }

        /// <summary>
        /// Verifies text serialization so Git can inspect scenes, prefabs, and project assets.
        /// </summary>
        [Test]
        public void ProjectUsesForceTextSerialization()
        {
            Assert.That(EditorSettings.serializationMode, Is.EqualTo(SerializationMode.ForceText));
            AppLog.Development("Tests", "Force Text serialization is enabled.");
        }

        /// <summary>
        /// Verifies that Unity writes visible meta files carrying stable asset identifiers.
        /// </summary>
        [Test]
        public void ProjectUsesVisibleMetaFiles()
        {
            Assert.That(File.Exists(VersionControlSettingsPath), Is.True);

            string settings = File.ReadAllText(VersionControlSettingsPath);
            Assert.That(settings, Does.Contain("m_Mode: Visible Meta Files"));
            AppLog.Development("Tests", "Visible Meta Files mode is enabled.");
        }
    }
}
