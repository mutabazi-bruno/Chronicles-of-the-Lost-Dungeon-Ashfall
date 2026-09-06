using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ashfall.Core;
using Ashfall.Interfaces;

namespace Ashfall.Systems
{
    //The only class that touches the save file on disk
    
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public SaveData CurrentSave { get; private set; }

        string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        readonly List<ISaveable> registeredSaveables = new List<ISaveable>();

        [Header("Autosave")]
        [Tooltip("Master switch. Off means progress is only written when a level is completed, " +
                 "which is how the game behaved before autosave existed.")]
        public bool autosaveEnabled = true;

        [Tooltip("Seconds between periodic autosaves during gameplay. Set to 0 to only save on " +
                 "pause, focus loss and quit.")]
        public float autosaveIntervalSeconds = 30f;

        [Tooltip("Ignore autosave requests that arrive closer together than this. Backgrounding " +
                 "an app usually fires both OnApplicationPause and OnApplicationFocus, and there " +
                 "is no reason to write the file twice.")]
        public float minSecondsBetweenAutosaves = 2f;

        [Tooltip("Log every autosave to the console. Useful while testing, noisy in a build.")]
        public bool logAutosaves = false;

        // unscaled so a paused game (timeScale 0) still throttles correctly
        float lastAutosaveTime = float.NegativeInfinity;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadOrCreate();

            SceneManager.sceneLoaded += OnSceneLoaded;

#if UNITY_WEBGL && !UNITY_EDITOR
            // Ask the browser to tell us when the page is going away, since closing a
            // tab does not raise OnApplicationQuit.
            AshfallRegisterUnloadHook(gameObject.name, nameof(WebGLFlush));
#endif
        }

        void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // LoadIntoScene had no caller at all, which is why a saved inventory was
        // written to disk and never came back. sceneLoaded is the right moment: it
        // runs after every Awake/OnEnable, so all ISaveables have registered, and
        // before any Start, so nothing has acted on default values yet.
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var manager = GameManager.Instance;
            bool gameplay = manager != null && manager.gameplayScenes.Contains(scene.name);
            if (!gameplay) return;

            LoadIntoScene();
        }


        public void Register(ISaveable saveable)
        {
            if (!registeredSaveables.Contains(saveable))
                registeredSaveables.Add(saveable);
        }

        public void Unregister(ISaveable saveable)
        {
            registeredSaveables.Remove(saveable);
        }

        public bool HasSaveFile()
        {
            return File.Exists(SavePath);
        }

        // Saves all registered ISaveable objects.
        public void SaveAll()
        {
            if (CurrentSave == null) CurrentSave = SaveData.CreateNew();

            // Saving from inside a level is what makes it resumable. Marking it here
            // rather than in one of the ISaveables keeps the flag from depending on
            // which components happen to be present in the scene.
            var manager = GameManager.Instance;
            string scene = SceneManager.GetActiveScene().name;
            if (manager != null && manager.gameplayScenes.Contains(scene))
                CurrentSave.runInProgress = true;

            foreach (var saveable in registeredSaveables)
                saveable.Save(CurrentSave);

            Save();
        }

        // Forgets a level's world state and the resume point, so it starts clean the
        // next time it is entered. Used when a level is finished and when the player
        // dies and restarts it.
        public void ClearLevelProgress(string sceneName)
        {
            if (CurrentSave == null) return;

            CurrentSave.ClearSceneState(sceneName);

            if (CurrentSave.lastScene == sceneName)
                CurrentSave.ClearResumePoint();

            Save();
        }

        // Mirror of SaveAll - pushes the loaded file back into the live scene objects.
        public void LoadIntoScene()
        {
            if (CurrentSave == null) return;

            foreach (var saveable in registeredSaveables)
                saveable.Load(CurrentSave);
        }

        public void Save()
        {
            if (CurrentSave == null) return;

            try
            {
                string json = JsonUtility.ToJson(CurrentSave, true);
                File.WriteAllText(SavePath, json);

#if UNITY_WEBGL && !UNITY_EDITOR
                // Required to flush WebGL filesystem to IndexedDB.
                SyncWebGLFiles();
#endif
            }
            catch (System.Exception e)
            {
                // a failed write should never take the game down with it
                Debug.LogError($"[SaveManager] couldn't write save file: {e.Message}");
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // Implemented in Assets/Plugins/WebGL/SaveSync.jslib. This replaces
        // Application.ExternalEval, which is deprecated in Unity 6.
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void AshfallSyncSaveFs();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void AshfallRegisterUnloadHook(string objectName, string methodName);

        void SyncWebGLFiles()
        {
            AshfallSyncSaveFs();
        }

        // Called from the browser when the page is being hidden or unloaded, which
        // is the one moment a browser reliably gives us and OnApplicationQuit does not.
        // Deliberately bypasses the autosave throttle - there is no "next time".
        public void WebGLFlush()
        {
            if (!IsGameplayState()) return;

            lastAutosaveTime = Time.unscaledTime;
            SaveAll();
        }
#endif

        public void Load()
        {
            if (!HasSaveFile())
            {
                CurrentSave = SaveData.CreateNew();
                return;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                CurrentSave = JsonUtility.FromJson<SaveData>(json);

                // Handle corrupt save files.
                if (CurrentSave == null)
                {
                    Debug.LogWarning("[SaveManager] save file unreadable, starting fresh");
                    CurrentSave = SaveData.CreateNew();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] couldn't read save file: {e.Message}");
                CurrentSave = SaveData.CreateNew();
            }
        }

        // called once on startup, loads existing save or makes a fresh one
        void LoadOrCreate()
        {
            if (HasSaveFile())
                Load();
            else
                CurrentSave = SaveData.CreateNew();
        }


        public void DeleteSave()
        {
            try
            {
                if (HasSaveFile())
                    File.Delete(SavePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] couldn't delete save file: {e.Message}");
            }

            CurrentSave = SaveData.CreateNew();
        }

        // --- autosave -

        void Update()
        {
            if (!autosaveEnabled || autosaveIntervalSeconds <= 0f) return;
            if (Time.unscaledTime - lastAutosaveTime < autosaveIntervalSeconds) return;

            Autosave("interval");
        }
        void OnApplicationPause(bool isPaused)
        {
            if (isPaused) Autosave("app backgrounded");
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Autosave("focus lost");
        }

        void OnApplicationQuit()
        {
            Autosave("quit");
        }
        public void Autosave(string reason)
        {
            if (!autosaveEnabled) return;
            if (!IsGameplayState()) return;
            if (Time.unscaledTime - lastAutosaveTime < minSecondsBetweenAutosaves) return;

            lastAutosaveTime = Time.unscaledTime;
            SaveAll();

            if (logAutosaves)
                Debug.Log($"[SaveManager] autosaved ({reason})");
        }
        bool IsGameplayState()
        {
            var manager = GameManager.Instance;
            if (manager == null) return false;

            return manager.CurrentState == GameState.Playing
                || manager.CurrentState == GameState.Paused;
        }

        [ContextMenu("Log Save Path")]
        void LogSavePath() => Debug.Log(SavePath);
    }
}