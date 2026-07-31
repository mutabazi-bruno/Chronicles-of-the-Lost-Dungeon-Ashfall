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

        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
        }

        void HandleStateChanged(GameState state)
        {
            panel.SetActive(state == GameState.Paused);
        }

        // hook this up to a "Resume" button
        public void OnResumeClicked()
        {
            GameManager.Instance.TogglePause(); // Paused -> Playing, unpauses timescale too
        }

        // hook this up to a "Main Menu" button
        public void OnMainMenuClicked()
        {
            GameManager.Instance.ChangeState(GameState.MainMenu); // resets timescale back to 1
            SceneManager.LoadScene("MainMenu");
        }
    }
}