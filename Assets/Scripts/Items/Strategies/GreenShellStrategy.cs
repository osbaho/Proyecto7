using Combat;
using Unity.Netcode;
using UnityEngine;

namespace Items.Strategies
{
    public class GreenShellStrategy : IItemStrategy
    {
        public void Use(KartItemSystem user)
        {
            if (user.GreenShellPrefab == null || user.FirePoint == null) return;

            GameObject projectile = Object.Instantiate(user.GreenShellPrefab, user.FirePoint.position, user.FirePoint.rotation);
            var netObj = projectile.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.SpawnWithOwnership(user.OwnerClientId);
            }
        }
    }
}
