using Items;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat
{
    public class KartItemSystem : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Player.KartController kartController;
        [SerializeField] private Transform firePoint;

        [Header("Item Prefabs")]
        [SerializeField] private GameObject greenShellPrefab;

        [Header("Settings")]
        [SerializeField] private float boostMultiplier = 2f;
        [SerializeField] private float boostDuration = 2f;

        public NetworkVariable<ItemType> CurrentItem = new NetworkVariable<ItemType>(ItemType.None);
        public event System.Action<ItemType> OnItemChanged;

        public override void OnNetworkSpawn()
        {
            CurrentItem.OnValueChanged += OnCurrentItemChanged;
            if (IsOwner) OnItemChanged?.Invoke(CurrentItem.Value);
        }

        public override void OnNetworkDespawn()
        {
            CurrentItem.OnValueChanged -= OnCurrentItemChanged;
        }

        private void OnCurrentItemChanged(ItemType previousValue, ItemType newValue)
        {
            if (IsOwner)
            {
                OnItemChanged?.Invoke(newValue);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                UseItem();
            }
        }

        public bool CanPickupItem()
        {
            return CurrentItem.Value == ItemType.None;
        }

        [ServerRpc]
        public void EquipItemServerRpc(ItemType item)
        {
            CurrentItem.Value = item;
        }

        private void UseItem()
        {
            if (CurrentItem.Value == ItemType.None) return;

            UseItemServerRpc(CurrentItem.Value);
        }

        [ServerRpc]
        private void UseItemServerRpc(ItemType item)
        {
            switch (item)
            {
                case ItemType.GreenShell:
                    FireGreenShell();
                    break;
                case ItemType.Mushroom:
                    ActivateBoostClientRpc();
                    break;
            }

            // Consume item
            CurrentItem.Value = ItemType.None;
        }

        private void FireGreenShell()
        {
            if (greenShellPrefab == null) return;

            GameObject projectile = Instantiate(greenShellPrefab, firePoint.position, firePoint.rotation);
            var netObj = projectile.GetComponent<NetworkObject>();
            netObj.SpawnWithOwnership(OwnerClientId);
        }

        [ClientRpc]
        private void ActivateBoostClientRpc()
        {
            if (kartController != null)
            {
                kartController.ApplySpeedBoost(boostMultiplier, boostDuration);
            }
        }
    }
}
