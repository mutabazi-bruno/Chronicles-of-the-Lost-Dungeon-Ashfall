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

        void OnEnable()
        {
            // load current saved values into the ui when this panel opens
            var save = SaveManager.Instance.CurrentSave;
            volumeSlider.value = save.musicVolume;
            muteToggle.isOn = save.musicVolume <= 0f;

            ApplyVolume(save.musicVolume);
        }

        public void OnVolumeChanged(float value)
        {
            if (muteToggle.isOn && value > 0f)
                muteToggle.isOn = false; // moving the slider unmutes automatically

            volumeBeforeMute = value > 0f ? value : volumeBeforeMute;
            ApplyVolume(value);
            SaveVolume(value);
        }

        public void OnMuteToggled(bool isMuted)
        {
            float value = isMuted ? 0f : volumeBeforeMute;
            volumeSlider.value = value; // this also fires OnVolumeChanged, which applies + saves
        }

        void ApplyVolume(float value)
        {
            AudioManager.Instance?.SetMusicVolume(value);
        }

        void SaveVolume(float value)
        {
            SaveManager.Instance.CurrentSave.musicVolume = value;
            SaveManager.Instance.Save();
        }
    }
}