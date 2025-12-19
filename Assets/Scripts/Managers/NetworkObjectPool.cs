using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// A generic NetworkObject pool for reusing objects in Multiplayer.
    /// Handles spawning and despawning via Netcode for GameObjects.
    /// </summary>
    public class NetworkObjectPool : NetworkBehaviour
    {
        public static NetworkObjectPool Singleton { get; private set; }

        [SerializeField] private List<PoolConfig> pooledPrefabsList;

        private Dictionary<GameObject, Queue<NetworkObject>> _pooledObjects = new();
        private Dictionary<NetworkObject, GameObject> _spawnedObjects = new();

        private void Awake()
        {
            if (Singleton != null && Singleton != this)
            {
                Destroy(gameObject);
                return;
            }
            Singleton = this;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                InitializePool();
            }
        }

        private void InitializePool()
        {
            foreach (var config in pooledPrefabsList)
            {
                RegisterPrefab(config.prefab, config.prewarmCount);
            }
        }

        private void RegisterPrefab(GameObject prefab, int prewarmCount)
        {
            if (!_pooledObjects.ContainsKey(prefab))
            {
                // Validation: ensure prefab has NetworkObject and is registered in NetworkManager
                var netObj = prefab != null ? prefab.GetComponent<NetworkObject>() : null;
                if (netObj == null)
                {
                    Debug.LogError("[NetworkObjectPool] Prefab missing NetworkObject component: " + (prefab != null ? prefab.name : "<null>"));
                }
                else if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig != null && NetworkManager.Singleton.NetworkConfig.Prefabs != null)
                {
                    if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(prefab))
                    {
                        Debug.LogError($"[NetworkObjectPool] CRITICAL CONFIG ERROR: Prefab '{prefab.name}' is NOT registered in NetworkManager's NetworkPrefabs list! Clients will NOT spawn this object.");
                    }
                }

                _pooledObjects[prefab] = new Queue<NetworkObject>();

                for (int i = 0; i < prewarmCount; i++)
                {
                    NetworkObject obj = CreateInstance(prefab);
                    obj.gameObject.SetActive(false); // Hide until used
                    _pooledObjects[prefab].Enqueue(obj);
                }
            }
        }

        private NetworkObject CreateInstance(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();
            return networkObject;
        }

        /// <summary>
        /// Gets a NetworkObject from the pool.
        /// MUST be called on the Server.
        /// </summary>
        public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!IsServer)
            {
                Debug.LogError("[NetworkObjectPool] GetNetworkObject called on Client! Pooling is Server-only.");
                return null;
            }

            if (!_pooledObjects.ContainsKey(prefab))
            {
                RegisterPrefab(prefab, 1);
            }

            Queue<NetworkObject> queue = _pooledObjects[prefab];
            NetworkObject netObj;

            if (queue.Count > 0)
            {
                netObj = queue.Dequeue();
            }
            else
            {
                netObj = CreateInstance(prefab);
            }

            // Track relationship
            _spawnedObjects[netObj] = prefab;

            // Prepare object
            netObj.transform.position = position;
            netObj.transform.rotation = rotation;
            netObj.gameObject.SetActive(true);

            if (!netObj.IsSpawned)
            {
                netObj.Spawn(true);
            }

            return netObj;
        }

        /// <summary>
        /// Returns a NetworkObject to the pool.
        /// MUST be called on the Server.
        /// </summary>
        public void ReturnNetworkObject(NetworkObject networkObject)
        {
            if (!IsServer) return;

            if (!_spawnedObjects.TryGetValue(networkObject, out GameObject prefab))
            {
                Debug.LogWarning($"[NetworkObjectPool] Returned object {networkObject.name} was not spawned by pool. Destroying.");
                if (networkObject.IsSpawned) networkObject.Despawn();
                Destroy(networkObject.gameObject);
                return;
            }

            _spawnedObjects.Remove(networkObject);

            // Hide and Despawn (keep on server)
            if (networkObject.IsSpawned)
            {
                networkObject.Despawn(false);
            }
            networkObject.gameObject.SetActive(false);

            if (_pooledObjects.TryGetValue(prefab, out var queue))
            {
                queue.Enqueue(networkObject);
            }
        }
    }

    [System.Serializable]
    public struct PoolConfig
    {
        public GameObject prefab;
        public int prewarmCount;
    }
}
