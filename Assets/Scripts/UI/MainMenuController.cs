using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Systems;

namespace Ashfall.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject mainPanel;
        public GameObject settingsPanel;
        public GameObject levelSelectPanel;
        public GameObject confirmWipePanel;

        [Header("Player identity")]
        [Tooltip("Optional - if assigned, players are asked for a name before a run starts " +
                 "so leaderboard submissions aren't all attributed to the same default name.")]
        public PlayerNameEntryController nameEntry;

        void Start()
        {
            ShowMainPanel();
        }

        // Panels are wired in the inspector and destroyed with the scene, so every
        // toggle goes through here rather than calling SetActive on a field directly.
        static void Show(GameObject panel, bool visible)
        {
            if (panel == null)
            {
                return;
            }

            panel.SetActive(visible);
        }

        // -- panel switching --
        public void ShowMainPanel()
        {
            Show(mainPanel, true);
            Show(settingsPanel, false);
            Show(levelSelectPanel, false);
            Show(confirmWipePanel, false);
        }

        public void ShowSettingsPanel()
        {
            Show(mainPanel, false);
            Show(settingsPanel, true);
        }

        public void ShowLevelSelectPanel()
        {
            Show(mainPanel, false);
            Show(levelSelectPanel, true);
        }

        // -- button actions --
        public void OnStartClicked()
        {
            if (SaveManager.Instance.HasSaveFile())
            {
                // has existing progress, confirm before wiping it
                Show(mainPanel, false);
                Show(confirmWipePanel, true);
            }
            else
            {
                BeginNewGame();
            }
        }

        public void OnConfirmWipeYes()
        {
            BeginNewGame();
        }

        public void OnConfirmWipeNo()
        {
            Show(confirmWipePanel, false);
            Show(mainPanel, true);
        }

        void BeginNewGame()
        {
            SaveManager.Instance.DeleteSave();
            RequestNameThenLoad("Level1");
        }

        // Asks for a player name before the run actually starts, unless no name-entry
        // screen was wired up (in which case behaviour falls back to the old direct load).
        void RequestNameThenLoad(string sceneName)
        {
            if (nameEntry != null)
            {
                Show(mainPanel, false);
                nameEntry.Show(() => SceneManager.LoadScene(sceneName));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void OnExitClicked()
        {
#if UNITY_EDITOR
            // Application.Quit is a no-op in the editor, so stop play mode instead
            // - otherwise the button looks broken every time you test it.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }   
}