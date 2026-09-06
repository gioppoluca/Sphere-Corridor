using UnityEngine;

namespace SphereCorridor.Foundation
{
    /// <summary>
    /// Draws a temporary dependency-free M0 menu used only to prove scene flow.
    /// A designed UI will replace this diagnostic presenter in a later milestone.
    /// </summary>
    public sealed class FoundationMainMenu : MonoBehaviour
    {
        private const float PanelWidth = 420f;
        private const float PanelHeight = 220f;

        /// <summary>
        /// Draws the smoke-test controls without requiring production UI assets.
        /// </summary>
        private void OnGUI()
        {
            Rect panel = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                (Screen.height - PanelHeight) * 0.5f,
                PanelWidth,
                PanelHeight);

            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Space(16f);
            GUILayout.Label("SPHERE CORRIDOR — M0 FOUNDATION", GUI.skin.label);
            GUILayout.Space(16f);

            if (GUILayout.Button("Open Gameplay Sandbox", GUILayout.Height(48f)))
            {
                RequestGameplay();
            }

            if (GUILayout.Button("Quit", GUILayout.Height(40f)))
            {
                RequestQuit();
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// Delegates scene ownership to the persistent bootstrap.
        /// </summary>
        private void RequestGameplay()
        {
            if (GameBootstrap.Instance == null)
            {
                AppLog.Error("MainMenu", "Cannot start Gameplay because GameBootstrap is missing.", this);
                return;
            }

            GameBootstrap.Instance.LoadGameplay();
        }

        /// <summary>
        /// Delegates application shutdown to the persistent bootstrap.
        /// </summary>
        private void RequestQuit()
        {
            if (GameBootstrap.Instance == null)
            {
                AppLog.Error("MainMenu", "Cannot quit because GameBootstrap is missing.", this);
                return;
            }

            GameBootstrap.Instance.QuitApplication();
        }
    }
}
