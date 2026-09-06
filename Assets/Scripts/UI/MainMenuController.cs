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

        [Header("Buttons that depend on save state")]
        public GameObject continueButton;

        void Start()
        {
            ShowMainPanel();
            continueButton.SetActive(SaveManager.Instance.HasSaveFile());
        }

        // -- panel switching --
        public void ShowMainPanel()
        {
            mainPanel.SetActive(true);
            settingsPanel.SetActive(false);
            levelSelectPanel.SetActive(false);
            confirmWipePanel.SetActive(false);
        }

        public void ShowSettingsPanel()
        {
            mainPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        public void ShowLevelSelectPanel()
        {
            mainPanel.SetActive(false);
            levelSelectPanel.SetActive(true);
        }

        // -- button actions --
        public void OnStartClicked()
        {
            if (SaveManager.Instance.HasSaveFile())
            {
                // has existing progress, confirm before wiping it
                mainPanel.SetActive(false);
                confirmWipePanel.SetActive(true);
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
            confirmWipePanel.SetActive(false);
            mainPanel.SetActive(true);
        }

        void BeginNewGame()
        {
            SaveManager.Instance.DeleteSave();
            SceneManager.LoadScene("Level1");
        }

        public void OnContinueClicked()
        {
            var save = SaveManager.Instance.CurrentSave;
            string furthest = save.unlockedLevels[save.unlockedLevels.Count - 1];
            SceneManager.LoadScene(furthest);
        }

        public void OnExitClicked()
        {
            Application.Quit();
        }
    }   
}