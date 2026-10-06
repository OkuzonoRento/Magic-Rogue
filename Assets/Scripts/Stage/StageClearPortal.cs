using System.Collections;
using UnityEngine;

namespace MagicRogue
{
    [RequireComponent(typeof(Collider))]
    public class StageClearPortal : MonoBehaviour
    {
        private bool isTriggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered || !other.CompareTag("Player")) return;

            isTriggered = true;

            // プレイヤーの操作停止・無敵化
            if (other.TryGetComponent<PlayerController>(out var playerController))
            {
                playerController.SetInvincible(true);
                playerController.SetControlActive(false);
            }

            // ★ GameSceneManager 経由で Fade 遷移してショップへ
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.OnStageCleared();
            }
        }
    }
}