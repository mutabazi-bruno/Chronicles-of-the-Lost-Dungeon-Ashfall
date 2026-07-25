using System;
using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Player.Abilities;

namespace Ashfall.Player
{
    public class PlayerAbilities : MonoBehaviour
    {
        PlayerHealth health;

        IAbility dash = new DashAbility();
        IAbility heavyStrike = new HeavyStrikeAbility();

        public event Action<string> OnAbilityUsed; // observer, for ui/audio hooks later

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
                TryUseAbility(dash, "Dash");

            if (Input.GetButtonDown("Fire2"))
                TryUseAbility(heavyStrike, "Heavy Strike");
        }

        void TryUseAbility(IAbility ability, string name)
        {
            if (!health.stats.SpendStamina(ability.StaminaCost))
            {
                Debug.Log($"not enough stamina for {name}");
                return;
            }

            ability.Activate(gameObject);
            OnAbilityUsed?.Invoke(name);
        }
    }
}