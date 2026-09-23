using UnityEngine;

namespace MagicRogue
{
    public class TargetMarker : MonoBehaviour
    {
        [Header("位置調整")]
        [Tooltip("TargetPoint からの基本オフセット")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.5f, 0f);

        [Header("上下ふわふわ設定")]
        [Tooltip("上下移動の振り幅")]
        [SerializeField] private float floatAmplitude = 0.2f;

        [Tooltip("上下移動のスピード")]
        [SerializeField] private float floatSpeed = 3f;

        [Header("回転設定")]
        [Tooltip("回転のスピード（Y軸回り）")]
        [SerializeField] private float rotateSpeed = 180f;

        private Transform targetPointTransform;

        public void SetTarget(Transform targetRoot)
        {
            if (targetRoot == null)
            {
                targetPointTransform = null;
                gameObject.SetActive(false);
                return;
            }

            // 階層の深さに関わらず全子要素から "TargetPoint" を検索
            targetPointTransform = FindDeepChild(targetRoot, "TargetPoint");

            if (targetPointTransform == null)
            {
                targetPointTransform = targetRoot;
            }

            gameObject.SetActive(true);
            UpdateMarkerTransform();
        }

        private void LateUpdate()
        {
            if (targetPointTransform == null || !targetPointTransform.gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            UpdateMarkerTransform();
        }

        private void UpdateMarkerTransform()
        {
            // 1. 上下にふわふわ揺れる位置計算
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            Vector3 animatedOffset = offset + new Vector3(0f, yOffset, 0f);

            transform.position = targetPointTransform.position + animatedOffset;

            // 2. Z軸を中心にクルクル回転
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime, Space.Self);
        }

        // 深層検索用メソッド
        private Transform FindDeepChild(Transform aParent, string aName)
        {
            foreach (Transform child in aParent)
            {
                if (child.name == aName)
                    return child;
                Transform result = FindDeepChild(child, aName);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}