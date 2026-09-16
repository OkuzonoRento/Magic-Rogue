using UnityEngine;
using UnityEditor;

namespace MagicRogue
{
    [CustomEditor(typeof(EnemyController))]
    public class EnemyControllerEditor : Editor
    {
        private static readonly Color[] AutoAttackColors = new Color[]
        {
            new Color(1.0f, 0.2f, 0.2f),
            new Color(1.0f, 0.5f, 0.0f),
            new Color(1.0f, 0.9f, 0.1f),
            new Color(0.8f, 0.2f, 1.0f),
            new Color(0.0f, 0.8f, 1.0f)
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        private static void RenderCustomGizmos(EnemyController enemy, GizmoType gizmoType)
        {
            if (enemy == null || enemy.enemyData == null) return;

            EnemyData data = enemy.enemyData;
            Vector3 pos = enemy.transform.position;
            Vector3 forward = enemy.transform.forward;

            // --------------------------------------------------
            // 1. 索敵エリア（緑色・半透明）
            // --------------------------------------------------
            Color sightColor = data.sightRangeColor;
            sightColor.a = 0.2f;
            Handles.color = sightColor;

            Vector3 sightLeftDir = Quaternion.Euler(0, -data.sightAngle * 0.5f, 0) * forward;
            Handles.DrawSolidArc(pos, Vector3.up, sightLeftDir, data.sightAngle, data.sightRange);

            Handles.color = new Color(data.sightRangeColor.r, data.sightRangeColor.g, data.sightRangeColor.b, 0.8f);
            Handles.DrawWireArc(pos, Vector3.up, sightLeftDir, data.sightAngle, data.sightRange);
            Handles.DrawLine(pos, pos + sightLeftDir * data.sightRange);
            Vector3 sightRightDir = Quaternion.Euler(0, data.sightAngle * 0.5f, 0) * forward;
            Handles.DrawLine(pos, pos + sightRightDir * data.sightRange);

            // --------------------------------------------------
            // 2. 攻撃パターン（ドーナツ内抜き＆投石）
            // --------------------------------------------------
            if (data.attackPatterns == null) return;

            for (int i = 0; i < data.attackPatterns.Count; i++)
            {
                var attack = data.attackPatterns[i];

                Color baseColor = attack.attackRangeColor;
                if (baseColor.a <= 0.05f)
                {
                    baseColor = AutoAttackColors[i % AutoAttackColors.Length];
                }

                Color fillColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.25f);
                Color wireColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.85f);

                switch (attack.attackType)
                {
                    case AttackType.Circle:
                        float angle = attack.attackAngle;
                        Vector3 leftDir = Quaternion.Euler(0, -angle * 0.5f, 0) * forward;
                        Vector3 rightDir = Quaternion.Euler(0, angle * 0.5f, 0) * forward;

                        // --- 内径と外径の間だけを塗りつぶす描画ロジック ---
                        Handles.color = fillColor;
                        int segments = Mathf.Max(1, Mathf.CeilToInt(angle / 6f));
                        float deltaAngle = angle / segments;
                        float startAngle = -angle * 0.5f;

                        for (int s = 0; s < segments; s++)
                        {
                            float a1 = startAngle + s * deltaAngle;
                            float a2 = startAngle + (s + 1) * deltaAngle;

                            Vector3 dir1 = Quaternion.Euler(0, a1, 0) * forward;
                            Vector3 dir2 = Quaternion.Euler(0, a2, 0) * forward;

                            Vector3[] quad = new Vector3[4]
                            {
                                pos + dir1 * attack.minAttackRange,
                                pos + dir1 * attack.maxAttackRange,
                                pos + dir2 * attack.maxAttackRange,
                                pos + dir2 * attack.minAttackRange
                            };

                            Handles.DrawSolidRectangleWithOutline(quad, fillColor, Color.clear);
                        }

                        // --- 輪郭線（ワイヤーフレーム） ---
                        Handles.color = wireColor;
                        Handles.DrawWireArc(pos, Vector3.up, leftDir, angle, attack.maxAttackRange);

                        if (attack.minAttackRange > 0f)
                        {
                            Handles.DrawWireArc(pos, Vector3.up, leftDir, angle, attack.minAttackRange);
                        }

                        if (angle < 360f)
                        {
                            Handles.DrawLine(pos + leftDir * attack.minAttackRange, pos + leftDir * attack.maxAttackRange);
                            Handles.DrawLine(pos + rightDir * attack.minAttackRange, pos + rightDir * attack.maxAttackRange);
                        }
                        break;

                    case AttackType.RangedTarget:
                        Handles.color = new Color(wireColor.r, wireColor.g, wireColor.b, 0.3f);
                        Handles.DrawWireDisc(pos, Vector3.up, attack.maxAttackRange);

                        Vector3 targetPos = pos + forward * attack.maxAttackRange;

                        Handles.color = fillColor;
                        Handles.DrawSolidDisc(targetPos, Vector3.up, attack.impactRadius);

                        Handles.color = wireColor;
                        Handles.DrawWireDisc(targetPos, Vector3.up, attack.impactRadius);
                        Handles.DrawLine(pos, targetPos);
                        break;
                }
            }
        }
    }
}