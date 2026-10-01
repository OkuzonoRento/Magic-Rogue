using UnityEngine;
using UnityEngine.UI;

namespace MagicRogue
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;

        private void Start()
        {
            bool hasSave = GameSceneManager.Instance != null && GameSceneManager.Instance.HasSavedGame();

            // 中断データがある場合のみ「つづきから」を有効化
            if (continueButton != null)
            {
                continueButton.interactable = hasSave;
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
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

        private void SetButtonsInteractable(bool interactable)
        {
            if (newGameButton != null) newGameButton.interactable = interactable;
            if (continueButton != null) continueButton.interactable = interactable;
        }
    }
}