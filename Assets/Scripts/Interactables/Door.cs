using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Animator))]
    public class Door : MonoBehaviour, IInteractable
    {
        public bool isLocked;
        public bool requiresKey;
        public string requiredKeyName = "Key";
        public AudioClip openSound;
        [Tooltip("Optional - played when the door refuses to open")]
        public AudioClip lockedSound;

        [Header("Prompt")]
        [Tooltip("Shown when the door will actually open. The control name is added automatically.")]
        public string promptAction = "open the door";

        [Tooltip("Shown when the door is locked and the linked lever has not been pulled yet.")]
        public string lockedBySwitchPrompt = "Locked. Find the lever that opens it";

        [Tooltip("leave empty if this door isn't gated by a switch. If set, the switch must be " +
                 "activated AND (if requiresKey is on) the player must have the key, before Interact() will open it.")]
        public Switch linkedSwitch;

        bool isOpen;
        Collider2D col;
        Animator animator;

        // Looked up once and kept. The prompt is read every frame while the player
        // stands here, and FindGameObjectWithTag on all those frames is wasteful.
        Ashfall.Player.PlayerInventory cachedInventory;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            animator = GetComponent<Animator>();
        }

        void OnEnable()
        {
            Switch.OnSwitchActivated += HandleSwitchActivated;
        }

        void OnDisable()
        {
            Switch.OnSwitchActivated -= HandleSwitchActivated;
        }

        void HandleSwitchActivated(Switch triggeredSwitch)
        {
            // only care about the switch actually linked to this door
            if (triggeredSwitch != linkedSwitch) return;

        }

        // The refusal reasons already went to the console and a sound. This puts the
        // same information on screen, where the player can actually see it.
        public string InteractionPrompt
        {
            get
            {
                if (isOpen) return string.Empty;

                if (isLocked)
                {
                    if (linkedSwitch != null && !linkedSwitch.isActivated)
                        return lockedBySwitchPrompt;

                    if (requiresKey && !PlayerHasKey())
                        return $"Locked. You need the {requiredKeyName}";

                    if (requiresKey)
                        return $"{Ashfall.Systems.GameInput.InteractActionLabel} to unlock the door";
                }

                return $"{Ashfall.Systems.GameInput.InteractActionLabel} to {promptAction}";
            }
        }

        public bool CanInteract
        {
            get
            {
                if (isOpen) return false;
                if (!isLocked) return true;

                if (linkedSwitch != null && !linkedSwitch.isActivated) return false;
                if (requiresKey && !PlayerHasKey()) return false;

                return true;
            }
        }

        Ashfall.Player.PlayerInventory Inventory
        {
            get
            {
                if (cachedInventory == null)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                        cachedInventory = player.GetComponent<Ashfall.Player.PlayerInventory>();
                }

                return cachedInventory;
            }
        }

        bool PlayerHasKey()
        {
            var inventory = Inventory;
            return inventory != null && inventory.HasKey(requiredKeyName);
        }

        public void Interact()
        {
            if (isOpen) return;

            if (isLocked)
            {
                if (linkedSwitch != null && !linkedSwitch.isActivated)
                {
                    // used to return in silence, which reads as a broken door
                    Refuse($"{name} is held by a switch that has not been activated yet");
                    return;
                }

                if (requiresKey)
                {
                    if (!PlayerHasKey())
                    {
                        Refuse($"{name} needs a '{requiredKeyName}' and the player does not have one");
                        return;
                    }

                    Inventory.RemoveKey(requiredKeyName); // key gets used up
                }
            }

            Open();
        }

        // Every refusal path used to be a bare return, so a player standing at a
        // door with the right key had no way to tell what was missing.
        void Refuse(string reason)
        {
            Debug.Log($"[Door] {reason}", this);

            if (lockedSound != null)
            {
                Ashfall.Systems.AudioManager.Instance?.PlaySFX(lockedSound);
            }
        }

        public bool IsOpen => isOpen;

        // Restores a door the player already opened. Skips the sound and the
        // animation trigger and simply lands in the open state, so reloading a save
        // does not replay every door opening in the level.
        public void RestoreOpened()
        {
            if (isOpen) return;

            isOpen = true;
            if (col != null) col.enabled = false;

            // the controller only exposes an "Open" trigger, no open/closed bool,
            // so this is the one way to land in the opened state
            if (animator != null)
                animator.SetTrigger("Open");
        }

        public void Open()
        {
            if (isOpen) return;

            isOpen = true;
            col.enabled = false; // player can walk through as soon as it starts opening

            Ashfall.Systems.AudioManager.Instance?.PlaySFX(openSound);

            animator.SetTrigger("Open");
        }
    }
}