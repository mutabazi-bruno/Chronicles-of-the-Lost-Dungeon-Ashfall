using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Enemies;
using Ashfall.Systems;
using Ashfall.Combat;

namespace Ashfall.Enemies.Behaviours
{
    // ranged guy, keeps distance and shoots instead of chasing in close
    public class ArcherBehaviour : IEnemyBehaviour
    {
        float lastShotTime = -999f;
        const float shootCooldown = 1.5f;
        const float preferredDistance = 4f; // tries to stay around this far away
        const float facingDeadzone = 0.3f; // ignore tiny x differences so it doesnt flicker

        bool facingRight = true;

        public void Tick(GameObject enemyObj, Transform player)
        {
            if (player == null) return;

            var enemy = enemyObj.GetComponent<Enemy>();
            var rb = enemy.Rigidbody;
            var animator = enemy.Animator;
            var spriteRenderer = enemyObj.GetComponent<SpriteRenderer>();

            float distance = Vector2.Distance(enemyObj.transform.position, player.position);
            bool isMoving = false;

            if (distance > enemy.detectionRange)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            else
            {
                // back away if player gets too close, otherwise just hold position
                Vector2 toPlayer = (player.position - enemyObj.transform.position);

                // only flip if the player is clearly on one side, stops flicker when directly overlapping
                if (toPlayer.x > facingDeadzone) facingRight = false;
                else if (toPlayer.x < -facingDeadzone) facingRight = true;

                if (spriteRenderer != null)
                    spriteRenderer.flipX = facingRight;

                if (distance < preferredDistance - 0.5f)
                {
                    // horizontal only, gravity/ground collision handles vertical
                    float xDir = toPlayer.x > 0 ? -1f : 1f;
                    rb.linearVelocity = new Vector2(xDir * enemy.moveSpeed, rb.linearVelocity.y);
                    isMoving = true;
                }
                else
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                }

                if (Time.time >= lastShotTime + shootCooldown)
                {
                    lastShotTime = Time.time;
                    animator?.SetTrigger("Attack");
                    Shoot(enemy, enemyObj.transform, player);
                }
            }

            if (animator != null)
                animator.SetInteger("AnimState", isMoving ? 1 : 0);
        }

        void Shoot(Enemy enemy, Transform enemyTransform, Transform player)
        {
            if (enemy.projectilePrefab == null || enemy.firePoint == null) return;

            Vector2 direction = (player.position - enemy.firePoint.position).normalized;

            GameObject proj = ObjectPoolManager.Instance.GetFromPool(
                enemy.projectilePrefab,
                enemy.firePoint.position,
                Quaternion.identity);

            var projectile = proj.GetComponent<Projectile>();
            projectile.Fire(enemy.projectilePrefab, direction, enemy.attackDamage, "Player");
        }
    }
}