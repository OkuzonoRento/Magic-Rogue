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
            selectedMapBuffs.Clear();
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
        }

        #endregion

        #region ステージ適用＆シーン遷移

        /// <summary>
        /// ゲームステージ開始時、Global ＋ Map の全バフを一括適用
        /// </summary>
        public void ApplyAllBuffsToPlayer(BuffHandler playerBuffHandler)
        {
            if (playerBuffHandler == null) return;

            foreach (var buff in selectedGlobalBuffs)
            {
                if (buff != null) playerBuffHandler.AddBuff(buff.buffType, buff.value, 99999f);
            }

            foreach (var buff in selectedMapBuffs)
            {
                if (buff != null) playerBuffHandler.AddBuff(buff.buffType, buff.value, 99999f);
            }

            Debug.Log($"[GameSceneManager] Global:{selectedGlobalBuffs.Count}個, Map:{selectedMapBuffs.Count}個 のバフを適用しました。");
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

        #endregion
    }
}