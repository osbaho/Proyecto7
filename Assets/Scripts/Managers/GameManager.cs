using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        private List<ulong> _activePlayers = new List<ulong>();

        public NetworkVariable<bool> IsGameOver = new NetworkVariable<bool>(false);
        public NetworkVariable<ulong> WinnerId = new NetworkVariable<ulong>(ulong.MaxValue);

        [SerializeField] private NetworkObject playerPrefab;

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
            if (playerPrefab == null)
            {
                Debug.LogError("Player Prefab not assigned in GameManager!");
                return;
            }

            var playerInstance = Instantiate(playerPrefab);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }

        public void RegisterPlayer(ulong clientId)
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

        public void OnPlayerEliminated(ulong clientId)
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
