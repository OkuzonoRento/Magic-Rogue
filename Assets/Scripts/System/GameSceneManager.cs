using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicRogue
{
    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance { get; private set; }

        // --- 既存フィールドそのまま ---
        [SerializeField] private List<BuffData> selectedGlobalBuffs = new List<BuffData>();
        [SerializeField] private List<BuffData> selectedMapBuffs = new List<BuffData>();

        // (中略)

        /// <summary>
        /// 選択されたグローバルバフおよびマップバフを対象のBuffHandler（主にプレイヤー）へまとめて適用します
        /// </summary>
        public void ApplyAllBuffsToPlayer(BuffHandler playerBuffHandler)
        {
            if (playerBuffHandler == null) return;

            // 1. グローバルバフの適用 (自身対象のもの)
            foreach (var buff in selectedGlobalBuffs)
            {
                if (buff != null && buff.targetType == BuffTarget.Self)
                {
                    buff.ApplyBuff(playerBuffHandler);
                }
            }

            // 2. マップバフの適用 (MapBuffManager 経由または直接)
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

            Debug.Log($"[GameSceneManager] プレイヤーに Global/Map バフを一括適用しました。");
        }

        // --- 既存のメソッド（StartNewGame, ResumeGame, ConfirmSelectionsAndStartInGame など）はそのまま運用 ---
    }
}