using UnityEngine;

namespace MagicRogue
{
    public class TargetMarker : MonoBehaviour
    {
        [Header("位置調整")]
        [Tooltip("TargetPoint からの微調整用オフセット（少し浮かせる場合はYを大きくする）")]
        [SerializeField] private Vector3 offset = Vector3.zero;

        private Transform targetPointTransform;

        public void SetTarget(Transform targetRoot)
        {
            if (targetRoot == null)
            {
                targetPointTransform = null;
                gameObject.SetActive(false);
                return;
            }

            // 敵のオブジェクト配下から "TargetPoint" という名前の Transform を検索
            targetPointTransform = targetRoot.Find("TargetPoint");

            // 子要素に見つからない場合は深層検索
            if (targetPointTransform == null)
            {
                var points = targetRoot.GetComponentsInChildren<Transform>();
                foreach (var p in points)
                {
                    if (p.name == "TargetPoint")
                    {
                        targetPointTransform = p;
                        break;
                    }
                }
            }

            if (targetPointTransform != null)
            {
                gameObject.SetActive(true);
                UpdatePosition();
            }
            else
            {
                Debug.LogWarning($"[TargetMarker] {targetRoot.name} に 'TargetPoint' が見つかりません。");
                gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (targetPointTransform == null || !targetPointTransform.gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            UpdatePosition();

            // カメラの方向に向ける（ビルボード処理）
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void UpdatePosition()
        {
            // TargetPoint の位置 ＋ オフセットに移動
            transform.position = targetPointTransform.position + offset;
        }
    }
}