using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Player;

namespace Ashfall.Interactables
{
    public class Chest : MonoBehaviour, IInteractable
    {
        public int healthReward = 20;
        public AudioClip openSound;

        bool isOpened;
        Animator animator;
        Collider2D col;

        void Awake()
        {
            animator = GetComponent<Animator>(); 
            col = GetComponent<Collider2D>();
        }

        public void Interact()
        {
            if (isOpened) return;
            isOpened = true;
            
            GiveReward();

            if (animator != null)
                animator.SetBool("IsOpened", true);

            Ashfall.Systems.AudioManager.Instance?.PlaySFX(openSound);

            // stop it from being interactable again, but keep it visible (opened) instead of
            // disappearing like before
            if (col != null)
                col.enabled = false;
        }

        void GiveReward()
        {
            // find the player thats interacting - simplest way for now is just grabbing by tag
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;

            var health = playerObj.GetComponent<PlayerHealth>();
            if (health == null) return;

            health.Heal(healthReward);
            Debug.Log($"chest gave {healthReward} health");
        }
    }
}