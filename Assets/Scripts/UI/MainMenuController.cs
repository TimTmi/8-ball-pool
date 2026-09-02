using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using EightBall.Rules;

namespace EightBall.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private VisualElement _menuView;
        private VisualElement _creditsView;
        private VisualElement _rulesModal;
        private VisualElement _rulesList;

        private Button _playButton;
        private Button _creditsButton;
        private Button _quitButton;
        private Button _startButton;
        private Button _rulesBackButton;
        private Button _creditsBackButton;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            _menuView = root.Q<VisualElement>("menu-view");
            _creditsView = root.Q<VisualElement>("credits-view");
            _rulesModal = root.Q<VisualElement>("rules-modal");
            _rulesList = root.Q<VisualElement>("rules-list");

            _playButton = root.Q<Button>("play-button");
            _creditsButton = root.Q<Button>("credits-button");
            _quitButton = root.Q<Button>("quit-button");
            _startButton = root.Q<Button>("start-button");
            _rulesBackButton = root.Q<Button>("rules-back-button");
            _creditsBackButton = root.Q<Button>("back-button");

            if (_playButton != null) _playButton.clicked += OnPlayClicked;
            if (_creditsButton != null) _creditsButton.clicked += OnCreditsClicked;
            if (_quitButton != null) _quitButton.clicked += OnQuitClicked;
            if (_startButton != null) _startButton.clicked += OnStartClicked;
            if (_rulesBackButton != null) _rulesBackButton.clicked += OnRulesBackClicked;
            if (_creditsBackButton != null) _creditsBackButton.clicked += OnCreditsBackClicked;

            BuildRulesList();
        }

        private void OnDisable()
        {
            if (_playButton != null) _playButton.clicked -= OnPlayClicked;
            if (_creditsButton != null) _creditsButton.clicked -= OnCreditsClicked;
            if (_quitButton != null) _quitButton.clicked -= OnQuitClicked;
            if (_startButton != null) _startButton.clicked -= OnStartClicked;
            if (_rulesBackButton != null) _rulesBackButton.clicked -= OnRulesBackClicked;
            if (_creditsBackButton != null) _creditsBackButton.clicked -= OnCreditsBackClicked;
        }

        private void OnPlayClicked()
        {
            if (_rulesModal != null) _rulesModal.style.display = DisplayStyle.Flex;
        }

        private void OnStartClicked()
        {
            SceneManager.LoadScene("Gameplay");
        }

        private void OnRulesBackClicked()
        {
            if (_rulesModal != null) _rulesModal.style.display = DisplayStyle.None;
        }

        private void OnCreditsClicked()
        {
            ShowView(_creditsView);
        }

        private void OnCreditsBackClicked()
        {
            ShowView(_menuView);
        }

        private void OnQuitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        /// <summary>Shows the menu or credits view and hides the other. The rules
        /// modal is independent — it opens on top of whichever view is active.</summary>
        private void ShowView(VisualElement view)
        {
            if (_menuView != null) _menuView.style.display = view == _menuView ? DisplayStyle.Flex : DisplayStyle.None;
            if (_creditsView != null) _creditsView.style.display = view == _creditsView ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>One toggle row per rule in <see cref="RuleCatalog.Toggleable"/>, persisted
        /// through <see cref="RuleSettings"/>. Core rules are not listed — they are always on.</summary>
        private void BuildRulesList()
        {
            if (_rulesList == null) return;

            _rulesList.Clear();
            foreach (RuleCatalog.Entry entry in RuleCatalog.Toggleable)
            {
                var row = new VisualElement();
                row.AddToClassList("rule-row");

                var text = new VisualElement();
                text.AddToClassList("rule-text");

                var nameLabel = new Label(entry.DisplayName);
                nameLabel.AddToClassList("rule-name");
                var descriptionLabel = new Label(entry.Description);
                descriptionLabel.AddToClassList("rule-description");
                text.Add(nameLabel);
                text.Add(descriptionLabel);

                var toggle = new Toggle();
                toggle.value = RuleSettings.IsEnabled(entry.Id);
                string ruleId = entry.Id;
                toggle.RegisterValueChangedCallback(change => RuleSettings.SetEnabled(ruleId, change.newValue));

                row.Add(text);
                row.Add(toggle);
                _rulesList.Add(row);
            }
        }
    }
}
