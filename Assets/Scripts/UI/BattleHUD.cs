using Assets.Scripts.Combat;
using Assets.Scripts.Items;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class BattleHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image itemImage;
        [SerializeField] private Sprite greenShellSprite;
        [SerializeField] private Sprite mushroomSprite;
        [SerializeField] private Sprite emptyItemSprite;

        private void Start()
        {
            // Find local player and subscribe to events
            // Since player might spawn later, we might need to wait or check periodically
            // Or better: Player registers itself to HUD when spawned locally

#if UNITY_EDITOR
            Debug.Log($"[BattleHUD] Start - GameObject active: {gameObject.activeInHierarchy}, Canvas: {GetComponentInParent<Canvas>()?.enabled}, ItemImage: {itemImage != null}");
#endif
            UpdateItemUI(ItemType.None);
        }

        public void RegisterPlayer(HealthSystem health, KartItemSystem items)
        {
            if (items == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[BattleHUD] RegisterPlayer called with null KartItemSystem. HUD cannot subscribe.");
#endif
                return;
            }

            items.OnItemChanged += UpdateItemUI;

#if UNITY_EDITOR
            Debug.Log($"[BattleHUD] RegisterPlayer complete. Subscribed to item changes. Current item: {items.CurrentItem.Value}");
#endif
            // Initial update
            UpdateItemUI(items.CurrentItem.Value);
        }

        private void UpdateItemUI(ItemType item)
        {
#if UNITY_EDITOR
            Debug.Log($"[BattleHUD] UpdateItemUI called with item: {item}, itemImage null? {itemImage == null}");
#endif
            if (itemImage == null) return;

            switch (item)
            {
                case ItemType.GreenShell:
                    itemImage.sprite = greenShellSprite;
                    itemImage.enabled = true;
                    break;
                case ItemType.Mushroom:
                    itemImage.sprite = mushroomSprite;
                    itemImage.enabled = true;
                    break;
                case ItemType.None:
                default:
                    if (emptyItemSprite != null)
                    {
                        itemImage.sprite = emptyItemSprite;
                        itemImage.enabled = true;
                    }
                    else
                    {
                        itemImage.enabled = false;
                    }
                    break;
            }
        }
    }
}
