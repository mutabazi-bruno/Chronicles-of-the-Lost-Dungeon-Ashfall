using UnityEngine;
using Ashfall.Core;
using Ashfall.Interfaces;
using Ashfall.Player;

namespace Ashfall.Interactables
{
    public class Chest : MonoBehaviour, IInteractable
    {
        [Header("Loot")]
        [Tooltip("Name shown in the inventory")]
        public string potionName = "Health Potion";

        [Tooltip("How much this potion heals when drunk")]
        public int potionHealAmount = 25;

        [Tooltip("How many potions this chest contains")]
        public int potionCount = 1;

        public AudioClip openSound;

        bool isOpened;
        Animator animator;
        Collider2D col;

        void Awake()
        {
            animator = GetComponent<Animator>();
            col = GetComponent<Collider2D>();
        }

        public void Interact()
        {
            if (isOpened) return;
            isOpened = true;

            GiveReward();

            if (animator != null)
                animator.SetBool("IsOpened", true);

            Ashfall.Systems.AudioManager.Instance?.PlaySFX(openSound);

            // stop it being interactable again, but keep it visible in its opened state
            if (col != null)
                col.enabled = false;
        }

        // Hand over a potion to inventory.
        void GiveReward()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;

            var inventory = playerObj.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning("[Chest] player has no PlayerInventory component");
                return;
            }

            for (int i = 0; i < potionCount; i++)
            {
                // value doubles as the heal amount, which is what SortByValue orders on
                inventory.AddItem(new Item(potionName, ItemType.Potion, potionHealAmount));
            }
        }
    }
}