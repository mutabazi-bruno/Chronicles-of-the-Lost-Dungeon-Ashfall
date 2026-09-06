using UnityEngine;
using UnityEngine.UI;
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

        [Header("Low health hint")]
        [Tooltip("When the player is hurt and carrying a potion, remind them the potion exists. " +
                 "An interactable in range always wins, so the two never fight for the same line.")]
        public bool showLowHealthHint = true;

        [Tooltip("Fraction of max health at or below which the hint appears. 0.5 means half health.")]
        [Range(0.05f, 0.9f)]
        public float lowHealthFraction = 0.5f;

        [Header("Fallback")]
        [Tooltip("When nothing is wired above, build a plain label at runtime. This lets the " +
                 "component be dropped onto the HUD prefab and work in every level without " +
                 "editing five scenes. Turn it off once a designed panel is assigned.")]
        public bool buildFallbackWhenUnassigned = true;

        [Tooltip("Font size of the fallback label only. Ignored when you assign your own.")]
        public float fallbackFontSize = 28f;

        [Tooltip("Matches the HUD's control hints so the prompt reads as part of the same " +
                 "interface. The font asset is already shared - TMP's default is the same " +
                 "LiberationSans SDF every other label uses - so this is weight, caps and colour.")]
        public Color fallbackTextColor = new Color32(237, 228, 210, 255);

        [Tooltip("Card drawn behind the prompt. Assign the shared HUD panel sprite so the " +
                 "prompt sits on the same furniture as the control hints and objective board. " +
                 "Leave empty for bare text on the scene.")]
        public Sprite fallbackBackground;

        [Tooltip("Space between the text and the edge of its card, in canvas units.")]
        public Vector2 fallbackPadding = new Vector2(34f, 14f);

        Transform followTarget;
        IInteractable focused;
        RectTransform canvasRect;
        Camera worldCamera;

        PlayerHealth playerHealth;
        PlayerInventory playerInventory;

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

        // Only caches what the interactor found. The text itself is worked out every frame,
        // because a door's prompt changes the moment the player picks up its key and no focus
        // event fires for that.
        void HandleFocusChanged(IInteractable interactable, Transform target)
        {
            focused = interactable;
            followTarget = target;
        }

        string CurrentPrompt()
        {
            if (focused == null) return string.Empty;
            return focused.InteractionPrompt;
        }

        void Refresh()
        {
            // Nothing belongs on screen unless the level is actually being played. The
            // potion hint was showing over the level complete and pause screens, because
            // it only ever checked health.
            if (!IsPlaying())
            {
                Hide();
                return;
            }

            // An interactable in range beats the potion reminder. Standing at a chest on low
            // health should tell you about the chest, not about the potion you already have.
            string text = CurrentPrompt();

            if (string.IsNullOrEmpty(text))
                text = LowHealthHint();

            if (string.IsNullOrEmpty(text))
                Hide();
            else
                Show(text);
        }

        static bool IsPlaying()
        {
            var manager = Ashfall.Systems.GameManager.Instance;

            // No manager usually means a test scene, so stay permissive rather than
            // silently showing nothing at all.
            return manager == null
                || manager.CurrentState == Ashfall.Systems.GameState.Playing;
        }

        string LowHealthHint()
        {
            if (!showLowHealthHint) return string.Empty;

            CachePlayer();

            if (playerHealth == null || playerInventory == null) return string.Empty;
            if (playerHealth.IsDead) return string.Empty;

            var stats = playerHealth.stats;
            if (stats == null || stats.maxHealth <= 0) return string.Empty;

            float fraction = (float)stats.currentHealth / stats.maxHealth;
            if (fraction > lowHealthFraction) return string.Empty;

            // No point nagging about a potion the player does not have.
            if (!HasAnyPotion()) return string.Empty;

            return $"{Ashfall.Systems.GameInput.PotionActionLabel} to use a Health Potion";
        }

        bool HasAnyPotion()
        {
            foreach (var item in playerInventory.inventory.items)
            {
                if (item.type == Ashfall.Core.ItemType.Potion) return true;
            }

            return false;
        }

        // The player is spawned per scene, so this can't be resolved once at Awake.
        void CachePlayer()
        {
            if (playerHealth != null && playerInventory != null) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            playerHealth = player.GetComponent<PlayerHealth>();
            playerInventory = player.GetComponent<PlayerInventory>();
        }

        // LateUpdate so the camera has already moved for this frame, otherwise the label
        // trails the object by a frame whenever the camera is following the player.
        void LateUpdate()
        {
            Refresh();

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

            // The card is the thing that gets shown, hidden and positioned; the label
            // lives inside it so the box hugs whatever text is currently set.
            var cardObject = new GameObject("PromptCard");
            cardObject.transform.SetParent(canvas.transform, false);

            var card = cardObject.AddComponent<Image>();
            card.sprite = fallbackBackground;
            card.type = Image.Type.Sliced;
            card.raycastTarget = false;

            // With no sprite assigned an Image still paints a white block, which would
            // be worse than no card at all - so make it invisible in that case and let
            // the text sit on the scene as before.
            if (fallbackBackground == null)
                card.color = new Color(1f, 1f, 1f, 0f);

            // Layout group plus fitter means the card resizes itself to the prompt.
            // "Press E to open the chest" and "Press E to pull the lever" are different
            // lengths, and a fixed 900px box would leave one of them swimming in space.
            var layout = cardObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(
                Mathf.RoundToInt(fallbackPadding.x), Mathf.RoundToInt(fallbackPadding.x),
                Mathf.RoundToInt(fallbackPadding.y), Mathf.RoundToInt(fallbackPadding.y));
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var fitter = cardObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var labelObject = new GameObject("PromptLabel");
            labelObject.transform.SetParent(cardObject.transform, false);

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fallbackFontSize;
            label.raycastTarget = false;

            // The HUD's control hints are bold small-caps in a warm off-white. A plain
            // white regular-weight label next to them reads as a different font even
            // though it is the same asset, so match the treatment here.
            label.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
            label.color = fallbackTextColor;

            promptLabel = label;
            promptRoot = cardObject.GetComponent<RectTransform>();
        }
    }
}
