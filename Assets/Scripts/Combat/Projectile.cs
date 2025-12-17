using Assets.Scripts.Managers;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Combat
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
                // Reset timer when reused
                CancelInvoke(nameof(Despawn));
                Invoke(nameof(Despawn), lifeTime);
            }
        }

        private void Despawn()
        {
            if (NetworkObjectPool.Singleton != null)
            {
                NetworkObjectPool.Singleton.ReturnNetworkObject(NetworkObject);
            }
            else
            {
                // Fallback if pool missing
                Destroy(gameObject);
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

            if (other.TryGetComponent<Assets.Scripts.Interfaces.IDamageable>(out var damageable))
            {
                if (damageable.OwnerClientId != OwnerClientId)
                {
                    damageable.TakeDamage(damage);
                    Despawn();
                }
            }
            else
            {
                Despawn();
            }
        }
    }
}
