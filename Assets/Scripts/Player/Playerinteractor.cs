using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Systems;

namespace Ashfall.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        public float interactRange = 1f;
        public LayerMask interactableLayer;

        void Update()
        {
            if (GameInput.InteractPressed)
            {
                TryInteract();
            }
        }

        // Find the closest interactable object in range.
        void TryInteract()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange, interactableLayer);

            if (hits.Length == 0) return;

            Collider2D closest = hits[0];
            float closestDist = Vector2.Distance(transform.position, closest.transform.position);

            foreach (var hit in hits)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closest = hit;
                    closestDist = dist;
                }
            }

            var interactable = closest.GetComponent<IInteractable>();
            interactable?.Interact();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}