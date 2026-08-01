using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Enemies.Behaviours
{
    public class BomberBehaviour : IEnemyBehaviour
    {
        const float DetonateRange = 1.2f;

        const float ChargeSpeedMultiplier = 1.5f;

        const int DetonationMultiplier = 3;

        bool hasDetonated;

        public void Tick(GameObject enemyObj, Transform player)
        {
           
            if (player == null || hasDetonated) return;

            var enemy = enemyObj.GetComponent<Enemy>();
            if (enemy == null) return;

            float distance = Vector2.Distance(enemyObj.transform.position, player.position);

            if (distance <= DetonateRange)
            {
                Detonate(enemy, player);
                return;
            }

            ChargeToward(enemy, enemyObj.transform, player);
        }

        void Detonate(Enemy enemy, Transform player)
        {
            hasDetonated = true;
          
            var damageable = player.GetComponent<IDamageable>();
            damageable?.TakeDamage(enemy.attackDamage * DetonationMultiplier);

            enemy.TakeDamage(enemy.maxHealth);
        }

        void ChargeToward(Enemy enemy, Transform self, Transform player)
        {
            float direction = player.position.x > self.position.x ? 1f : -1f;

            enemy.Rigidbody.linearVelocity = new Vector2(
                direction * enemy.moveSpeed * ChargeSpeedMultiplier,
                enemy.Rigidbody.linearVelocity.y);
        }
    }
}