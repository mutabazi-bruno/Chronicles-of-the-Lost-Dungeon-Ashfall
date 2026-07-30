using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashfall.Systems
{
    // singleton - knows the level order and talks to SaveManager for unlock/complete state
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        // order matters here, this defines level progression
        public List<string> levelOrder = new List<string>
        {
            "Level1", "Level2", "Level3", "Level4", "Level5"
        };

        public event Action<string> OnLevelUnlocked; // observer, level select ui listens to this
        public event Action<string> OnLevelCompleted; // observer, audio/ui can listen for the win moment

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsUnlocked(string levelId)
        {
            return SaveManager.Instance.CurrentSave.IsLevelUnlocked(levelId);
        }

        public bool IsCompleted(string levelId)
        {
            return SaveManager.Instance.CurrentSave.IsLevelCompleted(levelId);
        }

        // called when the player finishes a level
        public void CompleteLevel(string levelId)
        {
            var save = SaveManager.Instance.CurrentSave;
            save.CompleteLevel(levelId);
            OnLevelCompleted?.Invoke(levelId);

            string next = GetNextLevel(levelId);
            if (next != null && !save.IsLevelUnlocked(next))
            {
                save.UnlockLevel(next);
                OnLevelUnlocked?.Invoke(next);
            }

            SaveManager.Instance.Save();
        }

        string GetNextLevel(string currentLevelId)
        {
            int index = levelOrder.IndexOf(currentLevelId);
            if (index < 0 || index + 1 >= levelOrder.Count) return null;
            return levelOrder[index + 1];
        }
    }
}