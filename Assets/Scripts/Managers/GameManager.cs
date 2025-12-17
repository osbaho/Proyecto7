using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Managers
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        private readonly List<ulong> _activePlayers = new();
        private int _spawnIndex; // Track which spawn point to use next
        private readonly List<int> _usedPrefabIndices = new(); // Track used prefab models

        public NetworkVariable<bool> IsGameOver = new(false);
        public NetworkVariable<ulong> WinnerId = new(ulong.MaxValue);

        [SerializeField] private NetworkObject[] playerPrefabs; // Pool of player models
        [SerializeField] private float spawnDistance = 10f; // Distance from center for spawn points

        // Predefined spawn positions: North, South, East, West at Y=1
        private readonly Vector3[] spawnPositions =
        {
            new(0, 1, 10),   // North
            new(0, 1, -10),  // South
            new(10, 1, 0),   // East
            new(-10, 1, 0)   // West
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

                // Subscribe to Player Events
                Assets.Scripts.Events.PlayerEvents.OnPlayerJoined += RegisterPlayer;
                Assets.Scripts.Events.PlayerEvents.OnPlayerEliminated += OnPlayerEliminated;

                // Spawn for clients already connected (scene transition)
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    SpawnPlayerForClient(clientId);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

                // Unsubscribe
                Assets.Scripts.Events.PlayerEvents.OnPlayerJoined -= RegisterPlayer;
                Assets.Scripts.Events.PlayerEvents.OnPlayerEliminated -= OnPlayerEliminated;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            SpawnPlayerForClient(clientId);
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (_activePlayers.Contains(clientId))
            {
                OnPlayerEliminated(clientId);
            }
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            if (playerPrefabs == null || playerPrefabs.Length == 0)
            {
                Debug.LogError("Player Prefabs array is empty or not assigned in GameManager!");
                return;
            }

            // Select a player prefab from the pool
            NetworkObject selectedPrefab = SelectPlayerPrefab();
            if (selectedPrefab == null)
            {
                Debug.LogError("Failed to select a valid player prefab!");
                return;
            }

            // Get spawn position (cycle through available positions)
            Vector3 spawnPosition = GetNextSpawnPosition();

            // Instantiate at spawn position with no rotation
            var playerInstance = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

#if UNITY_EDITOR
            Debug.Log($"Spawned player {clientId} at position {spawnPosition} with model index {_usedPrefabIndices[^1]}");
#endif
        }

        private NetworkObject SelectPlayerPrefab()
        {
            // If all prefabs used, reset the pool
            if (_usedPrefabIndices.Count >= playerPrefabs.Length)
            {
                _usedPrefabIndices.Clear();
#if UNITY_EDITOR
                Debug.Log("[PlayerPool] All models used, resetting pool for variety.");
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

        // Changed to private as it's now event-driven (though maintained public if other scripts call it, but checking usage shows only HealthSystem used it)
        // Leaving public for now in case of other external calls, but logic is driven by Events
        private void RegisterPlayer(ulong clientId)
        {
            if (!IsServer) return;

            if (!_activePlayers.Contains(clientId))
            {
                _activePlayers.Add(clientId);
#if UNITY_EDITOR
                Debug.Log($"Player {clientId} registered. Total: {_activePlayers.Count}");
#endif
            }
        }

        // Changed to private or kept public? It was public.
        private void OnPlayerEliminated(ulong clientId)
        {
            if (!IsServer || IsGameOver.Value) return;

            if (_activePlayers.Contains(clientId))
            {
                _activePlayers.Remove(clientId);
#if UNITY_EDITOR
                Debug.Log($"Player {clientId} eliminated. Remaining: {_activePlayers.Count}");
#endif

                CheckWinCondition();
            }
        }

        private void CheckWinCondition()
        {
            if (_activePlayers.Count <= 1)
            {
                IsGameOver.Value = true;
                if (_activePlayers.Count == 1)
                {
                    WinnerId.Value = _activePlayers[0];
#if UNITY_EDITOR
                    Debug.Log($"Winner is Player {WinnerId.Value}!");
#endif
                }
                else
                {
                    // Draw or empty
#if UNITY_EDITOR
                    Debug.Log("Game Over! Draw/Empty");
#endif
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RestartGameServerRpc()
        {
            if (!IsServer) return;

            IsGameOver.Value = false;
            WinnerId.Value = ulong.MaxValue;
            _activePlayers.Clear(); // Will be repopulated on spawn/restart logic

            // Reload scene
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }
}
