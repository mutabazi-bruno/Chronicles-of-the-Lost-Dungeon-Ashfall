using System;
using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Player.Abilities;
using Ashfall.Systems;

namespace Ashfall.Player
{
    public class PlayerAbilities : MonoBehaviour
    {
        PlayerHealth health;

        // strategy pattern - both abilities are just IAbility, the player never knows
        // which concrete class it is holding
        IAbility dash = new DashAbility();
        IAbility heavyStrike = new HeavyStrikeAbility();

        // observer - HUD ability icons / audio listen to this
        public event Action<string> OnAbilityUsed;
        public event Action<string> OnAbilityFailed; // not enough stamina

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            if (GameInput.DashPressed)
                TryUseAbility(dash, "Dash");

            if (GameInput.HeavyStrikePressed)
                TryUseAbility(heavyStrike, "Heavy Strike");
        }

        void TryUseAbility(IAbility ability, string abilityName)
        {
            if (!health.stats.SpendStamina(ability.StaminaCost))
            {
                OnAbilityFailed?.Invoke(abilityName);
                return;
            }

            ability.Activate(gameObject);
            OnAbilityUsed?.Invoke(abilityName);
        }

        // Expose costs for UI indicators.
        public float DashCost => dash.StaminaCost;
        public float HeavyStrikeCost => heavyStrike.StaminaCost;
    }
}