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

    [Test]
    public void SpendStamina_NotEnoughStamina_ReturnsFalseAndDoesNotDrain()
    {
        // 100 stamina, ability costs 150, should fail and stamina unchanged
        var stats = new PlayerStats(maxStamina: 100);

        bool spent = stats.SpendStamina(150);

        Assert.IsFalse(spent);
        Assert.AreEqual(100, stats.currentStamina);
    }
}