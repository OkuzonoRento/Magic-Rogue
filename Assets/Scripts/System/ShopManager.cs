using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MagicRogue
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("参照SO")]
        [SerializeField] private InventorySO playerInventory;
        [Header("ショップ出現候補となる魔法マスターリスト")]
        [SerializeField] private List<MagicData> availableShopMagics = new();

        [Header("スロットグリッド親 Transform (自動生成用)")]
        [SerializeField] private Transform inventoryGridParent;
        [SerializeField] private Transform attackGridParent;

        [Header("UIテキスト・スライダー")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI transactionPriceText;
        [SerializeField] private TextMeshProUGUI sellAmountText;
        [SerializeField] private Slider sellQuantitySlider;

        [Header("UIテキスト（ショップ商品価格表示用）")]
        [SerializeField] private List<TextMeshProUGUI> shopPriceTexts = new(); // 各商品のNewText

        [Header("UIボタン")]
        [SerializeField] private Button buyButton;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button exitButton;

        [Header("UIイメージ（スロットアイコン直接参照用）")]
        [SerializeField] private Image buyItemImage;
        [SerializeField] private Image sellItemImage;

        [Header("固定スロット（手動割り当て用）")]
        [SerializeField] private ItemSlotUI buyPendingSlot;
        [SerializeField] private ItemSlotUI sellSlot;
        [SerializeField] private List<ItemSlotUI> shopSlots = new();

        [Header("文字色設定")]
        [SerializeField] private Color buyColor = Color.red;
        [SerializeField] private Color sellColor = Color.yellow;
        [SerializeField] private Color defaultPriceColor = Color.white;

        [Header("シーン遷移")]
        [SerializeField] private string nextSceneName = "03_MapSelect";

        // 自動生成されるスロットの保持用リスト
        private List<ItemSlotUI> inventorySlots = new();
        private List<ItemSlotUI> attackSlots = new();

        // 内部状態
        private MagicData pendingBuyMagic;
        private int pendingShopIndex = -1;

        private ItemData sellPendingItem;
        private int sellPendingTotalAmount;
        private int sellPendingSelectAmount = 1;
        private int sellFromInventoryIndex = -1;

        private MagicData[] currentShopMagics = new MagicData[3];

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (buyButton != null) buyButton.onClick.AddListener(ConfirmBuy);
            if (sellButton != null) sellButton.onClick.AddListener(ConfirmSell);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);

            if (sellQuantitySlider != null)
            {
                sellQuantitySlider.onValueChanged.AddListener(OnSellSliderValueChanged);
            }

            // 自動生成されたスロットを取得してセットアップ
            RefreshDynamicSlots();

            // 毎再生時に重複なしでショップアイテムを新規ランダム初期化
            GenerateRandomShopItems();

            // 起動時に初期左詰め（前詰め）を実行
            if (playerInventory != null)
            {
                playerInventory.CompactInventory();
            }

            UpdateAllUI();
        }

        public InventorySO GetPlayerInventory() => playerInventory;

        /// <summary>
        /// 自動生成された親要素の子スロット（ItemSlotUI）を自動取得・セットアップ
        /// </summary>
        public void RefreshDynamicSlots()
        {
            inventorySlots.Clear();
            if (inventoryGridParent != null)
            {
                for (int i = 0; i < inventoryGridParent.childCount; i++)
                {
                    ItemSlotUI slot = inventoryGridParent.GetChild(i).GetComponent<ItemSlotUI>();
                    if (slot != null)
                    {
                        slot.SetupSlot(SlotType.Inventory, i);
                        inventorySlots.Add(slot);
                    }
                }
            }

            attackSlots.Clear();
            if (attackGridParent != null)
            {
                for (int i = 0; i < attackGridParent.childCount; i++)
                {
                    ItemSlotUI slot = attackGridParent.GetChild(i).GetComponent<ItemSlotUI>();
                    if (slot != null)
                    {
                        slot.SetupSlot(SlotType.AttackSlot, i);
                        attackSlots.Add(slot);
                    }
                }
            }
        }

        /// <summary>
        /// 重複しないようにランダムで3つの魔法を抽選・初期化する
        /// </summary>
        public void GenerateRandomShopItems()
        {
            if (availableShopMagics == null || availableShopMagics.Count == 0)
            {
                Debug.LogWarning("[ShopManager] availableShopMagics が空です。");
                return;
            }

            // nullを除外した重複なしリストの作成
            List<MagicData> uniquePool = new List<MagicData>();
            foreach (var magic in availableShopMagics)
            {
                if (magic != null && !uniquePool.Contains(magic))
                {
                    uniquePool.Add(magic);
                }
            }

            // シャッフル（Fisher-Yates アルゴリズム）
            for (int i = 0; i < uniquePool.Count; i++)
            {
                int randIndex = Random.Range(i, uniquePool.Count);
                (uniquePool[i], uniquePool[randIndex]) = (uniquePool[randIndex], uniquePool[i]);
            }

            // 被りなしでショップ枠（3枠）に割り当て
            for (int i = 0; i < currentShopMagics.Length; i++)
            {
                currentShopMagics[i] = (i < uniquePool.Count) ? uniquePool[i] : null;
            }

            pendingBuyMagic = null;
            pendingShopIndex = -1;
        }

        /// <summary>
        /// 画面全体の表示を一括更新（UI描画のみを行い、勝手なソート/左詰めはしない）
        /// </summary>
        public void UpdateAllUI()
        {
            if (inventorySlots.Count == 0 || attackSlots.Count == 0)
            {
                RefreshDynamicSlots();
            }

            // 1. 所持コイン表示
            if (coinText != null && playerInventory != null)
            {
                coinText.text = $"{playerInventory.coins}";
            }

            // 2. 各スロットUIの描画更新
            foreach (var slot in inventorySlots) if (slot != null) slot.UpdateSlotUI();
            foreach (var slot in attackSlots) if (slot != null) slot.UpdateSlotUI();

            // ショップスロットの描画 ＆ Buy選択中アイテムのグレーアウト制御 ＆ 価格テキスト更新
            for (int i = 0; i < shopSlots.Count; i++)
            {
                if (shopSlots[i] == null) continue;
                shopSlots[i].UpdateSlotUI();

                CanvasGroup canvasGroup = shopSlots[i].GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = shopSlots[i].gameObject.AddComponent<CanvasGroup>();
                }

                if (i == pendingShopIndex && pendingBuyMagic != null)
                {
                    canvasGroup.alpha = 0.4f; // グレーアウト
                    canvasGroup.blocksRaycasts = false; // 操作不能にする
                }
                else
                {
                    canvasGroup.alpha = 1.0f;
                    canvasGroup.blocksRaycasts = true;
                }

                // ショップ商品の下にあるNewTextに価格を設定
                if (i < shopPriceTexts.Count && shopPriceTexts[i] != null)
                {
                    MagicData magic = GetShopSlotMagic(i);
                    if (magic != null)
                    {
                        shopPriceTexts[i].text = $"{magic.buyPrice}";
                    }
                    else
                    {
                        shopPriceTexts[i].text = "----";
                    }
                }
            }

            if (buyPendingSlot != null) buyPendingSlot.UpdateSlotUI();
            if (sellSlot != null) sellSlot.UpdateSlotUI();

            // 3. Buy / Sell アイテム画像の更新
            if (buyItemImage != null)
            {
                buyItemImage.gameObject.SetActive(pendingBuyMagic != null);
                if (pendingBuyMagic != null) buyItemImage.sprite = pendingBuyMagic.icon;
            }

            if (sellItemImage != null)
            {
                bool hasSellItem = sellPendingItem != null && sellPendingTotalAmount > 0;
                sellItemImage.gameObject.SetActive(hasSellItem);
                if (hasSellItem) sellItemImage.sprite = sellPendingItem.icon;
            }

            // 4. カウントのテキストの更新 (Buy優先、次にSell)
            if (transactionPriceText != null)
            {
                if (pendingBuyMagic != null)
                {
                    transactionPriceText.text = $"-{pendingBuyMagic.buyPrice}";
                    transactionPriceText.color = buyColor;
                }
                else if (sellPendingItem != null && sellPendingTotalAmount > 0)
                {
                    int totalPrice = sellPendingItem.sellPrice * sellPendingSelectAmount;
                    transactionPriceText.text = $"+{totalPrice}";
                    transactionPriceText.color = sellColor;
                }
                else
                {
                    transactionPriceText.text = "";
                    transactionPriceText.color = defaultPriceColor;
                }
            }

            // 5. Sellスライダーの半透明・操作可否制御 ＆ 個数テキスト更新
            if (sellQuantitySlider != null)
            {
                sellQuantitySlider.gameObject.SetActive(true); // 常時アクティブ
                sellQuantitySlider.wholeNumbers = true;

                CanvasGroup sliderCanvasGroup = sellQuantitySlider.GetComponent<CanvasGroup>();
                if (sliderCanvasGroup == null)
                {
                    sliderCanvasGroup = sellQuantitySlider.gameObject.AddComponent<CanvasGroup>();
                }

                if (sellPendingItem != null && sellPendingTotalAmount > 0)
                {
                    // Sellアイテムが入っている時：くっきり表示＆操作可能
                    sliderCanvasGroup.alpha = 1.0f;
                    sliderCanvasGroup.blocksRaycasts = true;

                    if (sellPendingTotalAmount <= 1)
                    {
                        sellQuantitySlider.minValue = 0;
                        sellQuantitySlider.maxValue = 1;
                        sellQuantitySlider.SetValueWithoutNotify(1);
                        sellQuantitySlider.interactable = false;
                    }
                    else
                    {
                        sellQuantitySlider.minValue = 1;
                        sellQuantitySlider.maxValue = sellPendingTotalAmount;
                        sellQuantitySlider.SetValueWithoutNotify(sellPendingSelectAmount);
                        sellQuantitySlider.interactable = true;
                    }

                    if (sellAmountText != null)
                    {
                        sellAmountText.text = $"{sellPendingSelectAmount} / {sellPendingTotalAmount}";
                    }
                }
                else
                {
                    // Sellアイテムが入っていない時：半透明＆操作不能
                    sliderCanvasGroup.alpha = 0.5f;
                    sliderCanvasGroup.blocksRaycasts = false;
                    sellQuantitySlider.interactable = false;

                    if (sellAmountText != null)
                    {
                        sellAmountText.text = "0 / 0";
                    }
                }
            }
        }

        // --- Buy ロジック ---

        public MagicData GetShopSlotMagic(int index) => (index >= 0 && index < currentShopMagics.Length) ? currentShopMagics[index] : null;
        public MagicData GetBuyPendingMagic() => pendingBuyMagic;

        public void SetBuyPendingMagic(MagicData magic, int shopIndex)
        {
            // 排他処理：Buyにセットする時、Sellにアイテムがあればインベントリへ戻してクリア
            if (sellPendingItem != null)
            {
                ClearSellPendingInternal();
            }

            pendingBuyMagic = magic;
            pendingShopIndex = shopIndex;
            UpdateAllUI();
        }

        public void ClearBuyPending()
        {
            ClearBuyPendingInternal();
            UpdateAllUI();
        }

        private void ClearBuyPendingInternal()
        {
            pendingBuyMagic = null;
            pendingShopIndex = -1;
        }

        public void ConfirmBuy()
        {
            if (pendingBuyMagic == null || playerInventory == null) return;

            if (playerInventory.coins < pendingBuyMagic.buyPrice)
            {
                Debug.Log("[ShopManager] 所持コインが足りません！");
                return;
            }

            playerInventory.coins -= pendingBuyMagic.buyPrice;
            playerInventory.AddItem(pendingBuyMagic, 1);

            // 購入確定後、該当のショップ商品を消去
            if (pendingShopIndex >= 0 && pendingShopIndex < currentShopMagics.Length)
            {
                currentShopMagics[pendingShopIndex] = null;
            }
            pendingBuyMagic = null;
            pendingShopIndex = -1;

            // 購入確定時は左詰めを実行
            playerInventory.CompactInventory();
            UpdateAllUI();
        }

        // --- Sell ロジック ---

        public ItemData GetSellSlotItem() => sellPendingItem;
        public int GetSellSlotTotalAmount() => sellPendingTotalAmount;
        public int GetSellSlotSelectAmount() => sellPendingSelectAmount;

        public void SetSellSlotItem(ItemData item, int amount, int fromInvIndex)
        {
            // 排他処理：Sellにセットする時、Buyにアイテムがあればキャンセルする
            if (pendingBuyMagic != null)
            {
                ClearBuyPendingInternal();
            }

            // もしすでに別のアイテムがSell枠に入っていたらインベントリへ戻す
            if (sellPendingItem != null)
            {
                ClearSellPendingInternal();
            }

            sellPendingItem = item;
            sellPendingTotalAmount = amount; // 所持数を記録
            sellPendingSelectAmount = 1;     // 初期選択数は 1
            sellFromInventoryIndex = fromInvIndex;
            UpdateAllUI();
        }

        public void ClearSellPending()
        {
            ClearSellPendingInternal();
            // キャンセルして戻した後は綺麗に詰める
            if (playerInventory != null) playerInventory.CompactInventory();
            UpdateAllUI();
        }

        private void ClearSellPendingInternal()
        {
            if (sellPendingItem != null && sellPendingTotalAmount > 0 && playerInventory != null)
            {
                playerInventory.AddItem(sellPendingItem, sellPendingTotalAmount);
            }

            sellPendingItem = null;
            sellPendingTotalAmount = 0;
            sellPendingSelectAmount = 1;
            sellFromInventoryIndex = -1;
        }

        public ItemData ExtractSellPendingItem(out int totalAmount)
        {
            ItemData item = sellPendingItem;
            totalAmount = sellPendingTotalAmount;

            sellPendingItem = null;
            sellPendingTotalAmount = 0;
            sellPendingSelectAmount = 1;
            sellFromInventoryIndex = -1;

            return item;
        }

        public void OnSellSliderValueChanged(float value)
        {
            sellPendingSelectAmount = Mathf.Clamp((int)value, 1, Mathf.Max(1, sellPendingTotalAmount));

            if (sellPendingItem != null && transactionPriceText != null)
            {
                int totalPrice = sellPendingItem.sellPrice * sellPendingSelectAmount;
                transactionPriceText.text = $"+{totalPrice}";
                if (sellAmountText != null) sellAmountText.text = $"{sellPendingSelectAmount} / {sellPendingTotalAmount}";
            }
        }

        public void ConfirmSell()
        {
            if (sellPendingItem == null || sellPendingTotalAmount <= 0 || playerInventory == null) return;

            int gainCoins = sellPendingItem.sellPrice * sellPendingSelectAmount;
            playerInventory.coins += gainCoins;

            int remainCount = sellPendingTotalAmount - sellPendingSelectAmount;

            // 売却しなかった残りの個数をインベントリに戻す
            if (remainCount > 0)
            {
                playerInventory.AddItem(sellPendingItem, remainCount);
            }

            sellPendingItem = null;
            sellPendingTotalAmount = 0;
            sellPendingSelectAmount = 1;
            sellFromInventoryIndex = -1;

            // 売却確定後は空きができるので左詰めを実行
            playerInventory.CompactInventory();
            UpdateAllUI();
        }

        private void OnExitButtonClicked()
        {
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ChangeScene(nextSceneName);
            }
        }
    }
}