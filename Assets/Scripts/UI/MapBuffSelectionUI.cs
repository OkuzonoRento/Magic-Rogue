using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MagicRogue
{
    public class MapBuffSelectionUI : MonoBehaviour
    {
        [Header("カード動的生成設定 (GlobalBuffSelectionUIと統一)")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _cardPrefab;

        [Header("ボタン類参照")]
        [SerializeField] private Button _confirmButton;

        [Header("マップバフデータベース")]
        [SerializeField] private List<BuffData> _mapBuffPool = new List<BuffData>();

        private readonly List<BuffCardUI> _spawnedCards = new List<BuffCardUI>();
        private BuffCardUI _selectedCard = null;

        private void Start()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }

            OpenSelectionUI();
        }

        public void OpenSelectionUI()
        {
            RenderSettings.fog = false;

            _selectedCard = null;

            if (_confirmButton != null)
            {
                _confirmButton.interactable = false;
            }

            GenerateThreeCards();
        }

        private void GenerateThreeCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();

            List<BuffData> selectedBuffs = GetRandomUniqueBuffs(_mapBuffPool, 3);

            if (_cardContainer != null && _cardPrefab != null)
            {
                foreach (var buffData in selectedBuffs)
                {
                    GameObject cardObj = Instantiate(_cardPrefab, _cardContainer);
                    if (cardObj.TryGetComponent<BuffCardUI>(out var cardUI))
                    {
                        cardUI.SetupMap(buffData, OnCardSelected);
                        _spawnedCards.Add(cardUI);
                    }
                }
            }
        }

        private void OnCardSelected(BuffCardUI clickedCard)
        {
            if (_selectedCard == clickedCard)
            {
                _selectedCard = null;
            }
            else
            {
                _selectedCard = clickedCard;
            }

            foreach (var card in _spawnedCards)
            {
                if (card == null) continue;
                bool isTarget = (card == _selectedCard);
                card.SetSelected(isTarget);
            }

            if (_confirmButton != null)
            {
                _confirmButton.interactable = (_selectedCard != null);
            }
        }

        private void OnConfirmButtonClicked()
        {
            if (_selectedCard == null) return;

            Time.timeScale = 1f;

            BuffData chosenBuff = _selectedCard.GetData();
            if (chosenBuff != null && GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ToggleMapBuff(chosenBuff, true);
            }

            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ConfirmMapBuffsAndStartInGame();
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
    }
}