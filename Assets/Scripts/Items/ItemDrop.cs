using UnityEngine;

namespace Assets.Scripts.Items
{
    /// <summary>
    /// Defines an item drop with weighted probability
    /// </summary>
    [System.Serializable]
    public class ItemDrop
    {
        [Tooltip("Type of item to drop")]
        public ItemType itemType;

        [Tooltip("Relative weight/probability (higher = more common)")]
        [Range(0f, 100f)]
        public float weight = 10f;
    }
}
