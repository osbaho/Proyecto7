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

            // Retry for a few seconds if not found immediately (e.g. scene loading)
            float timeout = 5f;
            var wait = new WaitForSeconds(0.5f);
            while (cam == null && timeout > 0)
            {
                yield return wait;
                timeout -= 0.5f;
                cam = FindFirstObjectByType<CinemachineCamera>();
            }

            if (cam != null)
            {
                Debug.Log($"[ClientCameraHandler] Assigning camera to {name}");
                cam.Follow = transform;
                cam.LookAt = transform;

                // Ensure Rotation Control is set to Hard Lock To Target if not already
                // Or inform user if they forgot (we already did in walkthrough)
            }
            else
            {
                Debug.LogWarning("[ClientCameraHandler] No CinemachineCamera found in scene after waiting!");
            }
        }
    }
}
