using System.Collections.Generic;
using System.IO;
using UnityEngine;
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
        }

        // Called by ISaveable objects in their OnEnable, so SaveManager
        // never has to scan the whole scene to find them.
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

            foreach (var saveable in registeredSaveables)
                saveable.Save(CurrentSave);

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
        void SyncWebGLFiles()
        {
            Application.ExternalEval("FS.syncfs(false, function(err){});");
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

        // wipes progress, used by "New Game" and for testing
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

        [ContextMenu("Log Save Path")]
        void LogSavePath() => Debug.Log(SavePath);
    }
}
