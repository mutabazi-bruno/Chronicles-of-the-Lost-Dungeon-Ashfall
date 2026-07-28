using System.IO;
using UnityEngine;
using Ashfall.Core;

namespace Ashfall.Systems
{
    // singleton - one place that knows how to read/write the save file
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public SaveData CurrentSave { get; private set; }

        string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

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

        public bool HasSaveFile()
        {
            return File.Exists(SavePath);
        }

        public void Save()
        {
            if (CurrentSave == null) return;

            string json = JsonUtility.ToJson(CurrentSave, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"saved to {SavePath}");
        }

        public void Load()
        {
            if (!HasSaveFile())
            {
                CurrentSave = SaveData.CreateNew();
                return;
            }

            string json = File.ReadAllText(SavePath);
            CurrentSave = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("save loaded");
        }

        // called once on startup, loads existing save or makes a fresh one
        void LoadOrCreate()
        {
            if (HasSaveFile())
                Load();
            else
                CurrentSave = SaveData.CreateNew();
        }

        // wipes progress, mainly useful for testing
        public void DeleteSave()
        {
            if (HasSaveFile())
                File.Delete(SavePath);

            CurrentSave = SaveData.CreateNew();
        }
    }
}