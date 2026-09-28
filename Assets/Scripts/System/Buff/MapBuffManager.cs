using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    public class MapBuffManager : MonoBehaviour
    {
        public static MapBuffManager Instance { get; private set; }

        [Header("現在アクティブなMapバフ一覧（確認用）")]
        [SerializeField] private List<BuffData> activeMapBuffs = new List<BuffData>();

        private readonly List<BuffHandler> registeredHandlers = new List<BuffHandler>();

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

        /// <summary>
        /// マップ移動・切り替え時に呼び出し、前マップのバフを一括リセット（削除）します
        /// </summary>
        public void ResetMapBuffs()
        {
            foreach (var handler in registeredHandlers)
            {
                if (handler == null) continue;

                foreach (var buff in activeMapBuffs)
                {
                    if (buff != null)
                    {
                        handler.ClearBuffsOfType(buff.buffType);
                    }
                }
            }

            registeredHandlers.Clear();
            activeMapBuffs.Clear();

            Debug.Log("[MapBuffManager] マップ切り替えに伴い、全てのMapバフをリセットしました。");
        }

        /// <summary>
        /// 選択されたMapバフ（BuffDataアセット）を一括適用します
        /// </summary>
        public void SetAndApplyMapBuffs(List<BuffData> newBuffs, BuffHandler targetHandler)
        {
            ResetMapBuffs();

            if (newBuffs != null)
            {
                activeMapBuffs = new List<BuffData>(newBuffs);
            }

            ApplyActiveBuffsTo(targetHandler);
        }

        /// <summary>
        /// 現在保持しているMapバフを対象に適用します
        /// </summary>
        private void ApplyActiveBuffsTo(BuffHandler targetHandler)
        {
            if (targetHandler == null) return;

            if (!registeredHandlers.Contains(targetHandler))
            {
                registeredHandlers.Add(targetHandler);
            }

            foreach (var buff in activeMapBuffs)
            {
                if (buff != null)
                {
                    buff.ApplyBuff(targetHandler);
                }
            }

            Debug.Log($"[MapBuffManager] {targetHandler.name} に {activeMapBuffs.Count} 個のMapバフを一括適用しました。");
        }
    }
}