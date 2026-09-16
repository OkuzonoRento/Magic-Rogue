using UnityEditor;
using UnityEngine;

namespace MagicRogue
{
    [CustomEditor(typeof(MagicData))]
    public class MagicDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // --- 基本情報（ItemData共通） ---
            EditorGUILayout.LabelField("基本情報", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rarity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sellPrice"));

            EditorGUILayout.Space(10);

            // --- 魔法基本性能 ---
            EditorGUILayout.LabelField("魔法基本性能", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("damage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));

            EditorGUILayout.Space(10);

            // --- 挙動タイプ設定 ---
            EditorGUILayout.LabelField("挙動タイプ設定", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnType"));

            SerializedProperty movementTypeProp = serializedObject.FindProperty("movementType");
            EditorGUILayout.PropertyField(movementTypeProp);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectilePrefab"));

            // 選択された MovementType に応じて必要なプロパティのみ描画
            MovementType movementType = (MovementType)movementTypeProp.enumValueIndex;

            switch (movementType)
            {
                case MovementType.Spread:
                case MovementType.Homing:
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("拡散・発射設定", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileCount"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("spreadAngle"));
                    break;

                case MovementType.Split:
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("散弾・発射設定", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileCount"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("spreadAngle"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSplitCount"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("splitSubProjectiles"));
                    break;

                case MovementType.Boomerang:
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("ブーメラン・発射設定", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileCount"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("spreadAngle"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("returnTime"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("returnSpeedMultiplier"));
                    break;

                case MovementType.Laser:
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("レーザー設定", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("laserDamageInterval"));
                    break;
            }

            EditorGUILayout.Space(10);

            // --- ビジュアル & 貫通 ---
            EditorGUILayout.LabelField("ビジュアル＆貫通設定", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("castEffectPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hitEffectPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("castSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hitSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("piercesEnemy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("piercesWall"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}