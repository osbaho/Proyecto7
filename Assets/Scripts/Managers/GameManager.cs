using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Manages Game State, Win Conditions, and Round Logic.
    /// Spawning responsibility has been moved to PlayerSpawner.cs.
    /// </summary>
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        private readonly List<ulong> _activePlayers = new();

        public NetworkVariable<bool> IsGameOver = new(false);
        public NetworkVariable<ulong> WinnerId = new(ulong.MaxValue);

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

                // Subscribe to Player Events (Decoupled from Spawner)
                Assets.Scripts.Events.PlayerEvents.OnPlayerJoined += RegisterPlayer;
                Assets.Scripts.Events.PlayerEvents.OnPlayerEliminated += OnPlayerEliminated;
            }

            // Client notifies server when its Gameplay scene is loaded
            if (IsClient && !IsServer)
            {
#if UNITY_EDITOR
                Debug.Log("[GameManager] Client loaded Gameplay, notifying server to spawn player...");
#endif
                ClientSceneReadyServerRpc();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

                // Unsubscribe
                Assets.Scripts.Events.PlayerEvents.OnPlayerJoined -= RegisterPlayer;
                Assets.Scripts.Events.PlayerEvents.OnPlayerEliminated -= OnPlayerEliminated;
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (_activePlayers.Contains(clientId))
            {
                OnPlayerEliminated(clientId);
            }
        }

        private void RegisterPlayer(ulong clientId)
        {
            if (!IsServer) return;

            if (!_activePlayers.Contains(clientId))
            {
                _activePlayers.Add(clientId);
#if UNITY_EDITOR
                Debug.Log($"[GameManager] Player {clientId} joined. Total Active: {_activePlayers.Count}");
#endif
            }
        }

        private void OnPlayerEliminated(ulong clientId)
        {
            if (!IsServer || IsGameOver.Value) return;

            if (_activePlayers.Contains(clientId))
            {
                _activePlayers.Remove(clientId);
#if UNITY_EDITOR
                Debug.Log($"[GameManager] Player {clientId} eliminated. Remaining: {_activePlayers.Count}");
#endif

                CheckWinCondition();
            }
        }

        private void CheckWinCondition()
        {
            // Win condition logic (Last Man Standing)
            // If strictly 1 player remains, they win.
            // Adjust logic if you want to support solo play testing (where count is 1 initially).
            // Usually check if Count <= 1 AND we started with > 1, or just if Count <= 1.

            if (_activePlayers.Count <= 1)
            {
                IsGameOver.Value = true;
                if (_activePlayers.Count == 1)
                {
                    WinnerId.Value = _activePlayers[0];
#if UNITY_EDITOR
                    Debug.Log($"[GameManager] Winner is Player {WinnerId.Value}!");
#endif
                }
                else
                {
                    // Draw or empty
#if UNITY_EDITOR
                    Debug.Log("[GameManager] Game Over! Draw/Empty");
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
            _activePlayers.Clear(); // Will be repopulated via events on restart

            // Reset spawner state to avoid stale spawn indices/prefab usage
            var spawner = FindFirstObjectByType<PlayerSpawner>();
            if (spawner != null)
            {
                spawner.SendMessage("ResetSpawnerState", SendMessageOptions.DontRequireReceiver);
            }

            // Reload scene
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ClientSceneReadyServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            var spawner = FindFirstObjectByType<PlayerSpawner>();
            if (spawner != null)
            {
                // Guard: prevent duplicate player spawn if RPC is received twice
                foreach (var obj in FindObjectsByType<NetworkObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (obj.IsPlayerObject && obj.OwnerClientId == clientId)
                    {
#if UNITY_EDITOR
                        Debug.Log($"[GameManager] Client {clientId} already has a player object; skipping duplicate spawn.");
#endif
                        return;
                    }
                }
                // Ensure host player exists for this new client, then spawn the client's player
                spawner.SpawnHostIfNeeded();
                spawner.SpawnPlayerForClient(clientId);
#if UNITY_EDITOR
                Debug.Log($"[GameManager] Server spawning player for client {clientId} upon scene ready.");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError("[GameManager] PlayerSpawner not found in Gameplay scene!");
#endif
            }
        }
    }
}
