using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Systems;

namespace Ashfall.Combat
{
    public class Projectile : MonoBehaviour
    {
        public float speed = 10f;
        public int damage = 10;
        public float lifetime = 3f; // auto return to pool if it never hits anything

        GameObject sourcePrefab; // which prefab to return to when done
        Vector2 direction;
        float spawnTime;

        public void Fire(GameObject prefabRef, Vector2 dir, int dmg)
        {
            sourcePrefab = prefabRef;
            direction = dir.normalized;
            damage = dmg;
            spawnTime = Time.time;
        }

        void Update()
        {
            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            if (Time.time >= spawnTime + lifetime)
                ReturnToPool();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                ReturnToPool();
            }
        }

        void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReturnToPool(sourcePrefab, gameObject);
        }
    }
}