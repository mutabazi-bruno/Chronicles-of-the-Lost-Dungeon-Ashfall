using System;
using UnityEngine;
using Ashfall.Core;

namespace Ashfall.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        public InventoryLogic inventory = new InventoryLogic();

        public event Action<Item> OnItemAdded; // observer, ui can react later

        public void AddItem(Item item)
        {
            inventory.AddItem(item);
            OnItemAdded?.Invoke(item);
            Debug.Log($"picked up {item.name}");
        }

        public bool HasKey(string keyName = "Key")
        {
            foreach (var item in inventory.items)
            {
                if (item.type == ItemType.Key && item.name == keyName)
                    return true;
            }
            return false;
        }

        public void RemoveKey(string keyName = "Key")
        {
            for (int i = 0; i < inventory.items.Count; i++)
            {
                if (inventory.items[i].type == ItemType.Key && inventory.items[i].name == keyName)
                {
                    inventory.items.RemoveAt(i);
                    return;
                }
            }
        }
    }
}