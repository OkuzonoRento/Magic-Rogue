using System;
using UnityEngine;

namespace MagicRogue
{
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

        // --- Map バフ固有種別 ---
        StationaryTurret,     // 固定砲台（停止中攻撃力UP）
        Overload,             // 魔術回路過剰暴走（前半強体化・後半弱体化）
        NoonPower,            // 正午の力（時間経過による攻撃力変動）
        Vengeance,            // 復讐心（被弾時一時ステータスUP）
        CurseStaff,           // 呪術師の杖（ヒット時敵弱体化）
        DivineProtection,     // 聖なる守り（攻撃無効化シールド）
        GraveRobber,          // 盗掘者（回収範囲UP＋アイテム拾い時攻撃力UP）
        Stepper,              // ステッパー（移動中攻撃力UP）
        FrogInAWell,          // 井の中の蛙（スロット一致時超強化 / 不一致時弱体）
        PathToAscension,      // 神仙へと至る道（初期超弱体化 ＋ 撃破解禁で無敵覚醒）
        Abyss,                // 奈落（ヒット時吸引ブラックホール生成）
        LastStand,            // 背水の陣（HP20%以下で攻撃力大幅UP）
        MagicCirculation,     // 魔力循環（連続ヒットで攻撃速度・CD短縮）
        FleshCut              // 肉斬骨断（受けるダメージ・攻撃力・CD悪化 ＋ 被弾時爆発力）
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