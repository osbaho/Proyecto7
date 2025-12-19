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
            if (_strategies.TryGetValue(item, out var strategy))
            {
                strategy.Use(this);
            }

            // Consume item
            CurrentItem.Value = ItemType.None;
        }
    }
}
