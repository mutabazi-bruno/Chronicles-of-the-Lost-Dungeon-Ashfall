using System;
using System.Collections.Generic;

namespace Ashfall.Core
{
    public enum ItemType
    {
        Coin,
        Potion,
        Key,
        Collectible
    }

    [Serializable]
    public class Item
    {
        public string name;
        public ItemType type;
        public int value;

        public Item(string name, ItemType type, int value)
        {
            this.name = name;
            this.type = type;
            this.value = value;
        }
    }

    // plain inventory, no unity stuff so its easy to test
    public class InventoryLogic
    {
        public List<Item> items = new List<Item>();

        public void AddItem(Item item)
        {
            items.Add(item);
        }

        public bool RemoveItem(Item item)
        {
            return items.Remove(item);
        }

        // sort algorithm #1 - highest value first
        public void SortByValue()
        {
            items.Sort((a, b) => b.value.CompareTo(a.value));
        }

        // sort algorithm #2 - group by item type (alphabetical by enum order)
        public void SortByType()
        {
            items.Sort((a, b) => a.type.CompareTo(b.type));
        }
    }
}   