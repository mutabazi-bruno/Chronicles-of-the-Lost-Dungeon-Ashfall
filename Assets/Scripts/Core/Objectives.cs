using System;

namespace Ashfall.Core
{
    // Objective rules logic.
    public interface IObjective
    {
        string Description { get; }
        bool IsComplete { get; }
        int Current { get; }
        int Required { get; }
    }

    public enum ObjectiveType
    {
        DefeatEnemies,
        CollectCoins,
        ReachExit
    }

    public class DefeatEnemiesObjective : IObjective
    {
        public int Required { get; private set; }
        public int Current { get; private set; }

        public DefeatEnemiesObjective(int required)
        {
            Required = required < 0 ? 0 : required;
        }

        public string Description => $"Defeat enemies  {Current}/{Required}";

        public bool IsComplete => Current >= Required;

        public void RegisterKill()
        {
            // clamped so an extra kill after completion can't inflate the counter
            if (Current < Required) Current++;
        }
    }

  
    public class CollectCoinsObjective : IObjective
    {
        public int Required { get; private set; }
        public int Current { get; private set; }

        public CollectCoinsObjective(int required)
        {
            Required = required < 0 ? 0 : required;
        }

        public string Description => $"Collect coins  {Current}/{Required}";

        public bool IsComplete => Current >= Required;

        public void SetCollected(int amount)
        {
            Current = amount < 0 ? 0 : amount;
        }
    }

    // Always the last objective - satisfied by touching the exit, so it reads as complete
    // only once the player is actually standing there.
    public class ReachExitObjective : IObjective
    {
        public string Description => "Reach the exit";
        public bool IsComplete { get; private set; }
        public int Current => IsComplete ? 1 : 0;
        public int Required => 1;

        public void Reach() => IsComplete = true;
    }
}