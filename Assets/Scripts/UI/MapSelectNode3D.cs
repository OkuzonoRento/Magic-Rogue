using System;
using UnityEngine;

namespace MagicRogue
{
    public class MapSelectNode3D : MonoBehaviour
    {
        [Header("回転設定")]
        [SerializeField] private Vector3 _rotationSpeed = new Vector3(0, 20, 0);

        [Header("演出設定")]
        [SerializeField] private float _hoverScaleMultiplier = 1.1f;
        [SerializeField] private float _selectedScaleMultiplier = 1.3f;
        [SerializeField] private float _animationSpeed = 10f;
        [SerializeField] private float _selectedYOffset = 0.6f;

        private MapData _mapData;
        private Action<MapSelectNode3D, MapData> _onClickedCallback;
        private bool _isSelected = false;
        private bool _isHovered = false;

        private Vector3 _baseScale;
        private Vector3 _basePosition;

        private void Start()
        {
            _baseScale = transform.localScale;
            _basePosition = transform.localPosition;
        }

        public void Setup(MapData data, Action<MapSelectNode3D, MapData> onClicked)
        {
            _mapData = data;
            _onClickedCallback = onClicked;
        }

        public void SetHoverState(bool isHovered)
        {
            _isHovered = isHovered;
        }

        public void SetSelectedState(bool isSelected)
        {
            _isSelected = isSelected;
        }

        private void Update()
        {
            // ゆっくり回転
            transform.Rotate(_rotationSpeed * Time.deltaTime);

            // ホバー＆選択時のアニメーション演出
            float targetScaleMultiplier = 1f;
            float targetYOffset = 0f;

            if (_isSelected)
            {
                targetScaleMultiplier = _selectedScaleMultiplier;
                targetYOffset = _selectedYOffset;
            }
            else if (_isHovered)
            {
                targetScaleMultiplier = _hoverScaleMultiplier;
            }

            Vector3 targetScale = _baseScale * targetScaleMultiplier;
            Vector3 targetPos = _basePosition + new Vector3(0, targetYOffset, 0);

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * _animationSpeed);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * _animationSpeed);
        }
    }
}