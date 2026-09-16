using UnityEngine;

namespace MagicRogue
{
    public enum ItemRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(fileName = "NewItemData", menuName = "MagicRogue/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("基本情報")]
        public string itemId;
        public string itemName;
        public Sprite icon;
        public ItemRarity rarity = ItemRarity.Common;

        [Header("売却設定")]
        [Tooltip("ショップでの売却価格")]
        public int sellPrice = 50;

        [Tooltip("1スロットあたりの最大スタック数")]
        public int maxStackSize = 99;
    }
}