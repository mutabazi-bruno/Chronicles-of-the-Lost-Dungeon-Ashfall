using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Systems;

namespace Ashfall.UI
{
    public class LevelCompleteController : MonoBehaviour
    {
        [Tooltip("When on, the level id is taken from the scene name at runtime. Keeps the " +
                 "shared HUD prefab correct in every scene without a per-scene override.")]
        public bool useSceneNameAsLevelId = true;

        [Tooltip("Only used when useSceneNameAsLevelId is off")]
        public string levelIdOverride;

        public GameObject panel;

        string nextLevel;

        public string LevelId => useSceneNameAsLevelId
            ? SceneManager.GetActiveScene().name
            : levelIdOverride;

        void Start()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnLevelCompleted += HandleLevelCompleted;

            panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnLevelCompleted -= HandleLevelCompleted;
        }

        void HandleLevelCompleted(string completedLevelId)
        {
            if (completedLevelId != LevelId) return; // not this level, ignore

            nextLevel = LevelManager.Instance.GetNextLevel(completedLevelId);
            panel.SetActive(true);

            // GameManager manages timescale to prevent pause conflicts.
            GameManager.Instance?.ChangeState(GameState.LevelComplete);
        }

        public void OnNextLevelClicked()
        {
            GameManager.Instance?.ChangeState(GameState.Playing);

            if (!string.IsNullOrEmpty(nextLevel))
                SceneManager.LoadScene(nextLevel);
            else
                SceneManager.LoadScene("MainMenu"); // last level done, nowhere else to go yet
        }

        public void OnMainMenuClicked()
        {
            GameManager.Instance?.ChangeState(GameState.MainMenu);
            SceneManager.LoadScene("MainMenu");
        }
    }
}