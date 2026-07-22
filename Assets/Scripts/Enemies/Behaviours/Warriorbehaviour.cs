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

            float distance = Vector2.Distance(enemyObj.transform.position, player.position);

            if (distance <= enemy.attackRange)
            {
                // close enough, stop and attack
                rb.linearVelocity = Vector2.zero;

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    lastAttackTime = Time.time;
                    var damageable = player.GetComponent<IDamageable>();
                    damageable?.TakeDamage(enemy.attackDamage);
                }
            }
            else if (distance <= enemy.detectionRange)
            {
                // chase, direction vector math right here
                Vector2 direction = (player.position - enemyObj.transform.position).normalized;
                rb.linearVelocity = direction * enemy.moveSpeed;
            }
            else
            {
                // player too far, just stand still
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
}