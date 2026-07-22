using System;
using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Player
{
    public class PlayerAttack : MonoBehaviour
    {
        [Header("Attack")]
        public Transform attackPoint;
        public float attackRange = 0.6f;
        public int attackDamage = 15;
        public float attackCooldown = 0.4f;
        public LayerMask enemyLayer;

        public event Action OnAttack; // for animation/audio hooks later

        float lastAttackTime = -999f;

        void Update()
        {
            if (Input.GetButtonDown("Fire1") && Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }

        void Attack()
        {
            lastAttackTime = Time.time;
            OnAttack?.Invoke();

            // find anything hittable in range and hit it
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(attackDamage);
            }
        }

        // draw the attack range in the editor so its easy to see/tune
        void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}