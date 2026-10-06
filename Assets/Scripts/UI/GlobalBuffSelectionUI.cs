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
            DebuffPhase,
            BuffPhase
        }

        [Header("フェーズ設定")]
        [SerializeField] private SelectionPhase _currentPhase = SelectionPhase.DebuffPhase;

        [Header("所持クレジット設定")]
        [SerializeField] private int _initialCredit = 0;
        private int _currentCredit;

        [Header("リロール設定")]
        [SerializeField] private int _maxRerolls = 4;
        private int _remainingRerolls;

        [Header("UIテキスト表示")]
        [SerializeField] private TextMeshProUGUI _creditText;
        [SerializeField] private TextMeshProUGUI _phaseText;
        [SerializeField] private TextMeshProUGUI _rerollText;

        [Header("カード生成設定")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _cardPrefab;

        [Header("ボタン類参照")]
        [SerializeField] private Button _rerollButton;
        [SerializeField] private Button _confirmButton;

        [Header("バフ・デバフデータベース")]
        [SerializeField] private List<BuffData> _availableBuffs = new List<BuffData>();
        [SerializeField] private List<BuffData> _availableDebuffs = new List<BuffData>();

        private readonly List<BuffCardUI> _spawnedCards = new List<BuffCardUI>();
        private readonly List<BuffData> _currentDisplayedBuffs = new List<BuffData>();
        private readonly HashSet<BuffData> _selectedBuffs = new HashSet<BuffData>();

        private void Start()
        {
            _currentCredit = _initialCredit;
            _remainingRerolls = _maxRerolls;

            if (_rerollButton != null) _rerollButton.onClick.AddListener(OnRerollButtonClicked);
            if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirmButtonClicked);

            StartDebuffPhase();
        }

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

            _remainingRerolls++;

            GenerateThreeCards();
            UpdateUI();
        }

        private void GenerateThreeCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();
            _currentDisplayedBuffs.Clear();

            List<BuffData> sourcePool = (_currentPhase == SelectionPhase.DebuffPhase) ? _availableDebuffs : _availableBuffs;
            bool isDebuff = (_currentPhase == SelectionPhase.DebuffPhase);

            _currentDisplayedBuffs.AddRange(GetRandomUniqueBuffs(sourcePool, 3));

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

        private List<BuffData> GetRandomUniqueBuffs(List<BuffData> sourceList, int count)
        {
            List<BuffData> pool = new List<BuffData>(sourceList);
            List<BuffData> result = new List<BuffData>();

            int pickCount = Mathf.Min(count, pool.Count);
            for (int i = 0; i < pickCount; i++)
            {
                int randomIndex = Random.Range(0, pool.Count);
                result.Add(pool[randomIndex]);
                pool.RemoveAt(randomIndex);
            }

            return result;
        }

        private void OnCardToggleChanged(BuffData buff, bool isSelected)
        {
            if (buff == null) return;

            bool isDebuff = (_currentPhase == SelectionPhase.DebuffPhase);

            if (isSelected)
            {
                _selectedBuffs.Add(buff);
                if (isDebuff) _currentCredit += buff.creditCost;
                else _currentCredit -= buff.creditCost;
            }
            else
            {
                _selectedBuffs.Remove(buff);
                if (isDebuff) _currentCredit -= buff.creditCost;
                else _currentCredit += buff.creditCost;
            }

            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ToggleGlobalBuff(buff, isSelected);
            }

            UpdateUI();
        }

        private void OnRerollButtonClicked()
        {
            if (_remainingRerolls <= 0) return;

            DeselectCurrentDisplayedCards();

            _remainingRerolls--;
            GenerateThreeCards();
            UpdateUI();
        }

        private void DeselectCurrentDisplayedCards()
        {
            foreach (var cardUI in _spawnedCards)
            {
                if (cardUI != null && cardUI.IsSelected())
                {
                    OnCardToggleChanged(cardUI.GetData(), false);
                }
            }
        }

        private void OnConfirmButtonClicked()
        {
            if (_currentPhase == SelectionPhase.DebuffPhase)
            {
                StartBuffPhase();
            }
            else
            {
                if (GameSceneManager.Instance != null)
                {
                    GameSceneManager.Instance.ConfirmGlobalBuffsAndGoToMapSelect();
                }
            }
        }

        private void UpdateUI()
        {
            if (_creditText != null) _creditText.text = $" {_currentCredit}";
            if (_rerollText != null) _rerollText.text = $"{_remainingRerolls}";
            if (_rerollButton != null) _rerollButton.interactable = (_remainingRerolls > 0);

            foreach (var cardUI in _spawnedCards)
            {
                if (cardUI == null) continue;

                BuffData data = cardUI.GetData();
                bool isDebuff = (_currentPhase == SelectionPhase.DebuffPhase);
                bool canAfford = isDebuff || (_currentCredit >= data.creditCost);

                cardUI.UpdateAffordability(canAfford);
            }
        }
    }
}