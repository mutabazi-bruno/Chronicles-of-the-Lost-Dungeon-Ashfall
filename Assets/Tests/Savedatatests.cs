using NUnit.Framework;
using Ashfall.Core;

public class SaveDataTests
{
    [Test]
    public void CreateNew_FreshSave_OnlyLevel1Unlocked()
    {
        var save = SaveData.CreateNew();

        Assert.IsTrue(save.IsLevelUnlocked("Level1"));
        Assert.IsFalse(save.IsLevelUnlocked("Level2"));
    }

    [Test]
    public void CompleteLevel_MarksLevelAsCompleted()
    {
        var save = SaveData.CreateNew();

        save.CompleteLevel("Level1");

        Assert.IsTrue(save.IsLevelCompleted("Level1"));
    }
}