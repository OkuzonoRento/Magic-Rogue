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
        [SerializeField] private float rotationSpeed = 15f;

        [Header("New Input System 設定")]
        [Tooltip("ターゲット切り替え用のアクション（例: Left Shift）")]
        [SerializeField] private InputActionProperty switchTargetAction;

        [Header("UI / マーカー参照")]
        [Tooltip("頭上に表示するターゲットマークのプレハブ")]
        [SerializeField] private TargetMarker targetMarkerPrefab;

        private Transform currentTarget;
        private TargetMarker activeMarker;
        private int currentTargetIndex = 0;

        public Transform CurrentTarget => currentTarget;
        public bool IsLockedOn => currentTarget != null;

        private void OnEnable()
        {
            if (switchTargetAction.action != null)
            {
                switchTargetAction.action.Enable();
                switchTargetAction.action.performed += OnSwitchTargetPerformed;
            }
        }

        private void OnDisable()
        {
            if (switchTargetAction.action != null)
            {
                switchTargetAction.action.performed -= OnSwitchTargetPerformed;
                switchTargetAction.action.Disable();
            }
        }

        private void Start()
        {
            if (targetMarkerPrefab != null)
            {
                activeMarker = Instantiate(targetMarkerPrefab);
                activeMarker.SetTarget(null);
            }
        }

        private void Update()
        {
            // 範囲内の敵リストを取得
            List<Transform> enemiesInRange = GetEnemiesInRange();

            if (enemiesInRange.Count == 0)
            {
                // 範囲内に敵が1匹もいなければ解除
                if (currentTarget != null)
                {
                    ClearTarget();
                }
                return;
            }

            // 現在のターゲットが「無効（破壊・非アクティブ）」または「範囲外」なら別の敵へ自動更新
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy || !enemiesInRange.Contains(currentTarget))
            {
                currentTargetIndex = 0;
                SetTarget(enemiesInRange[0]);
            }

            // ロックオン中の自動回転処理
            RotateTowardsTarget();
        }

        // Shiftキー入力時に次の敵へ切り替え
        private void OnSwitchTargetPerformed(InputAction.CallbackContext context)
        {
            SwitchToNextTarget();
        }

        public void SwitchToNextTarget()
        {
            List<Transform> enemiesInRange = GetEnemiesInRange();
            if (enemiesInRange.Count == 0) return;

            // 次の敵のインデックスへ（末尾を超えたら0に戻る）
            currentTargetIndex = (currentTargetIndex + 1) % enemiesInRange.Count;
            SetTarget(enemiesInRange[currentTargetIndex]);
        }

        // 範囲内の敵（Root Transform）を全取得
        private List<Transform> GetEnemiesInRange()
        {
            List<Transform> enemies = new List<Transform>();
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

            foreach (var col in hitColliders)
            {
                Transform enemyRoot = col.transform.root;
                if (!enemies.Contains(enemyRoot) && enemyRoot.gameObject.activeInHierarchy)
                {
                    enemies.Add(enemyRoot);
                }
            }

            // プレイヤーからの距離が近い順にソート（安定した切り替えのため）
            enemies.Sort((a, b) =>
                Vector3.Distance(transform.position, a.position).CompareTo(
                Vector3.Distance(transform.position, b.position))
            );

            return enemies;
        }

        private void SetTarget(Transform target)
        {
            currentTarget = target;
            if (activeMarker != null)
            {
                activeMarker.SetTarget(currentTarget);
            }
        }

        private void RotateTowardsTarget()
        {
            if (currentTarget == null) return;

            Vector3 direction = (currentTarget.position - transform.position);
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        public void ClearTarget()
        {
            currentTarget = null;
            currentTargetIndex = 0;
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