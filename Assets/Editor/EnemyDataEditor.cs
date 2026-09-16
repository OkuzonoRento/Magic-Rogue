using UnityEngine;
using UnityEditor;

namespace MagicRogue
{
    [CustomEditor(typeof(EnemyData))]
    public class EnemyDataEditor : Editor
    {
        // 攻撃パターン追加時に自動設定されるデフォルトカラーリスト（アルファ値 0.25 設定済み）
        private static readonly Color[] DefaultAttackColors = new Color[]
        {
            new Color(1.0f, 0.2f, 0.2f, 0.25f), // 赤
            new Color(1.0f, 0.5f, 0.0f, 0.25f), // オレンジ
            new Color(1.0f, 0.9f, 0.1f, 0.25f), // 黄色
            new Color(0.8f, 0.2f, 1.0f, 0.25f), // 紫
            new Color(0.0f, 0.8f, 1.0f, 0.25f)  // シアン
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 基礎情報
            DrawProperty("enemyName");
            DrawProperty("maxHp");
            DrawProperty("moveSpeed");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("索敵設定", EditorStyles.boldLabel);
            DrawProperty("sightRange");
            DrawProperty("sightAngle");
            DrawProperty("sightRangeColor");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("ドロップアイテム一覧", EditorStyles.boldLabel);
            DrawProperty("dropList");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("攻撃パターン一覧", EditorStyles.boldLabel);

            SerializedProperty attackPatternsProp = serializedObject.FindProperty("attackPatterns");

            for (int i = 0; i < attackPatternsProp.arraySize; i++)
            {
                SerializedProperty element = attackPatternsProp.GetArrayElementAtIndex(i);
                SerializedProperty attackTypeProp = element.FindPropertyRelative("attackType");
                AttackType attackType = (AttackType)attackTypeProp.enumValueIndex;

                string patternTitle = string.IsNullOrEmpty(element.FindPropertyRelative("attackName").stringValue)
                    ? $"Attack Pattern {i + 1}"
                    : element.FindPropertyRelative("attackName").stringValue;

                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, patternTitle, true);

                if (element.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(element.FindPropertyRelative("attackName"));
                    EditorGUILayout.PropertyField(attackTypeProp);
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("damage"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("attackCooldown"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("animationTriggerName"));

                    EditorGUILayout.Space(5);

                    switch (attackType)
                    {
                        case AttackType.Circle:
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("minAttackRange"), new GUIContent("最小射程 (安全地帯/内径)"));
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("maxAttackRange"), new GUIContent("最大射程 (外径)"));
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("attackAngle"), new GUIContent("攻撃角度"));
                            break;

                        case AttackType.RangedTarget:
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("maxAttackRange"), new GUIContent("最大射程/飛距離"));
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("impactRadius"), new GUIContent("着弾爆発半径"));
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("projectileSpeed"), new GUIContent("弾速"));
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("telegraphedDelay"), new GUIContent("着弾前予兆時間 (秒)"));
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("projectilePrefab"), new GUIContent("投石プレハブ"));
                            break;
                    }

                    EditorGUILayout.PropertyField(element.FindPropertyRelative("attackRangeColor"), new GUIContent("Gizmo描画色"));

                    EditorGUILayout.Space(5);
                    if (GUILayout.Button("このパターンを削除"))
                    {
                        attackPatternsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    EditorGUI.indentLevel--;
                }
            }

            if (GUILayout.Button("攻撃パターンを追加"))
            {
                int newIndex = attackPatternsProp.arraySize;
                attackPatternsProp.arraySize++;

                // 新しく追加された要素を取得し、自動で色を初期割り当て
                SerializedProperty newElement = attackPatternsProp.GetArrayElementAtIndex(newIndex);
                Color assignColor = DefaultAttackColors[newIndex % DefaultAttackColors.Length];
                newElement.FindPropertyRelative("attackRangeColor").colorValue = assignColor;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProperty(string propertyName)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, true);
            }
        }
    }
}