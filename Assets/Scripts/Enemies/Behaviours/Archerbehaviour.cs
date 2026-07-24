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

        public void Tick(GameObject enemyObj, Transform player)
        {
            if (player == null) return;

            var enemy = enemyObj.GetComponent<Enemy>();
            var rb = enemy.Rigidbody;

            float distance = Vector2.Distance(enemyObj.transform.position, player.position);

            if (distance > enemy.detectionRange)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // back away if player gets too close, otherwise just hold position
            Vector2 toPlayer = (player.position - enemyObj.transform.position);
            if (distance < preferredDistance - 0.5f)
                rb.linearVelocity = -toPlayer.normalized * enemy.moveSpeed;
            else
                rb.linearVelocity = Vector2.zero;

            if (Time.time >= lastShotTime + shootCooldown)
            {
                lastShotTime = Time.time;
                Shoot(enemy, enemyObj.transform, player);
            }
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
            projectile.Fire(enemy.projectilePrefab, direction, enemy.attackDamage);
        }
    }
}