using UnityEngine;
using TMPro;
using Ashfall.Interfaces;
using Ashfall.Player;

namespace Ashfall.UI
{
    public enum PromptPlacement
    {
        // Fixed spot near the bottom of the screen. Reads like a subtitle, and never ends up
        // behind level art or off the edge of the camera.
        BottomCentre,

        // Floats above the object itself.
        FollowTarget
    }

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

        [Header("Placement")]
        public PromptPlacement placement = PromptPlacement.BottomCentre;

        [Tooltip("Distance up from the bottom edge, in canvas units. Only used by BottomCentre.")]
        public float bottomMargin = 140f;

        [Tooltip("Leave empty to keep the prompt wherever you placed it on the canvas. Assign " +
                 "the HUD canvas to make the prompt float above whatever the player is near.")]
        public Canvas canvas;

        [Tooltip("How far above the object the prompt sits, in world units. Only used by FollowTarget.")]
        public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Fallback")]
        [Tooltip("When nothing is wired above, build a plain label at runtime. This lets the " +
                 "component be dropped onto the HUD prefab and work in every level without " +
                 "editing five scenes. Turn it off once a designed panel is assigned.")]
        public bool buildFallbackWhenUnassigned = true;

        [Tooltip("Font size of the fallback label only. Ignored when you assign your own.")]
        public float fallbackFontSize = 28f;

        Transform followTarget;
        RectTransform canvasRect;
        Camera worldCamera;

        void Awake()
        {
            if (promptLabel == null && buildFallbackWhenUnassigned)
                BuildFallbackUI();

            if (canvas != null)
                canvasRect = canvas.transform as RectTransform;

            ApplyPlacementAnchors();
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

        // Bottom centre pins itself once. Follow mode gets repositioned every frame instead.
        void ApplyPlacementAnchors()
        {
            if (promptRoot == null) return;

            if (placement == PromptPlacement.BottomCentre)
            {
                promptRoot.anchorMin = new Vector2(0.5f, 0f);
                promptRoot.anchorMax = new Vector2(0.5f, 0f);
                promptRoot.pivot = new Vector2(0.5f, 0f);
                promptRoot.anchoredPosition = new Vector2(0f, bottomMargin);
            }
            else
            {
                promptRoot.anchorMin = new Vector2(0.5f, 0.5f);
                promptRoot.anchorMax = new Vector2(0.5f, 0.5f);
                promptRoot.pivot = new Vector2(0.5f, 0.5f);
            }
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

        // LateUpdate so the camera has already moved for this frame, otherwise the label
        // trails the object by a frame whenever the camera is following the player.
        void LateUpdate()
        {
            if (placement != PromptPlacement.FollowTarget) return;
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

        // Deliberately plain. This exists so the feature works the moment the component is
        // added, not to compete with a proper designed panel.
        void BuildFallbackUI()
        {
            // Reuse the canvas we were pointed at. Only build our own when there isn't one,
            // otherwise the label ends up on a second canvas that ignores the HUD's scaler.
            if (canvas == null)
            {
                var canvasObject = new GameObject("InteractionPromptCanvas");
                canvasObject.transform.SetParent(transform, false);

                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // above the HUD, below anything modal

                var scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            var labelObject = new GameObject("PromptLabel");
            labelObject.transform.SetParent(canvas.transform, false);

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fallbackFontSize;
            label.raycastTarget = false;

            promptLabel = label;
            promptRoot = labelObject.GetComponent<RectTransform>();
            promptRoot.sizeDelta = new Vector2(900f, 60f);
        }
    }
}
