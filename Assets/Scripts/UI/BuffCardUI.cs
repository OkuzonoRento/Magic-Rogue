using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MagicRogue
{
    public class BuffCardUI : MonoBehaviour
    {
        [Header("UIテキスト・アイコン")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Image _iconImage;

        [Header("羊皮紙カード演出設定")]
        [SerializeField] private Image _parchmentBackground;
        [SerializeField] private GameObject _selectedHighlight;
        [SerializeField] private CanvasGroup _mainCanvasGroup;
        [SerializeField] private Button _cardButton;

        [Header("演出パラメータ")]
        [SerializeField] private Vector3 _selectedScale = new Vector3(1.08f, 1.08f, 1.08f); // 選択/ホバー時の拡大
        [SerializeField] private float _scaleAnimationSpeed = 12f; // アニメーション速度
        [SerializeField] private Color _insufficientCreditColor = Color.red;
        [SerializeField] private float _insufficientCreditAlpha = 0.6f;

        private BuffData _data;
        private Action<BuffData, bool> _onToggleChanged;
        private Action<BuffCardUI> _onCardClickedCallback;

        private bool _isGlobalMode = true; // Global(複数選択) か Map(単一選択) かのモードフラグ
        private bool _isSelected = false;
        private bool _isDebuff = false;

        private Color _normalCostColor = Color.white;
        private Vector3 _targetScale = Vector3.one;

        private void Update()
        {
            // 毎フレーム目標サイズへ滑らかにスケーリング
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _scaleAnimationSpeed);
        }

        #region セットアップ

        /// <summary>
        /// 【Globalバフ用】トグル選択 & コスト計算あり
        /// </summary>
        public void SetupGlobal(BuffData data, bool isDebuff, bool canAfford, Action<BuffData, bool> onToggleChanged)
        {
            _isGlobalMode = true;
            _data = data;
            _onToggleChanged = onToggleChanged;
            _onCardClickedCallback = null;
            _isSelected = false;
            _isDebuff = isDebuff;

            SetCommonData(data);

            if (_costText != null)
            {
                _costText.gameObject.SetActive(true);
                string prefix = isDebuff ? "+" : "-";
                _costText.text = $"{prefix}{data.creditCost}";
            }

            UpdateAffordability(canAfford);
            UpdateVisual();
            BindButton();
        }

        /// <summary>
        /// 【Mapバフ用】コスト表示なし & クリックで選択（決定ボタンで確定）
        /// </summary>
        public void SetupMap(BuffData data, Action<BuffCardUI> onCardClickedCallback)
        {
            _isGlobalMode = false;
            _data = data;
            _onCardClickedCallback = onCardClickedCallback;
            _onToggleChanged = null;
            _isSelected = false;
            _isDebuff = false;

            SetCommonData(data);

            if (_costText != null) _costText.gameObject.SetActive(false);
            if (_mainCanvasGroup != null) _mainCanvasGroup.alpha = 1.0f;

            UpdateVisual();
            BindButton();
        }

        private void SetCommonData(BuffData data)
        {
            if (_costText != null && _normalCostColor == Color.white) _normalCostColor = _costText.color;

            if (_titleText != null && data != null) _titleText.text = data.buffName;
            if (_descriptionText != null && data != null) _descriptionText.text = data.description;

            if (_iconImage != null && data != null)
            {
                if (data.icon != null)
                {
                    _iconImage.sprite = data.icon;
                    _iconImage.gameObject.SetActive(true);
                }
                else
                {
                    _iconImage.gameObject.SetActive(false);
                }
            }
        }

        private void BindButton()
        {
            if (_cardButton == null) _cardButton = GetComponentInChildren<Button>();

            if (_cardButton != null)
            {
                _cardButton.onClick.RemoveAllListeners();
                _cardButton.onClick.AddListener(OnCardClicked);
            }
        }

        #endregion

        #region イベント・表示更新

        private void OnCardClicked()
        {
            if (_isGlobalMode)
            {
                // Globalバフ：トグル切り替え
                _isSelected = !_isSelected;
                UpdateVisual();
                _onToggleChanged?.Invoke(_data, _isSelected);
            }
            else
            {
                // Mapバフ：マネージャーへクリックイベントを通知
                _onCardClickedCallback?.Invoke(this);
            }
        }

        /// <summary>
        /// 外部（マネージャー）から明示的に選択状態を設定する
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisual();
        }

        public void UpdateAffordability(bool canAfford)
        {
            if (!_isGlobalMode) return;

            if (_isDebuff) canAfford = true;

            bool isUsable = _isSelected || canAfford;

            if (_costText != null)
            {
                _costText.color = isUsable ? _normalCostColor : _insufficientCreditColor;
            }

            if (_mainCanvasGroup != null)
            {
                _mainCanvasGroup.alpha = isUsable ? 1.0f : _insufficientCreditAlpha;
            }

            if (_cardButton != null)
            {
                _cardButton.interactable = isUsable;
            }
        }

        private void UpdateVisual()
        {
            if (_selectedHighlight != null)
            {
                _selectedHighlight.SetActive(_isSelected);
            }

            // 選択されているときに拡大アニメーション
            _targetScale = _isSelected ? _selectedScale : Vector3.one;
        }

        public BuffData GetData() => _data;
        public bool IsSelected() => _isSelected;

        #endregion
    }
}