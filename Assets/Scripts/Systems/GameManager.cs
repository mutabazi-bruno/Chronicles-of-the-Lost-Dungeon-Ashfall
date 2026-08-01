using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ashfall.Systems
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        LevelComplete,
        GameOver
    }

    // singleton - the one source of truth for what state the game is in
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        // observer pattern - ui/audio/anything reacts to this instead of polling CurrentState every frame
        public event Action<GameState> OnGameStateChanged;

        [Tooltip("scenes that count as gameplay - entering any of these puts us in Playing")]
        public List<string> gameplayScenes = new List<string>
        {
            "Level1", "Level2", "Level3", "Level4", "Level5"
        };

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Start()
        {
            // the very first scene doesn't fire sceneLoaded for us, so set state manually
            ApplyStateForScene(SceneManager.GetActiveScene().name);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyStateForScene(scene.name);
        }

        // Ensure we can leave the MainMenu state.
        void ApplyStateForScene(string sceneName)
        {
            if (gameplayScenes.Contains(sceneName))
                ForceState(GameState.Playing);
            else
                ForceState(GameState.MainMenu);
        }

        // Force state and always re-apply timescale.
        void ForceState(GameState newState)
        {
            CurrentState = newState;
            ApplyTimeScale(newState);
            OnGameStateChanged?.Invoke(newState);
        }

        void Update()
        {
            // works on PC/WebGL keyboard; mobile uses the on-screen pause button
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            ApplyTimeScale(newState);
            OnGameStateChanged?.Invoke(newState);
        }

        void ApplyTimeScale(GameState state)
        {
            switch (state)
            {
                case GameState.Paused:
                case GameState.GameOver:
                case GameState.LevelComplete:
                    Time.timeScale = 0f;
                    break;
                default:
                    Time.timeScale = 1f;
                    break;
            }
        }

        public void TogglePause()
        {
            // Disable un-pausing during death or complete screens.
            if (CurrentState == GameState.Playing)
                ChangeState(GameState.Paused);
            else if (CurrentState == GameState.Paused)
                ChangeState(GameState.Playing);
        }

        // kept for backwards compatibility if anything still points here
        public void OnPauseButtonClicked() => TogglePause();
    }
}