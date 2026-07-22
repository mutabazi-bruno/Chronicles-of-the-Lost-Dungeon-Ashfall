using NUnit.Framework;
using Ashfall.Core;

public class DamageCalculatorTests
{
    [Test]
    public void CalculateDamage_AttackHigherThanDefense_ReturnsDifference()
    {
        int dmg = DamageCalculator.CalculateDamage(attackPower: 10, defense: 4);

        Assert.AreEqual(6, dmg);
    }

    [Test]
    public void CalculateDamage_DefenseHigherThanAttack_StillDealsMinimumOne()
    {
        int dmg = DamageCalculator.CalculateDamage(attackPower: 1, defense: 100);

        Assert.AreEqual(1, dmg);
    }
}