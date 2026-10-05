using UnityEngine;

namespace MagicRogue
{
    public class ObjectIdleAnimation : MonoBehaviour
    {
        [Header("動作モード")]
        [SerializeField] private bool enableFloating = true;   // 上下フワフワ
        [SerializeField] private bool enableLooking = true;    // 左右振り向き

        [Header("上下フワフワ (Floating) 設定")]
        [SerializeField] private float floatSpeed = 1.5f;     // 浮遊する速さ
        [SerializeField] private float floatHeight = 0.2f;     // 上下の振幅（メートル）

        [Header("左右振り向き (Looking) 設定")]
        [SerializeField] private float lookSpeed = 0.8f;      // 首振り/向きを変える速さ
        [SerializeField] private float lookAngle = 20f;       // 左右に振る角度（度）

        private Vector3 startPosition;
        private Quaternion startRotation;
        private float randomOffset;

        private void Start()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;

            // 複数モデルを置いたときにアニメーションのタイミングをずらすためのランダム値
            randomOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            float time = Time.time + randomOffset;

            // 1. 上下にフワフワ動かす
            if (enableFloating)
            {
                float newY = startPosition.y + Mathf.Sin(time * floatSpeed) * floatHeight;
                transform.position = new Vector3(startPosition.x, newY, startPosition.z);
            }

            // 2. 左右にゆっくり振り向かせる (Y軸回転)
            if (enableLooking)
            {
                float angle = Mathf.Sin(time * lookSpeed) * lookAngle;
                transform.rotation = startRotation * Quaternion.Euler(0f, angle, 0f);
            }
        }
    }
}