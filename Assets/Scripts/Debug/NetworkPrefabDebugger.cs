using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Diagnostics
{
    /// <summary>
    /// Temporary diagnostic script to log all registered NetworkPrefabs.
    /// Attach to NetworkManager GameObject to diagnose spawn issues.
    /// </summary>
    public class NetworkPrefabDebugger : MonoBehaviour
    {
        private void Start()
        {
            var networkManager = GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                UnityEngine.Debug.LogError("[NetworkPrefabDebugger] NetworkManager not found on this GameObject!");
                return;
            }

            if (networkManager.NetworkConfig == null || networkManager.NetworkConfig.Prefabs == null)
            {
                UnityEngine.Debug.LogError("[NetworkPrefabDebugger] NetworkConfig or Prefabs list is null!");
                return;
            }

            var prefabsList = networkManager.NetworkConfig.Prefabs;
            var prefabEntries = prefabsList.Prefabs;
            UnityEngine.Debug.Log($"[NetworkPrefabDebugger] ========== REGISTERED NETWORK PREFABS ({prefabEntries.Count}) ==========");
            
            for (int i = 0; i < prefabEntries.Count; i++)
            {
                var prefab = prefabEntries[i];
                if (prefab == null || prefab.Prefab == null)
                {
                    UnityEngine.Debug.LogWarning($"[NetworkPrefabDebugger] [{i}] NULL PREFAB");
                }
                else
                {
                    var netObj = prefab.Prefab.GetComponent<NetworkObject>();
                    UnityEngine.Debug.Log($"[NetworkPrefabDebugger] [{i}] '{prefab.Prefab.name}' - HasNetworkObject: {netObj != null}");
                }
            }
            
            UnityEngine.Debug.Log($"[NetworkPrefabDebugger] ========== END PREFAB LIST ==========");
        }
    }
}
