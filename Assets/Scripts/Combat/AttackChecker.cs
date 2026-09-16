using UnityEngine;

namespace MagicRogue
{
    public static class AttackChecker
    {
        /// <summary>
        /// ターゲットが攻撃範囲内にいるかを判定する
        /// </summary>
        /// <param name="attacker">攻撃者の Transform</param>
        /// <param name="target">ターゲットの Transform</param>
        /// <param name="pattern">判定対象の AttackPattern</param>
        /// <param name="targetPositionWorld">投石（RangedTarget）などの着弾目標座標（オプション）</param>
        /// <returns>範囲内にいれば true</returns>
        public static bool IsTargetInAttackRange(Transform attacker, Transform target, AttackPattern pattern, Vector3 targetPositionWorld = default)
        {
            if (attacker == null || target == null) return false;

            switch (pattern.attackType)
            {
                case AttackType.Circle:
                    return IsInCircleRange(attacker, target.position, pattern);

                case AttackType.RangedTarget:
                    // 着弾指定地点からの距離で判定
                    float distToImpact = Vector3.Distance(targetPositionWorld, target.position);
                    return distToImpact <= pattern.impactRadius;

                default:
                    return false;
            }
        }

        private static bool IsInCircleRange(Transform attacker, Vector3 targetPos, AttackPattern pattern)
        {
            Vector3 origin = attacker.position;
            Vector3 toTarget = targetPos - origin;
            toTarget.y = 0; // 水平面（XZ軸）のみで判定

            float distance = toTarget.magnitude;

            // 1. 最小射程（内径安全地帯）～ 最大射程（外径）のチェック
            if (distance < pattern.minAttackRange || distance > pattern.maxAttackRange)
            {
                return false;
            }

            // 2. 全方位（360度）ならこの時点でヒット確定
            if (pattern.attackAngle >= 360f)
            {
                return true;
            }

            // 3. 扇形（角度指定）のチェック
            float angleToTarget = Vector3.Angle(attacker.forward, toTarget);
            return angleToTarget <= (pattern.attackAngle * 0.5f);
        }
    }
}