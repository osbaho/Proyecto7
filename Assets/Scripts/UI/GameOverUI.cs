using Assets.Scripts.Managers;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private Button restartButton;

        private void Start()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
                restartButton.gameObject.SetActive(false);
            }

            StartCoroutine(WaitForGameManager());
        }

        private System.Collections.IEnumerator WaitForGameManager()
        {
            while (GameManager.Instance == null)
            {
                yield return null;
            }

            // Subscribe
            GameManager.Instance.IsGameOver.OnValueChanged += OnGameOverChanged;

            // Check initial state
            if (GameManager.Instance.IsGameOver.Value)
            {
                ShowGameOver(GameManager.Instance.WinnerId.Value);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.IsGameOver.OnValueChanged -= OnGameOverChanged;
            }
        }

        private void OnGameOverChanged(bool previous, bool current)
        {
            if (current)
            {
                ShowGameOver(GameManager.Instance.WinnerId.Value);
                if (NetworkManager.Singleton.IsServer)
                {
                    restartButton.gameObject.SetActive(true);
                }
            }
            else
            {
                if (gameOverPanel.activeSelf) gameOverPanel.SetActive(false);
                restartButton.gameObject.SetActive(false);
            }
        }

        private void ShowGameOver(ulong winnerId)
        {
            if (!gameOverPanel.activeSelf)
            {
                gameOverPanel.SetActive(true);
                if (winnerText != null)
                {
                    if (winnerId == ulong.MaxValue)
                        winnerText.text = "GAME OVER\nNo Winner";
                    else
                        winnerText.text = $"GAME OVER\nWINNER: Player {winnerId}";
                }
            }

            // Allow restart button visibility update instantly if server
            if (NetworkManager.Singleton.IsServer && restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGameServerRpc();
            }
        }
    }
}
