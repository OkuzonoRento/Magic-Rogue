using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MagicRogue
{
    public class MapBuffSelectionUI : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private GameObject _panelObject;
        [SerializeField] private BuffCardUI[] _cardUIList;
        [SerializeField] private Button _confirmButton;

        [Header("Buff Database")]
        [SerializeField] private List<BuffData> _mapBuffPool = new List<BuffData>();

        private BuffCardUI _selectedCard = null;

        private void Start()
        {
            if (_panelObject != null) _panelObject.SetActive(false);

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }
        }

        public void OpenSelectionUI()
        {
            if (_panelObject != null) _panelObject.SetActive(true);
            Time.timeScale = 0f;

            _selectedCard = null;
            if (_confirmButton != null) _confirmButton.interactable = false;

            List<BuffData> selectedBuffs = GetRandomMapBuffs(3);

            for (int i = 0; i < _cardUIList.Length; i++)
            {
                if (i < selectedBuffs.Count)
                {
                    _cardUIList[i].gameObject.SetActive(true);
                    _cardUIList[i].SetupMap(selectedBuffs[i], OnCardSelected);
                }
                else
                {
                    _cardUIList[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnCardSelected(BuffCardUI clickedCard)
        {
            _selectedCard = clickedCard;

            foreach (var card in _cardUIList)
            {
                bool isTarget = (card == clickedCard);
                card.SetSelected(isTarget);
            }

            if (_confirmButton != null) _confirmButton.interactable = true;
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

            if (_panelObject != null) _panelObject.SetActive(false);

            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ConfirmMapBuffsAndStartInGame();
            }
        }

        private List<BuffData> GetRandomMapBuffs(int count)
        {
            List<BuffData> pool = new List<BuffData>(_mapBuffPool);
            List<BuffData> result = new List<BuffData>();

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, pool.Count);
                result.Add(pool[randomIndex]);
                pool.RemoveAt(randomIndex);
            }

            return result;
        }
    }
}