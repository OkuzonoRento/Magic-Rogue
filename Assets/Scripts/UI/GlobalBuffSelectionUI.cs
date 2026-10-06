using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MagicRogue
{
    public class GlobalBuffSelectionUI : MonoBehaviour
    {
        public enum SelectionPhase
        {
            DebuffPhase, // まずデバフを選択してクレジットを獲得
            BuffPhase    // 獲得したクレジットでバフを選択
        }

        [Header("フェーズ設定")]
        [SerializeField] private SelectionPhase _currentPhase = SelectionPhase.DebuffPhase;

        [Header("所持クレジット設定")]
        [SerializeField] private int _initialCredit = 0; // 初期クレジット（0スタート。デバフで稼ぐ）
        private int _currentCredit;

        [Header("リロール設定")]
        [SerializeField] private int _maxRerolls = 4; // 基本最大4回
        private int _remainingRerolls;

        [Header("UIテキスト表示")]
        [SerializeField] private TextMeshProUGUI _creditText;
        [SerializeField] private TextMeshProUGUI _phaseText;
        [SerializeField] private TextMeshProUGUI _rerollText;

        [Header("カード生成設定")]
        [SerializeField] private Transform _cardContainer; // カード3枚を並べる親要素 (BuffContainer 等)
        [SerializeField] private GameObject _cardPrefab;    // BuffCardUI 付きプレハブ

        [Header("ボタン類参照")]
        [SerializeField] private Button _rerollButton;
        [SerializeField] private Button _confirmButton; // 「デバフ決定/バフへ」または「確定して次へ」

        [Header("バフ・デバフデータベース（Inspectorでセット）")]
        [SerializeField] private List<BuffData> _availableBuffs = new List<BuffData>();
        [SerializeField] private List<BuffData> _availableDebuffs = new List<BuffData>();

        // 現在表示されている3枚のUI制御リスト
        private readonly List<BuffCardUI> _spawnedCards = new List<BuffCardUI>();
        private readonly List<BuffData> _currentDisplayedBuffs = new List<BuffData>();

        // 選択されたバフ/デバフの保持リスト
        private readonly HashSet<BuffData> _selectedBuffs = new HashSet<BuffData>();

        private void Start()
        {
            _currentCredit = _initialCredit;
            _remainingRerolls = _maxRerolls;

            if (_rerollButton != null)
            {
                _rerollButton.onClick.AddListener(OnRerollButtonClicked);
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }

            StartDebuffPhase();
        }

        #region フェーズ進行

        private void StartDebuffPhase()
        {
            _currentPhase = SelectionPhase.DebuffPhase;
            if (_phaseText != null) _phaseText.text = "DebuffSelect";

            GenerateThreeCards();
            UpdateUI();
        }

        private void StartBuffPhase()
        {
            _currentPhase = SelectionPhase.BuffPhase;
            if (_phaseText != null) _phaseText.text = "BuffSelect";

            // デバフ決定時にリロール回数を1回回復！
            _remainingRerolls++;

            GenerateThreeCards();
            UpdateUI();
        }

        #endregion

        #region カード生成・重複防止ロジック

        /// <summary>
        /// 重複なしでランダムに3枚のカードを生成します
        /// </summary>
        private void GenerateThreeCards()
        {
            // 画面上の旧カードを削除
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();
            _currentDisplayedBuffs.Clear();

            // 現在のフェーズに応じてプールを切り替え
            List<BuffData> sourcePool = (_currentPhase == SelectionPhase.DebuffPhase) ? _availableDebuffs : _availableBuffs;
            bool isDebuff = (_currentPhase == SelectionPhase.DebuffPhase);

            // 重複なしで3枚ランダムピック
            _currentDisplayedBuffs.AddRange(GetRandomUniqueBuffs(sourcePool, 3));

            // UIオブジェクトの生成
            if (_cardContainer != null && _cardPrefab != null)
            {
                foreach (var buffData in _currentDisplayedBuffs)
                {
                    GameObject cardObj = Instantiate(_cardPrefab, _cardContainer);
                    if (cardObj.TryGetComponent<BuffCardUI>(out var cardUI))
                    {
                        bool canAfford = isDebuff || (_currentCredit >= buffData.creditCost);

                        cardUI.SetupGlobal(buffData, isDebuff, canAfford, OnCardToggleChanged);
                        _spawnedCards.Add(cardUI);
                    }
                }
            }
        }

        /// <summary>
        /// リストから重複せずに指定された個数のBuffDataを取得する helper
        /// </summary>
        private List<BuffData> GetRandomUniqueBuffs(List<BuffData> sourceList, int count)
        {
            List<BuffData> pool = new List<BuffData>(sourceList);
            List<BuffData> result = new List<BuffData>();

            int pickCount = Mathf.Min(count, pool.Count);
            for (int i = 0; i < pickCount; i++)
            {
                int randomIndex = Random.Range(0, pool.Count);
                result.Add(pool[randomIndex]);
                pool.RemoveAt(randomIndex); // 選んだものを除外して重複を防止
            }

            return result;
        }

        #endregion

        #region インタラクション (選択・リロール・確定)

        private void OnCardToggleChanged(BuffData buff, bool isSelected)
        {
            if (buff == null) return;

            bool isDebuff = (_currentPhase == SelectionPhase.DebuffPhase);

            if (isSelected)
            {
                _selectedBuffs.Add(buff);
                if (isDebuff) _currentCredit += buff.creditCost; // デバフはクレジット増加
                else _currentCredit -= buff.creditCost;          // バフはクレジット消費
            }
            else
            {
                _selectedBuffs.Remove(buff);
                if (isDebuff) _currentCredit -= buff.creditCost; // 解除で戻す
                else _currentCredit += buff.creditCost;
            }

            // GameSceneManager へデータ同期
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ToggleGlobalBuff(buff, isSelected);
            }

            UpdateUI();
        }

        private void OnRerollButtonClicked()
        {
            if (_remainingRerolls <= 0) return;

            // リロール前に「現在画面で選択されているカード」の選択を全て解除して計算を戻す
            DeselectCurrentDisplayedCards();

            _remainingRerolls--;
            GenerateThreeCards();
            UpdateUI();
        }

        /// <summary>
        /// 現在画面に出ているカードのうち、選択状態にあるものをすべて解除します
        /// </summary>
        private void DeselectCurrentDisplayedCards()
        {
            foreach (var cardUI in _spawnedCards)
            {
                if (cardUI != null && cardUI.IsSelected())
                {
                    // カードの選択を外してクレジットとデータ管理を巻き戻す
                    OnCardToggleChanged(cardUI.GetData(), false);
                }
            }
        }

        private void OnConfirmButtonClicked()
        {
            if (_currentPhase == SelectionPhase.DebuffPhase)
            {
                // デバフ選択完了 → バフ選択フェーズへ移行
                StartBuffPhase();
            }
            else
            {
                // バフ選択完了 → マップ選択シーンへ遷移
                if (GameSceneManager.Instance != null)
                {
                    GameSceneManager.Instance.ChangeSceneWithFade("03_MapSelect");
                }
            }
        }

        #endregion

        #region UI更新

        private void UpdateUI()
        {
            // クレジット表示更新
            if (_creditText != null)
            {
                _creditText.text = $" {_currentCredit}";
            }

            // リロール表示＆ボタン活性更新
            if (_rerollText != null)
            {
                _rerollText.text = $"{_remainingRerolls}";
            }

            if (_rerollButton != null)
            {
                _rerollButton.interactable = (_remainingRerolls > 0);
            }

            // 各カードの選択可否（クレジット不足判定）を最新化
            foreach (var cardUI in _spawnedCards)
            {
                if (cardUI == null) continue;

                BuffData data = cardUI.GetData();
                bool isDebuff = (_currentPhase == SelectionPhase.DebuffPhase);
                bool canAfford = isDebuff || (_currentCredit >= data.creditCost);

                cardUI.UpdateAffordability(canAfford);
            }
        }

        #endregion
    }
}