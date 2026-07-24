using System;
using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Interactables
{
    public class Switch : MonoBehaviour, IInteractable
    {
        public bool isActivated;

        // observer pattern - doors (or anything else) subscribe to this
        // switch doesnt need to know what listens, keeps things decoupled
        public static event Action<Switch> OnSwitchActivated;

        public void Interact()
        {
            if (isActivated) return; // already used, one time switch for now

            isActivated = true;
            OnSwitchActivated?.Invoke(this);
        }
    }
}