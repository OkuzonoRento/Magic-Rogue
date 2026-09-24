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

        [Header("見た目設定")]
        [Tooltip("このアイテム専用の3Dモデル（未設定の場合はレアリティ指定モデルが使用されます）")]
        public GameObject customWorldModelPrefab;

        [Header("売買設定")]
        [Tooltip("ショップでの購入価格")]
        public int buyPrice = 100;

        [Tooltip("ショップでの売却価格")]
        public int sellPrice = 50;

        [Tooltip("1スロットあたりの最大スタック数")]
        public int maxStackSize = 99;
    }
}