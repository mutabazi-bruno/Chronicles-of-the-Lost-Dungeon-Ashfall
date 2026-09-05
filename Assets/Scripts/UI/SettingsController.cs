using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ashfall.Systems;

namespace Ashfall.UI
{
    // Drives the audio section of the settings panel: master, music and SFX
    // levels plus a mute switch. Changes are applied live so you can hear them
    // while dragging, but only written to disk on Save (or on close).
    public class SettingsController : MonoBehaviour
    {
        [Header("Sliders")]
        public Slider masterSlider;
        public Slider musicSlider;
        public Slider sfxSlider;

        [Header("Percentage readouts (optional)")]
        public TextMeshProUGUI masterValueLabel;
        public TextMeshProUGUI musicValueLabel;
        public TextMeshProUGUI sfxValueLabel;

        [Header("Mute")]
        public Toggle muteToggle;

        [Header("Panel to return to on Save & Close")]
        public GameObject settingsPanel;
        public GameObject mainPanel;

        const float DefaultMaster = 1f;
        const float DefaultMusic = 1f;
        const float DefaultSfx = 1f;

        // Guards the setters against re-entrancy while we push values into the UI.
        bool isRefreshing;
        bool needsSave;

        void OnEnable()
        {
            Refresh();
        }

        void OnDisable()
        {
            CommitSave();
        }

        void Refresh()
        {
            var save = SaveManager.Instance != null ? SaveManager.Instance.CurrentSave : null;
            if (save == null)
            {
                return;
            }

            isRefreshing = true;

            SetSlider(masterSlider, save.masterVolume);
            SetSlider(musicSlider, save.musicVolume);
            SetSlider(sfxSlider, save.sfxVolume);

            if (muteToggle != null)
            {
                muteToggle.SetIsOnWithoutNotify(save.audioMuted);
            }

            isRefreshing = false;

            ApplyAll(save.masterVolume, save.musicVolume, save.sfxVolume, save.audioMuted);
            RefreshLabels();
        }

        static void SetSlider(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            }
        }

        static float Read(Slider slider, float fallback)
        {
            return slider != null ? Mathf.Clamp01(slider.value) : fallback;
        }

        static void SetPercent(TextMeshProUGUI label, float value01)
        {
            if (label != null)
            {
                label.text = Mathf.RoundToInt(value01 * 100f) + "%";
            }
        }

        void RefreshLabels()
        {
            SetPercent(masterValueLabel, Read(masterSlider, DefaultMaster));
            SetPercent(musicValueLabel, Read(musicSlider, DefaultMusic));
            SetPercent(sfxValueLabel, Read(sfxSlider, DefaultSfx));
        }

        void ApplyAll(float master, float music, float sfx, bool isMuted)
        {
            var audio = AudioManager.Instance;
            if (audio == null)
            {
                return;
            }

            audio.SetMusicVolume(music);
            audio.SetSfxVolume(sfx);
            audio.SetMasterVolume(master);
            audio.SetMuted(isMuted);
        }

        void ApplyFromUI()
        {
            ApplyAll(Read(masterSlider, DefaultMaster),
                     Read(musicSlider, DefaultMusic),
                     Read(sfxSlider, DefaultSfx),
                     muteToggle != null && muteToggle.isOn);
            RefreshLabels();
            needsSave = true;
        }

        // -- hooked up to the slider / toggle events in the scene --

        public void OnMasterVolumeChanged(float value)
        {
            if (isRefreshing) return;
            ApplyFromUI();
        }

        public void OnMusicVolumeChanged(float value)
        {
            if (isRefreshing) return;
            ApplyFromUI();
        }

        public void OnSfxVolumeChanged(float value)
        {
            if (isRefreshing) return;
            ApplyFromUI();
        }

        public void OnMuteToggled(bool isMuted)
        {
            if (isRefreshing) return;
            ApplyFromUI();
        }

        // -- buttons --

        public void OnResetDefaultsClicked()
        {
            isRefreshing = true;

            SetSlider(masterSlider, DefaultMaster);
            SetSlider(musicSlider, DefaultMusic);
            SetSlider(sfxSlider, DefaultSfx);
            if (muteToggle != null)
            {
                muteToggle.SetIsOnWithoutNotify(false);
            }

            isRefreshing = false;

            ApplyFromUI();
        }

        public void OnSaveAndCloseClicked()
        {
            CommitSave();

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
            }
        }

        void CommitSave()
        {
            if (!needsSave || SaveManager.Instance == null)
            {
                return;
            }

            var save = SaveManager.Instance.CurrentSave;
            save.masterVolume = Read(masterSlider, DefaultMaster);
            save.musicVolume = Read(musicSlider, DefaultMusic);
            save.sfxVolume = Read(sfxSlider, DefaultSfx);
            save.audioMuted = muteToggle != null && muteToggle.isOn;

            SaveManager.Instance.Save();
            needsSave = false;
        }
    }
}
