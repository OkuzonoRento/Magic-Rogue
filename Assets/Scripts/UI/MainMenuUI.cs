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
            if (continueButton != null)
            {
                continueButton.interactable = false; // セーブ機能を一時オフにするため非活性化
            }

            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(CloseSettings);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void OnNewGameClicked()
        {
            SetButtonsInteractable(false);
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.StartNewGame();
            }
        }

        private void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        private void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
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
            if (settingsButton != null) settingsButton.interactable = interactable;
            if (quitButton != null) quitButton.interactable = interactable;
        }
    }
}