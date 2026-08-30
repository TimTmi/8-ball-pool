using UnityEngine;
using UnityEngine.UIElements;

namespace EightBall.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GameplayUIController : MonoBehaviour
    {
        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            var shootButton = root.Q<Button>("shoot-button");
            if (shootButton != null)
                shootButton.clicked += OnShootClicked;
        }

        private void OnShootClicked()
        {
            Debug.Log("Shoot button clicked!");
        }
    }
}
