using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Player;

namespace Ashfall.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour, ICollectable
    {
        public int coinValue = 5;
        public AudioClip collectSound;

        void Awake()
        {
            // make sure this is set as a trigger so touching it doesnt block movement
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            Collect();
        }

        public void Collect()
        {
            var health = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerHealth>();
            health?.AddCoins(coinValue); // goes through PlayerHealth now so the HUD hears about it

            Ashfall.Systems.AudioManager.Instance?.PlaySFX(collectSound);

            Destroy(gameObject);
        }
    }
}