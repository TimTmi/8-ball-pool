using System;
using UnityEngine;
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
        private Toggle _lockAimToggle;
        private Toggle _lockPowerToggle;
        private Label _turnLabel;
        private VisualElement _bottomBar;

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

        // ── Half-size constants (pixels) for indicator placement ──────
        private const float ButtonDotHalf = 7f;   // half of 14px dot
        private const float HitDotHalf = 11f;     // half of 22px dot

        // ── Lifecycle ─────────────────────────────────────────────────

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            BindShootButton();
            BindLockToggles();
            BindSpinButton();
            BindSpinPanel();
            _turnLabel = _root.Q<Label>("player-turn-label");
            _bottomBar = _root.Q<VisualElement>("bottom-bar");
        }

        private void OnDisable()
        {
            if (_shootButton != null)
                _shootButton.clicked -= OnShootClicked;

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
        }

        // ── Binding helpers ───────────────────────────────────────────

        private void BindShootButton()
        {
            _shootButton = _root.Q<Button>("shoot-button");
            if (_shootButton == null) return;

            _shootButton.clicked += OnShootClicked;
            SetShootButtonActive(false);
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

            if (_lockAimToggle != null) _lockAimToggle.SetValueWithoutNotify(false);
            if (_lockPowerToggle != null) _lockPowerToggle.SetValueWithoutNotify(false);
        }

        /// <summary>
        /// Shows or hides the interactive input HUD (lock toggles, shoot button, spin
        /// button). The turn label stays visible. Hiding also closes the spin panel.
        /// </summary>
        public void SetInputHudVisible(bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (_bottomBar != null) _bottomBar.style.display = display;
            if (_spinButton != null) _spinButton.style.display = display;

            if (!visible)
            {
                SetPanelOpen(false);
                SetShootButtonActive(false);
            }
        }

        /// <summary>Sets the top-bar label naming the player whose turn it is.</summary>
        public void SetTurnLabelText(string text)
        {
            if (_turnLabel != null) _turnLabel.text = text;
        }

        private void BindLockToggles()
        {
            _lockAimToggle = _root.Q<Toggle>("lock-aim-toggle");
            if (_lockAimToggle != null)
                _lockAimToggle.RegisterValueChangedCallback(evt => IsAimLocked = evt.newValue);

            _lockPowerToggle = _root.Q<Toggle>("lock-power-toggle");
            if (_lockPowerToggle != null)
                _lockPowerToggle.RegisterValueChangedCallback(evt => IsPowerLocked = evt.newValue);
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
                if (target == _shootButton || target == _lockAimToggle || target == _lockPowerToggle
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

        public void SetShootButtonActive(bool isActive)
        {
            if (_shootButton != null)
                _shootButton.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnShootClicked()
        {
            OnShootEvent?.Invoke();
            SetShootButtonActive(false);
        }
    }
}
