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

        public event Action OnAttack;
        public static event Action OnAttackSound;

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
            OnAttackSound?.Invoke();

            currentAttack++;
            if (currentAttack > 3) currentAttack = 1;

            animator?.SetTrigger("Attack" + currentAttack);

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(attackDamage);
            }
        }

        public static void RaiseAttackSound() => OnAttackSound?.Invoke();

        void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}