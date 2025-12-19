using System.Collections.Generic;
using Assets.Scripts.Items;
using Assets.Scripts.Items.Strategies;
using Assets.Scripts.Player.Input;
using DG.Tweening; // Required for Extension Methods like .SetEase
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Combat
{
    [RequireComponent(typeof(Player.KartController), typeof(Player.Input.KartInput), typeof(HealthSystem))]
    public class KartItemSystem : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Player.KartController kartController;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Player.Input.KartInput input; // Reference to input

        [Header("Item Prefabs")]
        [SerializeField] private GameObject greenShellPrefab;

        [Header("Settings")]
        [SerializeField] private float boostMultiplier = 2f;
        [SerializeField] private float boostDuration = 2f;

        public NetworkVariable<ItemType> CurrentItem = new(ItemType.None);
        public event System.Action<ItemType> OnItemChanged;
        public event System.Action<float> OnBoostActivated; // New Event for Visuals

        // Public properties for Strategies to access
        public Player.KartController KartController => kartController;
        public Transform FirePoint => firePoint;
        public GameObject GreenShellPrefab => greenShellPrefab;
        public float BoostMultiplier => boostMultiplier;
        public float BoostDuration => boostDuration;

        private Dictionary<ItemType, IItemStrategy> _strategies;
        private HealthSystem _healthSystem;

        private void Awake()
        {
            InitializeStrategies();
            _healthSystem = GetComponent<HealthSystem>();
            // Try get input if not assigned
            if (input == null) input = GetComponent<KartInput>();
        }

        private void InitializeStrategies()
        {
            _strategies = new Dictionary<ItemType, IItemStrategy>
            {
                { ItemType.GreenShell, new GreenShellStrategy() },
                { ItemType.Mushroom, new MushroomStrategy() }
            };
        }

        public override void OnNetworkSpawn()
        {
            CurrentItem.OnValueChanged += OnCurrentItemChanged;
            if (IsOwner)
            {
                OnItemChanged?.Invoke(CurrentItem.Value);
            }
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

            // Use the Input component to check firing
            if (input != null && input.IsFiring)
            {
                UseItem();
            }
        }

        // Made public for Strategy access
        [ClientRpc]
        public void ActivateBoostClientRpc()
        {
            // Logic Separation:
            // 1. Gameplay Effect (Speed)
            if (kartController != null)
            {
                kartController.ApplySpeedBoost(boostMultiplier, boostDuration);
            }

            // 2. Visual Effect (Event)
            if (IsOwner)
            {
                OnBoostActivated?.Invoke(boostDuration);
            }
        }

        public bool CanPickupItem()
        {
            return CurrentItem.Value == ItemType.None;
        }

        // Server-authoritative equip (called by ItemBox on the server)
        public void EquipItemServer(ItemType item)
        {
            if (!IsServer)
            {
                Debug.LogError("[KartItemSystem] EquipItemServer called on client!");
                return;
            }

            // Guard: ensure slot is actually empty and item is not None
            if (!CanPickupItem() || item == ItemType.None) 
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[KartItemSystem] Cannot equip {item}: slot not empty or item invalid");
#endif
                return;
            }

            CurrentItem.Value = item;
#if UNITY_EDITOR
            Debug.Log($"[KartItemSystem] Player {OwnerClientId} equipped {item}");
#endif
        }

        [ServerRpc]
        public void EquipItemServerRpc(ItemType item, ServerRpcParams rpcParams = default)
        {
            // Validate caller owns this object to prevent spoofing
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;

            if (!CanPickupItem()) return;

            CurrentItem.Value = item;
        }

        private ItemType _lastUsedItem = ItemType.None; // Guard against duplicate use

        private void UseItem()
        {
            if (CurrentItem.Value == ItemType.None) return;

            // Prevent using same item twice in same frame (network race)
            if (_lastUsedItem == CurrentItem.Value) return;
            
            _lastUsedItem = CurrentItem.Value;
            UseItemServerRpc(CurrentItem.Value);
        }

        [ServerRpc]
        private void UseItemServerRpc(ItemType item, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;

            // Validate item hasn't changed since client sent RPC (network race prevention)
            if (CurrentItem.Value != item)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[KartItemSystem] Item mismatch: RPC tried to use {item} but CurrentItem is {CurrentItem.Value}");
#endif
                return;
            }

            if (CurrentItem.Value == ItemType.None) return; // Already consumed

            if (_strategies.TryGetValue(item, out var strategy))
            {
                strategy.Use(this);
            }

            // Consume item
            CurrentItem.Value = ItemType.None;
            _lastUsedItem = ItemType.None; // Reset guard
        }
    }
}
