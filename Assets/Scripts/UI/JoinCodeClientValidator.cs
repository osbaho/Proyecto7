using Assets.Scripts.Managers;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Validates the join code after client connects to a lobby
    /// </summary>
    public class JoinCodeClientValidator : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (!IsOwner || IsServer)
                return;

            // Wait for JoinCodeManager to be available
            StartCoroutine(ValidateJoinCodeAfterConnection());
        }

        private System.Collections.IEnumerator ValidateJoinCodeAfterConnection()
        {
            // Wait for JoinCodeManager to spawn and sync with extended timeout
            int retries = 20; // 20 * 0.5s = 10s timeout
            var wait = new WaitForSeconds(0.5f);
            while (JoinCodeManager.Instance == null && retries > 0)
            {
                yield return wait;
                retries--;
            }

            if (JoinCodeManager.Instance == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[JoinCodeClientValidator] JoinCodeManager not found after timeout!");
#endif
                DisconnectClient("Failed to find server join code manager");
                yield break;
            }

            // Wait a bit more for the NetworkVariable to sync
            yield return new WaitForSeconds(0.5f);

            // Retrieve the pending join code
            string pendingCode = PlayerPrefs.GetString("PendingJoinCode", "");
            PlayerPrefs.DeleteKey("PendingJoinCode");

            if (string.IsNullOrEmpty(pendingCode))
            {
#if UNITY_EDITOR
                Debug.LogError("[JoinCodeClientValidator] No pending join code found!");
#endif
                DisconnectClient("No join code provided");
                yield break;
            }

            // Validate the code
            if (!JoinCodeManager.Instance.ValidateJoinCode(pendingCode))
            {
#if UNITY_EDITOR
                Debug.LogError($"[JoinCodeClientValidator] Invalid join code: {pendingCode}");
#endif
                DisconnectClient("Invalid join code");
                yield break;
            }

#if UNITY_EDITOR
            Debug.Log($"[JoinCodeClientValidator] Join code validated successfully: {pendingCode}");
#endif
        }

        private void DisconnectClient(string reason)
        {
#if UNITY_EDITOR
            Debug.LogError($"[JoinCodeClientValidator] Disconnecting: {reason}");
#endif
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // Return to main menu
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
