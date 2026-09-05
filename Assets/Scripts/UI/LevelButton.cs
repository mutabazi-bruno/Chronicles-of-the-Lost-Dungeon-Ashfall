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
        public Color lockedTextColor = new Color32(150, 132, 104, 255);
        public Color unlockedTextColor = new Color32(240, 226, 196, 255);

        Button button;

        void Start()
        {
            button = GetComponent<Button>();
            Refresh();
            button.onClick.AddListener(OnClicked);
        }

        public void Refresh()
        {
            bool unlocked = LevelManager.Instance.IsUnlocked(levelId);
            bool completed = LevelManager.Instance.IsCompleted(levelId);

            button.interactable = unlocked;

            if (lockIcon != null) lockIcon.SetActive(!unlocked);
            if (completedIcon != null) completedIcon.SetActive(completed);

            if (background != null)
                background.color = completed ? completedColor : (unlocked ? unlockedColor : lockedColor);

            if (stateLabel != null)
            {
                stateLabel.text = completed ? completedText : (unlocked ? unlockedText : lockedText);
                stateLabel.color = unlocked ? unlockedTextColor : lockedTextColor;
            }
        }

        void OnClicked()
        {
            if (!LevelManager.Instance.IsUnlocked(levelId)) return; // safety net, button shouldnt even be clickable
            SceneManager.LoadScene(levelId);
        }
    }
}