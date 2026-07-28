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
        }

        void OnClicked()
        {
            if (!LevelManager.Instance.IsUnlocked(levelId)) return; // safety net, button shouldnt even be clickable
            SceneManager.LoadScene(levelId);
        }
    }
}