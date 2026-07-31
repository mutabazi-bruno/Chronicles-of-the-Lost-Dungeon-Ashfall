using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Systems;

namespace Ashfall.UI
{
    public class PauseController : MonoBehaviour
    {
        public GameObject panel;

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += HandleStateChanged;

            panel.SetActive(false);
        }

        // was OnDisable - that unsubscribed the moment anything disabled this object
        // and Start never runs again to re-subscribe. OnDestroy is the safe pair for Start.
        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
        }

        void HandleStateChanged(GameState state)
        {
            panel.SetActive(state == GameState.Paused);
        }

        // hook the HUD's Pause button to THIS method, not to GameManager directly.
        // the button lives inside the HUD prefab, so it can only reference objects inside
        // that same prefab - pointing it at GameManager made Unity save a link to the
        // Systems *prefab asset* on disk instead of the live scene instance, so the click
        // ran on a dead object and nothing happened.
        public void OnPauseButtonClicked()
        {
            GameManager.Instance?.TogglePause();
        }

        // hook this up to a "Resume" button
        public void OnResumeClicked()
        {
            GameManager.Instance?.TogglePause(); // Paused -> Playing, unpauses timescale too
        }

        // hook this up to a "Main Menu" button
        public void OnMainMenuClicked()
        {
            GameManager.Instance?.ChangeState(GameState.MainMenu); // resets timescale back to 1
            SceneManager.LoadScene("MainMenu");
        }
    }
}