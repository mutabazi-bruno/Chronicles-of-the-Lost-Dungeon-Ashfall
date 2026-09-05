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

        LevelManager subscribedTo;

        void OnEnable()
        {
            Subscribe();
        }

        void Start()
        {
            Subscribe();

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        // Detach from the instance we actually attached to. Checking
        // LevelManager.Instance here instead would skip the detach whenever the
        // singleton is already gone or has been replaced, leaving a dead
        // delegate that fires into this destroyed object later.
        void Subscribe()
        {
            if (subscribedTo != null || LevelManager.Instance == null)
            {
                return;
            }

            subscribedTo = LevelManager.Instance;
            subscribedTo.OnLevelCompleted += HandleLevelCompleted;
        }

        void Unsubscribe()
        {
            if (subscribedTo == null)
            {
                return;
            }

            subscribedTo.OnLevelCompleted -= HandleLevelCompleted;
            subscribedTo = null;
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        void HandleLevelCompleted(string completedLevelId)
        {
            if (completedLevelId != LevelId) return; // not this level, ignore

            nextLevel = LevelManager.Instance.GetNextLevel(completedLevelId);

            if (panel == null)
            {
                return;
            }

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