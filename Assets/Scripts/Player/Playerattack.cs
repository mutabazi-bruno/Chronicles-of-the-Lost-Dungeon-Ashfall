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

        public event Action OnAttack; // for audio hooks later

        Animator animator;
        float lastAttackTime = -999f;
        int currentAttack = 0;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

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

            // cycle 1 -> 2 -> 3 -> back to 1, matches the combo animations
            currentAttack++;
            if (currentAttack > 3) currentAttack = 1;

            animator?.SetTrigger("Attack" + currentAttack);

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