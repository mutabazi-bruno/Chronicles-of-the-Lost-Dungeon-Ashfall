using System;
using System.Collections.Generic;
using UnityEngine;
using Ashfall.Core;
using Ashfall.Interfaces;
using Ashfall.Systems;

namespace Ashfall.Player
{
    public class PlayerInventory : MonoBehaviour, ISaveable
    {
        public InventoryLogic inventory = new InventoryLogic();

        public event Action<Item> OnItemAdded;   // observer, ui can react
        public event Action<Item> OnItemRemoved;
        public event Action<Item> OnItemUsed;

        PlayerHealth health;

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            if (GameInput.UsePotionPressed)
                ConsumeBestPotion();
        }

        public void AddItem(Item item)
        {
            inventory.AddItem(item);
            OnItemAdded?.Invoke(item);
        }

        // linear search by type + name. Inventories here are small (a handful of items), so
        // a plain O(n) scan beats the overhead of maintaining a dictionary, and it keeps the
        // ordering that the sort algorithms rely on.
        public Item FindItem(ItemType type, string itemName)
        {
            foreach (var item in inventory.items)
            {
                if (item.type == type && item.name == itemName)
                    return item;
            }
            return null;
        }

        // Greedy selection: sort by value descending, then take the first potion. Drinking
        // the strongest potion first is the choice that wastes the least healing when the
        // player is badly hurt, which is when they actually reach for one.
        public bool ConsumeBestPotion()
        {
            if (health == null || health.IsDead) return false;

            inventory.SortByValue();

            Item potion = null;
            foreach (var item in inventory.items)
            {
                if (item.type == ItemType.Potion)
                {
                    potion = item;
                    break;
                }
            }

            if (potion == null) return false;

            // don't burn a potion at full health
            if (health.stats.currentHealth >= health.stats.maxHealth) return false;

            health.Heal(potion.value);

            inventory.RemoveItem(potion);
            OnItemUsed?.Invoke(potion);
            OnItemRemoved?.Invoke(potion);

            return true;
        }

        public bool HasKey(string keyName = "Key")
        {
            return FindItem(ItemType.Key, keyName) != null;
        }

        public void RemoveKey(string keyName = "Key")
        {
            var key = FindItem(ItemType.Key, keyName);
            if (key == null) return;

            inventory.RemoveItem(key);
            OnItemRemoved?.Invoke(key);
        }

        // ISaveable - the inventory persists across levels along with the player's coins
        public void Save(SaveData data)
        {
            data.inventory = new List<Item>(inventory.items);
        }

        public void Load(SaveData data)
        {
            inventory.items = data.inventory != null
                ? new List<Item>(data.inventory)
                : new List<Item>();

            foreach (var item in inventory.items)
                OnItemAdded?.Invoke(item);
        }
    }
}