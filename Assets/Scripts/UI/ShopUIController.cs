using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MagicRogue
{
    public class ShopUIController : MonoBehaviour
    {
        public static ShopUIController Instance { get; private set; }

        [Header("ショップ商品スロット（右中央 3枠）")]
        [SerializeField] private Image[] shopItemImages = new Image[3];
        [SerializeField] private TextMeshProUGUI[] shopPriceTexts = new TextMeshProUGUI[3];

        [Header("購入待機スロット (Buy)")]
        [SerializeField] private Image buyItemImage;
        [SerializeField] private TextMeshProUGUI buyPriceText;

        [Header("売却スロット (Sell)")]
        [SerializeField] private Image sellItemImage;
        [SerializeField] private TextMeshProUGUI sellPriceText;
        [SerializeField] private TextMeshProUGUI sellAmountText; // 「売却個数」を表示するテキスト（例: 5 / 10）
        [SerializeField] private Slider sellQuantitySlider;     // 右下のスライダー

        [Header("ボタン")]
        [SerializeField] private Button buyButton;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button exitButton;

        [Header("UIテキスト")]
        [SerializeField] private TextMeshProUGUI coinText;

        [Header("遷移先シーン名")]
        [SerializeField] private string nextSceneName = "03_MapSelect";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitButtonClicked);

            if (buyButton != null)
                buyButton.onClick.AddListener(OnBuyButtonClicked);

            if (sellButton != null)
                sellButton.onClick.AddListener(OnSellButtonClicked);

            // スライダーの値が動いた時のイベント登録
            if (sellQuantitySlider != null)
            {
                sellQuantitySlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            UpdateShopUI();
        }

        /// <summary>
        /// ショップ画面全体のUI表示を更新
        /// </summary>
        public void UpdateShopUI()
        {
            if (ShopManager.Instance == null) return;

            InventorySO inventory = ShopManager.Instance.GetPlayerInventory();

            // 1. ショップ商品スロット
            for (int i = 0; i < 3; i++)
            {
                MagicData magic = ShopManager.Instance.GetShopSlotMagic(i);
                if (magic != null)
                {
                    if (shopItemImages[i] != null)
                    {
                        shopItemImages[i].sprite = magic.icon;
                        shopItemImages[i].gameObject.SetActive(true);
                    }
                    if (shopPriceTexts[i] != null) shopPriceTexts[i].text = $"{magic.buyPrice}G";
                }
                else
                {
                    if (shopItemImages[i] != null) shopItemImages[i].gameObject.SetActive(false);
                    if (shopPriceTexts[i] != null) shopPriceTexts[i].text = "SOLD OUT";
                }
            }

            // 2. Buyスロット
            MagicData buyMagic = ShopManager.Instance.GetBuyPendingMagic();
            if (buyMagic != null)
            {
                if (buyItemImage != null)
                {
                    buyItemImage.sprite = buyMagic.icon;
                    buyItemImage.gameObject.SetActive(true);
                }
                if (buyPriceText != null) buyPriceText.text = $"-{buyMagic.buyPrice}G";
            }
            else
            {
                if (buyItemImage != null) buyItemImage.gameObject.SetActive(false);
                if (buyPriceText != null) buyPriceText.text = "";
            }

            // 3. Sellスロット ＆ スライダーの設定
            ItemData sellItem = ShopManager.Instance.GetSellSlotItem();
            int totalCount = ShopManager.Instance.GetSellSlotTotalAmount();
            int selectCount = ShopManager.Instance.GetSellSlotSelectAmount();

            if (sellItem != null && totalCount > 0)
            {
                if (sellItemImage != null)
                {
                    sellItemImage.sprite = sellItem.icon;
                    sellItemImage.gameObject.SetActive(true);
                }

                // 売却合計金額の表示 (+〇〇G)
                if (sellPriceText != null)
                {
                    sellPriceText.text = $"+{sellItem.sellPrice * selectCount}G";
                }

                // スライダーの有効化と範囲設定
                if (sellQuantitySlider != null)
                {
                    sellQuantitySlider.gameObject.SetActive(true);
                    sellQuantitySlider.minValue = 1;
                    sellQuantitySlider.maxValue = totalCount;
                    sellQuantitySlider.wholeNumbers = true; // 整数（1, 2, 3...）のみ対応
                    sellQuantitySlider.value = selectCount;
                }

                if (sellAmountText != null)
                {
                    sellAmountText.text = $"{selectCount} / {totalCount}";
                }
            }
            else
            {
                if (sellItemImage != null) sellItemImage.gameObject.SetActive(false);
                if (sellPriceText != null) sellPriceText.text = "";
                if (sellAmountText != null) sellAmountText.text = "";
                if (sellQuantitySlider != null) sellQuantitySlider.gameObject.SetActive(false);
            }

            // 4. 所持コイン
            if (coinText != null && inventory != null)
            {
                coinText.text = $"{inventory.coins}G";
            }
        }

        private void OnSliderValueChanged(float value)
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.UpdateSellAmount((int)value);
            }
        }

        private void OnBuyButtonClicked()
        {
            if (ShopManager.Instance != null) ShopManager.Instance.ConfirmBuy();
        }

        private void OnSellButtonClicked()
        {
            if (ShopManager.Instance != null) ShopManager.Instance.SellMagicInSlot();
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