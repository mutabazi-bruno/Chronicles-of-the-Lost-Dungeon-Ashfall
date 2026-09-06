using UnityEngine;
using TMPro;
using Ashfall.Interfaces;
using Ashfall.Player;

namespace Ashfall.UI
{
    // Shows the contextual prompt for whatever the player is currently standing next to.
    //
    // Listens to PlayerInteractor.OnFocusChanged rather than searching the scene, so this can
    // live on the HUD prefab and never needs a reference to the player.
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("The object that gets shown and hidden. Usually a small panel holding the label.")]
        public RectTransform promptRoot;

        [Tooltip("Where the prompt text goes.")]
        public TMP_Text promptLabel;

        void OnEnable()
        {
            PlayerInteractor.OnFocusChanged += HandleFocusChanged;
            Hide();
        }

        void OnDisable()
        {
            PlayerInteractor.OnFocusChanged -= HandleFocusChanged;
        }

        void HandleFocusChanged(IInteractable interactable, Transform target)
        {
            if (interactable == null || target == null)
            {
                Hide();
                return;
            }

            string text = interactable.InteractionPrompt;

            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }

            Show(text);
        }

        void Show(string text)
        {
            if (promptLabel != null)
                promptLabel.text = text;

            if (promptRoot != null)
                promptRoot.gameObject.SetActive(true);
        }

        void Hide()
        {
            if (promptRoot != null)
                promptRoot.gameObject.SetActive(false);
        }
    }
}
