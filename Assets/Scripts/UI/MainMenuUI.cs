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
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        private void OnNewGameClicked()
        {
            GameSceneManager.Instance.StartNewGame();
        }

        private void OnContinueClicked()
        {
            GameSceneManager.Instance.ResumeGame();
        }
    }
}