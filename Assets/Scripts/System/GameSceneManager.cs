using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicRogue
{
    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance { get; private set; }

        [Header("選択データ")]
        [SerializeField] private List<BuffData> selectedGlobalBuffs = new List<BuffData>();
        [SerializeField] private List<BuffData> selectedMapBuffs = new List<BuffData>();
        [SerializeField] private string selectedMapName;

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

        #region バフ・マップ選択処理

        public void ToggleGlobalBuff(BuffData buff, bool isSelected)
        {
            if (buff == null) return;

            if (isSelected && !selectedGlobalBuffs.Contains(buff))
            {
                selectedGlobalBuffs.Add(buff);
            }
            else if (!isSelected && selectedGlobalBuffs.Contains(buff))
            {
                selectedGlobalBuffs.Remove(buff);
            }
        }

        public void SelectMap(string mapName)
        {
            selectedMapName = mapName;

            // マップ選択時に前マップの選択Mapバフをクリア
            selectedMapBuffs.Clear();

            // MapBuffManager 側の実バフもリセット
            if (MapBuffManager.Instance != null)
            {
                MapBuffManager.Instance.ResetMapBuffs();
            }
        }

        public void ToggleMapBuff(BuffData buff, bool isSelected)
        {
            if (buff == null) return;

            if (isSelected && !selectedMapBuffs.Contains(buff))
            {
                selectedMapBuffs.Add(buff);
            }
            else if (!isSelected && selectedMapBuffs.Contains(buff))
            {
                selectedMapBuffs.Remove(buff);
            }
        }

        public void ClearAllSelections()
        {
            selectedGlobalBuffs.Clear();
            selectedMapBuffs.Clear();
            selectedMapName = string.Empty;

            if (MapBuffManager.Instance != null)
            {
                MapBuffManager.Instance.ResetMapBuffs();
            }
        }

        #endregion

        #region ステージ適用＆シーン遷移

        /// <summary>
        /// ゲームステージ開始時、Global ＋ Map の全バフをプレイヤーに一括適用
        /// </summary>
        public void ApplyAllBuffsToPlayer(BuffHandler playerBuffHandler)
        {
            if (playerBuffHandler == null) return;

            // Globalバフの適用
            foreach (var buff in selectedGlobalBuffs)
            {
                if (buff != null) playerBuffHandler.AddBuff(buff.buffType, buff.value, 99999f);
            }

            // Mapバフの適用（MapBuffManager 経由で一括管理）
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

            Debug.Log($"[GameSceneManager] Global:{selectedGlobalBuffs.Count}個, Map:{selectedMapBuffs.Count}個 のバフをプレイヤーに適用しました。");
        }

        /// <summary>
        /// 敵全体に影響を与えるバフ/デバフ（EnemyStatUp / EnemyStatDown 等）を適用
        /// </summary>
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

        /// <summary>
        /// 選択されているマップシーンへ遷移
        /// </summary>
        public void LoadSelectedMapScene()
        {
            if (string.IsNullOrEmpty(selectedMapName))
            {
                Debug.LogError("[GameSceneManager] マップが選択されていません！");
                return;
            }

            SceneManager.LoadScene(selectedMapName);
        }

        /// <summary>
        /// 指定された名前のシーンへ直接遷移する（ショップ遷移用など）
        /// </summary>
        public void ChangeScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[GameSceneManager] 遷移先のシーン名が空です！");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        #endregion
    }
}