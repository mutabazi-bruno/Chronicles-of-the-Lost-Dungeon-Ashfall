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

                // The error used to sit there until the panel was reopened, so a player who
                // typed a name after a failed press still saw "Enter a name to continue" and
                // reasonably assumed the button was broken.
                nameInput.onValueChanged.RemoveListener(HandleTyping);
                nameInput.onValueChanged.AddListener(HandleTyping);

                // Enter submits, which is what anyone typing into a name box expects.
                nameInput.onSubmit.RemoveListener(HandleSubmit);
                nameInput.onSubmit.AddListener(HandleSubmit);
            }
        }

        void HandleTyping(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                SetError(string.Empty);
        }

        void HandleSubmit(string value)
        {
            OnConfirmClicked();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        // hook this to the panel's "Confirm"/"Play" button
        public void OnConfirmClicked()
        {
            string typed = ReadTypedName();
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

        // TMP_InputField.text is the source of truth, but read the visible label as a
        // fallback so a mis-wired viewport cannot silently swallow what the player typed.
        string ReadTypedName()
        {
            if (nameInput == null) return string.Empty;

            if (!string.IsNullOrWhiteSpace(nameInput.text))
                return nameInput.text;

            var label = nameInput.textComponent;
            return label != null ? label.text : string.Empty;
        }

        void SetError(string message)
        {
            if (errorText != null) errorText.text = message;
        }
    }
}
