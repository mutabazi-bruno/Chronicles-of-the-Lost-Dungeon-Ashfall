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
        public event Action OnSelectionChanged;

        // Identical items collapse into one row so the list stays short. The HUD
        // renders exactly this, and selection indexes into it, so the numbers on
        // screen and the slot the player picks cannot drift apart.
        public class Stack
        {
            public string name;
            public ItemType type;
            public int count;
            public int bestValue;
        }

        public int SelectedSlot { get; private set; } = -1;

        PlayerHealth health;

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        void OnEnable()
        {
            SaveManager.Instance?.Register(this);
        }

        void OnDisable()
        {
            SaveManager.Instance?.Unregister(this);
        }

        void Update()
        {
            int slot = GameInput.SlotPressed;
            if (slot > 0)
                SelectSlot(slot - 1);

            if (GameInput.UsePotionPressed)
                UseSelected();
        }

        public List<Stack> GetStacks()
        {
            var stacks = new List<Stack>();

            foreach (var item in inventory.items)
            {
                if (item.type == ItemType.Coin) continue;   // coins show on the coin counter

                var stack = stacks.Find(s => s.type == item.type && s.name == item.name);
                if (stack == null)
                {
                    stack = new Stack { name = item.name, type = item.type };
                    stacks.Add(stack);
                }

                stack.count++;
                stack.bestValue = Mathf.Max(stack.bestValue, item.value);
            }

            stacks.Sort((a, b) => a.type != b.type
                ? a.type.CompareTo(b.type)
                : string.Compare(a.name, b.name, StringComparison.Ordinal));

            return stacks;
        }

        public void SelectSlot(int index)
        {
            var stacks = GetStacks();
            int clamped = stacks.Count == 0 ? -1 : Mathf.Clamp(index, 0, stacks.Count - 1);

            if (clamped == SelectedSlot) return;

            SelectedSlot = clamped;
            OnSelectionChanged?.Invoke();
        }

        // Uses whatever is selected. Only potions are consumable by hand - keys are
        // spent by the doors themselves - so anything else is left alone.
        public bool UseSelected()
        {
            var stacks = GetStacks();
            if (stacks.Count == 0) return false;

            if (SelectedSlot < 0 || SelectedSlot >= stacks.Count)
                return ConsumeBestPotion();     // nothing picked: keep the old behaviour

            var stack = stacks[SelectedSlot];
            if (stack.type != ItemType.Potion)
                return false;

            return ConsumePotion(stack.name);
        }

        bool ConsumePotion(string potionName)
        {
            if (health == null || health.IsDead) return false;
            if (health.stats.currentHealth >= health.stats.maxHealth) return false;

            Item best = null;
            foreach (var item in inventory.items)
            {
                if (item.type != ItemType.Potion || item.name != potionName) continue;
                if (best == null || item.value > best.value) best = item;
            }

            if (best == null) return false;

            health.Heal(best.value);
            inventory.RemoveItem(best);
            OnItemUsed?.Invoke(best);
            OnItemRemoved?.Invoke(best);
            ClampSelection();
            return true;
        }

        void ClampSelection()
        {
            int count = GetStacks().Count;
            SelectedSlot = count == 0 ? -1 : Mathf.Min(SelectedSlot, count - 1);
            OnSelectionChanged?.Invoke();
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
            ClampSelection();
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
