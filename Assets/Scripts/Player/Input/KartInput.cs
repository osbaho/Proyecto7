using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Player.Input
{
    public class KartInput : NetworkBehaviour
    {
        private InputSystem_Actions _actions;

        public Vector2 MoveInput { get; private set; }
        public bool IsFiring { get; private set; }

        private void Awake()
        {
            _actions = new InputSystem_Actions();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _actions.Player.Enable();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _actions.Player.Disable();
            }
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
        }

        private void Update()
        {
            if (!IsOwner) return;

            MoveInput = _actions.Player.Move.ReadValue<Vector2>();
            IsFiring = _actions.Player.Attack.WasPressedThisFrame();
        }
    }
}
