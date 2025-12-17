using Assets.Scripts.Combat;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Items.Strategies
{
    public class GreenShellStrategy : IItemStrategy
    {
        public void Use(KartItemSystem user)
        {
            if (user.GreenShellPrefab == null || user.FirePoint == null) return;

            // Use NetworkObjectPool
            if (Assets.Scripts.Managers.NetworkObjectPool.Singleton != null)
            {
                NetworkObject netObj = Assets.Scripts.Managers.NetworkObjectPool.Singleton.GetNetworkObject(
                    user.GreenShellPrefab,
                    user.FirePoint.position,
                    user.FirePoint.rotation);

                if (netObj != null)
                {
                    // Ownership change if needed, though Spawning typically sets it to Server
                    // If we want the player to own it (for latency compensation logic on client), we change it
                    if (netObj.OwnerClientId != user.OwnerClientId)
                    {
                        netObj.ChangeOwnership(user.OwnerClientId);
                    }
                }
            }
            else
            {
                // Fallback (or Error)
                GameObject projectile = Object.Instantiate(user.GreenShellPrefab, user.FirePoint.position, user.FirePoint.rotation);
                var netObj = projectile.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.SpawnWithOwnership(user.OwnerClientId);
                }
            }
        }
    }
}
