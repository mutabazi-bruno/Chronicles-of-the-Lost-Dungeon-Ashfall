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

        bool isOpen;
        Collider2D col;

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
                    var inventory = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Ashfall.Player.PlayerInventory>();
                    if (inventory == null || !inventory.HasKey(requiredKeyName))
                    {
                        return;
                    }

                    inventory.RemoveKey(requiredKeyName); // key gets used up
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