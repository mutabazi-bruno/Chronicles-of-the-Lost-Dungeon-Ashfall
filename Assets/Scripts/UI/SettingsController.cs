using UnityEngine;
using UnityEngine.UI;
using Ashfall.Systems;

namespace Ashfall.UI
{
    public class SettingsController : MonoBehaviour
    {
        public Slider volumeSlider;
        public Toggle muteToggle;

        float volumeBeforeMute = 1f;

        // Prevents reentrancy when updating UI elements.
        bool isRefreshing;

        // Apply volume live but defer saving until panel close.
        bool needsSave;

        void OnEnable()
        {
            var save = SaveManager.Instance.CurrentSave;

            isRefreshing = true;

            volumeSlider.SetValueWithoutNotify(save.musicVolume);
            muteToggle.SetIsOnWithoutNotify(save.musicVolume <= 0f);

            if (save.musicVolume > 0f)
                volumeBeforeMute = save.musicVolume;

            isRefreshing = false;

            ApplyVolume(save.musicVolume);
        }

        void OnDisable()
        {
            CommitSave();
        }

        public void OnVolumeChanged(float value)
        {
            if (isRefreshing) return;

            isRefreshing = true;

            // Sync toggle and slider state.
            muteToggle.SetIsOnWithoutNotify(value <= 0f);

            isRefreshing = false;

            if (value > 0f)
                volumeBeforeMute = value;

            ApplyVolume(value);
            needsSave = true;
        }

        public void OnMuteToggled(bool isMuted)
        {
            if (isRefreshing) return;

            // Handle unmuting logic.
            float value = isMuted ? 0f : Mathf.Max(volumeBeforeMute, 0.1f);

            isRefreshing = true;
            volumeSlider.SetValueWithoutNotify(value);
            isRefreshing = false;

            ApplyVolume(value);
            needsSave = true;
        }

        void ApplyVolume(float value)
        {
            AudioManager.Instance?.SetMusicVolume(value);
        }

        void CommitSave()
        {
            if (!needsSave) return;
            if (SaveManager.Instance == null) return;

            SaveManager.Instance.CurrentSave.musicVolume = volumeSlider.value;
            SaveManager.Instance.Save();
            needsSave = false;
        }
    }
}