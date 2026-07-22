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
}