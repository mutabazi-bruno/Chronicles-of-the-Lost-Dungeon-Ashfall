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

        [Tooltip("leave empty if this door isn't opened by a switch")]
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
            // only react if its the switch actually linked to this door
            if (triggeredSwitch == linkedSwitch)
                Open();
        }

        public void Interact()
        {
            if (isOpen) return;

            if (isLocked)
            {
                if (!requiresKey)
                {
                    Debug.Log("door is locked");
                    return;
                }

                var inventory = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Ashfall.Player.PlayerInventory>();
                if (inventory == null || !inventory.HasKey(requiredKeyName))
                {
                    Debug.Log("need a key to open this door");
                    return;
                }

                inventory.RemoveKey(requiredKeyName); // key gets used up
            }

            Open();
        }

        public void Open()
        {
            if (isOpen) return;

            isOpen = true;
            col.enabled = false; // just disabling collision, swap for an anim later
            gameObject.SetActive(false); // simple for now, replace with open animation later
        }
    }
}