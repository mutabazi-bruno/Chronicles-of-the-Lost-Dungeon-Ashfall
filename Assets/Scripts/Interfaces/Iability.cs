using UnityEngine;

namespace Ashfall.Interfaces
{
    // strategy pattern - dash, heavy strike, whatever we add later
    // all plug into the player the same way
    public interface IAbility
    {
        void Activate(GameObject user);
        float StaminaCost { get; }
    }
}