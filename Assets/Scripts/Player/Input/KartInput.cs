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

        public override void OnDestroy()
        {
            _actions?.Dispose();
            base.OnDestroy();
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            // Sample input in FixedUpdate to match physics timestep and reduce stutter
            MoveInput = _actions.Player.Move.ReadValue<Vector2>();
            IsFiring = _actions.Player.Attack.IsPressed();
#if UNITY_EDITOR
            if (MoveInput != Vector2.zero)
            {
                Debug.Log($"[KartInput] FixedUpdate - MoveInput: {MoveInput}, IsFiring: {IsFiring}");
            }
#endif
        }
    }
}
