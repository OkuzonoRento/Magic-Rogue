using UnityEngine;

namespace MagicRogue
{
    [RequireComponent(typeof(BuffHandler))]
    public class EnemyController : MonoBehaviour
    {
        [Header("敵データ")]
        public EnemyData enemyData;

        [Header("ターゲット情報")]
        [SerializeField] private PlayerController targetPlayer;

        private BuffHandler buffHandler;

        private void Awake()
        {
            buffHandler = GetComponent<BuffHandler>();

            if (targetPlayer == null)
            {
                targetPlayer = FindFirstObjectByType<PlayerController>();
            }
        }

        /// <summary>
        /// Animation Eventから呼び出す攻撃ヒット判定処理
        /// </summary>
        /// <param name="patternIndex">実行中の攻撃パターンインデックス</param>
        public void OnAttackHit(int patternIndex)
        {
            if (enemyData == null || enemyData.attackPatterns == null) return;
            if (patternIndex < 0 || patternIndex >= enemyData.attackPatterns.Count) return;
            if (targetPlayer == null) return;

            AttackPattern pattern = enemyData.attackPatterns[patternIndex];

            // 攻撃範囲（色がついている部分）内にプレイヤーがいるかチェック
            bool isHit = AttackChecker.IsTargetInAttackRange(transform, targetPlayer.transform, pattern);

            if (isHit)
            {
                float atkMult = buffHandler.GetMultiplier(BuffType.AttackUp);
                float finalDamage = pattern.damage * atkMult;

                targetPlayer.TakeDamage(finalDamage);
            }
            else
            {
                Debug.Log($"[Enemy] 攻撃パターン {patternIndex + 1} は外れました。");
            }
        }
    }
}