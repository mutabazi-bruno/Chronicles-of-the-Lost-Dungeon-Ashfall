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

            GameObject obj;
            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }
            else
            {
                // pool empty, make a new one, itll get returned to the pool later
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