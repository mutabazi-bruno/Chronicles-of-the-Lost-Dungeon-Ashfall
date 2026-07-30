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
            var save = SaveManager.Instance.CurrentSave;

            volumeSlider.SetValueWithoutNotify(save.musicVolume);
            muteToggle.SetIsOnWithoutNotify(save.musicVolume <= 0f);

            if (save.musicVolume > 0f)
                volumeBeforeMute = save.musicVolume;

            ApplyVolume(save.musicVolume);
        }

        public void OnVolumeChanged(float value)
        {
            if (muteToggle.isOn && value > 0f)
                muteToggle.isOn = false;

            volumeBeforeMute = value > 0f ? value : volumeBeforeMute;
            ApplyVolume(value);
            SaveVolume(value);
        }

        public void OnMuteToggled(bool isMuted)
        {
            Debug.Log("OnMuteToggled fired: " + isMuted);
            float value = isMuted ? 0f : volumeBeforeMute;
            volumeSlider.value = value;
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