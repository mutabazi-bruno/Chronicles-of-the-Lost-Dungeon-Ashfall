using System;
using UnityEngine;
using Ashfall.Core;
using Ashfall.Interfaces;

namespace Ashfall.Player
{
    // handles player health, wraps PlayerStats and implements IDamageable
    // other systems (ui, audio, save) listen to the events instead of calling this directly
    public class PlayerHealth : MonoBehaviour, IDamageable, ISaveable
    {
        public PlayerStats stats;
        public float staminaRegenPerSecond = 15f;

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

        void Update()
        {
            // temp test key, remove once we have real combat/enemies dealing damage
            if (Input.GetKeyDown(KeyCode.K))
            {
                TakeDamage(10);
            }

            // temp test keys, real save triggers (level complete etc) come later
            if (Input.GetKeyDown(KeyCode.L))
            {
                Save(Ashfall.Systems.SaveManager.Instance.CurrentSave);
                Ashfall.Systems.SaveManager.Instance.Save();
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                Ashfall.Systems.SaveManager.Instance.Load();
                Load(Ashfall.Systems.SaveManager.Instance.CurrentSave);
            }

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

            Ashfall.Systems.GameManager.Instance?.ChangeState(Ashfall.Systems.GameState.GameOver);
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
            stats.maxHealth = data.maxHealth;
            stats.currentHealth = data.health;
            stats.maxStamina = (int)data.maxStamina;
            stats.currentStamina = data.stamina;
            stats.coins = data.coins;

            OnHealthChanged?.Invoke(stats.currentHealth, stats.maxHealth);
            OnCoinsChanged?.Invoke(stats.coins);
        }
    }
}