using System;
using UnityEngine;

namespace Ashfall.Systems
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    // singleton - the one source of truth for what state the game is in
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        // observer pattern - ui/audio/anything reacts to this instead of polling
        public event Action<GameState> OnGameStateChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            // temp test key, real pause button comes with the UI later
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;

            switch (newState)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
            }

            OnGameStateChanged?.Invoke(newState);
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
                ChangeState(GameState.Paused);
            else if (CurrentState == GameState.Paused)
                ChangeState(GameState.Playing);
        }
    }
}