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

        public event Action OnShootEvent;

        // ── UI element references ─────────────────────────────────────

        private Button _shootButton;
        private Toggle _lockAimToggle;
        private Toggle _lockPowerToggle;

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
            }

            if (_root != null)
                _root.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
        }

        // ── Binding helpers ───────────────────────────────────────────

        private void BindShootButton()
        {
            _shootButton = _root.Q<Button>("shoot-button");
            if (_shootButton == null) return;

            _shootButton.clicked += OnShootClicked;
            SetShootButtonActive(false);
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

            // Delay initial hit-dot placement until layout is resolved
            _spinBall.RegisterCallback<GeometryChangedEvent>(_ => RefreshHitDot());

            // Close panel when clicking anywhere outside it
            _root.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
        }

        // ── Compact spin button ───────────────────────────────────────

        private void OnSpinButtonPressed(PointerDownEvent evt)
        {
            evt.StopPropagation(); // Don't bubble to root close-handler
            SetPanelOpen(!_panelOpen);
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

        /// <summary>Close panel when the player taps anywhere outside it.</summary>
        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (!_panelOpen) return;

            // If the click target is inside the panel, keep it open
            if (_spinPanel != null && _spinPanel.Contains(evt.target as VisualElement))
                return;

            SetPanelOpen(false);
        }

        // ── Hit-point drag on the large ball ─────────────────────────

        private void OnHitPointPointerDown(PointerDownEvent evt)
        {
            evt.StopPropagation();
            _isDraggingHitPoint = true;
            _spinBall.CapturePointer(evt.pointerId);
            UpdateSpinFromLocalPosition(evt.localPosition);
        }

        private void OnHitPointPointerMove(PointerMoveEvent evt)
        {
            if (!_isDraggingHitPoint) return;
            UpdateSpinFromLocalPosition(evt.localPosition);
        }

        private void OnHitPointPointerUp(PointerUpEvent evt)
        {
            if (!_isDraggingHitPoint) return;
            _isDraggingHitPoint = false;
            _spinBall.ReleasePointer(evt.pointerId);
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
            if (_spinButton == null || _spinButtonDot == null) return;

            float w = _spinButton.resolvedStyle.width;
            float h = _spinButton.resolvedStyle.height;
            if (w == 0f || h == 0f) return;

            float xPos = (CurrentSpin.x + 1f) * 0.5f * w;
            float yPos = (1f - CurrentSpin.y) * 0.5f * h;

            _spinButtonDot.style.left = xPos - ButtonDotHalf;
            _spinButtonDot.style.top  = yPos - ButtonDotHalf;
        }

        /// <summary>Position the large hit-dot on the expanded ball to mirror CurrentSpin.</summary>
        private void RefreshHitDot()
        {
            if (_spinBall == null || _spinHitDot == null) return;

            float w = _spinBall.resolvedStyle.width;
            float h = _spinBall.resolvedStyle.height;
            if (w == 0f || h == 0f) return;

            float xPos = (CurrentSpin.x + 1f) * 0.5f * w;
            float yPos = (1f - CurrentSpin.y) * 0.5f * h;

            _spinHitDot.style.left = xPos - HitDotHalf;
            _spinHitDot.style.top  = yPos - HitDotHalf;
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
