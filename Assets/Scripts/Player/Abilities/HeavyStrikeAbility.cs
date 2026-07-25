using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Player.Abilities
{
    public class HeavyStrikeAbility : IAbility
    {
        public float StaminaCost => 40f;

        float rangeMultiplier = 1.8f;
        int bonusDamage = 20;

        public void Activate(GameObject user)
        {
            var attack = user.GetComponent<PlayerAttack>();
            if (attack == null || attack.attackPoint == null) return;

            // reuse attack3 as the "heavy" swing, pack doesnt have a dedicated heavy anim
            var animator = user.GetComponent<Animator>();
            animator?.SetTrigger("Attack3");

            float range = attack.attackRange * rangeMultiplier;
            int damage = attack.attackDamage + bonusDamage;

            Collider2D[] hits = Physics2D.OverlapCircleAll(attack.attackPoint.position, range, attack.enemyLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                damageable?.TakeDamage(damage);
            }
        }
    }
}