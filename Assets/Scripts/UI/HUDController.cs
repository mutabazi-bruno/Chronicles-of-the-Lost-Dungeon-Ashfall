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
    // Event-driven HUD updates to stay decoupled from gameplay.
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

            int keyCount = 0;
            int potionCount = 0;

            foreach (var item in playerInventory.inventory.items)
            {
                if (item.type == ItemType.Key) keyCount++;
                else if (item.type == ItemType.Potion) potionCount++;
            }

            var parts = new System.Collections.Generic.List<string>();
            if (keyCount > 0) parts.Add($"<sprite name=\"key\"> x{keyCount}");
            if (potionCount > 0) parts.Add($"<sprite name=\"potion\"> x{potionCount}");

            // side by side on one line, with some spacing between them, instead of stacked
            inventoryText.text = string.Join("      ", parts);
        }

        void RefreshObjectives()
        {
            if (objectivesText == null || ObjectiveManager.Instance == null) return;

            var sb = new StringBuilder();

            foreach (var objective in ObjectiveManager.Instance.Objectives)
            {
                // real icon instead of a text glyph, so it renders regardless of font coverage
                string mark = objective.IsComplete ? "<sprite name=\"check\">" : "\u2022";
                string line = $"{mark} {objective.Description}";

                // TMP rich text - green once complete, default color otherwise
                if (objective.IsComplete)
                    sb.AppendLine($"<color=#4CE082>{line}</color>");
                else
                    sb.AppendLine(line);
            }

            if (ObjectiveManager.Instance.AllComplete)
                sb.AppendLine("<color=#4CE082><sprite name=\"check\"> Exit is open</color>");

            objectivesText.text = sb.ToString();
        }
    }
}