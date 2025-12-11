using Combat;
using Unity.Netcode;
using UnityEngine;

namespace GameUI
{
    public class PlayerHUDConnector : NetworkBehaviour
    {
        [SerializeField] private HealthSystem healthSystem;
        [SerializeField] private KartItemSystem itemSystem;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                StartCoroutine(WaitForHUD());
            }
        }

        private System.Collections.IEnumerator WaitForHUD()
        {
            var hud = FindFirstObjectByType<BattleHUD>();
            
            float timeout = 5f;
            while (hud == null && timeout > 0)
            {
                yield return new WaitForSeconds(0.5f);
                timeout -= 0.5f;
                hud = FindFirstObjectByType<BattleHUD>();
            }

            if (hud != null)
            {
                hud.RegisterPlayer(healthSystem, itemSystem);
            }
            else
            {
                Debug.LogWarning("BattleHUD not found in scene after waiting!");
            }
        }
    }
}
