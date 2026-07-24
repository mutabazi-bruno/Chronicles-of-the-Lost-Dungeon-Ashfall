using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Player;

namespace Ashfall.Interactables
{
    public class Chest : MonoBehaviour, IInteractable
    {
        public int coinReward = 10;
        public int healthReward = 20;

        bool isOpened;

        public void Interact()
        {
            if (isOpened) return;
            isOpened = true;

            GiveRandomReward();

            // simple for now, swap for an open animation/sprite swap later
            gameObject.SetActive(false);
        }

        void GiveRandomReward()
        {
            // find the player thats interacting - simplest way for now is just grabbing by tag
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;

            var health = playerObj.GetComponent<PlayerHealth>();
            if (health == null) return;

            int roll = Random.Range(0, 2); // 0 = coins, 1 = health

            if (roll == 0)
            {
                health.stats.AddCoins(coinReward);
                Debug.Log($"chest gave {coinReward} coins");
            }
            else
            {
                health.Heal(healthReward);
                Debug.Log($"chest gave {healthReward} health");
            }
        }
    }
}