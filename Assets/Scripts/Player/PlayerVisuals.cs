using System.Collections.Generic;
using Combat;
using Unity.Netcode;
using UnityEngine;

namespace Player
{
    public class PlayerVisuals : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthSystem healthSystem;

        [Header("Visual Settings")]
        [SerializeField] private GameObject balloonPrefab;
        [SerializeField] private Transform balloonContainer;
        [SerializeField] private float balloonSpacing = 0.5f;

        [SerializeField]
        private List<Color> playerColors = new()
        {
            Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan
        };

        private readonly List<GameObject> _spawnedBalloons = new();

        public override void OnNetworkSpawn()
        {
            if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();

            if (healthSystem != null)
            {
                // Subscribe to network variable changes
                healthSystem.CurrentLives.OnValueChanged += OnLivesChanged;

                // Initial update
                UpdateVisuals(healthSystem.CurrentLives.Value);

                // Assign color based on OwnerClientId
                AssignBalloonColor();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (healthSystem != null)
            {
                healthSystem.CurrentLives.OnValueChanged -= OnLivesChanged;
            }
            ClearBalloons();
        }

        private void OnLivesChanged(int previousValue, int newValue)
        {
            UpdateVisuals(newValue);
        }

        private void UpdateVisuals(int currentLives)
        {
            if (balloonPrefab == null || balloonContainer == null) return;

            // 1. Adjust count
            int diff = currentLives - _spawnedBalloons.Count;

            if (diff > 0)
            {
                // Instantiate new balloons
                for (int i = 0; i < diff; i++)
                {
                    GameObject newBalloon = Instantiate(balloonPrefab, balloonContainer);
                    ApplyColorToBalloon(newBalloon);
                    _spawnedBalloons.Add(newBalloon);
                }
            }
            else if (diff < 0)
            {
                // Remove balloons (from the end)
                int removeCount = Mathf.Abs(diff);
                for (int i = 0; i < removeCount; i++)
                {
                    int lastIndex = _spawnedBalloons.Count - 1;
                    if (lastIndex >= 0)
                    {
                        GameObject obj = _spawnedBalloons[lastIndex];
                        _spawnedBalloons.RemoveAt(lastIndex);
                        Destroy(obj);
                    }
                }
            }

            // 2. Reposition balloons
            RepositionBalloons();
        }

        private void RepositionBalloons()
        {
            if (_spawnedBalloons.Count == 0) return;

            // Center them
            float totalWidth = (_spawnedBalloons.Count - 1) * balloonSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < _spawnedBalloons.Count; i++)
            {
                if (_spawnedBalloons[i] != null)
                {
                    _spawnedBalloons[i].transform.SetLocalPositionAndRotation(
                        new Vector3(startX + (i * balloonSpacing), 0, 0),
                        Quaternion.identity);
                }
            }
        }

        private void ClearBalloons()
        {
            foreach (var balloon in _spawnedBalloons)
            {
                if (balloon != null) Destroy(balloon);
            }
            _spawnedBalloons.Clear();
        }

        private void AssignBalloonColor()
        {
            // Re-apply to all existing balloons (in case we spawn late or disconnected)
            foreach (var balloon in _spawnedBalloons)
            {
                ApplyColorToBalloon(balloon);
            }
        }

        private void ApplyColorToBalloon(GameObject balloon)
        {
            if (playerColors.Count == 0 || balloon == null) return;

            int colorIndex = (int)(OwnerClientId % (ulong)playerColors.Count);
            Color playerColor = playerColors[colorIndex];

            if (balloon.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = playerColor;
            }
        }
    }
}
