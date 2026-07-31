using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Systems;

namespace Ashfall.UI
{
    public class GameOverController : MonoBehaviour
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
            panel.SetActive(state == GameState.GameOver);
        }

        public void OnRestartClicked()
        {
            string current = SceneManager.GetActiveScene().name;
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