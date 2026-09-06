using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Ashfall.Core;
using Ashfall.Enemies;
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
        public TMP_Text chapterText;
        public TMP_Text objectivesText;

        [Serializable]
        public class LevelTitle
        {
            public string sceneName;
            public string chapter;
            public string placeName;
        }

        [Tooltip("Chapter + place shown bottom-right. Falls back to the raw scene " +
                 "name if the level isn't listed.")]
        public List<LevelTitle> levelTitles = new List<LevelTitle>();

        [Header("Vital readouts (optional, e.g. \"140/180\")")]
        public TMP_Text healthValueText;
        public TMP_Text staminaValueText;

        [Header("Objectives board")]
        public TMP_Text enemiesText;
        [Tooltip("Board grows with the objective count; height = base + line * count")]
        public RectTransform objectivesCard;
        public float objectivesBaseHeight = 96f;
        public float objectivesLineHeight = 44f;

        [Header("Ability indicators")]
        [Tooltip("Icon dims when there isn't enough stamina to use the ability")]
        public Image dashIcon;
        public Image heavyStrikeIcon;
        public Color abilityReadyColor = Color.white;
        public Color abilityBlockedColor = new Color(1f, 1f, 1f, 0.35f);

        [Header("Inventory")]
        public TMP_Text inventoryText;
        [Tooltip("Card shrinks to the empty height and grows a row at a time")]
        public RectTransform inventoryCard;
        public float inventoryBaseHeight = 84f;
        public float inventoryRowHeight = 34f;
        public float inventoryEmptyHeight = 110f;

        int enemiesRemaining;
        ObjectiveManager subscribedObjectives;

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
                playerInventory.OnSelectionChanged += RefreshInventory;
                RefreshInventory();
            }

            if (ObjectiveManager.Instance != null)
            {
                subscribedObjectives = ObjectiveManager.Instance;
                subscribedObjectives.OnObjectivesChanged += RefreshObjectives;
                RefreshObjectives();
            }

            ApplyLevelTitle();

            // counted once from what the scene spawned with, then kept current from
            // the death event rather than rescanning every frame
            enemiesRemaining = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
            Enemy.OnAnyEnemyDeath += HandleEnemyDefeated;
            RefreshEnemies();
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
                playerInventory.OnSelectionChanged -= RefreshInventory;
            }

            if (subscribedObjectives != null)
            {
                subscribedObjectives.OnObjectivesChanged -= RefreshObjectives;
                subscribedObjectives = null;
            }

            Enemy.OnAnyEnemyDeath -= HandleEnemyDefeated;
        }

        void ApplyLevelTitle()
        {
            string scene = SceneManager.GetActiveScene().name;
            LevelTitle match = levelTitles.Find(t => t != null && t.sceneName == scene);

            if (levelNameText != null)
                levelNameText.text = match != null ? match.placeName : scene;

            if (chapterText != null)
                chapterText.text = match != null ? match.chapter : string.Empty;
        }

        void HandleEnemyDefeated()
        {
            enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
            RefreshEnemies();
        }

        void RefreshEnemies()
        {
            if (enemiesText != null)
                enemiesText.text = enemiesRemaining.ToString("000");
        }

        void Update()
        {
            if (playerHealth == null) return;

            // stamina regenerates continuously, so this one genuinely is a per-frame value
            // rather than an event
            if (staminaBar != null)
                staminaBar.value = playerHealth.stats.currentStamina;

            if (staminaValueText != null)
                staminaValueText.text =
                    $"{Mathf.CeilToInt(playerHealth.stats.currentStamina)}/{playerHealth.stats.maxStamina}";

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
            if (healthValueText != null)
                healthValueText.text = $"{current}/{max}";

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

            var stacks = playerInventory.GetStacks();

            if (stacks.Count == 0)
            {
                inventoryText.text = "<color=#7E7668>EMPTY</color>";
                ResizeInventoryCard(0);
                return;
            }

            var sb = new StringBuilder();

            for (int i = 0; i < stacks.Count; i++)
            {
                var stack = stacks[i];
                bool selected = i == playerInventory.SelectedSlot;

                // the number is the key that selects the row, so it is always shown
                string row = $"{i + 1}. {IconFor(stack.type)}{stack.name}";
                if (stack.count > 1) row += $" x{stack.count}";

                sb.AppendLine(selected
                    ? $"<color=#E8C06A>> {row}</color>"
                    : $"<color=#D8D0BE>  {row}</color>");
            }

            inventoryText.text = sb.ToString();
            ResizeInventoryCard(stacks.Count);
        }

        static string IconFor(ItemType type)
        {
            // only these two have sprites in the atlas; anything else stays text-only
            if (type == ItemType.Key) return "<sprite name=\"key\"> ";
            if (type == ItemType.Potion) return "<sprite name=\"potion\"> ";
            return string.Empty;
        }

        void ResizeInventoryCard(int rows)
        {
            if (inventoryCard == null) return;

            float height = rows == 0
                ? inventoryEmptyHeight
                : inventoryBaseHeight + inventoryRowHeight * rows;

            inventoryCard.sizeDelta = new Vector2(inventoryCard.sizeDelta.x, height);
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

            ResizeObjectivesBoard();
        }

        // The board is meant to grow with the list rather than clip it or leave a
        // gap, so its height is driven from the number of lines actually shown.
        void ResizeObjectivesBoard()
        {
            if (objectivesCard == null || ObjectiveManager.Instance == null) return;

            int lines = ObjectiveManager.Instance.Objectives.Count;
            if (ObjectiveManager.Instance.AllComplete) lines++;

            float height = objectivesBaseHeight + objectivesLineHeight * Mathf.Max(1, lines);
            objectivesCard.sizeDelta = new Vector2(objectivesCard.sizeDelta.x, height);
        }
    }
}