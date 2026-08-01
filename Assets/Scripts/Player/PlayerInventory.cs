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

        // O(n) search by type and name.
        public Item FindItem(ItemType type, string itemName)
        {
            foreach (var item in inventory.items)
            {
                if (item.type == type && item.name == itemName)
                    return item;
            }
            return null;
        }

        // Select highest value potion to maximize healing.
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