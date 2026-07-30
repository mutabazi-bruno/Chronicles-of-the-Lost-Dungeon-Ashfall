using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Core;
using Ashfall.Player;

namespace Ashfall.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    public class KeyPickup : MonoBehaviour, ICollectable
    {
        public string keyName = "Key";

        Collider2D col;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            col.enabled = false; // briefly off so it doesnt instantly get grabbed if it spawns on top of the player

            Invoke(nameof(EnableCollision), 0.3f);
        }

        void EnableCollision()
        {
            col.enabled = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            Collect();
        }

        public void Collect()
        {
            var inventory = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerInventory>();
            inventory?.AddItem(new Item(keyName, ItemType.Key, 0));

            Destroy(gameObject);
        }
    }
}