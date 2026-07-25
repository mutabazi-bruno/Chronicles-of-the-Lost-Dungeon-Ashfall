using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Enemies;

namespace Ashfall.Enemies.Behaviours
{
    // simple melee guy, walks at player if in range, hits when close enough
    public class WarriorBehaviour : IEnemyBehaviour
    {
        float lastAttackTime = -999f;
        const float attackCooldown = 1f;

        public void Tick(GameObject enemyObj, Transform player)
        {
            if (player == null) return;

            var enemy = enemyObj.GetComponent<Enemy>();
            var rb = enemy.Rigidbody;
            var animator = enemy.Animator;
            var spriteRenderer = enemyObj.GetComponent<SpriteRenderer>();

            float distance = Vector2.Distance(enemyObj.transform.position, player.position);
            bool isMoving = false;

            if (distance <= enemy.attackRange)
            {
                // close enough, stop and attack
                rb.linearVelocity = Vector2.zero;

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    lastAttackTime = Time.time;
                    animator?.SetTrigger("Attack");

                    var damageable = player.GetComponent<IDamageable>();
                    damageable?.TakeDamage(enemy.attackDamage);
                }
            }
            else if (distance <= enemy.detectionRange)
            {
                // chase, direction vector math right here
                Vector2 direction = (player.position - enemyObj.transform.position).normalized;
                rb.linearVelocity = direction * enemy.moveSpeed;
                isMoving = true;

                // face the direction we're moving
                if (spriteRenderer != null)
                    spriteRenderer.flipX = direction.x > 0;
            }
            else
            {
                // player too far, just stand still
                rb.linearVelocity = Vector2.zero;
            }

            if (animator != null)
            {
                animator.SetBool("Grounded", true); // ground enemy, never falls
                animator.SetInteger("AnimState", isMoving ? 2 : 0);
            }
        }
    }
}