using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Player;
using Ashfall.Enemies;

namespace Ashfall.Systems
{
    [Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip clip;
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music (one entry per scene, e.g. MainMenu, Level1, Level2...)")]
        public List<SceneMusic> sceneMusic = new List<SceneMusic>();

        [Header("SFX")]
        public AudioClip movementSound;
        public AudioClip footstepSound;
        public AudioClip attackSound;
        public AudioClip enemyDeathSound;
        public AudioClip gameOverSound;
        public AudioClip levelCompleteSound;

        AudioSource musicSource;
        AudioSource sfxSource;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;

            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        void OnEnable()
        {
            PlayerController.OnMovementSound += PlayMovementSound;
            PlayerController.OnFootstepSound += PlayFootstepSound;
            PlayerAttack.OnAttackSound += PlayAttackSound;
            Enemy.OnAnyEnemyDeath += PlayEnemyDeathSound;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            PlayerController.OnMovementSound -= PlayMovementSound;
            PlayerController.OnFootstepSound -= PlayFootstepSound;
            PlayerAttack.OnAttackSound -= PlayAttackSound;
            Enemy.OnAnyEnemyDeath -= PlayEnemyDeathSound;

            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

            if (LevelManager.Instance != null)
                LevelManager.Instance.OnLevelCompleted -= HandleLevelCompleted;
        }

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            if (LevelManager.Instance != null)
                LevelManager.Instance.OnLevelCompleted += HandleLevelCompleted;

            // Load saved volume on startup.
            ApplySavedVolume();

            PlayMusicForScene(SceneManager.GetActiveScene().name);
        }

        void ApplySavedVolume()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentSave == null)
                return;

            var save = SaveManager.Instance.CurrentSave;
            SetMusicVolume(save.musicVolume);
            SetSfxVolume(save.sfxVolume);
            SetMasterVolume(save.masterVolume);
            SetMuted(save.audioMuted);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayMusicForScene(scene.name);
        }

        void PlayMusicForScene(string sceneName)
        {
            AudioClip clip = null;
            foreach (var entry in sceneMusic)
            {
                if (entry.sceneName == sceneName)
                {
                    clip = entry.clip;
                    break;
                }
            }

            if (clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.Play();
        }

        void PlayMovementSound() => PlaySFX(movementSound);
        void PlayFootstepSound() => PlaySFX(footstepSound);
        void PlayAttackSound() => PlaySFX(attackSound);
        void PlayEnemyDeathSound() => PlaySFX(enemyDeathSound);

        void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.GameOver)
                PlaySFX(gameOverSound);
        }

        void HandleLevelCompleted(string levelId)
        {
            PlaySFX(levelCompleteSound);
        }

        // Shared audio source for world objects.
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        // Music and SFX are stored per-channel. Master scales both by driving the
        // global AudioListener, and mute is layered on top of master so changing
        // one does not silently cancel the other.
        float masterVolume = 1f;
        float musicVolume = 1f;
        float sfxVolume = 1f;
        bool muted;

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyOutputLevel();
        }

        public void SetMuted(bool value)
        {
            muted = value;
            ApplyOutputLevel();
        }

        void ApplyOutputLevel()
        {
            AudioListener.volume = muted ? 0f : masterVolume;
        }

        public float GetMusicVolume() => musicVolume;
        public float GetSfxVolume() => sfxVolume;
        public float GetMasterVolume() => masterVolume;
        public bool IsMuted() => muted;
    }
}