using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicRogue
{
    // プログレスバー表示用の属性
    public class ProgressBarAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    // インスペクター上にプログレスバーを描画するカスタムエディタ機能
    [CustomPropertyDrawer(typeof(ProgressBarAttribute))]
    public class ProgressBarDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Float)
            {
                float value = Mathf.Clamp01(property.floatValue);
                Rect barPosition = EditorGUI.PrefixLabel(position, label);
                EditorGUI.ProgressBar(barPosition, value, $"{Mathf.RoundToInt(value * 100)}%");
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
#endif
}