using Managers;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI
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

            // Initially hide start button for clients
            startButton.gameObject.SetActive(false);

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
            if (IsServer)
            {
                startButton.gameObject.SetActive(true);
                UpdateStartButton();

                // Subscribe to join code changes
                if (JoinCodeManager.Instance != null)
                {
                    JoinCodeManager.Instance.JoinCode.OnValueChanged += OnJoinCodeChanged;
                    // Initial update
                    UpdateJoinCodeDisplay();
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

        private void UpdatePlayerList()
        {
            // Clear existing
            foreach (Transform child in playerListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var player in LobbyManager.Instance.LobbyPlayers)
            {
                GameObject entry = Instantiate(playerListEntryPrefab, playerListContainer);
                TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    string status = player.IsReady ? "<color=green>READY</color>" : "<color=red>NOT READY</color>";
                    text.text = $"{player.PlayerName} - {status}";
                }
            }
        }

        private void UpdateStartButton()
        {
            // Logic to enable/disable start button based on min players and readiness
            // For now, we just keep it interactive if IsServer, but we could make it interactable = check
            bool canStart = CheckStartConditions();
            startButton.interactable = canStart;
        }

        private bool CheckStartConditions()
        {
            if (LobbyManager.Instance.LobbyPlayers.Count < 2) return false;
            foreach (var p in LobbyManager.Instance.LobbyPlayers)
            {
                if (!p.IsReady) return false;
            }
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
