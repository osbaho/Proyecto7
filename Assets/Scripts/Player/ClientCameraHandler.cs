using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Handles finding the local scene camera and assigning it to follow this player.
    /// Should be placed on the Player Prefab.
    /// </summary>
    public class ClientCameraHandler : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                StartCoroutine(WaitForCamera());
            }
        }

        private System.Collections.IEnumerator WaitForCamera()
        {
            // Cinemachine 3.x uses Unity.Cinemachine namespace
            // and CinemachineCamera component instead of VirtualCamera
            var cam = FindFirstObjectByType<CinemachineCamera>();

            // Retry for extended time with exponential backoff to ensure camera load
            int retries = 20; // 20 * 0.5s = 10s total timeout (increased from 5s)
            var wait = new WaitForSeconds(0.5f);
            while (cam == null && retries > 0)
            {
                yield return wait;
                retries--;
                cam = FindFirstObjectByType<CinemachineCamera>();
            }

            if (cam != null)
            {
                Debug.Log($"[ClientCameraHandler] Assigning camera to {name}");
                cam.Follow = transform;
                cam.LookAt = transform;
            }
            else
            {
                Debug.LogWarning("[ClientCameraHandler] No CinemachineCamera found in scene after extended timeout!");
                // Continue anyway - camera may be assigned later or player can still play
            }
        }
    }
}
