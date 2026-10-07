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

        [Header("演出パラメータ (Globalと同様)")]
        [SerializeField] private Vector3 _selectedScale = new Vector3(1.08f, 1.08f, 1.08f);
        [SerializeField] private float _scaleAnimationSpeed = 12f;
        [SerializeField] private Color _insufficientCreditColor = Color.red;
        [SerializeField] private float _insufficientCreditAlpha = 0.6f;

        private BuffData _data;
        private Action<BuffData, bool> _onToggleChanged;
        private Action<BuffCardUI> _onCardClickedCallback;

        private bool _isGlobalMode = true;
        private bool _isSelected = false;
        private bool _isDebuff = false;

        private Color _normalCostColor = Color.white;
        private Vector3 _targetScale = Vector3.one;

        private void Update()
        {
            // ★ Time.unscaledDeltaTime を使用することで Time.timeScale = 0f であっても滑らかにアニメーションする
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * _scaleAnimationSpeed);
        }

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

        private void OnCardClicked()
        {
            if (_isGlobalMode)
            {
                _isSelected = !_isSelected;
                UpdateVisual();
                _onToggleChanged?.Invoke(_data, _isSelected);
            }
            else
            {
                _onCardClickedCallback?.Invoke(this);
            }
        }

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

            _targetScale = _isSelected ? _selectedScale : Vector3.one;
        }

        public BuffData GetData() => _data;
        public bool IsSelected() => _isSelected;
    }
}