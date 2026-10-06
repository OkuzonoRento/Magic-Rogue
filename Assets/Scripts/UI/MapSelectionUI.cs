using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MagicRogue
{
    public class MapSelectionUI : MonoBehaviour
    {
        [Header("1Phaseあたりの総Stage数")]
        [SerializeField] private int _totalStagesPerPhase = 5;

        [Header("UI透明ボタン（3Dマップ選択用）")]
        [SerializeField] private Button[] _nodeButtons = new Button[3];

        [Header("3Dマップノード配置用Transform")]
        [SerializeField] private Transform[] _nodeSpawnPoints = new Transform[3];

        [Header("マッププール設定")]
        [SerializeField] private List<MapData> _availableNormalMaps = new List<MapData>();
        [SerializeField] private List<MapData> _availableEventMaps = new List<MapData>();
        [SerializeField] private MapData _bossMapData;

        [Header("決定ボタン（任意）")]
        [SerializeField] private Button _confirmButton;

        [Header("上部ロードマップUI")]
        [SerializeField] private Transform _progressContainer;
        [SerializeField] private GameObject _progressIconPrefab;
        [SerializeField] private Sprite _playerFaceSprite;

        [Header("ロードマップ用アイコン素材設定")]
        [SerializeField] private Sprite _firstNormalSprite;
        [SerializeField] private Sprite _normalWithLineSprite;
        [SerializeField] private Sprite _eventWithLineSprite;
        [SerializeField] private Sprite _bossWithLineSprite;

        [Header("重なり・サイズ調整")]
        [SerializeField] private float _overlapSpacing = -20f;
        [SerializeField] private float _playerIconScale = 0.5f;

        private MapSelectNode3D _currentlySelectedNode;
        private MapData _currentlySelectedMap;

        private readonly List<MapSelectNode3D> _spawnedNodes = new List<MapSelectNode3D>();

        private void Start()
        {
            if (_confirmButton != null)
            {
                _confirmButton.interactable = false;
                _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }

            BuildTopProgressBar();
            GenerateMapOptions();
        }

        #region 上部ロードマップUI

        private void BuildTopProgressBar()
        {
            if (_progressContainer == null || _progressIconPrefab == null) return;

            SetupProgressLayout();

            foreach (Transform child in _progressContainer)
            {
                Destroy(child.gameObject);
            }

            int currentStage = (GameSceneManager.Instance != null) ? GameSceneManager.Instance.CurrentStageIndex : 1;
            int midStage = Mathf.CeilToInt(_totalStagesPerPhase / 2.0f);

            for (int i = 1; i <= _totalStagesPerPhase; i++)
            {
                GameObject iconObj = Instantiate(_progressIconPrefab, _progressContainer);
                Image img = iconObj.GetComponent<Image>();

                int baseSortingOrder = (_totalStagesPerPhase - i + 1) * 10;

                if (img != null)
                {
                    img.raycastTarget = false;

                    if (i == 1)
                    {
                        if (_firstNormalSprite != null) img.sprite = _firstNormalSprite;
                    }
                    else if (i == _totalStagesPerPhase)
                    {
                        if (_bossWithLineSprite != null) img.sprite = _bossWithLineSprite;
                    }
                    else if (i == midStage)
                    {
                        if (_eventWithLineSprite != null) img.sprite = _eventWithLineSprite;
                    }
                    else
                    {
                        if (_normalWithLineSprite != null) img.sprite = _normalWithLineSprite;
                    }

                    img.color = (i < currentStage) ? new Color(0.6f, 0.6f, 0.6f, 1f) : Color.white;
                    img.SetNativeSize();

                    if (!iconObj.TryGetComponent<LayoutElement>(out var layoutElement))
                    {
                        layoutElement = iconObj.AddComponent<LayoutElement>();
                    }
                    layoutElement.preferredWidth = img.rectTransform.rect.width;
                    layoutElement.preferredHeight = img.rectTransform.rect.height;
                }

                if (!iconObj.TryGetComponent<Canvas>(out var nodeCanvas))
                {
                    nodeCanvas = iconObj.AddComponent<Canvas>();
                }
                nodeCanvas.overrideSorting = true;
                nodeCanvas.sortingOrder = baseSortingOrder;

                SetNodeText(iconObj, i, midStage);
                UpdatePlayerIconOnNode(iconObj, i == currentStage, baseSortingOrder + 5);
            }
        }

        private void SetupProgressLayout()
        {
            if (!_progressContainer.TryGetComponent<HorizontalLayoutGroup>(out var layout))
            {
                layout = _progressContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = _overlapSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (!_progressContainer.TryGetComponent<ContentSizeFitter>(out var fitter))
            {
                fitter = _progressContainer.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private void SetNodeText(GameObject iconObj, int stageIndex, int midStage)
        {
            string labelText = "";
            if (stageIndex == _totalStagesPerPhase) labelText = "BOSS";
            else if (stageIndex == midStage) labelText = "EVENT";
            else labelText = $"St.{stageIndex}";

            if (iconObj.TryGetComponent<TextMeshProUGUI>(out var text))
            {
                text.text = labelText;
                text.raycastTarget = false;
            }
            else
            {
                var childText = iconObj.GetComponentInChildren<TextMeshProUGUI>();
                if (childText != null)
                {
                    childText.text = labelText;
                    childText.raycastTarget = false;
                }
            }
        }

        private void UpdatePlayerIconOnNode(GameObject iconObj, bool isCurrentStage, int sortingOrder)
        {
            Transform playerIconTransform = iconObj.transform.Find("PlayerIcon");

            if (playerIconTransform == null && isCurrentStage)
            {
                GameObject playerObj = new GameObject("PlayerIcon", typeof(RectTransform), typeof(Image));
                playerObj.transform.SetParent(iconObj.transform, false);
                playerIconTransform = playerObj.transform;

                RectTransform rect = playerObj.GetComponent<RectTransform>();
                rect.anchoredPosition = Vector2.zero;
            }

            if (playerIconTransform != null)
            {
                playerIconTransform.gameObject.SetActive(isCurrentStage);

                if (isCurrentStage)
                {
                    if (playerIconTransform.TryGetComponent<Image>(out var playerImage))
                    {
                        playerImage.raycastTarget = false;
                        if (_playerFaceSprite != null)
                        {
                            playerImage.sprite = _playerFaceSprite;
                            playerImage.SetNativeSize();

                            RectTransform parentRect = iconObj.GetComponent<RectTransform>();
                            if (parentRect != null)
                            {
                                float targetSize = parentRect.rect.height * _playerIconScale;
                                playerImage.rectTransform.sizeDelta = new Vector2(targetSize, targetSize);
                            }
                        }
                    }

                    if (!playerIconTransform.TryGetComponent<Canvas>(out var playerCanvas))
                    {
                        playerCanvas = playerIconTransform.gameObject.AddComponent<Canvas>();
                    }
                    playerCanvas.overrideSorting = true;
                    playerCanvas.sortingOrder = sortingOrder;
                }
            }
        }

        #endregion

        #region 3Dマップ選択肢の生成

        private void GenerateMapOptions()
        {
            foreach (var node in _spawnedNodes)
            {
                if (node != null) Destroy(node.gameObject);
            }
            _spawnedNodes.Clear();
            _currentlySelectedNode = null;
            _currentlySelectedMap = null;

            int currentStage = (GameSceneManager.Instance != null) ? GameSceneManager.Instance.CurrentStageIndex : 1;
            int midStage = Mathf.CeilToInt(_totalStagesPerPhase / 2.0f);

            List<MapData> selectedMaps = new List<MapData>();

            if (currentStage >= _totalStagesPerPhase)
            {
                int bossIndex = Random.Range(0, _nodeSpawnPoints.Length);
                List<MapData> normalPicksForBoss = GetRandomUniqueMaps(_availableNormalMaps, 2);
                int normalIdx = 0;

                for (int i = 0; i < _nodeSpawnPoints.Length; i++)
                {
                    if (i == bossIndex)
                    {
                        selectedMaps.Add(_bossMapData);
                    }
                    else if (normalIdx < normalPicksForBoss.Count)
                    {
                        selectedMaps.Add(normalPicksForBoss[normalIdx]);
                        normalIdx++;
                    }
                }
            }
            else if (currentStage == midStage)
            {
                if (_availableEventMaps != null && _availableEventMaps.Count > 0)
                {
                    selectedMaps.Add(_availableEventMaps[Random.Range(0, _availableEventMaps.Count)]);
                }

                int needNormal = 3 - selectedMaps.Count;
                selectedMaps.AddRange(GetRandomUniqueMaps(_availableNormalMaps, needNormal));
                ShuffleList(selectedMaps);
            }
            else
            {
                selectedMaps = GetRandomUniqueMaps(_availableNormalMaps, 3);
            }

            for (int i = 0; i < selectedMaps.Count && i < _nodeSpawnPoints.Length; i++)
            {
                Button uiButton = (i < _nodeButtons.Length) ? _nodeButtons[i] : null;
                SpawnMapNode(selectedMaps[i], _nodeSpawnPoints[i], uiButton);
            }
        }

        private void SpawnMapNode(MapData data, Transform spawnPoint, Button uiButton)
        {
            if (data == null || data.model3DPrefab == null || spawnPoint == null) return;

            GameObject modelObj = Instantiate(data.model3DPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);

            if (!modelObj.TryGetComponent<MapSelectNode3D>(out var node3D))
            {
                node3D = modelObj.AddComponent<MapSelectNode3D>();
            }

            if (node3D != null)
            {
                node3D.Setup(data, OnMapNodeClicked);
                _spawnedNodes.Add(node3D);

                if (uiButton != null)
                {
                    uiButton.onClick.RemoveAllListeners();
                    uiButton.onClick.AddListener(() =>
                    {
                        OnMapNodeClicked(node3D, data);
                    });

                    SetupButtonHoverTrigger(uiButton, node3D);
                }
            }
        }

        private void SetupButtonHoverTrigger(Button button, MapSelectNode3D node3D)
        {
            if (!button.TryGetComponent<EventTrigger>(out var trigger))
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }
            trigger.triggers.Clear();

            var entryHover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryHover.callback.AddListener((_) => node3D.SetHoverState(true));
            trigger.triggers.Add(entryHover);

            var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener((_) => node3D.SetHoverState(false));
            trigger.triggers.Add(entryExit);
        }

        private List<MapData> GetRandomUniqueMaps(List<MapData> sourceList, int count)
        {
            List<MapData> pool = new List<MapData>(sourceList);
            List<MapData> result = new List<MapData>();

            int pickCount = Mathf.Min(count, pool.Count);
            for (int i = 0; i < pickCount; i++)
            {
                int randomIndex = Random.Range(0, pool.Count);
                result.Add(pool[randomIndex]);
                pool.RemoveAt(randomIndex);
            }
            return result;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        #endregion

        #region ノード選択 & 決定

        private void OnMapNodeClicked(MapSelectNode3D clickedNode, MapData selectedData)
        {
            // 同じノードオブジェクトが再度押された場合のみ選択解除
            if (_currentlySelectedNode == clickedNode)
            {
                _currentlySelectedNode = null;
                _currentlySelectedMap = null;

                foreach (var node in _spawnedNodes)
                {
                    if (node != null)
                    {
                        node.SetSelectedState(false);
                    }
                }

                if (_confirmButton != null)
                {
                    _confirmButton.interactable = false;
                }

                return;
            }

            // 新しいノード（または別のノード）を選択する場合
            _currentlySelectedNode = clickedNode;
            _currentlySelectedMap = selectedData;

            foreach (var node in _spawnedNodes)
            {
                if (node != null)
                {
                    node.SetSelectedState(node == clickedNode);
                }
            }

            if (_confirmButton != null)
            {
                _confirmButton.interactable = true;
            }
        }

        private void OnConfirmButtonClicked()
        {
            if (_currentlySelectedMap == null) return;

            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.SelectMap(_currentlySelectedMap);
            }
        }

        #endregion
    }
}