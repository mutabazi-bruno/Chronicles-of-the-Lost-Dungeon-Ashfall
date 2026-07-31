using System;
using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Interactables
{
    public class Switch : MonoBehaviour, IInteractable
    {
        public bool isActivated;
        public AudioClip activateSound;

        Animator animator;

        // observer pattern - doors (or anything else) subscribe to this
        // switch doesnt need to know what listens, keeps things decoupled
        public static event Action<Switch> OnSwitchActivated;

        void Awake()
        {
            animator = GetComponent<Animator>(); // null if you haven't added one yet, that's fine
        }

        public void Interact()
        {
            if (isActivated) return; // already used, one time switch for now

            isActivated = true;

            // fires the Red -> Red_To_Blue_0..3 sequence in the Animator Controller
            // (see the note below the code for how to wire that up)
            if (animator != null)
                animator.SetTrigger("Activate");

            Ashfall.Systems.AudioManager.Instance?.PlaySFX(activateSound);

            OnSwitchActivated?.Invoke(this);
        }
    }
}