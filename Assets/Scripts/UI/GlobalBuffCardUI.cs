using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MagicRogue
{
    public class GlobalBuffCardUI : MonoBehaviour
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
        [SerializeField] private Vector3 _selectedScale = new Vector3(1.08f, 1.08f, 1.08f); // 選択時の拡大
        [SerializeField] private float _scaleAnimationSpeed = 12f; // アニメーション速度
        [SerializeField] private Color _insufficientCreditColor = Color.red;
        [SerializeField] private float _insufficientCreditAlpha = 0.6f;

        private BuffData _data;
        private Action<BuffData, bool> _onToggleChanged;
        private bool _isSelected = false;
        private bool _isDebuff = false;

        private Color _normalCostColor = Color.white;
        private Vector3 _targetScale = Vector3.one;

        private void Update()
        {
            // 毎フレーム目標サイズへ滑らかにスケーリング
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _scaleAnimationSpeed);
        }

        public void Setup(BuffData data, bool isDebuff, bool canAfford, Action<BuffData, bool> onToggleChanged)
        {
            _data = data;
            _onToggleChanged = onToggleChanged;
            _isSelected = false;
            _isDebuff = isDebuff;

            if (_costText != null && _normalCostColor == Color.white) _normalCostColor = _costText.color;

            // BuffData のフィールドに合わせて取得
            if (_titleText != null && data != null) _titleText.text = data.buffName;
            if (_descriptionText != null && data != null) _descriptionText.text = data.description;
            if (_iconImage != null && data != null && data.icon != null) _iconImage.sprite = data.icon;

            if (_costText != null && data != null)
            {
                string prefix = isDebuff ? "+" : "-";
                _costText.text = $"{prefix}{data.creditCost} Cr";
            }

            if (_cardButton == null) _cardButton = GetComponentInChildren<Button>();

            UpdateAffordability(canAfford);
            UpdateVisual();

            if (_cardButton != null)
            {
                _cardButton.onClick.RemoveAllListeners();
                _cardButton.onClick.AddListener(OnCardClicked);
            }
        }

        private void OnCardClicked()
        {
            _isSelected = !_isSelected;
            UpdateVisual();
            _onToggleChanged?.Invoke(_data, _isSelected);
        }

        public void UpdateAffordability(bool canAfford)
        {
            if (_isDebuff) canAfford = true;

            // 選択済みのカード、または購入可能（canAfford）なカードのみアクティブ表示
            bool isUsable = _isSelected || canAfford;

            if (_costText != null)
            {
                // 未選択かつクレジット不足のときだけ赤字化
                _costText.color = isUsable ? _normalCostColor : _insufficientCreditColor;
            }

            if (_mainCanvasGroup != null)
            {
                // 未選択かつクレジット不足のときだけ半透明化
                _mainCanvasGroup.alpha = isUsable ? 1.0f : _insufficientCreditAlpha;
            }

            if (_cardButton != null)
            {
                // 未選択かつクレジット不足のときはボタン操作を完全に無効化（新規選択を阻止）
                _cardButton.interactable = isUsable;
            }
        }

        private void UpdateVisual()
        {
            if (_selectedHighlight != null)
            {
                _selectedHighlight.SetActive(_isSelected);
            }

            // 目標スケールを切り替えて Update で補間
            _targetScale = _isSelected ? _selectedScale : Vector3.one;
        }

        public BuffData GetData() => _data;
        public bool IsSelected() => _isSelected;
    }
}