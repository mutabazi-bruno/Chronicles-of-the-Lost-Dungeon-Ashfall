using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Ashfall.Systems;

namespace Ashfall.UI
{
    [RequireComponent(typeof(Button))]
    public class LevelButton : MonoBehaviour
    {
        public string levelId;

        [Header("Visuals (no text, colors/icons only)")]
        public Image background;
        public Color lockedColor = Color.gray;
        public Color unlockedColor = Color.white;
        public Color completedColor = Color.yellow;

        public GameObject lockIcon;      // shown only when locked
        public GameObject completedIcon; // shown only when completed

        [Header("State chip (optional)")]
        [Tooltip("One label that reads PLAY / LOCKED / CLEARED, so the row does not " +
                 "need three overlapping objects to say one thing.")]
        public TMP_Text stateLabel;
        public string unlockedText = "PLAY";
        public string lockedText = "LOCKED";
        public string completedText = "CLEARED";
        [Tooltip("Shown instead of PLAY when this is the level the player left part-played")]
        public string inProgressText = "CONTINUE";
        public Color lockedTextColor = new Color32(150, 132, 104, 255);
        public Color unlockedTextColor = new Color32(240, 226, 196, 255);
        public Color inProgressTextColor = new Color32(232, 192, 106, 255);

        Button button;

        void Start()
        {
            button = GetComponent<Button>();
            Refresh();
            button.onClick.AddListener(OnClicked);
        }

        // True when this is the level the player walked away from part-played.
        bool IsInProgress()
        {
            var save = SaveManager.Instance != null ? SaveManager.Instance.CurrentSave : null;
            return save != null && save.HasResumePointFor(levelId);
        }

        public void Refresh()
        {
            bool unlocked = LevelManager.Instance.IsUnlocked(levelId);
            bool completed = LevelManager.Instance.IsCompleted(levelId);
            bool inProgress = unlocked && IsInProgress();

            button.interactable = unlocked;

            if (lockIcon != null) lockIcon.SetActive(!unlocked);
            if (completedIcon != null) completedIcon.SetActive(completed && !inProgress);

            if (background != null)
                background.color = completed ? completedColor : (unlocked ? unlockedColor : lockedColor);

            if (stateLabel != null)
            {
                // in progress wins over cleared: a replayed level the player is
                // partway through should offer to resume, not claim to be finished
                if (inProgress)
                {
                    stateLabel.text = inProgressText;
                    stateLabel.color = inProgressTextColor;
                }
                else
                {
                    stateLabel.text = completed ? completedText : (unlocked ? unlockedText : lockedText);
                    stateLabel.color = unlocked ? unlockedTextColor : lockedTextColor;
                }
            }
        }

        void OnClicked()
        {
            if (!LevelManager.Instance.IsUnlocked(levelId)) return; // safety net, button shouldnt even be clickable

            // Picking a level that is not the one in progress starts it properly.
            // Without this the saved coordinates from another level would follow the
            // player across, dropping them at wherever they stood in a different map.
            var save = SaveManager.Instance != null ? SaveManager.Instance.CurrentSave : null;
            if (save != null && !save.HasResumePointFor(levelId))
            {
                save.ClearResumePoint();
                SaveManager.Instance.Save();
            }

            SceneManager.LoadScene(levelId);
        }
    }
}