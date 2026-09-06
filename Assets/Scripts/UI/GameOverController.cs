using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Systems;

namespace Ashfall.UI
{
    public class GameOverController : MonoBehaviour
    {
        public GameObject panel;

        GameManager subscribedTo;

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
        // GameManager.Instance here instead would skip the detach whenever the
        // singleton is already gone or has been replaced, leaving a dead
        // delegate that fires into this destroyed object later.
        void Subscribe()
        {
            if (subscribedTo != null || GameManager.Instance == null)
            {
                return;
            }

            subscribedTo = GameManager.Instance;
            subscribedTo.OnGameStateChanged += HandleStateChanged;
        }

        void Unsubscribe()
        {
            if (subscribedTo == null)
            {
                return;
            }

            subscribedTo.OnGameStateChanged -= HandleStateChanged;
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

        void HandleStateChanged(GameState state)
        {
            if (panel == null)
            {
                return;
            }

            panel.SetActive(state == GameState.GameOver);
        }

        public void OnRestartClicked()
        {
            string current = SceneManager.GetActiveScene().name;

            // Dying resets the level. Without this the player would reload into a
            // world where everything they already killed stays dead, so a hard level
            // could be ground down by dying repeatedly.
            SaveManager.Instance?.ClearLevelProgress(current);

            GameManager.Instance.ChangeState(GameState.Playing); // resets timescale before reload
            SceneManager.LoadScene(current);
        }

        public void OnMainMenuClicked()
        {
            GameManager.Instance.ChangeState(GameState.MainMenu); // resets timescale
            SceneManager.LoadScene("MainMenu");
        }
    }
}