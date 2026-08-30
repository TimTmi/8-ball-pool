using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace EightBall.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private VisualElement _menuView;
        private VisualElement _creditsView;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            _menuView = root.Q<VisualElement>("menu-view");
            _creditsView = root.Q<VisualElement>("credits-view");

            var playButton = root.Q<Button>("play-button");
            if (playButton != null)
                playButton.clicked += OnPlayClicked;

            var creditsButton = root.Q<Button>("credits-button");
            if (creditsButton != null)
                creditsButton.clicked += OnCreditsClicked;

            var backButton = root.Q<Button>("back-button");
            if (backButton != null)
                backButton.clicked += OnBackClicked;

            var quitButton = root.Q<Button>("quit-button");
            if (quitButton != null)
                quitButton.clicked += OnQuitClicked;
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene("Gameplay");
        }

        private void OnCreditsClicked()
        {
            if (_menuView != null) _menuView.style.display = DisplayStyle.None;
            if (_creditsView != null) _creditsView.style.display = DisplayStyle.Flex;
        }

        private void OnBackClicked()
        {
            if (_creditsView != null) _creditsView.style.display = DisplayStyle.None;
            if (_menuView != null) _menuView.style.display = DisplayStyle.Flex;
        }

        private void OnQuitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
