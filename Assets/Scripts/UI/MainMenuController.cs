using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace EightBall.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            var playButton = root.Q<Button>("play-button");
            if (playButton != null)
                playButton.clicked += OnPlayClicked;

            var quitButton = root.Q<Button>("quit-button");
            if (quitButton != null)
                quitButton.clicked += OnQuitClicked;
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene("Gameplay");
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
