using NUnit.Framework;
using Ashfall.Core;

public class InventoryLogicTests
{
    [Test]
    public void SortByValue_UnorderedItems_SortsHighestFirst()
    {
        var inventory = new InventoryLogic();
        inventory.AddItem(new Item("Coin", ItemType.Coin, 1));
        inventory.AddItem(new Item("Rare Gem", ItemType.Collectible, 50));
        inventory.AddItem(new Item("Potion", ItemType.Potion, 10));

        inventory.SortByValue();

        Assert.AreEqual("Rare Gem", inventory.items[0].name);
        Assert.AreEqual("Potion", inventory.items[1].name);
        Assert.AreEqual("Coin", inventory.items[2].name);
    }

    [Test]
    public void RemoveItem_ItemInInventory_RemovesAndReturnsTrue()
    {
        var inventory = new InventoryLogic();
        var potion = new Item("Potion", ItemType.Potion, 10);
        inventory.AddItem(potion);

        bool removed = inventory.RemoveItem(potion);

        Assert.IsTrue(removed);
        Assert.AreEqual(0, inventory.items.Count);
    }

    [Test]
    public void SortByType_UnorderedItems_GroupsByEnumOrder()
    {
        // enum order is Coin, Potion, Key, Collectible
        var inventory = new InventoryLogic();
        inventory.AddItem(new Item("Rare Gem", ItemType.Collectible, 50));
        inventory.AddItem(new Item("Coin", ItemType.Coin, 1));
        inventory.AddItem(new Item("Rusty Key", ItemType.Key, 0));

        inventory.SortByType();

        Assert.AreEqual(ItemType.Coin, inventory.items[0].type);
        Assert.AreEqual(ItemType.Key, inventory.items[1].type);
        Assert.AreEqual(ItemType.Collectible, inventory.items[2].type);
    }
}