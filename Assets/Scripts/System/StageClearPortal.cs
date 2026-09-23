using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicRogue
{
    [RequireComponent(typeof(Collider))]
    public class StageClearPortal : MonoBehaviour
    {
        [Header("遷移設定")]
        [Tooltip("遷移先のショップシーン名")]
        [SerializeField] private string shopSceneName = "06_Shop";

        [Header("演出時間設定")]
        [Tooltip("中央までプレイヤーを引き込む移動速度")]
        [SerializeField] private float moveSpeedToCenter = 5f;

        [Tooltip("ホワイトアウトにかかる時間（秒）")]
        [SerializeField] private float fadeDuration = 1.5f;

        private bool isTriggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered || !other.CompareTag("Player")) return;

            isTriggered = true;
            Debug.Log("[Portal] 魔法陣が起動。演出開始！");

            StartCoroutine(PortalTransitionCoroutine(other.gameObject));
        }

        private IEnumerator PortalTransitionCoroutine(GameObject playerObj)
        {
            // 1. プレイヤーを無敵化＆移動制限（PlayerController 経由）
            if (playerObj.TryGetComponent<PlayerController>(out var playerController))
            {
                playerController.SetInvincible(true);
                playerController.SetControlActive(false);
            }

            // 2. プレイヤーを魔法陣の中央位置へ徐々に吸い寄せる
            Vector3 targetPosition = new Vector3(transform.position.x, playerObj.transform.position.y, transform.position.z);
            while (Vector3.Distance(playerObj.transform.position, targetPosition) > 0.1f)
            {
                playerObj.transform.position = Vector3.MoveTowards(
                    playerObj.transform.position,
                    targetPosition,
                    moveSpeedToCenter * Time.deltaTime
                );
                yield return null;
            }
            playerObj.transform.position = targetPosition;

            // 3. 画面のホワイトアウトフェードを実行
            if (SceneFader.Instance != null)
            {
                yield return StartCoroutine(SceneFader.Instance.FadeToWhiteCoroutine(fadeDuration));
            }
            else
            {
                // Faderが無い場合の代替ウェイト
                yield return new WaitForSeconds(fadeDuration);
            }

            // 4. ショップシーンへ遷移
            LoadShopScene();
        }

        private void LoadShopScene()
        {
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ChangeScene(shopSceneName);
            }
            else
            {
                SceneManager.LoadScene(shopSceneName);
            }
        }
    }
}