using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private Button quitButton;

        [Header("Settings")]
        [SerializeField] private string gameplaySceneName = "Gameplay";

        private void Awake()
        {
            // Add listeners
            hostButton.onClick.AddListener(StartHost);
            clientButton.onClick.AddListener(StartClient);
            quitButton.onClick.AddListener(QuitGame);
        }

        private void StartHost()
        {
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }

        private void StartClient()
        {
            NetworkManager.Singleton.StartClient();
            // Client automatically syncs scene with host
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
