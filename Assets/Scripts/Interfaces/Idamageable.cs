namespace Ashfall.Interfaces
{
    // anything that can take damage - player, enemies, breakable stuff
    public interface IDamageable
    {
        void TakeDamage(int amount);
        bool IsDead { get; }
    }
}