using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MagicRogue
{
    public class MapBuffSelectionUI : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private GameObject _panelObject;
        [SerializeField] private BuffCardUI[] _cardUIList; // 3枚のBuffCardUI
        [SerializeField] private Button _confirmButton;   // 決定ボタン

        [Header("Buff Database")]
        [Tooltip("Mapバフのプール（MapBuffDataGeneratorで生成したアセット群）")]
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

        /// <summary>
        /// マップ決定時・ゲーム開始時に呼び出して3択画面を開く
        /// </summary>
        public void OpenSelectionUI()
        {
            if (_panelObject != null) _panelObject.SetActive(true);
            Time.timeScale = 0f;

            _selectedCard = null;

            // 初期状態では1つも選ばれていないため決定ボタンを押不可にする
            if (_confirmButton != null) _confirmButton.interactable = false;

            // プールから3つ抽出
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

        /// <summary>
        /// カードがクリックされた時（単一選択・アニメーション適用）
        /// </summary>
        private void OnCardSelected(BuffCardUI clickedCard)
        {
            _selectedCard = clickedCard;

            // 3つのカードのうち、押されたものだけを選択（拡大・ハイライト）にし、他を解除する
            foreach (var card in _cardUIList)
            {
                bool isTarget = (card == clickedCard);
                card.SetSelected(isTarget);
            }

            // 1つ選択されたので決定ボタンを有効化
            if (_confirmButton != null) _confirmButton.interactable = true;
        }

        /// <summary>
        /// 「決定ボタン」を押したときの処理
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            if (_selectedCard == null) return;

            Time.timeScale = 1f;

            BuffData chosenBuff = _selectedCard.GetData();
            if (chosenBuff != null && GameSceneManager.Instance != null)
            {
                // 選択されたバフをGameSceneManagerに登録
                GameSceneManager.Instance.ToggleMapBuff(chosenBuff, true);
                Debug.Log($"[MapBuffSelectionUI] 決定されたMapバフ: {chosenBuff.buffName}");
            }

            if (_panelObject != null) _panelObject.SetActive(false);

            // インゲームへ遷移・開始
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ConfirmSelectionsAndStartInGame();
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