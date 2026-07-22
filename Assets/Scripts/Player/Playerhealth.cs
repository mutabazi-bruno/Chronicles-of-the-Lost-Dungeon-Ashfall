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

        // observer pattern - anyone can subscribe, no direct references needed
        public event Action<int, int> OnHealthChanged; // current, max
        public event Action OnPlayerDied;

        void Awake()
        {
            stats = new PlayerStats();
        }

        void Update()
        {
            // temp test key, remove once we have real combat/enemies dealing damage
            if (Input.GetKeyDown(KeyCode.K))
            {
                TakeDamage(10);
                Debug.Log($"took damage, hp now {stats.currentHealth}/{stats.maxHealth}");
            }
        }

        public bool IsDead => stats.IsDead;

        public void TakeDamage(int amount)
        {
            if (stats.IsDead) return; // already dead, dont bother

            stats.TakeDamage(amount);
            OnHealthChanged?.Invoke(stats.currentHealth, stats.maxHealth);

            if (stats.IsDead)
                OnPlayerDied?.Invoke();
        }

        public void Heal(int amount)
        {
            if (stats.IsDead) return; // cant heal a dead player, need a respawn first

            stats.Heal(amount);
            OnHealthChanged?.Invoke(stats.currentHealth, stats.maxHealth);
        }
    }
}