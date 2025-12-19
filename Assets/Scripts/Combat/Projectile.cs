using Assets.Scripts.Managers;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Assets.Scripts.Combat
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))] // Ensure movement is synced to clients
    public class Projectile : NetworkBehaviour
    {
        [SerializeField] private float speed = 30f;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private int damage = 1;

        private bool _hasHit = false; // Guard against double-trigger collision
        private ulong _validOwner = ulong.MaxValue; // Track owner to prevent friendly fire

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Verify ownership is properly set before allowing damage
                if (OwnerClientId == ulong.MaxValue || NetworkManager.Singleton.LocalClientId == OwnerClientId)
                {
                    _validOwner = OwnerClientId;
                }
                else
                {
                    _validOwner = OwnerClientId;
                }

                // Reset timer when reused
                CancelInvoke(nameof(Despawn));
                Invoke(nameof(Despawn), lifeTime);
#if UNITY_EDITOR
                Debug.Log($"[Projectile] Spawned with owner {_validOwner}");
#endif
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                CancelInvoke(nameof(Despawn));
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
            if (!IsServer || _hasHit) return; // Only hit once per projectile

            if (other.TryGetComponent<Assets.Scripts.Interfaces.IDamageable>(out var damageable))
            {
                // Ensure we're not hitting the shooter (owner client)
                if (damageable.OwnerClientId != _validOwner)
                {
                    _hasHit = true; // Mark as hit before damage to prevent multiple triggers
                    damageable.TakeDamage(damage);
#if UNITY_EDITOR
                    Debug.Log($"[Projectile] Hit damageable owned by {damageable.OwnerClientId}, damage {damage}");
#endif
                    Despawn();
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[Projectile] Attempted friendly fire on owner {_validOwner}, ignoring");
#endif
                }
            }
            else
            {
                // Hit non-damageable object
                _hasHit = true;
                Despawn();
            }
        }
    }
}
