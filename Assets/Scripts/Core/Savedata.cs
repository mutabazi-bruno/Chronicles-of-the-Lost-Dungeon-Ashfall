using System;
using System.Collections.Generic;

namespace Ashfall.Core
{
    // everything in here gets written straight to the save file
    // keep it plain, no unity refs, so json utility can serialize it easy
    [Serializable]
    public class SaveData
    {
        public List<string> unlockedLevels = new List<string>();
        public List<string> completedLevels = new List<string>();

        public int health;
        public int maxHealth;
        public float stamina;
        public float maxStamina;
        public int coins;

        public List<Item> inventory = new List<Item>();

        public float musicVolume = 1f;
        public float sfxVolume = 1f;

        // fresh save, only level 1 unlocked
        public static SaveData CreateNew()
        {
            var data = new SaveData();
            data.unlockedLevels.Add("Level1");
            data.maxHealth = 100;
            data.health = 100;
            data.maxStamina = 100;
            data.stamina = 100;
            return data;
        }

        public bool IsLevelUnlocked(string levelId)
        {
            return unlockedLevels.Contains(levelId);
        }

        public bool IsLevelCompleted(string levelId)
        {
            return completedLevels.Contains(levelId);
        }

        public void UnlockLevel(string levelId)
        {
            if (!unlockedLevels.Contains(levelId))
                unlockedLevels.Add(levelId);
        }

        public void CompleteLevel(string levelId)
        {
            if (!completedLevels.Contains(levelId))
                completedLevels.Add(levelId);
        }
    }
}