using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EightBall.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GameplayUIController : MonoBehaviour
    {
        public bool IsAimLocked { get; private set; }
        public bool IsPowerLocked { get; private set; }
        public Vector2 CurrentSpin { get; private set; } = Vector2.zero; // x: left/right, y: top/bottom (-1 to 1)

        public event Action OnShootEvent;

        private Button _shootButton;
        private Toggle _lockAimToggle;
        private Toggle _lockPowerToggle;
        private VisualElement _spinControlArea;
        private VisualElement _spinIndicator;

        private bool _isDraggingSpin;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            _shootButton = root.Q<Button>("shoot-button");
            if (_shootButton != null)
            {
                _shootButton.clicked += OnShootClicked;
                SetShootButtonActive(false); // Hidden initially
            }

            _lockAimToggle = root.Q<Toggle>("lock-aim-toggle");
            if (_lockAimToggle != null)
            {
                _lockAimToggle.RegisterValueChangedCallback(evt => IsAimLocked = evt.newValue);
            }

            _lockPowerToggle = root.Q<Toggle>("lock-power-toggle");
            if (_lockPowerToggle != null)
            {
                _lockPowerToggle.RegisterValueChangedCallback(evt => IsPowerLocked = evt.newValue);
            }

            _spinControlArea = root.Q<VisualElement>("spin-control-placeholder");
            if (_spinControlArea != null)
            {
                // Create a simple indicator dot
                _spinIndicator = new VisualElement();
                _spinIndicator.style.width = 10;
                _spinIndicator.style.height = 10;
                _spinIndicator.style.backgroundColor = Color.red;
                _spinIndicator.style.position = Position.Absolute;
                _spinIndicator.style.borderTopLeftRadius = 5;
                _spinIndicator.style.borderTopRightRadius = 5;
                _spinIndicator.style.borderBottomLeftRadius = 5;
                _spinIndicator.style.borderBottomRightRadius = 5;
                _spinControlArea.Add(_spinIndicator);

                _spinControlArea.RegisterCallback<PointerDownEvent>(OnSpinPointerDown);
                _spinControlArea.RegisterCallback<PointerMoveEvent>(OnSpinPointerMove);
                _spinControlArea.RegisterCallback<PointerUpEvent>(OnSpinPointerUp);
                _spinControlArea.RegisterCallback<PointerLeaveEvent>(OnSpinPointerUp);

                UpdateSpinIndicatorPosition();
            }
        }

        private void OnDisable()
        {
            if (_shootButton != null)
                _shootButton.clicked -= OnShootClicked;
            
            if (_lockAimToggle != null)
                _lockAimToggle.UnregisterValueChangedCallback(evt => IsAimLocked = evt.newValue);
            
            if (_lockPowerToggle != null)
                _lockPowerToggle.UnregisterValueChangedCallback(evt => IsPowerLocked = evt.newValue);

            if (_spinControlArea != null)
            {
                _spinControlArea.UnregisterCallback<PointerDownEvent>(OnSpinPointerDown);
                _spinControlArea.UnregisterCallback<PointerMoveEvent>(OnSpinPointerMove);
                _spinControlArea.UnregisterCallback<PointerUpEvent>(OnSpinPointerUp);
                _spinControlArea.UnregisterCallback<PointerLeaveEvent>(OnSpinPointerUp);
            }
        }

        private void OnSpinPointerDown(PointerDownEvent evt)
        {
            _isDraggingSpin = true;
            _spinControlArea.CapturePointer(evt.pointerId);
            UpdateSpinFromPointer(evt.localPosition);
        }

        private void OnSpinPointerMove(PointerMoveEvent evt)
        {
            if (_isDraggingSpin)
            {
                UpdateSpinFromPointer(evt.localPosition);
            }
        }

        private void OnSpinPointerUp(EventBase evt)
        {
            if (_isDraggingSpin)
            {
                _isDraggingSpin = false;
                if (evt is PointerUpEvent pointerUpEvent)
                {
                    _spinControlArea.ReleasePointer(pointerUpEvent.pointerId);
                }
                else if (evt is PointerLeaveEvent pointerLeaveEvent)
                {
                    _spinControlArea.ReleasePointer(pointerLeaveEvent.pointerId);
                }
            }
        }

        private void UpdateSpinFromPointer(Vector2 localPosition)
        {
            float width = _spinControlArea.resolvedStyle.width;
            float height = _spinControlArea.resolvedStyle.height;

            if (width == 0 || height == 0) return;

            // Map local position to -1 to 1 range
            float normalizedX = Mathf.Clamp((localPosition.x / width) * 2f - 1f, -1f, 1f);
            float normalizedY = Mathf.Clamp(1f - (localPosition.y / height) * 2f, -1f, 1f); // Y is flipped in UI

            // Restrict to circle
            Vector2 spin = new Vector2(normalizedX, normalizedY);
            if (spin.magnitude > 1f)
            {
                spin.Normalize();
            }

            CurrentSpin = spin;
            UpdateSpinIndicatorPosition();
        }

        private void UpdateSpinIndicatorPosition()
        {
            if (_spinControlArea == null || _spinIndicator == null) return;

            float width = _spinControlArea.resolvedStyle.width;
            float height = _spinControlArea.resolvedStyle.height;

            if (width == 0 || height == 0) return;

            // Convert spin (-1 to 1) back to local coordinates
            float xPos = (CurrentSpin.x + 1f) * 0.5f * width;
            float yPos = (1f - CurrentSpin.y) * 0.5f * height; // Y is flipped in UI

            _spinIndicator.style.left = xPos - 5f; // Center the 10x10 dot
            _spinIndicator.style.top = yPos - 5f;
        }

        public void SetShootButtonActive(bool isActive)
        {
            if (_shootButton != null)
            {
                _shootButton.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void OnShootClicked()
        {
            OnShootEvent?.Invoke();
            SetShootButtonActive(false); // Hide after shooting
        }
    }
}
