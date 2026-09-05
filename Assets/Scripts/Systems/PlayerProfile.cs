using UnityEngine;

namespace Ashfall.Systems
{
    // singleton, same shape as our other managers - holds the player's chosen
    // display name so it survives scene loads and save wipes (backed by PlayerPrefs,
    // not the save file, since identity shouldn't reset with "New Game").
    public class PlayerProfile : MonoBehaviour
    {
        public static PlayerProfile Instance { get; private set; }

        const string PrefsKey = "PlayerProfile.PlayerName";
        const int MaxNameLength = 16;

        public string PlayerName { get; private set; } = string.Empty;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PlayerName = PlayerPrefs.GetString(PrefsKey, string.Empty);
        }

        public bool HasPlayerName() => !string.IsNullOrWhiteSpace(PlayerName);

        // Trims and caps length so long/blank input can't reach Firebase keys or UI rows.
        public void SetPlayerName(string rawName)
        {
            string trimmed = (rawName ?? string.Empty).Trim();
            if (trimmed.Length > MaxNameLength)
                trimmed = trimmed.Substring(0, MaxNameLength);

            PlayerName = trimmed;
            PlayerPrefs.SetString(PrefsKey, PlayerName);
            PlayerPrefs.Save();
        }
    }
}
