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
                DontDestroyOnLoad(gameObject); // シーンを跨いでも破棄しないように設定
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(true);
            }
        }

        private void Start()
        {
            // シーン開始時に自動でフェードイン（画面を透明にする）を開始
            StartCoroutine(FadeInCoroutine(0.5f));
        }

        /// <summary>
        /// 画面を白から透明に戻す（フェードイン）
        /// </summary>
        public IEnumerator FadeInCoroutine(float duration)
        {
            if (fadeImage == null) yield break;

            float timer = duration;
            Color color = fadeImage.color;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                color.a = Mathf.Clamp01(timer / duration);
                fadeImage.color = color;
                yield return null;
            }

            color.a = 0f;
            fadeImage.color = color;
        }

        /// <summary>
        /// 画面を透明から白にする（フェードアウト / ホワイトアウト）
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