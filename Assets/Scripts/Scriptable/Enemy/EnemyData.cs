using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    public enum AttackType
    {
        Circle,      // サークル（扇形・ドーナツ対応）
        RangedTarget // 投石（指定座標への範囲攻撃）
    }

    [Serializable]
    public class AttackPattern
    {
        public string attackName;
        public AttackType attackType = AttackType.Circle;
        public float damage = 10f;
        public float attackCooldown = 2f;
        public string animationTriggerName;

        [Header("射程・範囲設定")]
        public float minAttackRange = 0f;    // Circle時: 最小射程（安全地帯 / 内径）
        public float maxAttackRange = 3f;    // Circle時: 外径 / RangedTarget時: 判定・最大飛距離

        [Header("角度設定 (Circle時)")]
        [Range(0f, 360f)]
        public float attackAngle = 360f;     // 攻撃角度（360で全方位）

        [Header("投石・指定位置攻撃の設定 (RangedTarget時)")]
        public float impactRadius = 1.5f;    // 目標地点での着弾爆発半径
        public float projectileSpeed = 10f;  // 弾速 / 飛翔速度
        public float telegraphedDelay = 1.0f;// 攻撃位置確定から着弾までの猶予（予兆時間）
        public GameObject projectilePrefab;  // 投石などの飛翔体プレハブ

        [Header("Gizmo設定 (未設定時はインデックス順で自動着色)")]
        public Color attackRangeColor = Color.clear; // クリアにしておくと自動色割り当て
    }

    [Serializable]
    public class DropItemInfo
    {
        public ItemData item;
        [Range(0f, 1f)]
        public float dropChance = 0.5f;
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "MagicRogue/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("基礎ステータス")]
        public string enemyName;
        public float maxHp = 50f;
        public float moveSpeed = 3.5f;

        [Header("索敵設定（距離と角度）")]
        public float sightRange = 8f;
        [Range(0f, 360f)]
        public float sightAngle = 120f;

        [Header("Gizmo設定")]
        public Color sightRangeColor = new Color(0f, 1f, 0f, 0.2f); // 緑色・半透明

        [Header("攻撃パターン一覧")]
        public List<AttackPattern> attackPatterns = new List<AttackPattern>();

        [Header("ドロップアイテム一覧")]
        public List<DropItemInfo> dropList = new List<DropItemInfo>();
    }
}