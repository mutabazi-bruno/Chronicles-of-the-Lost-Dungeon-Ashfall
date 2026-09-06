using System;
using TMPro;
using UnityEngine;
using Ashfall.Systems;

namespace Ashfall.UI
{
    // Shown before a run starts (new game or continue) so every submitted score is
    // tied to a name the player actually typed, instead of the same generic default
    // for everyone. MainMenuController owns when this appears; this script only
    // owns the panel itself.
    public class PlayerNameEntryController : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panel;

        [Header("Fields")]
        public TMP_InputField nameInput;
        public TMP_Text errorText;

        Action onConfirmed;

        void Awake()
        {
            // NOTE: don't SetActive(false) on `panel` here - `panel` starts inactive in the
            // scene, so Awake is deferred until Show() activates it. Deactivating mid-activation
            // aborts the cascade before children (input field, button, etc.) get enabled.
            SetError(string.Empty);
        }

        // hook this up from MainMenuController right before loading a gameplay scene
        public void Show(Action confirmedCallback)
        {
            onConfirmed = confirmedCallback;

            if (panel != null) panel.SetActive(true);
            SetError(string.Empty);

            if (nameInput != null)
            {
                string existing = PlayerProfile.Instance != null ? PlayerProfile.Instance.PlayerName : string.Empty;
                nameInput.text = existing;
                nameInput.Select();
                nameInput.ActivateInputField();
            }
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        // hook this to the panel's "Confirm"/"Play" button
        public void OnConfirmClicked()
        {
            string typed = nameInput != null ? nameInput.text : string.Empty;
            string trimmed = (typed ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                SetError("Enter a name to continue.");
                return;
            }

            if (PlayerProfile.Instance != null)
                PlayerProfile.Instance.SetPlayerName(trimmed);

            // keep the currently-loaded LeaderboardService in sync immediately,
            // rather than waiting for it to re-read PlayerProfile on next level complete
            if (LeaderboardService.Instance != null)
                LeaderboardService.Instance.playerName = trimmed;

            Hide();

            var callback = onConfirmed;
            onConfirmed = null;
            callback?.Invoke();
        }

        void SetError(string message)
        {
            if (errorText != null) errorText.text = message;
        }
    }
}
