using UnityEngine;
using UnityEngine.UI;

namespace MagicRogue
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("メインボタン")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("設定パネル参照")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button settingsCloseButton;

        private void Start()
        {
            // 中断データの有無を判定して「つづきから」ボタンの有効化切り替え
            bool hasSave = GameSceneManager.Instance != null && GameSceneManager.Instance.HasSavedGame();

            if (continueButton != null)
            {
                continueButton.interactable = hasSave;
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
            }

            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.AddListener(CloseSettings);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            // 初期状態では設定パネルを閉じておく
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void OnNewGameClicked()
        {
            SetButtonsInteractable(false);
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.StartNewGame();
            }
        }

        private void OnContinueClicked()
        {
            SetButtonsInteractable(false);
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ResumeGame();
            }
        }

        private void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        private void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (newGameButton != null) newGameButton.interactable = interactable;
            if (continueButton != null && GameSceneManager.Instance != null && GameSceneManager.Instance.HasSavedGame())
            {
                continueButton.interactable = interactable;
            }
            if (settingsButton != null) settingsButton.interactable = interactable;
            if (quitButton != null) quitButton.interactable = interactable;
        }
    }
}