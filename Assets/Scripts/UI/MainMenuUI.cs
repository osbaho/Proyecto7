using System;
using System.Threading.Tasks;
using Assets.Scripts.Managers;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_InputField joinCodeInputField;

        [Header("Relay Settings")]
        [SerializeField] private bool useRelay = true; // Toggle for Relay vs Direct connection
        [SerializeField] private int maxPlayers = 8;
        
        // Auto-fallback guards to avoid infinite loops when Relay is blocked
        private bool relayFallbackAttemptedHost = false;
        private bool relayFallbackAttemptedClient = false;


        // [SerializeField] private string gameplaySceneName = "Gameplay"; // Unused, redirecting to Lobby

        private void Awake()
        {
            // Add listeners
            hostButton.onClick.AddListener(StartHostAsyncWrapper);
            clientButton.onClick.AddListener(StartClientAsyncWrapper);
            quitButton.onClick.AddListener(QuitGame);
        }

        private async void StartHostAsyncWrapper()
        {
            await StartHostAsync();
        }

        private async void StartClientAsyncWrapper()
        {
            await StartClientAsync();
        }

        private async Task StartHostAsync()
        {
#if UNITY_EDITOR
            Debug.Log($"[StartHost] useRelay={useRelay}, IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, ShutdownInProgress: {NetworkManager.Singleton.ShutdownInProgress}");
#endif

            if (!CanStartNetworkInstance())
            {
                return;
            }

            ToggleButtons(false);

            try
            {
                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    throw new InvalidOperationException("UnityTransport not found on NetworkManager.");
                }

                if (useRelay)
                {
                    var (allocation, joinCode) = await RelaySessionService.CreateHostAllocationAsync(maxPlayers);
                    transport.SetRelayServerData(
                        allocation.RelayServer.IpV4,
                        (ushort)allocation.RelayServer.Port,
                        allocation.AllocationIdBytes,
                        allocation.Key,
                        allocation.ConnectionData,
                        null,
                        true);
                    JoinCodeManager.PreGeneratedJoinCode = joinCode;
#if UNITY_EDITOR
                    Debug.Log($"[StartHost] Using Relay with join code: {joinCode}");
#endif
                }
                else
                {
                    // Direct connection mode for development
                    transport.SetConnectionData("127.0.0.1", 7777);
                    string localCode = JoinCodeValidator.GenerateRandomCode();
                    JoinCodeManager.PreGeneratedJoinCode = localCode;
#if UNITY_EDITOR
                    Debug.Log($"[StartHost] Direct mode (localhost:7777) - Join code: {localCode}");
#endif
                }

                if (NetworkManager.Singleton.StartHost())
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
                }
                else
                {
                    throw new InvalidOperationException("Failed to Start Host (Netcode)");
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[StartHost] Error: {ex.Message}\n{ex.StackTrace}");
#else
                Debug.LogError($"[StartHost] Error: {ex.Message}");
#endif

                // Auto-fallback to direct mode in Editor if Relay fails
                if (useRelay && !relayFallbackAttemptedHost && Application.isEditor)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[StartHost] Relay failed. Auto-fallback to Direct (localhost:7777).");
#endif
                    relayFallbackAttemptedHost = true;
                    try
                    {
                        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                        if (transport == null)
                        {
                            throw new InvalidOperationException("UnityTransport not found on NetworkManager.");
                        }

                        // Switch to direct connection
                        useRelay = false;
                        transport.SetConnectionData("127.0.0.1", 7777);

                        string localCode = JoinCodeValidator.GenerateRandomCode();
                        JoinCodeManager.PreGeneratedJoinCode = localCode;
#if UNITY_EDITOR
                        Debug.Log($"[StartHost] Fallback Direct mode (localhost:7777) - Join code: {localCode}");
#endif
                        if (NetworkManager.Singleton.StartHost())
                        {
                            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
                        }
                        else
                        {
                            throw new InvalidOperationException("Failed to Start Host (Netcode) after fallback");
                        }
                    }
                    catch (Exception ex2)
                    {
#if UNITY_EDITOR
                        Debug.LogError($"[StartHost] Fallback failed: {ex2.Message}\n{ex2.StackTrace}");
#else
                        Debug.LogError($"[StartHost] Fallback failed: {ex2.Message}");
#endif
                    }
                }
            }
            finally
            {
                ToggleButtons(true);
            }
        }

        private async Task StartClientAsync()
        {
#if UNITY_EDITOR
            Debug.Log($"[StartClient] useRelay={useRelay}, IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, ShutdownInProgress: {NetworkManager.Singleton.ShutdownInProgress}");
#endif

            if (!CanStartNetworkInstance())
            {
                return;
            }

            // Validate join code
            if (joinCodeInputField == null || string.IsNullOrWhiteSpace(joinCodeInputField.text))
            {
#if UNITY_EDITOR
                Debug.LogError("Please enter a join code!");
#endif
                return;
            }

            string code = joinCodeInputField.text.Trim().ToUpper();
            if (!JoinCodeValidator.IsValidFormat(code))
            {
#if UNITY_EDITOR
                Debug.LogError("Invalid join code format! Code must be 6 alphanumeric characters.");
#endif
                return;
            }

            ToggleButtons(false);

            try
            {
                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    throw new InvalidOperationException("UnityTransport not found on NetworkManager.");
                }

                if (useRelay)
                {
                    var allocation = await RelaySessionService.JoinAllocationAsync(code);
                    transport.SetRelayServerData(
                        allocation.RelayServer.IpV4,
                        (ushort)allocation.RelayServer.Port,
                        allocation.AllocationIdBytes,
                        allocation.Key,
                        allocation.ConnectionData,
                        allocation.HostConnectionData,
                        true);
#if UNITY_EDITOR
                    Debug.Log($"[StartClient] Connecting via Relay with code: {code}");
#endif
                }
                else
                {
                    // Direct connection mode
                    transport.SetConnectionData("127.0.0.1", 7777);
#if UNITY_EDITOR
                    Debug.Log($"[StartClient] Connecting directly to localhost:7777");
#endif
                }

                // Store the join code for in-session validation
                PlayerPrefs.SetString("PendingJoinCode", code);

                if (!NetworkManager.Singleton.StartClient())
                {
                    throw new InvalidOperationException("Failed to Start Client (Netcode)");
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[StartClient] Error: {ex.Message}\n{ex.StackTrace}");
#else
                Debug.LogError($"[StartClient] Error: {ex.Message}");
#endif

                // Auto-fallback to direct mode in Editor if Relay fails
                if (useRelay && !relayFallbackAttemptedClient && Application.isEditor)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[StartClient] Relay failed. Auto-fallback to Direct (localhost:7777).");
#endif
                    relayFallbackAttemptedClient = true;
                    try
                    {
                        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                        if (transport == null)
                        {
                            throw new InvalidOperationException("UnityTransport not found on NetworkManager.");
                        }

                        // Switch to direct connection
                        useRelay = false;
                        transport.SetConnectionData("127.0.0.1", 7777);

#if UNITY_EDITOR
                        Debug.Log("[StartClient] Fallback Direct mode: connecting to localhost:7777");
#endif
                        if (!NetworkManager.Singleton.StartClient())
                        {
                            throw new InvalidOperationException("Failed to Start Client (Netcode) after fallback");
                        }
                    }
                    catch (Exception ex2)
                    {
#if UNITY_EDITOR
                        Debug.LogError($"[StartClient] Fallback failed: {ex2.Message}\n{ex2.StackTrace}");
#else
                        Debug.LogError($"[StartClient] Fallback failed: {ex2.Message}");
#endif
                    }
                }
            }
            finally
            {
                ToggleButtons(true);
            }
        }

        private bool CanStartNetworkInstance()
        {
            if (NetworkManager.Singleton != null &&
                (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient) &&
                !NetworkManager.Singleton.ShutdownInProgress)
            {
#if UNITY_EDITOR
                Debug.LogWarning("NetworkManager is already running!");
#endif
                return false;
            }

            return true;
        }

        private void ToggleButtons(bool enabled)
        {
            if (hostButton != null) hostButton.interactable = enabled;
            if (clientButton != null) clientButton.interactable = enabled;
            if (quitButton != null) quitButton.interactable = enabled;
        }

        private void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
