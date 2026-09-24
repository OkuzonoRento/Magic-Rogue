using UnityEngine;

namespace MagicRogue
{
    public class UIGridGenerator : MonoBehaviour
    {
        [Header("生成プレハブ")]
        [Tooltip("インベントリスロット用のPrefab")]
        [SerializeField] private GameObject inventorySlotPrefab;

        [Tooltip("Attack(魔法)スロット用のPrefab（別のデザインを指定）")]
        [SerializeField] private GameObject attackSlotPrefab;

        [Header("生成先コンテナ (Transform)")]
        [SerializeField] private Transform inventoryGridParent; // 左側のインベントリ親オブジェクト
        [SerializeField] private Transform attackGridParent;    // 上部/中央のAttack(魔法)スロット親オブジェクト

        [Header("生成設定")]
        [SerializeField] private int inventorySlotCount = 30; // インベントリの生成数
        [SerializeField] private int attackSlotCount = 3;    // Attack(魔法)スロットの生成数

        private void Awake()
        {
            GenerateGrids();
        }

        /// <summary>
        /// InventoryとAttackのスロットを自動生成して配置
        /// </summary>
        public void GenerateGrids()
        {
            // 1. Inventory スロットの自動生成
            if (inventoryGridParent != null)
            {
                if (inventorySlotPrefab == null)
                {
                    Debug.LogError("[UIGridGenerator] inventorySlotPrefab がセットされていません！");
                    return;
                }

                // 既存の子要素があればクリア
                foreach (Transform child in inventoryGridParent)
                {
                    Destroy(child.gameObject);
                }

                for (int i = 0; i < inventorySlotCount; i++)
                {
                    GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryGridParent);
                    slotObj.name = $"InventorySlot_{i}";

                    ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                    if (slotUI != null)
                    {
                        slotUI.SetupSlot(SlotType.Inventory, i);
                    }
                }
            }

            // 2. Attack (魔法) スロットの自動生成
            if (attackGridParent != null)
            {
                // Attack用のPrefabが未設定の場合は、インベントリ用をフォールバック使用
                GameObject prefabToUse = (attackSlotPrefab != null) ? attackSlotPrefab : inventorySlotPrefab;

                if (prefabToUse == null)
                {
                    Debug.LogError("[UIGridGenerator] attackSlotPrefab がセットされていません！");
                    return;
                }

                // 既存の子要素があればクリア
                foreach (Transform child in attackGridParent)
                {
                    Destroy(child.gameObject);
                }

                for (int i = 0; i < attackSlotCount; i++)
                {
                    GameObject slotObj = Instantiate(prefabToUse, attackGridParent);
                    slotObj.name = $"AttackSlot_{i}";

                    ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                    if (slotUI != null)
                    {
                        slotUI.SetupSlot(SlotType.AttackSlot, i);
                    }
                }
            }
        }
    }
}