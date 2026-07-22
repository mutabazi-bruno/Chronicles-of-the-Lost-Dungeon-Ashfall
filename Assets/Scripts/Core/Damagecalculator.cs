namespace Ashfall.Core
{
    // simple damage formula, keeping it plain so its easy to test and tweak later
    public static class DamageCalculator
    {
        // attack minus defense, but always deal at least 1 dmg
        // so stacking defense cant make you literally unkillable
        public static int CalculateDamage(int attackPower, int defense)
        {
            int dmg = attackPower - defense;
            return dmg < 1 ? 1 : dmg;
        }
    }
}