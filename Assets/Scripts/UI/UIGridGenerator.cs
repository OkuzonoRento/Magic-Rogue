using UnityEngine;

namespace MagicRogue
{
    public class UIGridGenerator : MonoBehaviour
    {
        [Header("生成プレハブ")]
        [SerializeField] private GameObject inventorySlotPrefab;
        [SerializeField] private GameObject attackSlotPrefab;

        [Header("生成先コンテナ (Transform)")]
        [SerializeField] private Transform inventoryGridParent;
        [SerializeField] private Transform attackGridParent;

        [Header("生成設定")]
        [SerializeField] private int inventorySlotCount = 30;
        [SerializeField] private int attackSlotCount = 3;

        private void Awake()
        {
            GenerateGrids();
        }

        public void GenerateGrids()
        {
            if (inventoryGridParent != null && inventorySlotPrefab != null)
            {
                foreach (Transform child in inventoryGridParent) Destroy(child.gameObject);

                for (int i = 0; i < inventorySlotCount; i++)
                {
                    GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryGridParent);
                    slotObj.name = $"InventorySlot_{i}";
                    ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                    if (slotUI != null) slotUI.SetupSlot(SlotType.Inventory, i);
                }
            }

            if (attackGridParent != null)
            {
                GameObject prefabToUse = (attackSlotPrefab != null) ? attackSlotPrefab : inventorySlotPrefab;
                if (prefabToUse != null)
                {
                    foreach (Transform child in attackGridParent) Destroy(child.gameObject);

                    for (int i = 0; i < attackSlotCount; i++)
                    {
                        GameObject slotObj = Instantiate(prefabToUse, attackGridParent);
                        slotObj.name = $"AttackSlot_{i}";
                        ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                        if (slotUI != null) slotUI.SetupSlot(SlotType.AttackSlot, i);
                    }
                }
            }
        }
    }
}