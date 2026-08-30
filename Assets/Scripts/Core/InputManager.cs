using UnityEngine;
using UnityEngine.InputSystem;
using EightBall.UI;

namespace EightBall.Core
{
    public class InputManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameplayUIController _uiController;

        [Header("Sensitivity")]
        [SerializeField] private float _aimSensitivity = 0.5f;
        [SerializeField] private float _powerSensitivity = 0.002f;

        public float CurrentAimAngle { get; private set; }
        public float CurrentPower { get; private set; } // Normalized 0 to 1

        private Vector2 _lastPointerPosition;
        private bool _isDragging;

        private void Start()
        {
            if (_uiController != null)
            {
                _uiController.OnShootEvent += HandleShoot;
            }
        }

        private void OnDestroy()
        {
            if (_uiController != null)
            {
                _uiController.OnShootEvent -= HandleShoot;
            }
        }

        private void Update()
        {
            HandleDragInput();
        }

        private void HandleDragInput()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            // Simple touch/mouse input via new Input System
            if (pointer.press.wasPressedThisFrame)
            {
                _isDragging = true;
                _lastPointerPosition = pointer.position.ReadValue();
            }
            else if (pointer.press.isPressed && _isDragging)
            {
                Vector2 currentPosition = pointer.position.ReadValue();
                Vector2 delta = currentPosition - _lastPointerPosition;
                _lastPointerPosition = currentPosition;

                UpdateAimAndPower(delta);
            }
            else if (pointer.press.wasReleasedThisFrame)
            {
                if (_isDragging)
                {
                    _isDragging = false;
                    
                    // Show shoot button if we actually aimed/powered up
                    if (_uiController != null)
                    {
                        _uiController.SetShootButtonActive(true);
                    }
                }
            }
        }

        private void UpdateAimAndPower(Vector2 delta)
        {
            if (_uiController == null) return;

            // X-axis drag for Aim
            if (!_uiController.IsAimLocked)
            {
                CurrentAimAngle += delta.x * _aimSensitivity;
                CurrentAimAngle = Mathf.Repeat(CurrentAimAngle, 360f);
            }

            // Y-axis drag for Power (dragging down increases power, like pulling back)
            if (!_uiController.IsPowerLocked)
            {
                CurrentPower -= delta.y * _powerSensitivity;
                CurrentPower = Mathf.Clamp01(CurrentPower);
            }
        }

        private void HandleShoot()
        {
            Debug.Log($"Executing Shoot! Angle: {CurrentAimAngle}, Power: {CurrentPower}");
            
            // TODO: Apply force to cue ball in physics system
            
            // Reset for next turn
            CurrentPower = 0f;
        }
    }
}
