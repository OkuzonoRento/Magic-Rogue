using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicRogue
{
    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance { get; private set; }

        [Header("選択中データ")]
        [SerializeField] private List<BuffData> selectedGlobalBuffs = new List<BuffData>();
        [SerializeField] private List<BuffData> selectedMapBuffs = new List<BuffData>();
        [SerializeField] private string selectedMapName;

        [Header("全バフデータベース")]
        [SerializeField] private List<BuffData> allBuffDatabase = new List<BuffData>();

        [Header("ショップ・回復設定")]
        [SerializeField] private float shopHealRatio = 0.3f; // ショップ到達時に最大HPの30%回復

        // 中断データ管理
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

        #region 新規ゲーム開始 & 中断判定

        /// <summary>
        /// 中断データ（続きから）が存在するか
        /// </summary>
        public bool HasSavedGame()
        {
            return CurrentSaveData != null && CurrentSaveData.stateType != GameStateType.None;
        }

        /// <summary>
        /// 【新規ゲーム開始】グローバルバフや選択情報を完全リセットしてシーン遷移
        /// </summary>
        public void StartNewGame()
        {
            selectedGlobalBuffs.Clear();
            selectedMapBuffs.Clear();
            selectedMapName = string.Empty;

            CurrentSaveData = new SaveData
            {
                stateType = GameStateType.InBuffSelection,
                randomSeed = Random.Range(int.MinValue, int.MaxValue), // 厳選対策用Seed生成
                hasConfirmedSelections = false,
                currentHealth = 100f, // 初期HP
                maxHealth = 100f,
                playerGold = 0
            };

            // 乱数シードを適用
            Random.InitState(CurrentSaveData.randomSeed);

            SaveDataToDisk();
            ChangeSceneWithFade("GlobalBuffSelect");
        }

        /// <summary>
        /// 【続きから】中断した状態から再開
        /// </summary>
        public void ResumeGame()
        {
            if (!HasSavedGame()) return;

            // 復元処理
            RestoreFromSaveData();

            switch (CurrentSaveData.stateType)
            {
                case GameStateType.InBuffSelection:
                    Random.InitState(CurrentSaveData.randomSeed);
                    ChangeSceneWithFade("GlobalBuffSelect");
                    break;

                case GameStateType.InGame:
                    LoadSelectedMapScene();
                    break;

                case GameStateType.InShop:
                    ChangeSceneWithFade("Shop");
                    break;
            }
        }

        #endregion

        #region バフ・マップ選択＆厳選対策

        public void ToggleGlobalBuff(BuffData buff, bool isSelected)
        {
            if (buff == null) return;

            if (isSelected && !selectedGlobalBuffs.Contains(buff))
                selectedGlobalBuffs.Add(buff);
            else if (!isSelected)
                selectedGlobalBuffs.Remove(buff);

            SaveSelectionState();
        }

        public bool IsGlobalBuffSelected(BuffData buff)
        {
            return selectedGlobalBuffs.Contains(buff);
        }

        public void SelectMap(string mapName)
        {
            selectedMapName = mapName;
            selectedMapBuffs.Clear();

            if (MapBuffManager.Instance != null)
            {
                MapBuffManager.Instance.ResetMapBuffs();
            }

            SaveSelectionState();
        }

        public void ToggleMapBuff(BuffData buff, bool isSelected)
        {
            if (buff == null) return;

            if (isSelected && !selectedMapBuffs.Contains(buff))
                selectedMapBuffs.Add(buff);
            else if (!isSelected)
                selectedMapBuffs.Remove(buff);

            SaveSelectionState();
        }

        /// <summary>
        /// インゲーム開始ボタンを押した時（選択確定時）
        /// </summary>
        public void ConfirmSelectionsAndStartInGame()
        {
            CurrentSaveData.stateType = GameStateType.InGame;
            CurrentSaveData.hasConfirmedSelections = true; // 選び直し防止フラグ
            SaveSelectionState();

            LoadSelectedMapScene();
        }

        #endregion

        #region ショップ & リザルト＆回復処理

        public void TransitionToShop(float currentHp, float maxHp, int gold, List<string> inventory)
        {
            CurrentSaveData.stateType = GameStateType.InShop;
            CurrentSaveData.maxHealth = maxHp;
            CurrentSaveData.playerGold = gold;
            CurrentSaveData.inventoryItemIDs = new List<string>(inventory);

            // HPを一定割合回復（最大HPの shopHealRatio %）
            float healAmount = maxHp * shopHealRatio;
            CurrentSaveData.currentHealth = Mathf.Min(maxHp, currentHp + healAmount);

            // マップバフ・マップ選択はクリアし、グローバルバフのみ保持
            selectedMapBuffs.Clear();
            selectedMapName = string.Empty;

            SaveSelectionState();
            ChangeSceneWithFade("Shop");
        }

        public void LeaveShopToMapSelect()
        {
            CurrentSaveData.stateType = GameStateType.InBuffSelection;
            CurrentSaveData.randomSeed = Random.Range(int.MinValue, int.MaxValue);
            Random.InitState(CurrentSaveData.randomSeed);

            SaveSelectionState();
            ChangeSceneWithFade("MapSelect");
        }

        public void ClearSaveDataOnResult()
        {
            CurrentSaveData = new SaveData();
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
        }

        #endregion

        #region バフ適用処理（プレイヤー・敵）

        public void ApplyAllBuffsToPlayer(BuffHandler playerBuffHandler)
        {
            if (playerBuffHandler == null) return;

            foreach (var buff in selectedGlobalBuffs)
            {
                if (buff != null) playerBuffHandler.AddBuff(buff.buffType, buff.value, 99999f);
            }

            if (MapBuffManager.Instance != null)
            {
                MapBuffManager.Instance.SetAndApplyMapBuffs(selectedMapBuffs, playerBuffHandler);
            }
            else
            {
                foreach (var buff in selectedMapBuffs)
                {
                    if (buff != null) playerBuffHandler.AddBuff(buff.buffType, buff.value, 99999f);
                }
            }

            Debug.Log($"[GameSceneManager] Global:{selectedGlobalBuffs.Count}個, Map:{selectedMapBuffs.Count}個 のバフを適用しました。");
        }

        public void ApplyAllBuffsToEnemy(BuffHandler enemyBuffHandler)
        {
            if (enemyBuffHandler == null) return;

            foreach (var buff in selectedGlobalBuffs)
            {
                if (buff != null && (buff.buffType == BuffType.EnemyStatUp || buff.buffType == BuffType.EnemyStatDown))
                {
                    enemyBuffHandler.AddBuff(buff.buffType, buff.value, 99999f);
                }
            }

            foreach (var buff in selectedMapBuffs)
            {
                if (buff != null && (buff.buffType == BuffType.EnemyStatUp || buff.buffType == BuffType.EnemyStatDown))
                {
                    enemyBuffHandler.AddBuff(buff.buffType, buff.value, 99999f);
                }
            }
        }

        #endregion

        #region シーン遷移 ＆ フェード処理

        /// <summary>
        /// 互換用：外部（StageClearPortal等）から呼ばれるシーン遷移
        /// </summary>
        public void ChangeScene(string sceneName)
        {
            ChangeSceneWithFade(sceneName);
        }

        /// <summary>
        /// フェードアウト → シーンロード → フェードイン を一貫して行う
        /// </summary>
        public void ChangeSceneWithFade(string sceneName, float fadeDuration = 0.5f)
        {
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeAndLoadScene(sceneName, fadeDuration);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        /// <summary>
        /// 選択されたマップシーンへの遷移
        /// </summary>
        public void LoadSelectedMapScene(float fadeDuration = 0.5f)
        {
            if (!string.IsNullOrEmpty(selectedMapName))
            {
                ChangeSceneWithFade(selectedMapName, fadeDuration);
            }
        }

        #endregion

        #region セーブデータ保存・同期処理

        private void SaveSelectionState()
        {
            CurrentSaveData.selectedMapName = selectedMapName;
            CurrentSaveData.selectedGlobalBuffNames = selectedGlobalBuffs.ConvertAll(b => b.name);
            CurrentSaveData.selectedMapBuffNames = selectedMapBuffs.ConvertAll(b => b.name);

            SaveDataToDisk();
        }

        private void SaveDataToDisk()
        {
            string json = JsonUtility.ToJson(CurrentSaveData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadSaveData()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                CurrentSaveData = JsonUtility.FromJson<SaveData>(json);
                RestoreFromSaveData();
            }
            else
            {
                CurrentSaveData = new SaveData();
            }
        }

        private void RestoreFromSaveData()
        {
            if (CurrentSaveData == null) return;

            selectedMapName = CurrentSaveData.selectedMapName;
            selectedGlobalBuffs = RestoreBuffsFromNames(CurrentSaveData.selectedGlobalBuffNames);
            selectedMapBuffs = RestoreBuffsFromNames(CurrentSaveData.selectedMapBuffNames);
        }

        private List<BuffData> RestoreBuffsFromNames(List<string> names)
        {
            List<BuffData> list = new List<BuffData>();
            foreach (var name in names)
            {
                BuffData found = allBuffDatabase.Find(b => b != null && b.name == name);
                if (found != null) list.Add(found);
            }
            return list;
        }

        #endregion
    }
}