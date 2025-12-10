using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class KartController : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float turnSpeed = 100f;

        private Rigidbody _rb;
        private Vector2 _moveInput;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            // Setup camera follow if needed, or input
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Simple Input System polling for prototype
            // Assuming "Move" action exists in default map or using legacy for simplicity if not set up
            // For now, let's use legacy Input for immediate prototype speed unless Input System is strictly required by user setup
            // User has InputSystem package, let's try to use it safely or fallback to legacy wrapper if possible.
            // But to be safe and "Hypercasual", let's use simple Key checks for now which InputSystem supports if enabled, 
            // or we can use the InputSystem API directly.

            float move = 0f;
            float turn = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move = -1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) turn = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) turn = 1f;
            }

            _moveInput = new Vector2(turn, move);
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            Move();
            Turn();
        }

        private void Move()
        {
            // Arcade movement: simple forward force
            if (_moveInput.y != 0)
            {
                Vector3 force = transform.forward * (_moveInput.y * speed);
                _rb.AddForce(force, ForceMode.Acceleration);
            }
        }

        private void Turn()
        {
            // Arcade turning: rotate only when moving or allow rotate in place? 
            // Mario Kart allows rotate in place usually or only when moving. 
            // Let's allow rotate always for easier control.
            if (_moveInput.x != 0)
            {
                float turn = _moveInput.x * turnSpeed * Time.fixedDeltaTime;
                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
                _rb.MoveRotation(_rb.rotation * turnRotation);
            }
        }

        public void ApplySpeedBoost(float multiplier, float duration)
        {
            StartCoroutine(SpeedBoostRoutine(multiplier, duration));
        }

        private System.Collections.IEnumerator SpeedBoostRoutine(float multiplier, float duration)
        {
            float originalSpeed = speed;
            speed *= multiplier;
            yield return new WaitForSeconds(duration);
            speed = originalSpeed;
        }
    }
}
