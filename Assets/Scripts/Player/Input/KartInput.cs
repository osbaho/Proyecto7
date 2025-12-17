using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Player.Input
{
    public class KartInput : NetworkBehaviour
    {
        [Header("Input Actions")]
        [Tooltip("Assign 'Player/Move' action here")]
        [SerializeField] private InputActionReference moveAction;

        [Tooltip("Assign 'Player/Attack' or 'Player/Jump' (Space) action here")]
        [SerializeField] private InputActionReference fireAction;

        public Vector2 MoveInput { get; private set; }
        public bool IsFiring { get; private set; }

        public override void OnNetworkSpawn()
        {
            // Ensure inputs are enabled for the owner
            if (IsOwner)
            {
                if (moveAction != null) moveAction.action.Enable();
                if (fireAction != null) fireAction.action.Enable();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                if (moveAction != null) moveAction.action.Disable();
                if (fireAction != null) fireAction.action.Disable();
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (moveAction != null)
            {
                MoveInput = moveAction.action.ReadValue<Vector2>();
            }

            if (fireAction != null)
            {
                IsFiring = fireAction.action.WasPressedThisFrame();
            }
        }
    }
}
