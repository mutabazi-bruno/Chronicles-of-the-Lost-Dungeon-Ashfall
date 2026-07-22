using System;

namespace Ashfall.Core
{
    // holds the raw numbers for the player, nothing unity related here
    // keeping this plain so we can unit test it without needing a scene
    [Serializable]
    public class PlayerStats
    {
        public int maxHealth;
        public int currentHealth;

        public int maxStamina;
        public float currentStamina; // float cause we regen it gradually

        public int coins;

        public PlayerStats(int maxHealth = 100, int maxStamina = 100)
        {
            this.maxHealth = maxHealth;
            currentHealth = maxHealth;

            this.maxStamina = maxStamina;
            currentStamina = maxStamina;

            coins = 0;
        }

        public bool IsDead => currentHealth <= 0;

        public void TakeDamage(int amount)
        {
            if (amount < 0) amount = 0; // no negative damage healing us lol
            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0;
        }

        public void Heal(int amount)
        {
            if (amount < 0) return;
            currentHealth += amount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
        }

        public bool SpendStamina(float amount)
        {
            if (currentStamina < amount) return false; // not enough, ability cant fire
            currentStamina -= amount;
            return true;
        }

        public void RegenStamina(float amount)
        {
            currentStamina += amount;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }

        public void AddCoins(int amount)
        {
            if (amount < 0) return;
            coins += amount;
        }
    }
}