using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EightBall.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GameplayUIController : MonoBehaviour
    {
        // ── Public state ──────────────────────────────────────────────

        public bool IsAimLocked { get; private set; }
        public bool IsPowerLocked { get; private set; }

        /// <summary>Hit position on the cue ball face. x/y each in [-1, 1]. (0,0) = centre.</summary>
        public Vector2 CurrentSpin { get; private set; } = Vector2.zero;

        /// <summary>True while the current pointer press belongs to the HUD — it started on an
        /// interactive HUD element or was used to close the spin panel. InputManager uses this
        /// to keep table aim/power from reacting to HUD presses.</summary>
        public bool IsPointerPressOnUI { get; private set; }

        public event Action OnShootEvent;

        // ── UI element references ─────────────────────────────────────

        private Button _shootButton;
        private bool _isShootUnlocked;
        private Button _lockAimButton;
        private Button _lockPowerButton;
        private VisualElement[] _playerPanels;
        private VisualElement[] _playerBallRows;
        private Label[] _playerOpenLabels;
        private Label _turnBanner;
        private VisualElement _bottomBar;
        private Label _shootHint;

        // Game-over overlay
        private VisualElement _gameOver;
        private Label _gameOverTitle;
        private Button _gameOverPlayAgain;
        private Button _gameOverMenu;

        // Compact cue ball button
        private VisualElement _spinButton;
        private VisualElement _spinButtonDot;

        // Expanded interactive panel
        private VisualElement _spinPanel;
        private VisualElement _spinBall;
        private VisualElement _spinHitDot;

        // Root — used to detect clicks outside the panel
        private VisualElement _root;

        // ── Internal state ────────────────────────────────────────────

        private bool _panelOpen;
        private bool _isDraggingHitPoint;
        private Coroutine _turnBannerRoutine;
        private Coroutine _shootDenyRoutine;

        // ── Half-size constants (pixels) for indicator placement ──────
        private const float ButtonDotHalf = 7f;   // half of 14px dot
        private const float HitDotHalf = 11f;     // half of 22px dot
        private const string LockedClass = "hud-button--locked";
        private const string TurnHiddenClass = "turn-banner--hidden";
        private const string TurnFadingClass = "turn-banner--fading";
        private const string PanelActiveClass = "player-panel--active";
        private const string PanelPulseClass = "player-panel--pulse";
        // How long the banner stays fully visible before dissolving (seconds)
        private const float TurnBannerHoldDuration = 1.1f;
        private const string ShootHintHiddenClass = "shoot-hint--hidden";
        private const string ShootHintFadingClass = "shoot-hint--fading";
        // How long the denied-shoot hint stays fully visible before dissolving (seconds)
        private const float ShootHintHoldDuration = 1.2f;
        private const string DefaultShootHintText = "Drag back from the cue ball to set power";
        private const string GameOverHiddenClass = "game-over--hidden";
        private const float ShootShakeAmplitude = 8f;
        private const int ShootShakeHalfCycles = 4;
        private const float ShootShakeHalfCycleDuration = 0.05f;

        // ── Lifecycle ─────────────────────────────────────────────────

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            BindShootButton();
            BindLockButtons();
            BindSpinButton();
            BindSpinPanel();
            BindGameOverOverlay();
            BindPlayerPanels();
            _turnBanner = _root.Q<Label>("turn-banner");
            _bottomBar = _root.Q<VisualElement>("bottom-bar");
            _shootHint = _root.Q<Label>("shoot-hint");
        }

        private void OnDisable()
        {
            if (_shootButton != null)
                _shootButton.clicked -= OnShootClicked;

            if (_lockAimButton != null)
                _lockAimButton.clicked -= OnAimLockClicked;

            if (_lockPowerButton != null)
                _lockPowerButton.clicked -= OnPowerLockClicked;

            if (_spinButton != null)
                _spinButton.UnregisterCallback<PointerDownEvent>(OnSpinButtonPressed);

            if (_spinBall != null)
            {
                _spinBall.UnregisterCallback<PointerDownEvent>(OnHitPointPointerDown);
                _spinBall.UnregisterCallback<PointerMoveEvent>(OnHitPointPointerMove);
                _spinBall.UnregisterCallback<PointerUpEvent>(OnHitPointPointerUp);
                _spinBall.UnregisterCallback<PointerCancelEvent>(OnHitPointPointerUp);
            }

            if (_root != null)
            {
                _root.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
                _root.UnregisterCallback<PointerUpEvent>(OnRootPointerReleased, TrickleDown.TrickleDown);
                _root.UnregisterCallback<PointerCancelEvent>(OnRootPointerReleased, TrickleDown.TrickleDown);
            }

            if (_gameOverPlayAgain != null)
                _gameOverPlayAgain.clicked -= OnPlayAgainClicked;

            if (_gameOverMenu != null)
                _gameOverMenu.clicked -= OnMenuClicked;

            HideShootDeniedFeedback();
        }

        /// <summary>Sets the denied-shoot hint's text; null restores the default power hint.</summary>
        public void SetShootDeniedHint(string text)
        {
            if (_shootHint != null)
                _shootHint.text = text ?? DefaultShootHintText;
        }

        /// <summary>Shows the full-screen game-over overlay with the winner's colour.</summary>
        public void ShowGameOver(int winnerIndex, string winnerName)
        {
            if (_gameOver == null) return;

            if (_gameOverTitle != null)
            {
                _gameOverTitle.text = $"{winnerName} Wins!";
                _gameOverTitle.EnableInClassList("game-over-title--p1", winnerIndex == 0);
                _gameOverTitle.EnableInClassList("game-over-title--p2", winnerIndex == 1);
            }

            _gameOver.RemoveFromClassList(GameOverHiddenClass);
        }

        private void BindGameOverOverlay()
        {
            _gameOver = _root.Q<VisualElement>("game-over");
            _gameOverTitle = _root.Q<Label>("game-over-title");
            _gameOverPlayAgain = _root.Q<Button>("game-over-play-again");
            _gameOverMenu = _root.Q<Button>("game-over-menu");

            if (_gameOverPlayAgain != null) _gameOverPlayAgain.clicked += OnPlayAgainClicked;
            if (_gameOverMenu != null) _gameOverMenu.clicked += OnMenuClicked;
        }

        private void OnPlayAgainClicked() => SceneManager.LoadScene("Gameplay");

        private void OnMenuClicked() => SceneManager.LoadScene("MainMenu");

        // ── Binding helpers ───────────────────────────────────────────

        private void BindShootButton()
        {
            _shootButton = _root.Q<Button>("shoot-button");
            if (_shootButton == null) return;

            _shootButton.clicked += OnShootClicked;
            SetShootButtonUnlocked(false);
        }

        /// <summary>Centres the spin so the next shot starts without carry-over english.</summary>
        public void ResetSpin()
        {
            CurrentSpin = Vector2.zero;
            RefreshHitDot();
            RefreshButtonDot();
        }

        /// <summary>Unlock aim and power so the next turn starts with both free.</summary>
        public void UnlockAimAndPower()
        {
            IsAimLocked = false;
            IsPowerLocked = false;
            RefreshLockButtons();
        }

        /// <summary>
        /// Shows or hides the interactive input HUD (lock toggles, shoot button, spin
        /// button). The player panels stay visible. Hiding also closes the spin panel.
        /// </summary>
        public void SetInputHudVisible(bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (_bottomBar != null) _bottomBar.style.display = display;
            if (_spinButton != null) _spinButton.style.display = display;

            if (!visible)
            {
                SetPanelOpen(false);
                HideShootDeniedFeedback();
            }
        }

        /// <summary>
        /// Announces a turn change: lights the new player's panel, pulses it, and flashes
        /// a centre-screen banner with the player's name that fades out.
        /// </summary>
        public void SetTurnPlayer(int playerIndex, string playerName)
        {
            if (_playerPanels == null) return;

            for (int i = 0; i < _playerPanels.Length; i++)
                _playerPanels[i]?.EnableInClassList(PanelActiveClass, i == playerIndex);

            if (_turnBanner != null)
            {
                _turnBanner.text = $"{playerName}'s Turn";
                _turnBanner.EnableInClassList("turn-banner--p1", playerIndex == 0);
                _turnBanner.EnableInClassList("turn-banner--p2", playerIndex == 1);
                _turnBanner.RemoveFromClassList(TurnFadingClass);
                _turnBanner.RemoveFromClassList(TurnHiddenClass);
            }

            if (_turnBannerRoutine != null) StopCoroutine(_turnBannerRoutine);
            _turnBannerRoutine = StartCoroutine(TurnChangeRoutine(playerIndex));
        }

        /// <summary>Pulses the active player's panel and shows the banner, then dissolves it out.</summary>
        private IEnumerator TurnChangeRoutine(int playerIndex)
        {
            VisualElement panel = _playerPanels[playerIndex];
            panel?.AddToClassList(PanelPulseClass);
            yield return new WaitForSeconds(0.25f);
            panel?.RemoveFromClassList(PanelPulseClass);

            yield return new WaitForSeconds(TurnBannerHoldDuration);

            if (_turnBanner != null)
            {
                _turnBanner.AddToClassList(TurnFadingClass);
                yield return new WaitForSeconds(0.4f);
                _turnBanner.AddToClassList(TurnHiddenClass);
                _turnBanner.RemoveFromClassList(TurnFadingClass);
            }
        }

        /// <summary>
        /// Shows one player's remaining balls as dot sprites of the balls they still owe.
        /// While the table is open (no group assigned) an "Open table" note stands in for
        /// the dots; once a player's whole group is down, only the 8 is shown.
        /// </summary>
        public void SetPlayerPanel(int playerIndex, bool isOpen, IReadOnlyList<int> remainingBalls)
        {
            if (_playerBallRows == null) return;

            VisualElement row = _playerBallRows[playerIndex];
            Label openLabel = _playerOpenLabels[playerIndex];
            if (row == null) return;

            if (openLabel != null) openLabel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
            row.style.display = isOpen ? DisplayStyle.None : DisplayStyle.Flex;
            if (isOpen) return;

            row.Clear();
            foreach (int ballNumber in remainingBalls)
            {
                var dot = new VisualElement();
                dot.AddToClassList("ball-dot");
                dot.AddToClassList($"ball-dot--{ballNumber}");
                row.Add(dot);
            }
        }

        private void BindPlayerPanels()
        {
            _playerPanels = new VisualElement[2];
            _playerBallRows = new VisualElement[2];
            _playerOpenLabels = new Label[2];

            for (int i = 0; i < 2; i++)
            {
                _playerPanels[i] = _root.Q<VisualElement>($"player-panel-{i}");
                _playerBallRows[i] = _root.Q<VisualElement>($"player-panel-balls-{i}");
                _playerOpenLabels[i] = _root.Q<Label>($"player-panel-open-{i}");
            }
        }

        private void BindLockButtons()
        {
            _lockAimButton = _root.Q<Button>("lock-aim-toggle");
            if (_lockAimButton != null)
                _lockAimButton.clicked += OnAimLockClicked;

            _lockPowerButton = _root.Q<Button>("lock-power-toggle");
            if (_lockPowerButton != null)
                _lockPowerButton.clicked += OnPowerLockClicked;
        }

        private void OnAimLockClicked() => SetAimLocked(!IsAimLocked);

        private void OnPowerLockClicked() => SetPowerLocked(!IsPowerLocked);

        private void SetAimLocked(bool locked)
        {
            IsAimLocked = locked;
            RefreshLockButtons();
        }

        private void SetPowerLocked(bool locked)
        {
            IsPowerLocked = locked;
            RefreshLockButtons();
        }

        /// <summary>Shows the padlock badge and accent while a lock button is engaged.</summary>
        private void RefreshLockButtons()
        {
            if (_lockAimButton != null)
                _lockAimButton.EnableInClassList(LockedClass, IsAimLocked);

            if (_lockPowerButton != null)
                _lockPowerButton.EnableInClassList(LockedClass, IsPowerLocked);
        }

        private void BindSpinButton()
        {
            _spinButton = _root.Q<VisualElement>("spin-button");
            _spinButtonDot = _root.Q<VisualElement>("spin-button-dot");

            if (_spinButton == null) return;

            _spinButton.RegisterCallback<PointerDownEvent>(OnSpinButtonPressed);

            // Delay initial dot placement until layout is resolved
            _spinButton.RegisterCallback<GeometryChangedEvent>(_ => RefreshButtonDot());
        }

        private void BindSpinPanel()
        {
            _spinPanel = _root.Q<VisualElement>("spin-panel");
            _spinBall  = _root.Q<VisualElement>("spin-ball");
            _spinHitDot = _root.Q<VisualElement>("spin-hit-dot");

            if (_spinBall == null) return;

            _spinBall.RegisterCallback<PointerDownEvent>(OnHitPointPointerDown);
            _spinBall.RegisterCallback<PointerMoveEvent>(OnHitPointPointerMove);
            _spinBall.RegisterCallback<PointerUpEvent>(OnHitPointPointerUp);
            _spinBall.RegisterCallback<PointerCancelEvent>(OnHitPointPointerUp);

            // Delay initial hit-dot placement until layout is resolved
            _spinBall.RegisterCallback<GeometryChangedEvent>(_ => RefreshHitDot());

            // Track which presses the HUD owns, and close the panel if a pointer
            // lands outside it (e.g. a second finger while a spin gesture is active)
            _root.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerUpEvent>(OnRootPointerReleased, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerCancelEvent>(OnRootPointerReleased, TrickleDown.TrickleDown);
        }

        // ── Compact spin button ───────────────────────────────────────

        /// <summary>
        /// Opening the panel is its own tap: press shows the panel and the release
        /// ends that gesture without touching spin. The next press-drag-release on
        /// the ball sets the spin and closes the panel (<see cref="OnHitPointPointerUp"/>).
        /// </summary>
        private void OnSpinButtonPressed(PointerDownEvent evt)
        {
            SetPanelOpen(true);
        }

        // ── Expanded panel open/close ─────────────────────────────────

        private void SetPanelOpen(bool open)
        {
            _panelOpen = open;

            if (_spinPanel == null) return;

            if (open)
                _spinPanel.RemoveFromClassList("spin-panel--hidden");
            else
                _spinPanel.AddToClassList("spin-panel--hidden");
        }

        /// <summary>
        /// Root-level press handler: marks presses the HUD owns so InputManager ignores
        /// them for aim/power, and closes the panel when a press lands outside it.
        /// </summary>
        private void OnRootPointerDown(PointerDownEvent evt)
        {
            var target = evt.target as VisualElement;

            IsPointerPressOnUI = IsOverInteractiveElement(target) || _panelOpen;

            if (!_panelOpen) return;

            // The spin button toggles the panel itself; pressing the panel keeps it open
            if (_spinButton != null && _spinButton.Contains(target)) return;
            if (_spinPanel != null && _spinPanel.Contains(target)) return;

            SetPanelOpen(false);
        }

        private void OnRootPointerReleased(EventBase evt)
        {
            IsPointerPressOnUI = false;
        }

        /// <summary>True when the pressed element sits inside an interactive HUD element.</summary>
        private bool IsOverInteractiveElement(VisualElement target)
        {
            while (target != null)
            {
                if (target == _shootButton || target == _lockAimButton || target == _lockPowerButton
                    || target == _spinButton || target == _spinPanel)
                {
                    return true;
                }
                target = target.parent;
            }
            return false;
        }

        // ── Hit-point drag on the large ball ─────────────────────────

        private void OnHitPointPointerDown(PointerDownEvent evt)
        {
            _isDraggingHitPoint = true;
            _spinBall.CapturePointer(evt.pointerId);
            UpdateSpinFromLocalPosition(evt.localPosition);
        }

        private void OnHitPointPointerMove(PointerMoveEvent evt)
        {
            if (!_isDraggingHitPoint) return;
            UpdateSpinFromLocalPosition(evt.localPosition);
        }

        private void OnHitPointPointerUp(EventBase evt)
        {
            if (!_isDraggingHitPoint) return;
            _isDraggingHitPoint = false;

            // The pointer is captured, so Up/Cancel arrive here even off the element
            if (evt is PointerUpEvent pointerUp)
                _spinBall.ReleasePointer(pointerUp.pointerId);
            else if (evt is PointerCancelEvent pointerCancel)
                _spinBall.ReleasePointer(pointerCancel.pointerId);

            // The spin gesture ends with the finger lift, so the panel closes with it
            SetPanelOpen(false);
        }

        private void UpdateSpinFromLocalPosition(Vector2 localPos)
        {
            float width  = _spinBall.resolvedStyle.width;
            float height = _spinBall.resolvedStyle.height;
            if (width == 0f || height == 0f) return;

            float nx = Mathf.Clamp((localPos.x / width)  * 2f - 1f, -1f, 1f);
            float ny = Mathf.Clamp(1f - (localPos.y / height) * 2f, -1f, 1f); // UI Y is flipped

            Vector2 spin = new Vector2(nx, ny);
            if (spin.magnitude > 1f)
                spin.Normalize(); // Constrain to unit circle (cue ball face)

            CurrentSpin = spin;
            RefreshHitDot();
            RefreshButtonDot();
        }

        // ── Dot positioning helpers ───────────────────────────────────

        /// <summary>Position the small dot on the compact button to mirror CurrentSpin.</summary>
        private void RefreshButtonDot()
        {
            PlaceSpinDot(_spinButton, _spinButtonDot, ButtonDotHalf);
        }

        /// <summary>Position the large hit-dot on the expanded ball to mirror CurrentSpin.</summary>
        private void RefreshHitDot()
        {
            PlaceSpinDot(_spinBall, _spinHitDot, HitDotHalf);
        }

        /// <summary>
        /// Place a spin indicator dot inside its ball, mirroring CurrentSpin.
        /// Absolute children are positioned relative to the parent's padding box, so the
        /// border must be subtracted from the resolved size (box-sizing is border-box).
        /// </summary>
        private void PlaceSpinDot(VisualElement ball, VisualElement dot, float dotHalf)
        {
            if (ball == null || dot == null) return;

            var style = ball.resolvedStyle;
            float w = ball.resolvedStyle.width - style.borderLeftWidth - style.borderRightWidth;
            float h = ball.resolvedStyle.height - style.borderTopWidth - style.borderBottomWidth;
            if (w <= 0f || h <= 0f) return;

            float xPos = (CurrentSpin.x + 1f) * 0.5f * w;
            float yPos = (1f - CurrentSpin.y) * 0.5f * h;

            dot.style.left = xPos - dotHalf;
            dot.style.top  = yPos - dotHalf;
        }

        // ── Shoot button ──────────────────────────────────────────────

        /// <summary>Arms the shoot button (green). While unarmed it shows red and ignores clicks.</summary>
        public void SetShootButtonUnlocked(bool isUnlocked)
        {
            _isShootUnlocked = isUnlocked;

            if (_shootButton != null)
                _shootButton.EnableInClassList(LockedClass, !isUnlocked);
        }

        private void OnShootClicked()
        {
            if (!_isShootUnlocked)
            {
                ShowShootDeniedFeedback();
                return;
            }

            OnShootEvent?.Invoke();
            SetShootButtonUnlocked(false);
        }

        // ── Denied-shoot feedback ─────────────────────────────────────

        /// <summary>
        /// Feedback for tapping the shoot button while it is locked (power not set):
        /// shakes the red button, then flashes the shoot-hint label above the bottom bar.
        /// </summary>
        private void ShowShootDeniedFeedback()
        {
            if (_shootDenyRoutine != null) StopCoroutine(_shootDenyRoutine);
            _shootDenyRoutine = StartCoroutine(ShootDeniedRoutine());
        }

        /// <summary>Reverts a running denied-feedback display to its hidden rest state.</summary>
        private void HideShootDeniedFeedback()
        {
            if (_shootDenyRoutine != null)
            {
                StopCoroutine(_shootDenyRoutine);
                _shootDenyRoutine = null;
            }

            if (_shootButton != null)
                _shootButton.style.translate = new Translate(0f, 0f, 0f);

            if (_shootHint != null)
            {
                _shootHint.AddToClassList(ShootHintHiddenClass);
                _shootHint.RemoveFromClassList(ShootHintFadingClass);
            }
        }

        private IEnumerator ShootDeniedRoutine()
        {
            // Horizontal shake so the tap reads as "denied", not as a dead button
            if (_shootButton != null)
            {
                for (int i = 0; i < ShootShakeHalfCycles; i++)
                {
                    float direction = (i % 2 == 0) ? 1f : -1f;
                    _shootButton.style.translate = new Translate(ShootShakeAmplitude * direction, 0f, 0f);
                    yield return new WaitForSeconds(ShootShakeHalfCycleDuration);
                }
                _shootButton.style.translate = new Translate(0f, 0f, 0f);
            }

            if (_shootHint != null)
            {
                _shootHint.RemoveFromClassList(ShootHintFadingClass);
                _shootHint.RemoveFromClassList(ShootHintHiddenClass);
            }

            yield return new WaitForSeconds(ShootHintHoldDuration);

            if (_shootHint != null)
            {
                _shootHint.AddToClassList(ShootHintFadingClass);
                yield return new WaitForSeconds(0.3f);
                _shootHint.AddToClassList(ShootHintHiddenClass);
                _shootHint.RemoveFromClassList(ShootHintFadingClass);
            }
        }
    }
}
