using UnityEngine;

namespace MagicRogue
{
    public static class AttackChecker
    {
        public static bool IsTargetInAttackRange(Transform attacker, Transform target, AttackPattern pattern, Vector3 targetPositionWorld = default)
        {
            if (attacker == null || target == null) return false;

            switch (pattern.attackType)
            {
                case AttackType.Circle:
                    return IsInCircleRange(attacker, target.position, pattern);

                case AttackType.RangedTarget:
                    float distToImpact = Vector3.Distance(targetPositionWorld, target.position);
                    return distToImpact <= pattern.impactRadius;

                default:
                    return false;
            }
        }

        public static bool IsInCircleRange(Transform attacker, Vector3 targetPos, AttackPattern pattern)
        {
            if (attacker == null || pattern == null) return false;

            Vector3 origin = attacker.position;
            Vector3 toTarget = targetPos - origin;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;

            if (distance < pattern.minAttackRange || distance > pattern.maxAttackRange)
            {
                return false;
            }

            if (pattern.attackAngle >= 360f)
            {
                return true;
            }

            float angleToTarget = Vector3.Angle(attacker.forward, toTarget);
            return angleToTarget <= (pattern.attackAngle * 0.5f);
        }
    }
}