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

        // Guards against reentrancy. Setting slider.value fires onValueChanged, and setting
        // toggle.isOn fires onValueChanged too - so the two controls used to trigger each
        // other mid-call, with the nested call applying a different volume than the one the
        // player actually chose. Everything inside a refresh is ignored.
        bool isRefreshing;

        // volume changes are applied live but only committed to disk when the panel closes,
        // instead of rewriting the whole save file on every frame of a slider drag
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

            // keep the toggle in step with the slider in BOTH directions - dragging to zero
            // used to leave the toggle showing "unmuted" while the audio was silent
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

            // if the slider was already at zero there is nothing sensible to restore to
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