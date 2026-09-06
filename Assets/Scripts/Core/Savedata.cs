using System;
using System.Collections.Generic;

namespace Ashfall.Core
{
    // One placed object's persisted state: an enemy, chest, switch, door or pickup.
    [Serializable]
    public class ObjectState
    {
        public string id;        // hierarchy path id, see PersistentId
        public bool consumed;    // dead / opened / activated / collected
        public int health = -1;  // enemies only, -1 when it does not apply
    }

    // World state is stored per scene. JsonUtility cannot serialise dictionaries,
    // so this is a list that gets looked up by name.
    [Serializable]
    public class SceneState
    {
        public string sceneName;
        public List<ObjectState> objects = new List<ObjectState>();
    }

    // Serializable save file data structure.
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

        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool audioMuted = false;

        // player position and last scene, so autosave can resume mid-level
        public float playerX;
        public float playerY;
        public string lastScene = "";

        // true while a level is part-played, which is what makes the level select
        // row offer CONTINUE instead of PLAY
        public bool runInProgress;

        // per-scene world state: which enemies are dead, what has been opened or taken
        public List<SceneState> sceneStates = new List<SceneState>();

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

        // -- world state --

        public SceneState GetSceneState(string sceneName, bool createIfMissing)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;

            foreach (var state in sceneStates)
            {
                if (state != null && state.sceneName == sceneName)
                    return state;
            }

            if (!createIfMissing) return null;

            var created = new SceneState { sceneName = sceneName };
            sceneStates.Add(created);
            return created;
        }

        // Forgetting a scene is how a level goes back to being untouched - used when
        // the level is finished and when the player dies and restarts it.
        public void ClearSceneState(string sceneName)
        {
            sceneStates.RemoveAll(s => s == null || s.sceneName == sceneName);
        }

        // Drops the mid-level resume point without touching unlocks or completion.
        public void ClearResumePoint()
        {
            runInProgress = false;
            lastScene = "";
            playerX = 0f;
            playerY = 0f;
        }

        public bool HasResumePointFor(string levelId)
        {
            return runInProgress
                && !string.IsNullOrEmpty(lastScene)
                && lastScene == levelId;
        }
    }
}
