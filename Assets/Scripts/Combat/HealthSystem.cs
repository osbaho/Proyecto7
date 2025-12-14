using Interfaces;
using Managers;
using Unity.Netcode;
using UnityEngine;

namespace Combat
{
    public class HealthSystem : NetworkBehaviour, IDamageable
    {
        [SerializeField] private int maxLives = 3;

        public NetworkVariable<int> CurrentLives = new();

        public event System.Action<int> OnLivesChangedLocal;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CurrentLives.Value = maxLives;
                if (GameManager.Instance != null) GameManager.Instance.RegisterPlayer(OwnerClientId);
            }

            CurrentLives.OnValueChanged += OnLivesChanged;

            // Initial update for local player event
            if (IsOwner) OnLivesChangedLocal?.Invoke(CurrentLives.Value);
        }

        public override void OnNetworkDespawn()
        {
            CurrentLives.OnValueChanged -= OnLivesChanged;
        }

        private void OnLivesChanged(int previousValue, int newValue)
        {
            Debug.Log($"Player {OwnerClientId} lives changed: {previousValue} -> {newValue}");

            if (IsOwner)
            {
                OnLivesChangedLocal?.Invoke(newValue);
            }

            if (newValue <= 0)
            {
                Debug.Log($"Player {OwnerClientId} Eliminated!");
                if (IsServer)
                {
                    if (GameManager.Instance != null) GameManager.Instance.OnPlayerEliminated(OwnerClientId);
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void TakeDamageServerRpc(int damage = 1)
        {
            if (CurrentLives.Value > 0)
            {
                CurrentLives.Value -= damage;
            }
        }

        // IDamageable implementation
        public void TakeDamage(int amount)
        {
            TakeDamageServerRpc(amount);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void AddLivesServerRpc(int amount)
        {
            CurrentLives.Value += amount;
        }
    }
}
