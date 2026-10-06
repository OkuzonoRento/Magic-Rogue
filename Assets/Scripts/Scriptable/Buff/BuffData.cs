using UnityEngine;

namespace MagicRogue
{
    [CreateAssetMenu(fileName = "NewBuffData", menuName = "MagicRogue/Buff Data")]
    public class BuffData : ScriptableObject
    {
        [Header("基本情報")]
        [Tooltip("バフ/デバフの名前")]
        public string buffName = "新しいバフ";

        [Tooltip("アイコン（UI表示用）")]
        public Sprite icon;

        [TextArea(2, 5)]
        [Tooltip("説明文")]
        public string description;

        [Header("コスト・ターゲット設定")]
        [Tooltip("バフの付与対象（Self: 自身 / Enemy: 攻撃対象の敵）")]
        public BuffTarget targetType = BuffTarget.Self;

        [Tooltip("グローバルバフ選択時のコスト（クレジット）")]
        public int creditCost = 10;

        [Header("効果パラメータ")]
        [Tooltip("バフ/デバフの種類")]
        public BuffType buffType;

        [Tooltip("効果値（例: 0.2 = +20% / 5 = 5ダメージ）")]
        public float value = 0.2f;

        [Tooltip("持続時間（秒）")]
        public float duration = 10f;

        public void ApplyBuff(BuffHandler target)
        {
            if (target != null)
            {
                target.AddBuff(buffType, value, duration);
                Debug.Log($"[Buff] {target.name} に {buffName} ({buffType}: {value}) を付与しました。");
            }
        }
    }
}