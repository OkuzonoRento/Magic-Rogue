using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
                DontDestroyOnLoad(gameObject); // シーン跨ぎで破棄されないように設定
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (fadeImage != null)
            {
                // 最初は透明に設定
                Color color = fadeImage.color;
                color.a = 0f;
                fadeImage.color = color;
                fadeImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// フェードアウト → シーン非同期ロード → フェードイン を順に行う
        /// </summary>
        public void FadeAndLoadScene(string sceneName, float fadeDuration = 0.5f)
        {
            StartCoroutine(FadeAndLoadSceneRoutine(sceneName, fadeDuration));
        }

        private IEnumerator FadeAndLoadSceneRoutine(string sceneName, float fadeDuration)
        {
            // 1. 画面を徐々に不透明（ホワイトアウト）にする（フェードアウト）
            yield return StartCoroutine(FadeToWhiteCoroutine(fadeDuration));

            // 2. 新しいシーンを非同期でロード
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // 3. ロード完了後、画面を透明に戻す（フェードイン）
            yield return StartCoroutine(FadeInCoroutine(fadeDuration));
        }

        /// <summary>
        /// 画面を不透明から透明にするコルーチン（フェードイン）
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
        /// 画面を透明から不透明にするコルーチン（フェードアウト）
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