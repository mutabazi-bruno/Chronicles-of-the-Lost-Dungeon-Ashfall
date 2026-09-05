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

        void Start()
        {
            // pull persisted values (coins especially) into this level's player
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentSave != null)
                Load(SaveManager.Instance.CurrentSave);
        }

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

        // route coin pickups through here (instead of touching stats.AddCoins directly)
        // so anything listening (like the HUD) actually finds out it happened
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

        // ISaveable implementation - copies our stats into/out of the save file
        public void Save(SaveData data)
        {
            data.health = stats.currentHealth;
            data.maxHealth = stats.maxHealth;
            data.stamina = stats.currentStamina;
            data.maxStamina = stats.maxStamina;
            data.coins = stats.coins;
        }

        public void Load(SaveData data)
        {
            // guard against a save written before these fields existed
            stats.maxHealth = data.maxHealth > 0 ? data.maxHealth : 100;
            stats.maxStamina = data.maxStamina > 0 ? (int)data.maxStamina : 100;

            stats.currentHealth = restoreHealthFromSave && data.health > 0
                ? data.health
                : stats.maxHealth;

            stats.currentStamina = stats.maxStamina;
            stats.coins = data.coins;

            OnHealthChanged?.Invoke(stats.currentHealth, stats.maxHealth);
            OnCoinsChanged?.Invoke(stats.coins);
        }
    }
}
