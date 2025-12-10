using Unity.Netcode;
using UnityEngine;

namespace Combat
{
    public class HealthSystem : NetworkBehaviour
    {
        [SerializeField] private int maxLives = 3;

        public NetworkVariable<int> CurrentLives = new NetworkVariable<int>();

        public event System.Action<int> OnLivesChangedLocal;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CurrentLives.Value = maxLives;
            }

            CurrentLives.OnValueChanged += OnLivesChanged;
            // Initial update for local player
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
                // Handle elimination (disable controls, show game over, etc.)
                if (IsServer)
                {
                    // Optional: Despawn or Respawn logic
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
    }
}
