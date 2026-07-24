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
                // for now just checking a bool, real key check happens once inventory hooks in
                Debug.Log("door is locked, need a key");
                return;
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