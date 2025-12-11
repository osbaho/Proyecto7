using Unity.Netcode;
using UnityEngine;

namespace Combat
{
    public class Projectile : NetworkBehaviour
    {
        [SerializeField] private float speed = 30f;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private int damage = 1;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                Destroy(gameObject, lifeTime);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            transform.Translate(Vector3.forward * (speed * Time.deltaTime));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            // Prevent hitting the owner if possible (needs owner setup on spawn)
            // For simple prototype, just check tag or component

            if (other.TryGetComponent<Interfaces.IDamageable>(out var damageable))
            {
                // Simple friendly fire check based on OwnerClientId if available on both
                // Validating owner requires IDamageable to expose it or casting to NetworkBehaviour

                // For now, let's assume we implement OwnerId on IDamageable or check simple equality
                if (damageable.OwnerClientId != OwnerClientId)
                {
                    damageable.TakeDamage(damage);
                    Destroy(gameObject);
                }
            }
            else
            {
                // Destroy on hitting walls etc.
                Destroy(gameObject);
            }
        }
    }
}
