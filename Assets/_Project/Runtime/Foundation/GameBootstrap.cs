using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SphereCorridor.Foundation
{
    /// <summary>
    /// Owns application-level scene transitions and survives scene changes.
    /// Gameplay systems must not independently guess how application flow works.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const string LogSubsystem = "Bootstrap";

        private static GameBootstrap instance;
        private bool sceneLoadInProgress;

        /// <summary>
        /// Gets the active bootstrap instance, or null before Bootstrap has initialized.
        /// </summary>
        public static GameBootstrap Instance => instance;

        /// <summary>
        /// Establishes the single persistent bootstrap before other gameplay scripts run.
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                AppLog.Warning(LogSubsystem, "Duplicate bootstrap detected; destroying the duplicate.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            AppLog.Info(LogSubsystem, $"Initialized from scene '{gameObject.scene.name}'.", this);
        }

        /// <summary>
        /// Moves a standalone launch from the minimal Bootstrap scene to MainMenu.
        /// </summary>
        private void Start()
        {
            if (SceneManager.GetActiveScene().name == SceneIds.BootstrapName)
            {
                LoadMainMenu();
            }
        }

        /// <summary>
        /// Starts the temporary M0 gameplay sandbox.
        /// </summary>
        public void LoadGameplay()
        {
            RequestSceneLoad(SceneIds.GameplayName);
        }

        /// <summary>
        /// Returns to the temporary M0 main menu.
        /// </summary>
        public void LoadMainMenu()
        {
            RequestSceneLoad(SceneIds.MainMenuName);
        }

        /// <summary>
        /// Exits a standalone player. In the Editor it logs the request without
        /// forcing Play mode to stop, keeping the control flow easy to debug.
        /// </summary>
        public void QuitApplication()
        {
            AppLog.Info(LogSubsystem, "Application quit requested.", this);

#if UNITY_EDITOR
            AppLog.Development(LogSubsystem, "Quit is ignored in the Editor; stop Play mode manually.", this);
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Rejects overlapping requests and begins an observable asynchronous scene load.
        /// </summary>
        private void RequestSceneLoad(string sceneName)
        {
            if (sceneLoadInProgress)
            {
                AppLog.Warning(LogSubsystem, $"Ignored overlapping request to load '{sceneName}'.", this);
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// Loads one configured scene and records both the request and completion.
        /// </summary>
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            sceneLoadInProgress = true;
            AppLog.Info(LogSubsystem, $"Loading scene '{sceneName}'.", this);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                sceneLoadInProgress = false;
                AppLog.Error(LogSubsystem, $"Unity could not begin loading scene '{sceneName}'.", this);
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            sceneLoadInProgress = false;
            AppLog.Info(LogSubsystem, $"Scene '{sceneName}' loaded successfully.", this);
        }

        /// <summary>
        /// Clears the static reference when the owning application object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                AppLog.Development(LogSubsystem, "Bootstrap instance released.");
            }
        }
    }
}
