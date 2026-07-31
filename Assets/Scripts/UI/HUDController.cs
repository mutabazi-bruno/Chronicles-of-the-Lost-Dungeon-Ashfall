using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Ashfall.Core;
using Ashfall.Player;
using Ashfall.Systems;

namespace Ashfall.UI
{
    // Reads nothing directly from gameplay every frame - it subscribes to events and redraws
    // only when something actually changed. That is what keeps the HUD swappable without
    // touching a single gameplay script (presentation question 13).
    public class HUDController : MonoBehaviour
    {
        [Header("Vitals")]
        public Slider healthBar;
        public Slider staminaBar;
        public TMP_Text coinText;

        [Header("Level info")]
        public TMP_Text levelNameText;
        public TMP_Text objectivesText;

        [Header("Ability indicators")]
        [Tooltip("Icon dims when there isn't enough stamina to use the ability")]
        public Image dashIcon;
        public Image heavyStrikeIcon;
        public Color abilityReadyColor = Color.white;
        public Color abilityBlockedColor = new Color(1f, 1f, 1f, 0.35f);

        [Header("Inventory")]
        public TMP_Text inventoryText;

        PlayerHealth playerHealth;
        PlayerAbilities playerAbilities;
        PlayerInventory playerInventory;

        void Start()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogWarning("[HUDController] no object tagged Player in this scene");
                return;
            }

            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerAbilities = playerObj.GetComponent<PlayerAbilities>();
            playerInventory = playerObj.GetComponent<PlayerInventory>();

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += HandleHealthChanged;
                playerHealth.OnCoinsChanged += HandleCoinsChanged;

                HandleHealthChanged(playerHealth.stats.currentHealth, playerHealth.stats.maxHealth);
                HandleCoinsChanged(playerHealth.stats.coins);

                if (staminaBar != null)
                    staminaBar.maxValue = playerHealth.stats.maxStamina;
            }

            if (playerInventory != null)
            {
                playerInventory.OnItemAdded += HandleInventoryChanged;
                playerInventory.OnItemRemoved += HandleInventoryChanged;
                RefreshInventory();
            }

            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.OnObjectivesChanged += RefreshObjectives;
                RefreshObjectives();
            }

            if (levelNameText != null)
                levelNameText.text = SceneManager.GetActiveScene().name;
        }

        void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandleHealthChanged;
                playerHealth.OnCoinsChanged -= HandleCoinsChanged;
            }

            if (playerInventory != null)
            {
                playerInventory.OnItemAdded -= HandleInventoryChanged;
                playerInventory.OnItemRemoved -= HandleInventoryChanged;
            }

            if (ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.OnObjectivesChanged -= RefreshObjectives;
        }

        void Update()
        {
            if (playerHealth == null) return;

            // stamina regenerates continuously, so this one genuinely is a per-frame value
            // rather than an event
            if (staminaBar != null)
                staminaBar.value = playerHealth.stats.currentStamina;

            RefreshAbilityIcons();
        }

        void RefreshAbilityIcons()
        {
            if (playerAbilities == null) return;

            float stamina = playerHealth.stats.currentStamina;

            if (dashIcon != null)
                dashIcon.color = stamina >= playerAbilities.DashCost
                    ? abilityReadyColor : abilityBlockedColor;

            if (heavyStrikeIcon != null)
                heavyStrikeIcon.color = stamina >= playerAbilities.HeavyStrikeCost
                    ? abilityReadyColor : abilityBlockedColor;
        }

        void HandleHealthChanged(int current, int max)
        {
            if (healthBar == null) return;
            healthBar.maxValue = max;
            healthBar.value = current;
        }

        void HandleCoinsChanged(int coins)
        {
            if (coinText != null)
                coinText.text = coins.ToString();
        }

        void HandleInventoryChanged(Item item) => RefreshInventory();

        void RefreshInventory()
        {
            if (inventoryText == null || playerInventory == null) return;

            var items = playerInventory.inventory.items;

            if (items.Count == 0)
            {
                inventoryText.text = "Empty";
                return;
            }

            // sorted so the display order is stable instead of pickup order
            playerInventory.inventory.SortByType();

            var sb = new StringBuilder();
            foreach (var item in items)
                sb.AppendLine(item.name);

            inventoryText.text = sb.ToString();
        }

        void RefreshObjectives()
        {
            if (objectivesText == null || ObjectiveManager.Instance == null) return;

            var sb = new StringBuilder();

            foreach (var objective in ObjectiveManager.Instance.Objectives)
            {
                // tick/cross instead of the word "complete", keeps it readable at a glance
                sb.AppendLine($"{(objective.IsComplete ? "\u2713" : "\u2022")} {objective.Description}");
            }

            if (ObjectiveManager.Instance.AllComplete)
                sb.AppendLine("\u2713 Exit is open");

            objectivesText.text = sb.ToString();
        }
    }
}