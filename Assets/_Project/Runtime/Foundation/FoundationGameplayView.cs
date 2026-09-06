using UnityEngine;

namespace SphereCorridor.Foundation
{
    /// <summary>
    /// Labels the M0 geometric sandbox and provides a route back to MainMenu.
    /// It contains no player behavior; movement begins in M1.
    /// </summary>
    public sealed class FoundationGameplayView : MonoBehaviour
    {
        /// <summary>
        /// Draws a small diagnostic panel over the geometric scene.
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20f, 20f, 360f, 145f), GUI.skin.box);
            GUILayout.Label("M0 GAMEPLAY SANDBOX");
            GUILayout.Label("The sphere is intentionally static. Movement begins in M1.");

            if (GUILayout.Button("Return to Main Menu", GUILayout.Height(36f)))
            {
                RequestMainMenu();
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// Delegates the transition to the persistent bootstrap.
        /// </summary>
        private void RequestMainMenu()
        {
            if (GameBootstrap.Instance == null)
            {
                AppLog.Error("Gameplay", "Cannot return to MainMenu because GameBootstrap is missing.", this);
                return;
            }

            GameBootstrap.Instance.LoadMainMenu();
        }
    }
}
