using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class LobbyManager : NetworkBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        public NetworkList<NetworkPlayerState> LobbyPlayers;

        [Header("Settings")]
        [SerializeField] private string gameplaySceneName = "Gameplay";
        [SerializeField] private int minPlayers = 2;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LobbyPlayers = new NetworkList<NetworkPlayerState>();
        }

        public override void OnNetworkSpawn()
        {
            // Subscribe on both Server (for logic) and Client (for cleanup)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            if (IsServer)
            {
                // Add Host if not already added
                AddPlayer(NetworkManager.Singleton.LocalClientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (IsServer)
            {
                AddPlayer(clientId);
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (IsServer)
            {
                RemovePlayer(clientId);
            }

            // Logic for Client: If we are a client and WE disconnected (or Host kicked/quit)
            // We need to go back to Menu.
            // Note: When Host shuts down, Client gets a disconnect callback with Server ID or Local ID depending on transport.
            // Usually simpler: If we are not the server anymore (disconnected), go to menu.
            if (!IsServer && !IsHost)
            {
                // We are just a client, and a disconnect happened.
                // It might be us, or another client.
                // If it's us (LocalClientId) OR the Server (ID 0 usually), we leave.
                if (clientId == NetworkManager.Singleton.LocalClientId || clientId == NetworkManager.ServerClientId)
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                }
            }
        }

        private void AddPlayer(ulong clientId)
        {
            // Avoid duplicates
            foreach (var p in LobbyPlayers)
            {
                if (p.ClientId == clientId) return;
            }

            LobbyPlayers.Add(new NetworkPlayerState(clientId, $"Player {clientId}", false));
        }

        private void RemovePlayer(ulong clientId)
        {
            for (int i = 0; i < LobbyPlayers.Count; i++)
            {
                if (LobbyPlayers[i].ClientId == clientId)
                {
                    LobbyPlayers.RemoveAt(i);
                    break;
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ToggleReadyServerRpc(RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            for (int i = 0; i < LobbyPlayers.Count; i++)
            {
                if (LobbyPlayers[i].ClientId == clientId)
                {
                    var player = LobbyPlayers[i];
                    player.IsReady = !player.IsReady;
                    LobbyPlayers[i] = player; // Trigger Dirty
                    break;
                }
            }
        }

        public void StartGame()
        {
            if (!IsServer) return;

            if (LobbyPlayers.Count < minPlayers)
            {
#if UNITY_EDITOR
                Debug.Log($"Not enough players! ({LobbyPlayers.Count}/{minPlayers})");
#endif
                return;
            }

            foreach (var p in LobbyPlayers)
            {
                if (!p.IsReady)
                {
#if UNITY_EDITOR
                    Debug.Log("Not all players ready!");
#endif
                    return;
                }
            }

            NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }
    }
}
