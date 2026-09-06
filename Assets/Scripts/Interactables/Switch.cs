using System;
using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Interactables
{
    public class Switch : MonoBehaviour, IInteractable
    {
        public bool isActivated;
        public AudioClip activateSound;

        [Header("Prompt")]
        [Tooltip("Shown next to the lever while it can still be pulled. The control name is " +
                 "added automatically.")]
        public string promptAction = "pull the lever";

        Animator animator;

        // observer pattern - doors (or anything else) subscribe to this
        // switch doesnt need to know what listens, keeps things decoupled
        public static event Action<Switch> OnSwitchActivated;

        void Awake()
        {
            animator = GetComponent<Animator>(); // null if you haven't added one yet, that's fine
        }

        // One time switch, so once it has fired there is nothing left to tell the player.
        public string InteractionPrompt =>
            isActivated ? string.Empty : $"{Ashfall.Systems.GameInput.InteractActionLabel} to {promptAction}";

        public bool CanInteract => !isActivated;

        public void Interact()
        {
            if (isActivated) return; // already used, one time switch for now

            isActivated = true;

            // fires the Red -> Red_To_Blue_0..3 sequence in the Animator Controller
            if (animator != null)
                animator.SetTrigger("Activate");

            Ashfall.Systems.AudioManager.Instance?.PlaySFX(activateSound);

            OnSwitchActivated?.Invoke(this);
        }
    }
}