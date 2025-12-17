using System.Collections.Generic;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class LobbyUI : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private TextMeshProUGUI readyButtonText;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListEntryPrefab; // Assume a prefab with a Text component
        [SerializeField] private TextMeshProUGUI joinCodeDisplay;

        private void Start()
        {
            readyButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.ToggleReadyServerRpc();
            });

            startButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.StartGame();
            });

            if (leaveButton != null)
            {
                leaveButton.onClick.AddListener(LeaveLobby);
            }

            // Subscribe to changes
            if (LobbyManager.Instance != null && LobbyManager.Instance.LobbyPlayers != null)
            {
                LobbyManager.Instance.LobbyPlayers.OnListChanged += HandleLobbyPlayersChanged;
            }
        }

        private void LeaveLobby()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            // Load Main Menu locally
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public override void OnNetworkSpawn()
        {
#if UNITY_EDITOR
            Debug.Log($"[LobbyUI] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}");
#endif

            if (IsServer)
            {
#if UNITY_EDITOR
                Debug.Log($"[LobbyUI] Activating start button. Button null? {startButton == null}");
#endif
                startButton.gameObject.SetActive(true);
                UpdateStartButton();

                // Subscribe to join code changes and show display
                if (JoinCodeManager.Instance != null)
                {
                    JoinCodeManager.Instance.JoinCode.OnValueChanged += OnJoinCodeChanged;
                    // Initial update
                    UpdateJoinCodeDisplay();
                }

                // Show join code display for host
                if (joinCodeDisplay != null)
                {
                    joinCodeDisplay.gameObject.SetActive(true);
                }
            }
            else
            {
                // Hide start button and join code display for clients
                startButton.gameObject.SetActive(false);

                if (joinCodeDisplay != null)
                {
                    joinCodeDisplay.gameObject.SetActive(false);
                }
            }

            // Initial Draw
            UpdatePlayerList();
        }

        public override void OnNetworkDespawn()
        {
            if (LobbyManager.Instance != null && LobbyManager.Instance.LobbyPlayers != null)
            {
                LobbyManager.Instance.LobbyPlayers.OnListChanged -= HandleLobbyPlayersChanged;
            }

            // Unsubscribe from join code changes
            if (IsServer && JoinCodeManager.Instance != null)
            {
                JoinCodeManager.Instance.JoinCode.OnValueChanged -= OnJoinCodeChanged;
            }
        }

        private void HandleLobbyPlayersChanged(NetworkListEvent<NetworkPlayerState> changeEvent)
        {
            UpdatePlayerList();
            if (IsServer)
            {
                UpdateStartButton();
            }
        }

        private List<GameObject> _playerListPool = new();

        private void UpdatePlayerList()
        {
            var players = LobbyManager.Instance.LobbyPlayers;

            // Ensure pool is large enough
            while (_playerListPool.Count < players.Count)
            {
                GameObject entry = Instantiate(playerListEntryPrefab, playerListContainer);
                _playerListPool.Add(entry);
            }

            // Update active entries
            for (int i = 0; i < _playerListPool.Count; i++)
            {
                if (i < players.Count)
                {
                    var entry = _playerListPool[i];
                    entry.SetActive(true);

                    var player = players[i];
                    TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        string status = player.IsReady ? "<color=green>READY</color>" : "<color=red>NOT READY</color>";
                        text.text = $"{player.PlayerName} - {status}";
                    }
                }
                else
                {
                    // Disable unused
                    _playerListPool[i].SetActive(false);
                }
            }
        }

        private void UpdateStartButton()
        {
            // Logic to enable/disable start button based on min players and readiness
            // For now, we just keep it interactive if IsServer, but we could make it interactable = check
            bool canStart = CheckStartConditions();
            startButton.interactable = canStart;

#if UNITY_EDITOR
            Debug.Log($"[LobbyUI] UpdateStartButton - canStart: {canStart}, Player Count: {LobbyManager.Instance.LobbyPlayers.Count}");
#endif
        }

        private bool CheckStartConditions()
        {
            if (LobbyManager.Instance.LobbyPlayers.Count < 2)
            {
#if UNITY_EDITOR
                Debug.Log($"[LobbyUI] CheckStartConditions - Not enough players: {LobbyManager.Instance.LobbyPlayers.Count}");
#endif
                return false;
            }

            foreach (var p in LobbyManager.Instance.LobbyPlayers)
            {
                if (!p.IsReady)
                {
#if UNITY_EDITOR
                    Debug.Log($"[LobbyUI] CheckStartConditions - Player {p.PlayerName} not ready");
#endif
                    return false;
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[LobbyUI] CheckStartConditions - All conditions met! Can start.");
#endif
            return true;
        }

        private void UpdateJoinCodeDisplay()
        {
            if (joinCodeDisplay == null || JoinCodeManager.Instance == null)
                return;

            string code = JoinCodeManager.Instance.GetJoinCodeString();
            joinCodeDisplay.text = $"Join Code: {code}";

#if UNITY_EDITOR
            Debug.Log($"[LobbyUI] Displaying join code: {code}");
#endif
        }

        private void OnJoinCodeChanged(Unity.Collections.FixedString32Bytes oldValue, Unity.Collections.FixedString32Bytes newValue)
        {
            UpdateJoinCodeDisplay();
        }
    }
}
