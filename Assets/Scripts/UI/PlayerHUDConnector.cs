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
                var hud = FindFirstObjectByType<BattleHUD>();
                if (hud != null)
                {
                    hud.RegisterPlayer(healthSystem, itemSystem);
                }
                else
                {
                    Debug.LogWarning("BattleHUD not found in scene!");
                }
            }
        }
    }
}
