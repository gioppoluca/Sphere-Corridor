namespace SphereCorridor.Foundation
{
    /// <summary>
    /// Owns scene names and asset paths so runtime, setup tools, builds, and tests
    /// cannot silently drift onto different string literals.
    /// </summary>
    public static class SceneIds
    {
        public const string BootstrapName = "Bootstrap";
        public const string MainMenuName = "MainMenu";
        public const string GameplayName = "Gameplay";

        public const string BootstrapPath = "Assets/_Project/Scenes/Bootstrap.unity";
        public const string MainMenuPath = "Assets/_Project/Scenes/MainMenu.unity";
        public const string GameplayPath = "Assets/_Project/Scenes/Gameplay.unity";

        /// <summary>
        /// Returns the canonical enabled scene order for builds and validation.
        /// Bootstrap must remain first because a standalone player starts at index zero.
        /// </summary>
        public static string[] GetBuildScenePaths()
        {
            return new[] { BootstrapPath, MainMenuPath, GameplayPath };
        }
    }
}
