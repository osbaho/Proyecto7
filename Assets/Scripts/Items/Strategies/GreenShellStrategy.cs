using Assets.Scripts.Combat;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Items.Strategies
{
    public class GreenShellStrategy : IItemStrategy
    {
        public void Use(KartItemSystem user)
        {
            // Server-only execution to prevent client-side projectile spawning
            if (!user.IsServer)
            {
                Debug.LogWarning("[GreenShellStrategy] Use called on client! This should only execute on server.");
                return;
            }

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
                    // Spawn with immediate ownership to avoid race condition
                    if (netObj.OwnerClientId != user.OwnerClientId)
                    {
                        netObj.ChangeOwnership(user.OwnerClientId);
#if UNITY_EDITOR
                        Debug.Log($"[GreenShellStrategy] Projectile {netObj.NetworkObjectId} spawned and ownership set to {user.OwnerClientId}");
#endif
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
#if UNITY_EDITOR
                    Debug.Log($"[GreenShellStrategy] Projectile fallback spawned with ownership {user.OwnerClientId}");
#endif
                }
            }
        }
    }
}
