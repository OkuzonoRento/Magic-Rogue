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
        [Header("魔法スロット")]
        public MagicData[] spellSlots = new MagicData[3];

        [Header("所持換金アイテム")]
        public List<ItemStack> items = new List<ItemStack>();

        public void InitializeInventory()
        {
            // 必要に応じて初期化処理
        }

        /// <summary>
        /// アイテムをインベントリに追加
        /// </summary>
        public bool AddItem(ItemData data, int amount)
        {
            if (data == null || amount <= 0) return false;

            // 既存のスタックを探して加算
            ItemStack existingStack = items.Find(x => x.itemData == data && x.amount < data.maxStackSize);
            if (existingStack != null)
            {
                int spaceLeft = data.maxStackSize - existingStack.amount;
                int addAmount = Mathf.Min(spaceLeft, amount);
                existingStack.amount += addAmount;

                int remaining = amount - addAmount;
                if (remaining > 0)
                {
                    return AddItem(data, remaining); // 残りを別枠に追加
                }
                return true;
            }

            // 新規スロット追加
            items.Add(new ItemStack { itemData = data, amount = amount });
            return true;
        }
    }
}