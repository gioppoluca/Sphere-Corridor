using System.IO;
using System.Linq;
using NUnit.Framework;
using SphereCorridor.Foundation;
using UnityEditor;
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

            Assert.That(actualPaths, Is.EqualTo(SceneIds.GetBuildScenePaths()));
            AppLog.Development("Tests", "Build scene order is canonical.");
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
            Assert.That(inputAsset.FindAction("Player/Jump"), Is.Not.Null);
            Assert.That(inputAsset.FindAction("Player/Fire"), Is.Not.Null);
            Assert.That(inputAsset.FindAction("Player/Interact"), Is.Not.Null);
            Assert.That(inputAsset.FindAction("Player/Pause"), Is.Not.Null);
            Assert.That(inputAsset.FindActionMap("UI"), Is.Not.Null);

            AppLog.Development("Tests", "Required input maps and actions exist.");
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
