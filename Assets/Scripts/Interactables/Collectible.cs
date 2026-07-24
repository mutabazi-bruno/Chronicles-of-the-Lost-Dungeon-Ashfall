using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Player;

namespace Ashfall.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour, ICollectable
    {
        public int coinValue = 5;

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
            health?.stats.AddCoins(coinValue);

            Debug.Log($"picked up {coinValue} coins");
            Destroy(gameObject);
        }
    }
}