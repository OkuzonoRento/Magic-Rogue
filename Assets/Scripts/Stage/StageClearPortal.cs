using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicRogue
{
    [RequireComponent(typeof(Collider))]
    public class StageClearPortal : MonoBehaviour
    {
        [Header("遷移設定")]
        [SerializeField] private string shopSceneName = "06_Shop";

        [Header("演出時間設定")]
        [SerializeField] private float moveSpeedToCenter = 5f;
        [SerializeField] private float fadeDuration = 1.5f;

        private bool isTriggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered || !other.CompareTag("Player")) return;

            isTriggered = true;
            StartCoroutine(PortalTransitionCoroutine(other.gameObject));
        }

        private IEnumerator PortalTransitionCoroutine(GameObject playerObj)
        {
            if (playerObj.TryGetComponent<PlayerController>(out var playerController))
            {
                playerController.SetInvincible(true);
                playerController.SetControlActive(false);
            }

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

            if (SceneFader.Instance != null)
            {
                yield return StartCoroutine(SceneFader.Instance.FadeToWhiteCoroutine(fadeDuration));
            }
            else
            {
                yield return new WaitForSeconds(fadeDuration);
            }

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