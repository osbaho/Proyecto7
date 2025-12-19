using Assets.Scripts.Combat;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class PlayerHUDConnector : NetworkBehaviour
    {
        [SerializeField] private HealthSystem healthSystem;
        [SerializeField] private KartItemSystem itemSystem;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // Ensure references are present even if not wired in inspector
                if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();
                if (itemSystem == null) itemSystem = GetComponent<KartItemSystem>();

                if (healthSystem == null || itemSystem == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[PlayerHUDConnector] Missing refs before HUD hookup: HealthSystem={healthSystem != null}, ItemSystem={itemSystem != null}");
#endif
                }
                StartCoroutine(WaitForHUD());
            }
        }

        private System.Collections.IEnumerator WaitForHUD()
        {
            // Optimization: Cache HUD singleton to avoid repeated FindFirstObjectByType
            var hud = FindFirstObjectByType<BattleHUD>();
            
            if (hud == null)
            {
                // Retry only if not found, with extended timeout
                int retries = 20; // 20 * 0.5s = 10s
                var wait = new WaitForSeconds(0.5f);
                while (hud == null && retries > 0)
                {
                    yield return wait;
                    retries--;
                    // Try active objects first
                    hud = FindFirstObjectByType<BattleHUD>();
                    // Fallback: include inactive in search
                    if (hud == null)
                    {
                        var all = FindObjectsByType<BattleHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        if (all != null && all.Length > 0)
                        {
                            hud = all[0];
                        }
                    }
                }
            }

            if (hud != null)
            {
                if (healthSystem != null && itemSystem != null)
                {
                    hud.RegisterPlayer(healthSystem, itemSystem);
#if UNITY_EDITOR
                    Debug.Log($"[PlayerHUDConnector] Registered player {OwnerClientId} with HUD");
#endif
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[PlayerHUDConnector] Cannot register with HUD: missing HealthSystem or KartItemSystem reference.");
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("[PlayerHUDConnector] BattleHUD not found in scene after timeout!");
#endif
            }
        }
    }
}
