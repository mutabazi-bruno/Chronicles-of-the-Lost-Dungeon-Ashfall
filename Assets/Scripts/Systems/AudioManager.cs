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
        }

        void Start()
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            LevelManager.Instance.OnLevelCompleted += HandleLevelCompleted;

            PlayMusicForScene(SceneManager.GetActiveScene().name);
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

        // was private - made public so Chest/Switch/Door/Collectible can play their own
        // one-off SFX through the same shared AudioSource instead of each needing their own
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void SetMusicVolume(float value)
        {
            musicSource.volume = value;
        }
    }
}