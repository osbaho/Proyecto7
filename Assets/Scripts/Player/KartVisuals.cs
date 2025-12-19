using Assets.Scripts.Combat;
using DG.Tweening;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Handles visual feedback for the Kart (Particles, Camera Shake, FOV).
    /// Listens to events from KartItemSystem.
    /// </summary>
    [RequireComponent(typeof(KartItemSystem))]
    public class KartVisuals : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float boostFovAmount = 90f;
        [SerializeField] private float fovTransitionDuration = 0.4f;

        private KartItemSystem _itemSystem;
        private CinemachineCamera _cam;

        private void Awake()
        {
            _itemSystem = GetComponent<KartItemSystem>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // Find local camera
                // Note: Logic similar to ClientCameraHandler, but we might want to wait or use the one assigned there.
                // For simplicity, we search on spawn, but ideally ClientCameraHandler assigns it to us or we find it dynamically.
                // We'll search dynamically when effect triggers if null, or try to cache it here.
                _cam = FindFirstObjectByType<CinemachineCamera>();

                _itemSystem.OnBoostActivated += TriggerBoostVisuals;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _itemSystem.OnBoostActivated -= TriggerBoostVisuals;
            }
        }

        private void TriggerBoostVisuals(float duration)
        {
            if (_cam == null) _cam = FindFirstObjectByType<CinemachineCamera>();
            if (_cam == null) return;

            // Visual Juice (FOV Kick)
            float startFOV = _cam.Lens.FieldOfView;
            float targetFOV = boostFovAmount; // Turbo vision!

            // Use DOTween Sequence
            DOTween.Sequence()
                .Append(DOVirtual.Float(startFOV, targetFOV, fovTransitionDuration, v =>
                {
                    var lens = _cam.Lens;
                    lens.FieldOfView = v;
                    _cam.Lens = lens;
                }).SetEase(Ease.OutQuad))
                .AppendInterval(duration - (fovTransitionDuration * 2)) // Hold the effect
                .Append(DOVirtual.Float(targetFOV, startFOV, fovTransitionDuration, v =>
                {
                    var lens = _cam.Lens;
                    lens.FieldOfView = v;
                    _cam.Lens = lens;
                }).SetEase(Ease.InQuad));
        }
    }
}
