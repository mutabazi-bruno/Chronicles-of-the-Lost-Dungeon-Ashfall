using UnityEngine;
using UnityEngine.UI;
using Ashfall.Player;

namespace Ashfall.UI
{
    public class HUDController : MonoBehaviour
    {
        public Slider healthBar;
        public Slider staminaBar;

        PlayerHealth playerHealth;

        void Start()
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogWarning("HUDController couldn't find a Player in this scene.");
                return;
            }

            playerHealth = playerObj.GetComponent<PlayerHealth>();
            if (playerHealth == null) return;

            playerHealth.OnHealthChanged += HandleHealthChanged;

            healthBar.maxValue = playerHealth.stats.maxHealth;
            healthBar.value = playerHealth.stats.currentHealth;

            staminaBar.maxValue = playerHealth.stats.maxStamina;
            staminaBar.value = playerHealth.stats.currentStamina;
        }

        void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged -= HandleHealthChanged;
        }

        void Update()
        {
            // stamina regenerates every frame and has no change event yet, so poll it
            if (playerHealth == null) return;
            staminaBar.value = playerHealth.stats.currentStamina;
        }

        void HandleHealthChanged(int current, int max)
        {
            healthBar.maxValue = max;
            healthBar.value = current;
        }
    }
}