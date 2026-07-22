using UnityEngine;

namespace Ashfall.Interfaces
{
    // strategy pattern - warrior/archer/guardian all plug into the same Enemy script
    public interface IEnemyBehaviour
    {
        void Tick(GameObject enemy, GameObject player);
    }
}