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
        string targetTag; // only this tag takes damage, stops enemies hitting each other

        public void Fire(GameObject prefabRef, Vector2 dir, int dmg, string targetTag = "Player")
        {
            sourcePrefab = prefabRef;
            direction = dir.normalized;
            damage = dmg;
            spawnTime = Time.time;
            this.targetTag = targetTag;
        }

        void Update()
        {
            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            if (Time.time >= spawnTime + lifetime)
                ReturnToPool();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(targetTag)) return;

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