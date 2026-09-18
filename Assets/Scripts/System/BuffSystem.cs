using System;
using UnityEngine;

namespace MagicRogue
{
    public enum BuffType
    {
        // --- バフ ---
        AttackUp,             // 攻撃力増加 (%)
        MaxHpUp,              // 最大体力増加 (%)
        CooldownReduction,    // クールタイム減少 (%)
        DropRateUp,           // ドロップ率増加 (%)
        SearchRangeUp,        // サーチ範囲増加 (%)
        EnemyStatDown,        // 敵ステータスの低下 (%)

        // --- デバフ ---
        AttackDown,           // 攻撃力低下 (%)
        CooldownIncrease,     // クールタイム増加 (%)
        MagicSlotReduction,   // 魔法スロット減少 (個数/値)
        SelfDamageOnAttack,   // 攻撃自傷 (固定値/割合)
        DamageReceivedUp,     // 被ダメージ増加 (%)
        ChanceToFail,         // 確率不発 (%)
        EnemyStatUp,          // 敵ステータス向上 (%)
        SearchRangeDown       // サーチ範囲低下 (%)
    }

    [Serializable]
    public class BuffInstance
    {
        public BuffType type;
        public float value;       // 効果値 (例: 0.2 = +20% / 固定値)
        public float duration;    // 残り持持続時間（秒）
        public float tickTimer;   // 定期発動用タイマー

        public BuffInstance(BuffType type, float value, float duration)
        {
            this.type = type;
            this.value = value;
            this.duration = duration;
            this.tickTimer = 0f;
        }
    }
}