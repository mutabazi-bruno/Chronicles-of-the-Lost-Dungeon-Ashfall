using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Systems;

namespace Ashfall.UI
{
    public class LevelCompleteController : MonoBehaviour
    {
        [Tooltip("must match this scene's level id, e.g. Level1")]
        public string levelId;

        public GameObject panel;

        string nextLevel;

        void OnEnable()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnLevelCompleted += HandleLevelCompleted;

            panel.SetActive(false);
        }

        void OnDisable()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnLevelCompleted -= HandleLevelCompleted;
        }

        void HandleLevelCompleted(string completedLevelId)
        {
            if (completedLevelId != levelId) return; // not this level, ignore

            nextLevel = LevelManager.Instance.GetNextLevel(levelId);
            Time.timeScale = 0f; // pause on the win screen too
            panel.SetActive(true);
        }

        public void OnNextLevelClicked()
        {
            Time.timeScale = 1f;
            if (!string.IsNullOrEmpty(nextLevel))
                SceneManager.LoadScene(nextLevel);
            else
                SceneManager.LoadScene("MainMenu"); // last level done, nowhere else to go yet
        }

        public void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}