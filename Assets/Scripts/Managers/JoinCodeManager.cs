using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Manages the join code for the multiplayer session
    /// </summary>
    public class JoinCodeManager : NetworkBehaviour
    {
        public static JoinCodeManager Instance { get; private set; }

        // Allows setting the join code before the manager spawns (e.g., Relay-generated).
        public static string PreGeneratedJoinCode { get; set; }

        public NetworkVariable<FixedString32Bytes> JoinCode = new NetworkVariable<FixedString32Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (!string.IsNullOrWhiteSpace(PreGeneratedJoinCode))
                {
                    JoinCode.Value = PreGeneratedJoinCode;
#if UNITY_EDITOR
                    Debug.Log($"[JoinCodeManager] Using pre-generated join code: {PreGeneratedJoinCode}");
#endif
                    PreGeneratedJoinCode = null;
                }
                else
                {
                    GenerateJoinCode();
                }
            }
        }

        /// <summary>
        /// Generates a new random join code (Server only)
        /// </summary>
        private void GenerateJoinCode()
        {
            if (!IsServer) return;

            string code = JoinCodeValidator.GenerateRandomCode();
            JoinCode.Value = code;

#if UNITY_EDITOR
            Debug.Log($"[JoinCodeManager] Generated join code: {code}");
#endif
        }

        /// <summary>
        /// Validates if the provided code matches the current join code
        /// </summary>
        public bool ValidateJoinCode(string code)
        {
            if (!JoinCodeValidator.IsValidFormat(code))
                return false;

            return code.ToUpper() == JoinCode.Value.ToString().ToUpper();
        }

        /// <summary>
        /// Gets the current join code as a string
        /// </summary>
        public string GetJoinCodeString()
        {
            return JoinCode.Value.ToString();
        }
    }
}
