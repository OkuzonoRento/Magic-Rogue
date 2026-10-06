using System;
using UnityEngine;

namespace MagicRogue
{
    public enum BuffTarget
    {
        Self,   // 自分（プレイヤーまたは付与対象自身）
        Enemy   // 敵（攻撃時などに相手へ付与）
    }

    public enum BuffType
    {
        // --- 通常 Global バフ・デバフ ---
        AttackUp,             // 攻撃力増加 (%)
        MaxHpUp,              // 最大体力増加 (%)
        CooldownReduction,    // クールタイム減少 (%)
        DropRateUp,           // ドロップ率増加 (%)
        SearchRangeUp,        // サーチ範囲増加 (%)
        EnemyStatDown,        // 敵ステータスの低下 (%)
        AttackDown,           // 攻撃力低下 (%)
        CooldownIncrease,     // クールタイム増加 (%)
        MagicSlotReduction,   // 魔法スロット減少 (個数/値)
        SelfDamageOnAttack,   // 攻撃自傷 (固定値/割合)
        DamageReceivedUp,     // 被ダメージ増加 (%)
        ChanceToFail,         // 確率不発 (%)
        EnemyStatUp,          // 敵ステータス向上 (%)
        SearchRangeDown,      // サーチ範囲低下 (%)

        // --- 移動速度バフ・デバフ ---
        MoveSpeedUp,          // 移動速度増加 (%)
        MoveSpeedDown,        // 移動速度低下 (%)

        // --- Map バフ固有種別 ---
        StationaryTurret,     // 固定砲台
        Overload,             // 魔術回路過剰暴走
        NoonPower,            // 正午の力
        Vengeance,            // 復讐心
        CurseStaff,           // 呪術師の杖
        DivineProtection,     // 聖なる守り
        GraveRobber,          // 盗掘者
        Stepper,              // ステッパー
        FrogInAWell,          // 井の中の蛙
        PathToAscension,      // 神仙へと至る道
        LastStand,            // 背水の陣
        MagicCirculation,     // 魔力循環
        FleshCut              // 肉斬骨断
    }

    [Serializable]
    public class BuffInstance
    {
        public BuffType type;
        public float value;       // 効果値
        public float duration;    // 残り持続時間（秒）
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