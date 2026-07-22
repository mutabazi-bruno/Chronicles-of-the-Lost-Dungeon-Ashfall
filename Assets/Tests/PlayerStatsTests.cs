using NUnit.Framework;
using Ashfall.Core;

public class PlayerStatsTests
{
    [Test]
    public void TakeDamage_MoreThanCurrentHealth_HealthClampsToZero()
    {
        // 100 hp, hit for 150, should not go negative
        var stats = new PlayerStats(maxHealth: 100);

        stats.TakeDamage(150);

        Assert.AreEqual(0, stats.currentHealth);
        Assert.IsTrue(stats.IsDead);
    }
}