using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ashfall.Player;

namespace Ashfall.UI
{
    public class HUDController : MonoBehaviour
    {
        public Slider healthBar;
        public Slider staminaBar;
        public TMP_Text coinText; // drag a TextMeshPro - Text (UI) object in here

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
            playerHealth.OnCoinsChanged += HandleCoinsChanged;

            healthBar.maxValue = playerHealth.stats.maxHealth;
            healthBar.value = playerHealth.stats.currentHealth;

            staminaBar.maxValue = playerHealth.stats.maxStamina;
            staminaBar.value = playerHealth.stats.currentStamina;

            HandleCoinsChanged(playerHealth.stats.coins);
        }

        void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandleHealthChanged;
                playerHealth.OnCoinsChanged -= HandleCoinsChanged;
            }
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

        void HandleCoinsChanged(int coins)
        {
            if (coinText != null)
                coinText.text = coins.ToString();
        }
    }
}