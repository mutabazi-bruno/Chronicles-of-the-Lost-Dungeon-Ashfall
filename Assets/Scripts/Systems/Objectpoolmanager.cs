using System.Collections.Generic;
using UnityEngine;

namespace Ashfall.Systems
{
    // singleton - one pool manager for the whole game
    // keeps a queue of inactive objects per prefab so we dont instantiate/destroy constantly
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<GameObject>();

            var queue = pools[prefab];
            
            GameObject obj = null;
            // Pooled objects live in whatever scene they were spawned in, but this

            while (queue.Count > 0 && obj == null)
                obj = queue.Dequeue();
                
            if (obj != null)
            {
              obj.transform.position = position;
              obj.transform.rotation = rotation;
              obj.SetActive(true);
            }
            else
            {
              // pool empty (or everything left in it was stale), make a new one
              obj = Instantiate(prefab, position, rotation);
            }

            return obj;
        }

        public void ReturnToPool(GameObject prefab, GameObject instance)
        {
            instance.SetActive(false);

            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<GameObject>();

            pools[prefab].Enqueue(instance);
        }
    }
}