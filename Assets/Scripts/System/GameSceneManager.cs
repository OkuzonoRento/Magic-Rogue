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
        [SerializeField] private SceneAsset mainMenuSceneAsset;
        [SerializeField] private SceneAsset globalBuffSelectSceneAsset;
        [SerializeField] private SceneAsset mapSelectSceneAsset;
        [SerializeField] private SceneAsset mapBuffSelectSceneAsset;
        [SerializeField] private SceneAsset inGameSceneAsset;
        [SerializeField] private SceneAsset shopSceneAsset;
#endif

        [Header("デフォルト遷移シーン名設定（自動同期）")]
        [SerializeField] private string mainMenuSceneName = "01_MainMenu";
        [SerializeField] private string globalBuffSelectSceneName = "02_GlobalBuffSelect";
        [SerializeField] private string mapSelectSceneName = "03_MapSelect";
        [SerializeField] private string mapBuffSelectSceneName = "04_MapBuffSelect";
        [SerializeField] private string inGameSceneName = "05_InGame";
        [SerializeField] private string shopSceneName = "06_Shop";

        [Header("選択中データ")]
        [SerializeField] private List<BuffData> selectedGlobalBuffs = new List<BuffData>();
        [SerializeField] private List<BuffData> selectedMapBuffs = new List<BuffData>();
        [SerializeField] private string selectedMapName;

        public MapData SelectedMapData { get; private set; }
        public int CurrentPhase { get; private set; } = 1;
        public int CurrentStageIndex { get; private set; } = 1;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (mainMenuSceneAsset != null) mainMenuSceneName = mainMenuSceneAsset.name;
            if (globalBuffSelectSceneAsset != null) globalBuffSelectSceneName = globalBuffSelectSceneAsset.name;
            if (mapSelectSceneAsset != null) mapSelectSceneName = mapSelectSceneAsset.name;
            if (mapBuffSelectSceneAsset != null) mapBuffSelectSceneName = mapBuffSelectSceneAsset.name;
            if (inGameSceneAsset != null) inGameSceneName = inGameSceneAsset.name;
            if (shopSceneAsset != null) shopSceneName = shopSceneAsset.name;
#endif
        }

        #region シーン進行メソッド（すべてFade付きで統一）

        /// <summary> 1. メインメニュー → グローバルバフ選択へ </summary>
        public void StartNewGame()
        {
            CurrentPhase = 1;
            CurrentStageIndex = 1;
            selectedGlobalBuffs.Clear();
            selectedMapBuffs.Clear();
            selectedMapName = string.Empty;
            SelectedMapData = null;

            ChangeSceneWithFade(globalBuffSelectSceneName);
        }

        /// <summary> 2. グローバルバフ決定 → マップ選択へ </summary>
        public void ConfirmGlobalBuffsAndGoToMapSelect()
        {
            ChangeSceneWithFade(mapSelectSceneName);
        }

        /// <summary> 3. マップ決定 → マップバフ選択へ </summary>
        public void SelectMapAndGoToMapBuffSelect(MapData mapData)
        {
            if (mapData != null)
            {
                SelectedMapData = mapData;
                selectedMapName = mapData.mapName;
            }

            ChangeSceneWithFade(mapBuffSelectSceneName);
        }

        /// <summary> 4. マップバフ決定 → インゲーム開始へ </summary>
        public void ConfirmMapBuffsAndStartInGame()
        {
            ChangeSceneWithFade(inGameSceneName);
        }

        /// <summary> 5. インゲームクリア（ポータル到達） → ショップへ </summary>
        public void OnStageCleared()
        {
            CurrentStageIndex++;
            ChangeSceneWithFade(shopSceneName);
        }

        /// <summary> 6. ショップ退出 → 次のマップ選択へ </summary>
        public void ExitShopAndGoToMapSelect()
        {
            ChangeSceneWithFade(mapSelectSceneName);
        }

        #endregion

        #region バフ操作 & 適用

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

            foreach (var buff in selectedMapBuffs)
            {
                if (buff != null && buff.targetType == BuffTarget.Self)
                {
                    buff.ApplyBuff(playerBuffHandler);
                }
            }
        }

        #endregion

        #region シーン遷移コアヘルパー

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

        #endregion
    }
}