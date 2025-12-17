using TMPro;
using Unity.Netcode;
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


        // [SerializeField] private string gameplaySceneName = "Gameplay"; // Unused, redirecting to Lobby

        private void Awake()
        {
            // Add listeners
            hostButton.onClick.AddListener(StartHost);
            clientButton.onClick.AddListener(StartClient);
            quitButton.onClick.AddListener(QuitGame);
        }

        private void StartHost()
        {
#if UNITY_EDITOR
            Debug.Log($"[StartHost] IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, ShutdownInProgress: {NetworkManager.Singleton.ShutdownInProgress}");
#endif

            // Check if NetworkManager is actually running
            if (NetworkManager.Singleton != null &&
                (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient) &&
                !NetworkManager.Singleton.ShutdownInProgress)
            {
#if UNITY_EDITOR
                Debug.LogWarning("NetworkManager is already running!");
#endif
                return;
            }

            if (NetworkManager.Singleton.StartHost())
            {
                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError("Failed to Start Host! (Check Console for Network errors)");
#endif
            }
        }

        private void StartClient()
        {
#if UNITY_EDITOR
            Debug.Log($"[StartClient] IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, ShutdownInProgress: {NetworkManager.Singleton.ShutdownInProgress}");
#endif

            // Check if NetworkManager is actually running
            if (NetworkManager.Singleton != null &&
                (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient) &&
                !NetworkManager.Singleton.ShutdownInProgress)
            {
#if UNITY_EDITOR
                Debug.LogWarning("NetworkManager is already running!");
#endif
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

            if (!Assets.Scripts.Managers.JoinCodeValidator.IsValidFormat(joinCodeInputField.text))
            {
#if UNITY_EDITOR
                Debug.LogError($"Invalid join code format! Code must be 6 alphanumeric characters.");
#endif
                return;
            }

            // Store the join code for validation after connection
            PlayerPrefs.SetString("PendingJoinCode", joinCodeInputField.text.ToUpper());

            if (!NetworkManager.Singleton.StartClient())
            {
#if UNITY_EDITOR
                Debug.LogError("Failed to Start Client!");
#endif
            }
            // Client auto-syncs if successful
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
