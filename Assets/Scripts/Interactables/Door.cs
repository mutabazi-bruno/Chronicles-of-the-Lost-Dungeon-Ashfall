using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    public class Door : MonoBehaviour, IInteractable
    {
        public bool isLocked;
        public bool requiresKey;
        public string requiredKeyName = "Key";
        public AudioClip openSound;

        [Tooltip("leave empty if this door isn't gated by a switch. If set, the switch must be " +
                 "activated AND (if requiresKey is on) the player must have the key, before Interact() will open it.")]
        public Switch linkedSwitch;

        [Header("Prompt")]
        [Tooltip("Shown when the door will actually open. The control name is added automatically.")]
        public string promptAction = "open the door";

        [Tooltip("Shown when the door is locked and the linked lever has not been pulled yet.")]
        public string lockedBySwitchPrompt = "Locked. Find the lever that opens it";

        bool isOpen;
        Collider2D col;

        // Looked up once and kept. The prompt is read every frame while the player stands
        // here, and FindGameObjectWithTag on every one of those frames is wasteful.
        Ashfall.Player.PlayerInventory cachedInventory;

        void Awake()
        {
            col = GetComponent<Collider2D>();
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

        // Tells the player why the door will not budge instead of leaving them pressing E
        // at a door that silently ignores them.
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
                    return;
                }

                if (requiresKey)
                {
                    if (!PlayerHasKey())
                    {
                        return;
                    }

                    Inventory.RemoveKey(requiredKeyName); // key gets used up
                }
            }

            Open();
        }

        public void Open()
        {
            if (isOpen) return;

            isOpen = true;
            col.enabled = false; // just disabling collision, swap for an anim later

            Ashfall.Systems.AudioManager.Instance?.PlaySFX(openSound);

            gameObject.SetActive(false); // simple for now, replace with open animation later
        }
    }
}