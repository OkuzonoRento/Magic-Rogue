using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("プレイヤーのインベントリデータ")]
        [SerializeField] private InventorySO playerInventory;

        [Header("ショップ設定")]
        [Tooltip("出現する可能性のある魔法のリスト（確率調整で複数登録があっても可）")]
        [SerializeField] private List<MagicData> availableMagics = new List<MagicData>();

        [Header("現在のショップ商品（3スロット）")]
        [SerializeField] private MagicData[] shopSlots = new MagicData[3];

        [Header("購入待機用スロット (Buy)")]
        [SerializeField] private MagicData buyPendingMagic;
        private int buyPendingShopIndex = -1;

        [Header("売却用スロット (Sell)")]
        [SerializeField] private ItemData sellSlotItem;
        [SerializeField] private int sellSlotTotalAmount = 0;   // Sellスロットに移動した総数
        [SerializeField] private int sellSlotSelectAmount = 1;  // スライダーで指定された売却数
        private int originalInventorySlotIndex = -1;             // 元いたインベントリスロット番号

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            RerollShopSlots();
        }

        /// <summary>
        /// ショップテーブルから重複（同種魔法）なしで3つの魔法を抽選してスロットにセットする
        /// </summary>
        public void RerollShopSlots()
        {
            if (availableMagics == null || availableMagics.Count == 0) return;

            // テーブルを汚さないよう抽選用の一時リストを作成
            List<MagicData> tempCandidates = new List<MagicData>(availableMagics);

            for (int i = 0; i < shopSlots.Length; i++)
            {
                if (tempCandidates.Count > 0)
                {
                    // 1. 一時リストからランダム選出
                    int randomIndex = Random.Range(0, tempCandidates.Count);
                    MagicData selectedMagic = tempCandidates[randomIndex];

                    // 2. ショップ枠にセット
                    shopSlots[i] = selectedMagic;

                    // ★ 3. 選ばれた魔法と「同じデータ（種類）」を一時リストからすべて一括除外
                    tempCandidates.RemoveAll(magic => magic == selectedMagic);
                }
                else
                {
                    // ユニークな魔法の種類が不足している場合は空枠にする
                    shopSlots[i] = null;
                }
            }

            if (ShopUIController.Instance != null)
            {
                ShopUIController.Instance.UpdateShopUI();
            }
        }

        public void SetBuyPendingMagic(MagicData magic, int fromShopIndex)
        {
            buyPendingMagic = magic;
            buyPendingShopIndex = fromShopIndex;
        }

        public bool ConfirmBuy()
        {
            if (playerInventory == null || buyPendingMagic == null) return false;

            int price = buyPendingMagic.buyPrice > 0 ? buyPendingMagic.buyPrice : 100;

            if (playerInventory.coins < price)
            {
                Debug.Log("[Shop] コインが足りません！");
                return false;
            }

            if (playerInventory.GetEmptySlotIndex() == -1)
            {
                Debug.Log("[Shop] インベントリに空きがありません！");
                return false;
            }

            playerInventory.coins -= price;
            playerInventory.AddItem(buyPendingMagic, 1);

            if (buyPendingShopIndex >= 0 && buyPendingShopIndex < shopSlots.Length)
            {
                shopSlots[buyPendingShopIndex] = null;
            }

            buyPendingMagic = null;
            buyPendingShopIndex = -1;

            if (ShopUIController.Instance != null)
            {
                ShopUIController.Instance.UpdateShopUI();
            }

            return true;
        }

        /// <summary>
        /// Sellスロットへアイテムをセット
        /// </summary>
        public void SetSellSlotItem(ItemData item, int totalAmount, int originalSlotIndex)
        {
            sellSlotItem = item;
            sellSlotTotalAmount = totalAmount;
            sellSlotSelectAmount = totalAmount; // デフォルトは全額
            originalInventorySlotIndex = originalSlotIndex;
        }

        /// <summary>
        /// スライダーから個数が変更された時の更新処理
        /// </summary>
        public void UpdateSellAmount(int amount)
        {
            sellSlotSelectAmount = Mathf.Clamp(amount, 1, sellSlotTotalAmount);
            if (ShopUIController.Instance != null)
            {
                ShopUIController.Instance.UpdateShopUI();
            }
        }

        /// <summary>
        /// Sellボタンが押された時の売却処理
        /// </summary>
        public void SellMagicInSlot()
        {
            if (playerInventory == null || sellSlotItem == null || sellSlotSelectAmount <= 0) return;

            // 1. 売却分だけのゴールドを加算
            int totalEarnedCoins = sellSlotItem.sellPrice * sellSlotSelectAmount;
            playerInventory.coins += totalEarnedCoins;

            Debug.Log($"[Shop] {sellSlotItem.itemName} × {sellSlotSelectAmount} 個を売却！ (+{totalEarnedCoins} G)");

            // 2. 売れ残った分（あまり）がある場合はインベントリへ戻す
            int remainingAmount = sellSlotTotalAmount - sellSlotSelectAmount;
            if (remainingAmount > 0)
            {
                if (originalInventorySlotIndex >= 0 && playerInventory.inventorySlots[originalInventorySlotIndex] == null)
                {
                    playerInventory.AddItemToSlot(sellSlotItem, remainingAmount, originalInventorySlotIndex);
                }
                else
                {
                    playerInventory.AddItem(sellSlotItem, remainingAmount);
                }
            }

            // 3. インベントリの空きスロット前詰め整理を実行
            playerInventory.CompactInventory();

            // 4. クリア
            sellSlotItem = null;
            sellSlotTotalAmount = 0;
            sellSlotSelectAmount = 0;
            originalInventorySlotIndex = -1;

            if (ShopUIController.Instance != null)
            {
                ShopUIController.Instance.UpdateShopUI();
            }
        }

        // --- ゲッター & セッター ---
        public InventorySO GetPlayerInventory() => playerInventory;
        public MagicData GetShopSlotMagic(int index) => (index >= 0 && index < shopSlots.Length) ? shopSlots[index] : null;
        public MagicData GetBuyPendingMagic() => buyPendingMagic;
        public ItemData GetSellSlotItem() => sellSlotItem;
        public int GetSellSlotTotalAmount() => sellSlotTotalAmount;
        public int GetSellSlotSelectAmount() => sellSlotSelectAmount;

        // 互換用
        public void SetSellSlotMagic(MagicData magic)
        {
            if (magic == null) SetSellSlotItem(null, 0, -1);
            else SetSellSlotItem(magic, 1, -1);
        }
        public MagicData GetSellSlotMagic() => sellSlotItem as MagicData;
    }
}