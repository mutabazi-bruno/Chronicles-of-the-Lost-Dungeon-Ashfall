using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Systems;

namespace Ashfall.UI
{
    public class PauseController : MonoBehaviour
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

            panel.SetActive(state == GameState.Paused);
        }

        // Pause toggle via HUD.
        public void OnPauseButtonClicked()
        {
            GameManager.Instance?.TogglePause();
        }

        // hook this up to a "Resume" button
        public void OnResumeClicked()
        {
            GameManager.Instance?.TogglePause(); // Paused -> Playing, unpauses timescale too
        }

        // hook this up to a "Restart" button on the pause panel
        public void OnRestartClicked()
        {
            // ChangeState first so the timescale is back to 1 before the reload,
            // otherwise the fresh scene starts frozen.
            GameManager.Instance?.ChangeState(GameState.Playing);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // hook this up to a "Main Menu" button
        public void OnMainMenuClicked()
        {
            GameManager.Instance?.ChangeState(GameState.MainMenu); // resets timescale back to 1
            SceneManager.LoadScene("MainMenu");
        }
    }
}