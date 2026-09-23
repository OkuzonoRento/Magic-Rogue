using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MagicRogue
{
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        [SerializeField] private Image fadeImage;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            if (fadeImage != null)
            {
                // 最初は透明
                Color color = fadeImage.color;
                color.a = 0f;
                fadeImage.color = color;
                fadeImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 指定時間かけて画面をアルファ1（ホワイトアウト）にするコルーチン
        /// </summary>
        public IEnumerator FadeToWhiteCoroutine(float duration)
        {
            if (fadeImage == null) yield break;

            float timer = 0f;
            Color color = fadeImage.color;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                color.a = Mathf.Clamp01(timer / duration);
                fadeImage.color = color;
                yield return null;
            }

            color.a = 1f;
            fadeImage.color = color;
        }
    }
}