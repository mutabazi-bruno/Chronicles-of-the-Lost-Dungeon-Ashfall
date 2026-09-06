using System;
using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Systems;

namespace Ashfall.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        public float interactRange = 1f;
        public LayerMask interactableLayer;

        // observer pattern, same shape as PlayerController's sound events. The prompt UI
        // subscribes to this instead of hunting for the player every frame.
        // Transform is the thing to point at, and is null when nothing is in range.
        public static event Action<IInteractable, Transform> OnFocusChanged;

        IInteractable focused;
        Transform focusedTarget;

        public IInteractable Focused => focused;

        void Update()
        {
            RefreshFocus();

            if (GameInput.InteractPressed)
            {
                TryInteract();
            }
        }

        void OnDisable()
        {
            SetFocus(null, null);
        }

        // Runs every frame so the prompt appears the moment the player walks into range and
        // updates itself when a locked door becomes unlocked while they are standing there.
        void RefreshFocus()
        {
            FindClosestInteractable(out IInteractable nearest, out Transform target);
            SetFocus(nearest, target);
        }

        void SetFocus(IInteractable next, Transform target)
        {
            if (ReferenceEquals(focused, next) && focusedTarget == target) return;

            focused = next;
            focusedTarget = target;

            OnFocusChanged?.Invoke(focused, focusedTarget);
        }

        void TryInteract()
        {
            FindClosestInteractable(out IInteractable interactable, out _);
            interactable?.Interact();
        }

        // Pulled out of TryInteract so the on-screen prompt can ask the same question every
        // frame without duplicating the search.
        void FindClosestInteractable(out IInteractable interactable, out Transform target)
        {
            interactable = null;
            target = null;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange, interactableLayer);
            if (hits.Length == 0) return;

            float closestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var candidate = hit.GetComponent<IInteractable>();
                if (candidate == null) continue;

                // An opened chest and a pulled lever both keep their collider so they stay
                // visible, but they have nothing left to say. Skipping them here stops a dead
                // prop stealing focus from a live one standing right next to it.
                if (string.IsNullOrEmpty(candidate.InteractionPrompt)) continue;

                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist >= closestDist) continue;

                closestDist = dist;
                interactable = candidate;
                target = hit.transform;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}