using UnityEngine;
using Ashfall.Player;
using Ashfall.Enemies;
using Ashfall.Systems;

namespace Ashfall.Systems
{

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        public AudioClip backgroundMusic;

        [Header("SFX")]
        public AudioClip movementSound;   
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
            PlayerAttack.OnAttackSound += PlayAttackSound;
            Enemy.OnAnyEnemyDeath += PlayEnemyDeathSound;
        }

        void OnDisable()
        {
            PlayerController.OnMovementSound -= PlayMovementSound;
            PlayerAttack.OnAttackSound -= PlayAttackSound;
            Enemy.OnAnyEnemyDeath -= PlayEnemyDeathSound;
        }

        void Start()
        {
            
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            LevelManager.Instance.OnLevelCompleted += HandleLevelCompleted;

            PlayMusic();
        }

        public void PlayMusic()
        {
            if (backgroundMusic == null) return;
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }

        void PlayMovementSound() => PlaySFX(movementSound);
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

        void PlaySFX(AudioClip clip)
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