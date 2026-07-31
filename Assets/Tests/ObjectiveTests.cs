using NUnit.Framework;
using Ashfall.Core;

public class ObjectiveTests
{
    [Test]
    public void DefeatEnemies_AllKilled_ObjectiveCompletes()
    {
        var objective = new DefeatEnemiesObjective(3);

        objective.RegisterKill();
        objective.RegisterKill();

        Assert.IsFalse(objective.IsComplete, "two of three kills should not complete it");

        objective.RegisterKill();

        Assert.IsTrue(objective.IsComplete);
        Assert.AreEqual(3, objective.Current);
    }

    [Test]
    public void DefeatEnemies_ExtraKills_DoNotInflateCounter()
    {
        // an enemy dying after the objective is met shouldn't push the counter past the total
        var objective = new DefeatEnemiesObjective(2);

        objective.RegisterKill();
        objective.RegisterKill();
        objective.RegisterKill();

        Assert.AreEqual(2, objective.Current);
    }

    [Test]
    public void CollectCoins_ReachingTarget_CompletesObjective()
    {
        var objective = new CollectCoinsObjective(10);

        objective.SetCollected(9);
        Assert.IsFalse(objective.IsComplete);

        objective.SetCollected(10);
        Assert.IsTrue(objective.IsComplete);
    }

    [Test]
    public void CollectCoins_NegativeAmount_ClampsToZero()
    {
        var objective = new CollectCoinsObjective(5);

        objective.SetCollected(-3);

        Assert.AreEqual(0, objective.Current);
        Assert.IsFalse(objective.IsComplete);
    }

    [Test]
    public void ReachExit_StartsIncomplete_AndCompletesOnReach()
    {
        var objective = new ReachExitObjective();

        Assert.IsFalse(objective.IsComplete);

        objective.Reach();

        Assert.IsTrue(objective.IsComplete);
    }
}