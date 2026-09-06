using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Core;
using Ashfall.Enemies;
using Ashfall.Interactables;
using Ashfall.Interfaces;

namespace Ashfall.Systems
{
    // Remembers what the player already did to a level: which enemies are dead and
    // how hurt the survivors are, which chests and doors are open, which switches
    // are thrown, and which pickups are gone.
    //
    // Implemented as an ISaveable so it rides the existing SaveAll/LoadIntoScene
    // machinery rather than inventing a second save path.
    public class WorldStateManager : MonoBehaviour, ISaveable
    {
        public static WorldStateManager Instance { get; private set; }

        [Tooltip("Log what gets captured and restored. Useful while testing, noisy in a build.")]
        public bool logWorldState = false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnEnable()
        {
            SaveManager.Instance?.Register(this);
        }

        void OnDisable()
        {
            SaveManager.Instance?.Unregister(this);
        }

        static bool IsGameplayScene(string sceneName)
        {
            var manager = GameManager.Instance;
            return manager != null && manager.gameplayScenes.Contains(sceneName);
        }

        // -- capture --

        public void Save(SaveData data)
        {
            if (data == null) return;

            string scene = SceneManager.GetActiveScene().name;
            if (!IsGameplayScene(scene)) return;

            var state = data.GetSceneState(scene, true);
            state.objects.Clear();

            int count = 0;

            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                Record(state, PersistentId.For(enemy), enemy.IsDead, enemy.currentHealth);
                count++;
            }

            foreach (var chest in FindObjectsByType<Chest>(FindObjectsSortMode.None))
            {
                Record(state, PersistentId.For(chest), chest.IsOpened, -1);
                count++;
            }

            foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                Record(state, PersistentId.For(door), door.IsOpen, -1);
                count++;
            }

            foreach (var toggle in FindObjectsByType<Switch>(FindObjectsSortMode.None))
            {
                Record(state, PersistentId.For(toggle), toggle.isActivated, -1);
                count++;
            }

            // Pickups are destroyed when taken, so anything still in the scene is
            // simply "not consumed". Their absence from a later scan is what marks
            // them collected, which is why the list is rebuilt from scratch above.
            foreach (var pickup in FindObjectsByType<Collectible>(FindObjectsSortMode.None))
            {
                Record(state, PersistentId.For(pickup), false, -1);
                count++;
            }

            foreach (var key in FindObjectsByType<KeyPickup>(FindObjectsSortMode.None))
            {
                Record(state, PersistentId.For(key), false, -1);
                count++;
            }

            if (logWorldState)
                Debug.Log($"[WorldState] captured {count} objects in {scene}");
        }

        static void Record(SceneState state, string id, bool consumed, int health)
        {
            if (string.IsNullOrEmpty(id)) return;

            state.objects.Add(new ObjectState
            {
                id = id,
                consumed = consumed,
                health = health
            });
        }

        // -- restore --

        public void Load(SaveData data)
        {
            if (data == null) return;

            string scene = SceneManager.GetActiveScene().name;
            if (!IsGameplayScene(scene)) return;

            var state = data.GetSceneState(scene, false);
            if (state == null || state.objects.Count == 0) return;

            // A save only describes the level the player was actually in. Restoring
            // it into a different level would be meaningless, and restoring it into a
            // level they finished would undo the reset.
            if (!data.HasResumePointFor(scene)) return;

            var saved = new Dictionary<string, ObjectState>();
            foreach (var entry in state.objects)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.id))
                    saved[entry.id] = entry;
            }

            int applied = 0;

            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                if (!saved.TryGetValue(PersistentId.For(enemy), out var entry)) continue;
                enemy.RestoreState(entry.consumed, entry.health);
                applied++;
            }

            foreach (var chest in FindObjectsByType<Chest>(FindObjectsSortMode.None))
            {
                if (!saved.TryGetValue(PersistentId.For(chest), out var entry)) continue;
                if (entry.consumed) chest.RestoreOpened();
                applied++;
            }

            foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                if (!saved.TryGetValue(PersistentId.For(door), out var entry)) continue;
                if (entry.consumed) door.RestoreOpened();
                applied++;
            }

            foreach (var toggle in FindObjectsByType<Switch>(FindObjectsSortMode.None))
            {
                if (!saved.TryGetValue(PersistentId.For(toggle), out var entry)) continue;
                if (entry.consumed) toggle.isActivated = true;
                applied++;
            }

            // Anything the save never listed was already gone when the game was
            // saved, so it stays gone.
            foreach (var pickup in FindObjectsByType<Collectible>(FindObjectsSortMode.None))
            {
                if (saved.ContainsKey(PersistentId.For(pickup))) continue;
                Destroy(pickup.gameObject);
                applied++;
            }

            foreach (var key in FindObjectsByType<KeyPickup>(FindObjectsSortMode.None))
            {
                if (saved.ContainsKey(PersistentId.For(key))) continue;
                Destroy(key.gameObject);
                applied++;
            }

            if (logWorldState)
                Debug.Log($"[WorldState] restored {applied} objects in {scene}");
        }
    }
}
