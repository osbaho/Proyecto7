using Unity.Netcode;
using UnityEngine;

namespace Combat
{
    public class Projectile : NetworkBehaviour
    {
        [SerializeField] private float speed = 30f;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private int damage = 1;

        private void Start()
        {
            if (IsServer)
            {
                Destroy(gameObject, lifeTime);
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                transform.Translate(Vector3.forward * speed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            // Prevent hitting the owner if possible (needs owner setup on spawn)
            // For simple prototype, just check tag or component

            if (other.TryGetComponent<HealthSystem>(out var health))
            {
                // Don't damage self if we are the owner (needs owner check logic passed to projectile)
                // For now, let's assume projectile is spawned slightly ahead so it doesn't hit self immediately,
                // or we assign an "OwnerID" to projectile.

                if (health.OwnerClientId != OwnerClientId)
                {
                    health.TakeDamageServerRpc(damage);
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
