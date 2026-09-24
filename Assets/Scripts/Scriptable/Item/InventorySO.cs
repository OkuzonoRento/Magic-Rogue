using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    [Serializable]
    public class ItemStack
    {
        public ItemData itemData;
        public int amount;
    }

    [CreateAssetMenu(fileName = "NewInventory", menuName = "MagicRogue/Inventory SO")]
    public class InventorySO : ScriptableObject
    {
        [Header("所持金")]
        public int coins = 1000;

        [Header("魔法スロット（3枠）")]
        public MagicData[] spellSlots = new MagicData[3];

        [Header("インベントリスロット（30枠）")]
        public ItemStack[] inventorySlots = new ItemStack[30];

        public void InitializeInventory()
        {
            if (spellSlots == null || spellSlots.Length != 3)
            {
                spellSlots = new MagicData[3];
            }

            if (inventorySlots == null || inventorySlots.Length != 30)
            {
                inventorySlots = new ItemStack[30];
            }
        }

        /// <summary>
        /// インベントリの空きスロットを自動で詰める（前詰め整理）
        /// </summary>
        public void CompactInventory()
        {
            if (inventorySlots == null) return;

            List<ItemStack> activeItems = new List<ItemStack>();

            // 有効なアイテム（データが存在し個数が1以上のもの）を抽出
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] != null && inventorySlots[i].itemData != null && inventorySlots[i].amount > 0)
                {
                    activeItems.Add(inventorySlots[i]);
                }
            }

            // 配列を一度クリアして前詰め再配置
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (i < activeItems.Count)
                {
                    inventorySlots[i] = activeItems[i];
                }
                else
                {
                    inventorySlots[i] = null;
                }
            }
        }

        /// <summary>
        /// 空いているインベントリスロット（30枠中）のインデックスを取得
        /// </summary>
        public int GetEmptySlotIndex()
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] == null || inventorySlots[i].itemData == null || inventorySlots[i].amount <= 0)
                {
                    return i;
                }
            }
            return -1; // 空きなし
        }

        /// <summary>
        /// 指定スロットへアイテムを追加
        /// </summary>
        public bool AddItemToSlot(ItemData data, int amount, int slotIndex)
        {
            if (data == null || amount <= 0 || slotIndex < 0 || slotIndex >= inventorySlots.Length) return false;

            if (inventorySlots[slotIndex] == null)
            {
                inventorySlots[slotIndex] = new ItemStack();
            }

            inventorySlots[slotIndex].itemData = data;
            inventorySlots[slotIndex].amount = amount;
            return true;
        }

        /// <summary>
        /// 自動で空いているスロットへアイテムを追加
        /// </summary>
        public bool AddItem(ItemData data, int amount)
        {
            if (data == null || amount <= 0) return false;

            int emptyIndex = GetEmptySlotIndex();
            if (emptyIndex != -1)
            {
                bool result = AddItemToSlot(data, amount, emptyIndex);
                CompactInventory(); // 追加後に前詰め整理
                return result;
            }

            Debug.Log("[Inventory] インベントリが満タンです！");
            return false;
        }

        /// <summary>
        /// 指定スロットのアイテムを削除/空にする
        /// </summary>
        public void RemoveItemFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= inventorySlots.Length) return;
            inventorySlots[slotIndex] = null;
        }
    }
}