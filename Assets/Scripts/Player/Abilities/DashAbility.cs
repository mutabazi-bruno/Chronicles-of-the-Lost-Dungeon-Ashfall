using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Player.Abilities
{
    public class DashAbility : IAbility
    {
        public float StaminaCost => 25f;

        float dashForce = 15f;
        float dashDuration = 0.2f;

        public void Activate(GameObject user)
        {
            var controller = user.GetComponent<PlayerController>();
            controller?.PerformDash(dashForce, dashDuration);
        }
    }
}