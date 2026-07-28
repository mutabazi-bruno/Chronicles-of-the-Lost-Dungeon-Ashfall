using System;
using UnityEngine;
using Ashfall.Core;
using Ashfall.Interfaces;

namespace Ashfall.Player
{
    // handles player health, wraps PlayerStats and implements IDamageable
    // other systems (ui, audio, save) listen to the events instead of calling this directly
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public PlayerStats stats;
        public float staminaRegenPerSecond = 15f;

        // observer pattern - anyone can subscribe, no direct references needed
        public event Action<int, int> OnHealthChanged; // current, max
        public event Action OnPlayerDied;

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

            stats.RegenStamina(staminaRegenPerSecond * Time.deltaTime);
        }

        // temp, just so we can see hp live while testing, real hud comes later
        void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 200, 30), $"HP: {stats.currentHealth}/{stats.maxHealth}");
            GUI.Label(new Rect(10, 30, 200, 30), $"Stamina: {stats.currentStamina:F0}/{stats.maxStamina}");
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

        void Die()
        {
            animator?.SetBool("noBlood", false); // we want blood on death
            animator?.SetTrigger("Death");
            controller?.SetDead();

            OnPlayerDied?.Invoke();

            Ashfall.Systems.GameManager.Instance?.ChangeState(Ashfall.Systems.GameState.GameOver);
        }
    }
}