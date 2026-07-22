using UnityEngine;

namespace Ashfall.Interfaces
{
    // anything that moves around via a direction, player mainly
    public interface IMovable
    {
        void Move(Vector2 direction);
    }
}