using Assets.Scripts.Interfaces;
using Assets.Scripts.Managers;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Combat
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
                // Decoupled registration
                Assets.Scripts.Events.PlayerEvents.InvokePlayerJoined(OwnerClientId);
            }

            CurrentLives.OnValueChanged += OnLivesChanged;

            // Initial update for local player event
            if (IsOwner) OnLivesChangedLocal?.Invoke(CurrentLives.Value);
        }

        public override void OnNetworkDespawn()
        {
            CurrentLives.OnValueChanged -= OnLivesChanged;
        }

        private bool _hasBeenEliminated = false; // Guard against duplicate elimination events

        private void OnLivesChanged(int previousValue, int newValue)
        {
            Debug.Log($"Player {OwnerClientId} lives changed: {previousValue} -> {newValue}");

            if (IsOwner)
            {
                OnLivesChangedLocal?.Invoke(newValue);
            }

            // Only trigger elimination event once per player
            if (newValue <= 0 && !_hasBeenEliminated)
            {
                _hasBeenEliminated = true;
                Debug.Log($"Player {OwnerClientId} Eliminated!");
                if (IsServer)
                {
                    // Decoupled elimination (will only fire once due to guard above)
                    Assets.Scripts.Events.PlayerEvents.InvokePlayerEliminated(OwnerClientId);
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
            public void TakeDamageServerRpc(int damage = 1)
        {
            // Guard against already eliminated player taking more damage
            if (_hasBeenEliminated)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[HealthSystem] Player {OwnerClientId} already eliminated, ignoring damage");
#endif
                return;
            }

            // Server-side validation: sanity check damage value
            if (damage < 0 || damage > 100)
            {
                Debug.LogWarning($"[HealthSystem] Invalid damage {damage} to Player {OwnerClientId}");
                return;
            }

            if (CurrentLives.Value > 0)
            {
                int newLives = Mathf.Max(0, CurrentLives.Value - damage); // Prevent negative
                CurrentLives.Value = newLives;
#if UNITY_EDITOR
                Debug.Log($"[HealthSystem] Player {OwnerClientId} took {damage} damage. Lives: {newLives}");
#endif
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
