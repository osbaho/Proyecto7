using Assets.Scripts.Player.Input;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class KartController : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float turnSpeed = 100f;

        private Rigidbody _rb;
        private KartInput _input;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            _input = GetComponent<KartInput>();
        }

        public override void OnNetworkDespawn()
        {
            // Cleanup references
            _input = null;
            _boostTimer = 0f;
        }

        private float _boostTimer = 0f;
        private float _baseSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _baseSpeed = speed;
        }

        private void FixedUpdate()
        {
            // Handle Boost Timer
            if (_boostTimer > 0)
            {
                _boostTimer -= Time.fixedDeltaTime;
                if (_boostTimer <= 0)
                {
                    speed = _baseSpeed; // Reset speed
                }
            }

            if (!IsOwner)
            {
#if UNITY_EDITOR
                if (Time.frameCount % 300 == 0) Debug.Log($"[KartController] Not owner: IsOwner={IsOwner}, IsClient={IsClient}, IsServer={IsServer}");
#endif
                return;
            }

            if (_input == null)
            {
#if UNITY_EDITOR
                if (Time.frameCount % 300 == 0) Debug.LogWarning($"[KartController] Input is null for owner {OwnerClientId}");
#endif
                return;
            }

            Vector2 moveInput = _input.MoveInput;
#if UNITY_EDITOR
            if (moveInput != Vector2.zero && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[KartController] Moving with input: {moveInput}");
            }
#endif
            Move(moveInput);
            Turn(moveInput);
        }

        private void Move(Vector2 input)
        {
            // Arcade movement: simple forward force
            if (input.y != 0)
            {
                Vector3 force = transform.forward * (input.y * speed);
                _rb.AddForce(force, ForceMode.Acceleration);
            }
        }

        private void Turn(Vector2 input)
        {
            if (input.x != 0)
            {
                float turn = input.x * turnSpeed * Time.fixedDeltaTime;
                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
                _rb.MoveRotation(_rb.rotation * turnRotation);
            }
        }

        public void ApplySpeedBoost(float multiplier, float duration)
        {
            if (_boostTimer <= 0) _baseSpeed = speed; // Capture base speed if not already boosting
            speed = _baseSpeed * multiplier;
            _boostTimer = duration;
        }
    }
}
