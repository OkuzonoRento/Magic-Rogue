using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicRogue
{
    public class TargetLockSystem : MonoBehaviour
    {
        [Header("ロックオン設定")]
        [Tooltip("ターゲットを検索・維持する最大距離")]
        [SerializeField] private float detectionRadius = 15f;

        [Tooltip("敵のレイヤー")]
        [SerializeField] private LayerMask enemyLayer;

        [Tooltip("ターゲット方向への回転速度")]
        [SerializeField] private float rotationSpeed = 10f;

        [Header("New Input System 設定")]
        [Tooltip("ロックオン切替用のアクション（例: Tabキー, R3ボタン）")]
        [SerializeField] private InputActionProperty lockOnAction;

        [Header("UI / マーカー参照")]
        [Tooltip("頭上に表示するターゲットマークのプレハブ")]
        [SerializeField] private TargetMarker targetMarkerPrefab;

        private Transform currentTarget;
        private TargetMarker activeMarker;

        public Transform CurrentTarget => currentTarget;
        public bool IsLockedOn => currentTarget != null;

        private void OnEnable()
        {
            // Input Action の購読を開始
            if (lockOnAction.action != null)
            {
                lockOnAction.action.Enable();
                lockOnAction.action.performed += OnLockOnPerformed;
            }
        }

        private void OnDisable()
        {
            // Input Action の購読を解除
            if (lockOnAction.action != null)
            {
                lockOnAction.action.performed -= OnLockOnPerformed;
                lockOnAction.action.Disable();
            }
        }

        private void Start()
        {
            // マーカーの生成
            if (targetMarkerPrefab != null)
            {
                activeMarker = Instantiate(targetMarkerPrefab);
                activeMarker.SetTarget(null);
            }
        }

        private void Update()
        {
            if (currentTarget != null)
            {
                // 1. 距離外脱出・非アクティブ（死亡）チェック
                float distance = Vector3.Distance(transform.position, currentTarget.position);
                if (distance > detectionRadius || !currentTarget.gameObject.activeInHierarchy)
                {
                    ClearTarget();
                    return;
                }

                // 2. ロックオン中：自動で敵の方向へ向く処理
                RotateTowardsTarget();
            }
        }

        // New Input System から入力があった際に呼ばれるコールバック
        private void OnLockOnPerformed(InputAction.CallbackContext context)
        {
            ToggleLockOn();
        }

        // ロックオン切り替え処理（切り替え時、別の敵がいればターゲット更新）
        public void ToggleLockOn()
        {
            if (currentTarget != null)
            {
                // 既にロックオン中の場合は一度解除
                ClearTarget();
            }
            else
            {
                // ロックオン実行
                AcquireTarget();
            }
        }

        // 最寄りの敵を取得してターゲット化
        private void AcquireTarget()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
            if (hitColliders.Length == 0) return;

            Transform closestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (var col in hitColliders)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestEnemy = col.transform;
                }
            }

            if (closestEnemy != null)
            {
                currentTarget = closestEnemy;
                if (activeMarker != null)
                {
                    activeMarker.SetTarget(currentTarget);
                }
            }
        }

        // ターゲットに向かって旋回する
        private void RotateTowardsTarget()
        {
            if (currentTarget == null) return;

            Vector3 direction = (currentTarget.position - transform.position);
            direction.y = 0f; // Y軸回転のみ（上下には傾けない）

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        public void ClearTarget()
        {
            currentTarget = null;
            if (activeMarker != null)
            {
                activeMarker.SetTarget(null);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}