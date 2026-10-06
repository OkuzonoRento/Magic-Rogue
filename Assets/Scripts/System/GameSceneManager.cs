using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicRogue
{
    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance { get; private set; }

        [Header("ローグライク進行設定")]
        [SerializeField] private int totalStagesPerPhase = 5;

#if UNITY_EDITOR
        [Header("遷移シーンアタッチ（Editor用ドラッグ＆ドロップ）")]
        [SerializeField] private SceneAsset buffSelectSceneAsset;
        [SerializeField] private SceneAsset mapSelectSceneAsset;
        [SerializeField] private SceneAsset shopSceneAsset;
#endif

        [Header("デフォルト遷移シーン名設定（自動同期）")]
        [SerializeField] private string buffSelectSceneName = "02_GlobalBuffSelect";
        [SerializeField] private string mapSelectSceneName = "03_MapSelect";
        [SerializeField] private string shopSceneName = "06_Shop";

        [Header("選択中データ")]
        [SerializeField] private List<BuffData> selectedGlobalBuffs = new List<BuffData>();
        [SerializeField] private List<BuffData> selectedMapBuffs = new List<BuffData>();
        [SerializeField] private string selectedMapName;

        public MapData SelectedMapData { get; private set; }

        [Header("全バフデータベース")]
        [SerializeField] private List<BuffData> allBuffDatabase = new List<BuffData>();

        [Header("ショップ・回復設定")]
        [SerializeField] private float shopHealRatio = 0.3f;

        public int CurrentPhase { get; private set; } = 1;
        public int CurrentStageIndex { get; private set; } = 1;
        public MapType CurrentSelectedMapType { get; private set; } = MapType.Normal;
        public int TotalStagesPerPhase => totalStagesPerPhase;

        public SaveData CurrentSaveData { get; private set; } = new SaveData();

        private const string SAVE_KEY = "MAGIC_ROGUE_SUSPEND_SAVE";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSaveData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Inspector で SceneAsset が指定された場合、対応する文字列フィールドへ自動同期
            if (buffSelectSceneAsset != null) buffSelectSceneName = buffSelectSceneAsset.name;
            if (mapSelectSceneAsset != null) mapSelectSceneName = mapSelectSceneAsset.name;
            if (shopSceneAsset != null) shopSceneName = shopSceneAsset.name;
#endif
        }

        #region メインメニュー & 新規・再開

        public bool HasSavedGame() => PlayerPrefs.HasKey(SAVE_KEY);

        public void StartNewGame()
        {
            DeleteSaveData();

            CurrentPhase = 1;
            CurrentStageIndex = 1;
            selectedGlobalBuffs.Clear();
            selectedMapBuffs.Clear();
            selectedMapName = string.Empty;
            SelectedMapData = null;

            CurrentSaveData = new SaveData
            {
                currentPhase = 1,
                currentStageIndex = 1,
                stateType = GameStateType.InBuffSelection
            };

            ChangeSceneWithFade(buffSelectSceneName);
        }

        public void ResumeGame()
        {
            LoadSaveData();

            if (CurrentSaveData == null)
            {
                StartNewGame();
                return;
            }

            switch (CurrentSaveData.stateType)
            {
                case GameStateType.InBuffSelection:
                    ChangeSceneWithFade(buffSelectSceneName);
                    break;
                case GameStateType.InGame:
                    ChangeSceneWithFade(!string.IsNullOrEmpty(selectedMapName) ? selectedMapName : mapSelectSceneName);
                    break;
                case GameStateType.InShop:
                    ChangeSceneWithFade(shopSceneName);
                    break;
                default:
                    ChangeSceneWithFade(mapSelectSceneName);
                    break;
            }
        }

        #endregion

        #region バフ管理

        public void ToggleGlobalBuff(BuffData buff, bool isSelected)
        {
            if (buff == null) return;
            if (isSelected && !selectedGlobalBuffs.Contains(buff)) selectedGlobalBuffs.Add(buff);
            else if (!isSelected) selectedGlobalBuffs.Remove(buff);
        }

        public void ToggleMapBuff(BuffData buff, bool isSelected)
        {
            if (buff == null) return;
            if (isSelected && !selectedMapBuffs.Contains(buff)) selectedMapBuffs.Add(buff);
            else if (!isSelected) selectedMapBuffs.Remove(buff);
        }

        public void ApplyAllBuffsToPlayer(BuffHandler playerBuffHandler)
        {
            if (playerBuffHandler == null) return;

            foreach (var buff in selectedGlobalBuffs)
            {
                if (buff != null && buff.targetType == BuffTarget.Self)
                {
                    buff.ApplyBuff(playerBuffHandler);
                }
            }

            if (MapBuffManager.Instance != null)
            {
                MapBuffManager.Instance.SetAndApplyMapBuffs(selectedMapBuffs, playerBuffHandler);
            }
            else
            {
                foreach (var buff in selectedMapBuffs)
                {
                    if (buff != null && buff.targetType == BuffTarget.Self)
                    {
                        buff.ApplyBuff(playerBuffHandler);
                    }
                }
            }

            Debug.Log($"[GameSceneManager] Global:{selectedGlobalBuffs.Count}個, Map:{selectedMapBuffs.Count}個 のバフをプレイヤーに適用しました。");
        }

        #endregion

        #region マップ選択・インゲーム生成

        public void SelectMap(MapData mapData)
        {
            if (mapData == null) return;

            SelectedMapData = mapData;
            selectedMapName = mapData.mapName;
            CurrentSelectedMapType = mapData.mapType;
            selectedMapBuffs.Clear();

            if (MapBuffManager.Instance != null)
            {
                MapBuffManager.Instance.ResetMapBuffs();
            }

            SaveSelectionState();
        }

        public void SelectMap(string mapName, MapType mapType = MapType.Normal)
        {
            selectedMapName = mapName;
            CurrentSelectedMapType = mapType;
            selectedMapBuffs.Clear();

            if (MapBuffManager.Instance != null)
            {
                MapBuffManager.Instance.ResetMapBuffs();
            }

            SaveSelectionState();
        }

        public void ConfirmSelectionsAndStartInGame()
        {
            CurrentSaveData.stateType = GameStateType.InGame;
            CurrentSaveData.hasConfirmedSelections = true;
            SaveSelectionState();

            ChangeSceneWithFade(selectedMapName);
        }

        public void SpawnInGameStage()
        {
            if (SelectedMapData == null)
            {
                Debug.LogWarning("[GameSceneManager] SelectedMapData が設定されていません。");
                return;
            }

            MapController spawnedMapController = null;

            // 1. マップPrefab（ステージモデル全体）の配置
            if (SelectedMapData.mapStagePrefab != null)
            {
                GameObject mapObj = Instantiate(SelectedMapData.mapStagePrefab, Vector3.zero, Quaternion.identity);
                spawnedMapController = mapObj.GetComponent<MapController>();

                if (spawnedMapController != null)
                {
                    spawnedMapController.ActivateMap();
                }
            }

            // 2. プレイヤーのスポーン位置適用
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Vector3 spawnPos = Vector3.zero;
                Quaternion spawnRot = Quaternion.identity;

                if (spawnedMapController != null && spawnedMapController.PlayerSpawnPoint != null)
                {
                    spawnPos = spawnedMapController.PlayerSpawnPoint.position;
                    spawnRot = spawnedMapController.PlayerSpawnPoint.rotation;
                }
                else if (SelectedMapData != null)
                {
                    spawnPos = SelectedMapData.playerSpawnPosition;
                    spawnRot = Quaternion.Euler(SelectedMapData.playerSpawnRotation);
                }

                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = spawnPos;
                player.transform.rotation = spawnRot;

                if (cc != null) cc.enabled = true;
            }
        }

        #endregion

        #region シーン遷移 & セーブ/ロード

        public void ChangeScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void ChangeSceneWithFade(string sceneName, float duration = 0.5f)
        {
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeAndLoadScene(sceneName, duration);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void SaveSelectionState()
        {
            CurrentSaveData.currentPhase = CurrentPhase;
            CurrentSaveData.currentStageIndex = CurrentStageIndex;
            CurrentSaveData.selectedMapName = selectedMapName;

            CurrentSaveData.selectedGlobalBuffNames = selectedGlobalBuffs.ConvertAll(b => b != null ? b.buffName : "");
            CurrentSaveData.selectedMapBuffNames = selectedMapBuffs.ConvertAll(b => b != null ? b.buffName : "");

            string json = JsonUtility.ToJson(CurrentSaveData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public void LoadSaveData()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                CurrentSaveData = JsonUtility.FromJson<SaveData>(json);
                if (CurrentSaveData != null)
                {
                    CurrentPhase = CurrentSaveData.currentPhase;
                    CurrentStageIndex = CurrentSaveData.currentStageIndex;
                    selectedMapName = CurrentSaveData.selectedMapName;
                }
            }
        }

        public void DeleteSaveData()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                PlayerPrefs.DeleteKey(SAVE_KEY);
                PlayerPrefs.Save();
            }
        }

        #endregion
    }
}