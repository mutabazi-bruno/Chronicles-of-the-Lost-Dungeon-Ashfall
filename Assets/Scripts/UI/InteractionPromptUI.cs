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

        [Header("Positioning")]
        [Tooltip("Leave empty to keep the prompt wherever you placed it on the canvas. Assign " +
                 "the HUD canvas to make the prompt float above whatever the player is near.")]
        public Canvas canvas;

        [Tooltip("How far above the object the prompt sits, in world units.")]
        public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

        Transform followTarget;
        RectTransform canvasRect;
        Camera worldCamera;

        void Awake()
        {
            if (canvas != null)
                canvasRect = canvas.transform as RectTransform;
        }

        // LateUpdate so the camera has already moved for this frame, otherwise the label
        // trails the object by a frame whenever the camera is following the player.
        void LateUpdate()
        {
            if (followTarget == null || canvasRect == null || promptRoot == null) return;

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null) return;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                worldCamera, followTarget.position + worldOffset);

            // Overlay canvases take a null camera here, anything else takes the canvas camera.
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPoint, uiCamera, out Vector2 local))
            {
                promptRoot.anchoredPosition = local;
            }
        }

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
            followTarget = target;

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
            followTarget = null;

            if (promptRoot != null)
                promptRoot.gameObject.SetActive(false);
        }
    }
}
