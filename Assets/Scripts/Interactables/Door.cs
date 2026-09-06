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

        [Tooltip("leave empty if this door isn't gated by a switch. If set, the switch must be " +
                 "activated AND (if requiresKey is on) the player must have the key, before Interact() will open it.")]
        public Switch linkedSwitch;

        bool isOpen;
        Collider2D col;
        Animator animator;

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
                    var inventory = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Ashfall.Player.PlayerInventory>();
                    if (inventory == null || !inventory.HasKey(requiredKeyName))
                    {
                        Refuse($"{name} needs a '{requiredKeyName}' and the player does not have one");
                        return;
                    }

                    inventory.RemoveKey(requiredKeyName); // key gets used up
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