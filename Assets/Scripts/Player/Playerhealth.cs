using System;
using UnityEngine;
using Ashfall.Core;
using Ashfall.Interfaces;
using Ashfall.Systems;

namespace Ashfall.Player
{
    // Handles player health logic and events.
    public class PlayerHealth : MonoBehaviour, IDamageable, ISaveable
    {
        public PlayerStats stats;
        public float staminaRegenPerSecond = 15f;

        [Tooltip("Off means every level starts at full health and only coins carry over. " +
                 "On means a level can be entered on low health, which can soft-lock a run.")]
        public bool restoreHealthFromSave = false;

        // observer pattern - anyone can subscribe, no direct references needed
        public event Action<int, int> OnHealthChanged; // current, max
        public event Action OnPlayerDied;
        public event Action<int> OnCoinsChanged; // new total coin count

        Animator animator;
        PlayerController controller;

        void Awake()
        {
            stats = new PlayerStats();
            animator = GetComponent<Animator>();
            controller = GetComponent<PlayerController>();
        }

        void OnEnable()
        {
            SaveManager.Instance?.Register(this);
        }

        void OnDisable()
        {
            SaveManager.Instance?.Unregister(this);
        }

        // No self-load here any more. SaveManager drives every ISaveable from
        // sceneLoaded so player, inventory and world state are applied together in a
        // known order, instead of each component racing to restore itself in Start.

        void Update()
        {
            stats.RegenStamina(staminaRegenPerSecond * Time.deltaTime);
        }

        public bool IsDead => stats.IsDead;

        public void TakeDamage(int amount)
        {
            if (stats.IsDead) return; // already dead, dont bother

            stats.TakeDamage(amount);
            OnHealthChanged?.Invoke(stats.currentHealth, stats.maxHealth);

            if (stats.IsDead)
                Die();
            else
                animator?.SetTrigger("Hurt");
        }

        public void Heal(int amount)
        {
            if (stats.IsDead) return; // cant heal a dead player, need a respawn first

            stats.Heal(amount);
            OnHealthChanged?.Invoke(stats.currentHealth, stats.maxHealth);
        }


        public void AddCoins(int amount)
        {
            stats.AddCoins(amount);
            OnCoinsChanged?.Invoke(stats.coins);
        }

        void Die()
        {
            animator?.SetBool("noBlood", false); // we want blood on death
            animator?.SetTrigger("Death");
            controller?.SetDead();

            OnPlayerDied?.Invoke();

            GameManager.Instance?.ChangeState(GameState.GameOver);
        }

        // ISaveable implementation
        public void Save(SaveData data)
        {
            data.health = stats.currentHealth;
            data.maxHealth = stats.maxHealth;
            data.stamina = stats.currentStamina;
            data.maxStamina = stats.maxStamina;
            data.coins = stats.coins;

            // position + scene, so autosave can resume mid-level
            data.playerX = transform.position.x;
            data.playerY = transform.position.y;
            data.lastScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        public void Load(SaveData data)
        {
            // guard against a save written before these fields existed
            stats.maxHealth = data.maxHealth > 0 ? data.maxHealth : 100;
            stats.maxStamina = data.maxStamina > 0 ? (int)data.maxStamina : 100;

            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // Resuming means picking this level back up exactly where it was left.
            // Entering it any other way is a fresh attempt.
            bool resuming = data.HasResumePointFor(currentScene);

            // Carrying low health into a level you are starting can soft-lock a run,
            // which is what restoreHealthFromSave guards against. That risk does not
            // apply when resuming the level you were already standing in, so the
            // saved health is honoured there regardless of the flag.
            bool useSavedHealth = (resuming || restoreHealthFromSave) && data.health > 0;

            stats.currentHealth = useSavedHealth ? data.health : stats.maxHealth;

            stats.currentStamina = stats.maxStamina;
            stats.coins = data.coins;

            if (resuming && (data.playerX != 0f || data.playerY != 0f))
            {
                transform.position = new Vector3(data.playerX, data.playerY, transform.position.z);
            }

            OnHealthChanged?.Invoke(stats.currentHealth, stats.maxHealth);
            OnCoinsChanged?.Invoke(stats.coins);
        }
    }
}
