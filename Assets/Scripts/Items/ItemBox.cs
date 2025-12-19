using Assets.Scripts.Combat;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Items
{
    public class ItemBox : NetworkBehaviour
    {
        [SerializeField] private float respawnTime = 3f;
        [SerializeField] private GameObject visualModel;
        [SerializeField] private Collider boxCollider;

        [Header("Item Drops")]
        [SerializeField]
        private ItemDrop[] itemDrops = new ItemDrop[]
        {
            new() { itemType = ItemType.Mushroom, weight = 50f },
            new() { itemType = ItemType.GreenShell, weight = 50f }
        };

        private readonly NetworkVariable<bool> _isActive = new(true);
        private float _respawnTimer;


        public override void OnNetworkSpawn()
        {
            _isActive.OnValueChanged += OnActiveStateChanged;
            UpdateVisuals(_isActive.Value);
        }

        public override void OnNetworkDespawn()
        {
            _isActive.OnValueChanged -= OnActiveStateChanged;
        }

        private void OnActiveStateChanged(bool previousValue, bool newValue)
        {
            UpdateVisuals(newValue);
        }

        private void UpdateVisuals(bool active)
        {
            if (visualModel != null) visualModel.SetActive(active);
            if (boxCollider != null) boxCollider.enabled = active;
        }

        private bool _inCollisionCheck = false; // Guard against simultaneous triggers
        private ulong _lastPickupClient = ulong.MaxValue; // Track last pickup to prevent duplicates

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !_isActive.Value || _inCollisionCheck) return; // Server authoritative pickup + guard duplicate triggers

            _inCollisionCheck = true;

            if (other.TryGetComponent<KartItemSystem>(out var itemSystem))
            {
                // Prevent same player from picking up twice in same frame
                if (itemSystem.OwnerClientId == _lastPickupClient)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[ItemBox] Duplicate pickup attempt by {itemSystem.OwnerClientId}");
#endif
                    _inCollisionCheck = false;
                    return;
                }

                if (itemSystem.CanPickupItem())
                {
                    // Select weighted random item
                    ItemType randomItem = SelectWeightedItem();

                    if (randomItem != ItemType.None)
                    {
                        // Server gives the item directly to avoid double-trigger RPCs
                        itemSystem.EquipItemServer(randomItem);
                        _lastPickupClient = itemSystem.OwnerClientId;

                        // Disable box and start respawn timer
                        _isActive.Value = false;
                        _respawnTimer = 0f;
#if UNITY_EDITOR
                        Debug.Log($"[ItemBox] Pickup granted: {randomItem} to {itemSystem.OwnerClientId}");
#endif
                    }
                }
            }

            _inCollisionCheck = false;
        }

        private ItemType SelectWeightedItem()
        {
            if (itemDrops == null || itemDrops.Length == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[ItemBox] No item drops configured!");
#endif
                return ItemType.None;
            }

            // Calculate total weight
            float totalWeight = 0f;
            foreach (var drop in itemDrops)
            {
                if (drop.weight > 0f)
                    totalWeight += drop.weight;
            }

            if (totalWeight <= 0f)
                return ItemType.None;

            // Select random value in range [0, totalWeight)
            float randomValue = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            // Find which item the random value falls into
            foreach (var drop in itemDrops)
            {
                if (drop.weight <= 0f) continue;

                cumulative += drop.weight;
                if (randomValue < cumulative)
                {
#if UNITY_EDITOR
                    Debug.Log($"[ItemBox] Selected {drop.itemType} (weight: {drop.weight}/{totalWeight})");
#endif
                    return drop.itemType;
                }
            }

            // Fallback (shouldn't reach here)
            return itemDrops[0].itemType;
        }

        private void Update()
        {
            if (!IsServer || _isActive.Value) return;

            _respawnTimer += Time.deltaTime;
            if (_respawnTimer >= respawnTime)
            {
                _isActive.Value = true;
                _respawnTimer = 0f;
                _lastPickupClient = ulong.MaxValue; // Reset on respawn
#if UNITY_EDITOR
                Debug.Log("[ItemBox] Respawned and reset pickup tracking");
#endif
            }
        }
    }
}
