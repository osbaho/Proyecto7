using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Responsible solely for spawning players when they connect.
    /// Extracts this responsibility from GameManager to adhere to SRP.
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour
    {
        [Header("Spawning Settings")]
        [SerializeField] private NetworkObject[] playerPrefabs; // Pool of player models
        [SerializeField] private float spawnDistance = 10f; // Distance from center for spawn points

        private int _spawnIndex; // Track which spawn point to use next
        private readonly List<int> _usedPrefabIndices = new(); // Track used prefab models

        // Predefined spawn positions: North, South, East, West at Y=1
        private readonly Vector3[] spawnPositions =
        {
            new(0, 1, 10),   // North
            new(0, 1, -10),  // South
            new(10, 1, 0),   // East
            new(-10, 1, 0)   // West
        };

        private void ResetSpawnerState()
        {
            _spawnIndex = 0;
            _usedPrefabIndices.Clear();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Validate Network Prefabs for debugging client spawn issues
                ValidateNetworkPrefabs();

                // Do not spawn host immediately; wait until at least one client is ready to avoid missed spawns
                ResetSpawnerState();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                // No subscriptions currently to remove
            }
        }

        // Client readiness is signaled via GameManager RPC; no OnClientConnected spawning here

        public void SpawnPlayerForClient(ulong clientId)
        {
            // Guard against duplicate spawns for the same client
            foreach (var obj in FindObjectsByType<NetworkObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (obj.IsPlayerObject && obj.OwnerClientId == clientId)
                {
#if UNITY_EDITOR
                    Debug.Log($"[PlayerSpawner] Client {clientId} already has a player object; skipping duplicate spawn.");
#endif
                    return;
                }
            }
            if (playerPrefabs == null || playerPrefabs.Length == 0)
            {
                Debug.LogError("[PlayerSpawner] Player Prefabs array is empty or not assigned!");
                return;
            }

            // Select a player prefab from the pool
            NetworkObject selectedPrefab = SelectPlayerPrefab();
            if (selectedPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] Failed to select a valid player prefab!");
                return;
            }

            // Get spawn position (cycle through available positions)
            Vector3 spawnPosition = GetNextSpawnPosition();

#if UNITY_EDITOR
            Debug.Log($"[PlayerSpawner] Spawning player {clientId}: Prefab='{selectedPrefab.name}', Position={spawnPosition}, PrefabIndex={_usedPrefabIndices[^1]}");
#endif

            // Instantiate at spawn position with no rotation
            var playerInstance = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            var netObj = playerInstance.GetComponent<NetworkObject>();
            
            if (netObj == null)
            {
                Debug.LogError($"[PlayerSpawner] Spawned instance '{playerInstance.name}' has NO NetworkObject component!");
                Destroy(playerInstance.gameObject);
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"[PlayerSpawner] Calling SpawnAsPlayerObject for client {clientId}, NetworkObjectId will be assigned...");
#endif

            netObj.SpawnAsPlayerObject(clientId, true);

#if UNITY_EDITOR
            Debug.Log($"[PlayerSpawner] Player {clientId} spawned successfully. NetworkObjectId: {netObj.NetworkObjectId}, IsSpawned: {netObj.IsSpawned}");
#endif
        }

        public void SpawnHostIfNeeded()
        {
            if (!IsServer) return;
            // If host already has a player object in scene, skip
            foreach (var obj in FindObjectsByType<NetworkObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (obj.IsPlayerObject && obj.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                {
                    return; // Host player already spawned
                }
            }

#if UNITY_EDITOR
            Debug.Log("[PlayerSpawner] Spawning host player as part of client-ready flow.");
#endif
            SpawnPlayerForClient(NetworkManager.Singleton.LocalClientId);
        }

        private NetworkObject SelectPlayerPrefab()
        {
            // If all prefabs used, reset the pool
            if (_usedPrefabIndices.Count >= playerPrefabs.Length)
            {
                _usedPrefabIndices.Clear();
#if UNITY_EDITOR
                Debug.Log("[PlayerSpawner] All models used, resetting pool for variety.");
#endif
            }

            // Get indices of available (unused) prefabs
            List<int> availableIndices = new();
            for (int i = 0; i < playerPrefabs.Length; i++)
            {
                if (!_usedPrefabIndices.Contains(i))
                {
                    availableIndices.Add(i);
                }
            }

            // Select random from available
            int selectedIndex = availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
            _usedPrefabIndices.Add(selectedIndex);

            return playerPrefabs[selectedIndex];
        }

        private Vector3 GetNextSpawnPosition()
        {
            // Use spawnDistance if configured in inspector, otherwise use hardcoded positions
            Vector3 position;

            if (spawnDistance > 0)
            {
                // Calculate position based on cardinal direction and spawnDistance
                position = _spawnIndex switch
                {
                    0 => new Vector3(0, 1, spawnDistance),      // North
                    1 => new Vector3(0, 1, -spawnDistance),     // South
                    2 => new Vector3(spawnDistance, 1, 0),      // East
                    3 => new Vector3(-spawnDistance, 1, 0),     // West
                    _ => new Vector3(0, 1, 0)                   // Fallback to center
                };
            }
            else
            {
                // Use hardcoded positions array
                position = spawnPositions[_spawnIndex % spawnPositions.Length];
            }

            // Increment and cycle spawn index
            _spawnIndex = (_spawnIndex + 1) % 4;

            return position;
        }

        private void ValidateNetworkPrefabs()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.NetworkConfig == null || NetworkManager.Singleton.NetworkConfig.Prefabs == null)
            {
                Debug.LogError("[PlayerSpawner] Cannot validate: NetworkManager or NetworkConfig is null!");
                return;
            }

            var networkPrefabsList = NetworkManager.Singleton.NetworkConfig.Prefabs;
            var prefabEntries = networkPrefabsList.Prefabs;
            
#if UNITY_EDITOR
            Debug.Log($"[PlayerSpawner] Validating {playerPrefabs?.Length ?? 0} player prefabs against {prefabEntries.Count} registered network prefabs.");
#endif
            
            if (playerPrefabs != null)
            {
                for (int i = 0; i < playerPrefabs.Length; i++)
                {
                    var prefab = playerPrefabs[i];
                    if (prefab == null)
                    {
                        Debug.LogError($"[PlayerSpawner] Player prefab at index {i} is NULL! Check PlayerSpawner inspector.");
                        continue;
                    }
                    
                    // Use public API Contains
                    bool found = networkPrefabsList.Contains(prefab.gameObject);

#if UNITY_EDITOR
                    Debug.Log($"[PlayerSpawner] Prefab [{i}]: '{prefab.name}' (GameObject: '{prefab.gameObject.name}') - Registered: {found}");
#endif

                    if (!found)
                    {
                        Debug.LogError($"[PlayerSpawner] CRITICAL CONFIG ERROR: Player Prefab '{prefab.name}' is NOT registered in NetworkManager's NetworkPrefabs list! Clients will NOT spawn this player.");
                    }
                }
            }
            else
            {
                Debug.LogError("[PlayerSpawner] playerPrefabs array is NULL! No players can spawn.");
            }
        }
    }
}
