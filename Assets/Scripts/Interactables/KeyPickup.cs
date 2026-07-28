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

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
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