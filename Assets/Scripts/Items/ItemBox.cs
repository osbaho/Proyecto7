using System.Collections;
using Combat;
using Unity.Netcode;
using UnityEngine;

namespace Items
{
    public class ItemBox : NetworkBehaviour
    {
        [SerializeField] private float respawnTime = 3f;
        [SerializeField] private GameObject visualModel;
        [SerializeField] private Collider boxCollider;

        private NetworkVariable<bool> _isActive = new NetworkVariable<bool>(true);

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

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !_isActive.Value) return;

            if (other.TryGetComponent<KartItemSystem>(out var itemSystem))
            {
                if (itemSystem.CanPickupItem())
                {
                    // Pick random item (excluding None)
                    ItemType randomItem = (ItemType)Random.Range(1, System.Enum.GetValues(typeof(ItemType)).Length);

                    itemSystem.EquipItemServerRpc(randomItem);

                    // Disable box and start respawn timer
                    _isActive.Value = false;
                    StartCoroutine(RespawnRoutine());
                }
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnTime);
            _isActive.Value = true;
        }
    }
}
