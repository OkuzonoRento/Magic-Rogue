using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicRogue
{
    public class CameraController : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("追従対象のプレイヤー Transform")]
        [SerializeField] private Transform player;

        [Tooltip("プレイヤーにアタッチされている TargetLockSystem")]
        [SerializeField] private TargetLockSystem targetLockSystem;

        [Header("カメラ画角・基本設定")]
        [Tooltip("カメラの見下ろし角度（X軸回転）")]
        [SerializeField] private float cameraPitchAngle = 55f;

        [Tooltip("ターゲットからの基本距離（高さ・奥行き）")]
        [SerializeField] private float defaultDistance = 12f;

        [Tooltip("カメラ移動の滑らかさ")]
        [SerializeField] private float smoothSpeed = 8f;

        [Header("マウス横回転設定")]
        [Tooltip("マウス横移動でのカメラ回転感度")]
        [SerializeField] private float mouseSensitivity = 0.2f;

        [Header("ロックオン時のズーム調整")]
        [Tooltip("敵と離れたときに引くカメラ距離の倍率")]
        [SerializeField] private float distanceZoomFactor = 0.2f;

        [Tooltip("距離に応じたカメラ引きの最小倍率")]
        [SerializeField] private float minZoomMultiplier = 0.8f;

        [Tooltip("距離に応じたカメラ引きの最大倍率")]
        [SerializeField] private float maxZoomMultiplier = 1.6f;

        private float currentYaw = 0f; // カメラの横回転角度（Y軸）

        private void Start()
        {
            if (player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            if (targetLockSystem == null && player != null)
            {
                targetLockSystem = player.GetComponent<TargetLockSystem>();
            }

            // 初期角度をカメラのY軸回転に合わせてセット
            currentYaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (player == null) return;

            // 1. マウスの横移動（Mouse X）による横回転（Yaw）の計算
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float mouseX = mouse.delta.x.ReadValue();
                currentYaw += mouseX * mouseSensitivity;
            }

            // 2. カメラの注視点（ターゲット位置）を計算
            Vector3 focusPoint;

            if (targetLockSystem != null && targetLockSystem.IsLockedOn && targetLockSystem.CurrentTarget != null)
            {
                // ロックオン時：プレイヤーと敵の中間地点
                Transform enemy = targetLockSystem.CurrentTarget;
                focusPoint = (player.position + enemy.position) * 0.5f;
            }
            else
            {
                // 非ロックオン時：プレイヤーの位置
                focusPoint = player.position;
            }

            // 3. ズーム倍率の計算（ロックオン中の距離に応じて引き）
            float zoomMultiplier = 1f;
            if (targetLockSystem != null && targetLockSystem.IsLockedOn && targetLockSystem.CurrentTarget != null)
            {
                float distance = Vector3.Distance(player.position, targetLockSystem.CurrentTarget.position);
                zoomMultiplier = Mathf.Clamp(1f + (distance * distanceZoomFactor * 0.1f), minZoomMultiplier, maxZoomMultiplier);
            }

            // 4. 回転角度（Pitch/Yaw）からカメラの配置位置を算出
            Quaternion rotation = Quaternion.Euler(cameraPitchAngle, currentYaw, 0f);
            Vector3 targetOffset = rotation * new Vector3(0f, 0f, -defaultDistance * zoomMultiplier);
            Vector3 targetPosition = focusPoint + targetOffset;

            // 5. カメラ位置と回転を滑らかに更新
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
            transform.rotation = rotation;
        }
    }
}