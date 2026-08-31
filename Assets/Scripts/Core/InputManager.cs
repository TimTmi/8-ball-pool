using UnityEngine;
using UnityEngine.InputSystem;
using EightBall.UI;
using EightBall.Gameplay;

namespace EightBall.Core
{
    public class InputManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameplayUIController _uiController;
        private TableSetup _tableSetup;
        private CueController _cueController;

        [Header("Sensitivity")]
        [SerializeField] private float _aimSensitivity = 0.5f;
        [SerializeField] private float _powerSensitivity = 0.002f;

        public float CurrentAimAngle { get; private set; }
        public float CurrentPower { get; private set; } // Normalized 0 to 1

        private Vector2 _lastPointerPosition;
        private bool _isDragging;

        /// <summary>Aiming is only allowed once every ball has come to rest.</summary>
        private bool CanAim => _cueController == null || _cueController.IsTableSettled;

        private void Start()
        {
            if (_uiController != null)
            {
                _uiController.OnShootEvent += HandleShoot;
            }
            _tableSetup = FindAnyObjectByType<TableSetup>();
            _cueController = FindAnyObjectByType<CueController>();
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
            UpdateCueVisuals();
        }

        private void HandleDragInput()
        {
            // Ignore input while the shot is still playing out
            if (!CanAim)
            {
                _isDragging = false;
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null) return;

            bool spinActive = _uiController != null && _uiController.IsSpinInteracting;

            // Simple touch/mouse input via new Input System
            if (pointer.press.wasPressedThisFrame)
            {
                // Don't start an aim/power drag if the press landed on spin UI
                if (!spinActive)
                {
                    _isDragging = true;
                    _lastPointerPosition = pointer.position.ReadValue();
                }
            }
            else if (pointer.press.isPressed && _isDragging)
            {
                // Stop updating aim/power if the finger moved onto spin UI mid-drag
                if (!spinActive)
                {
                    Vector2 currentPosition = pointer.position.ReadValue();
                    Vector2 delta = currentPosition - _lastPointerPosition;
                    _lastPointerPosition = currentPosition;

                    UpdateAimAndPower(delta);
                }
                else
                {
                    // Keep position in sync so there's no jump when spin interaction ends
                    _lastPointerPosition = pointer.position.ReadValue();
                }
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

        private void UpdateCueVisuals()
        {
            if (_tableSetup == null || _tableSetup.CueStick == null || _tableSetup.CueBall == null) return;

            GameObject cueStick = _tableSetup.CueStick;

            // The cue is only on the table while the player is aiming
            bool isAiming = CanAim;
            if (cueStick.activeSelf != isAiming) cueStick.SetActive(isAiming);
            if (!isAiming) return;

            Transform cueBall = _tableSetup.CueBall.transform;

            // Calculate the direction the player is aiming
            Vector3 aimDir = Quaternion.Euler(0f, 0f, CurrentAimAngle) * Vector3.right;

            // Cue stick is 8 units long, so center is 4 units from tip
            float cueLength = 8f; 
            float minDistance = (cueLength * 0.5f) + TableLayout.BallRadius + 0.1f;
            float maxDistance = minDistance + 2.5f; // Pull back max 2.5 units
            float currentDistance = Mathf.Lerp(minDistance, maxDistance, CurrentPower);

            // Position cue stick behind the cue ball, pointing towards the aim direction
            cueStick.transform.position = cueBall.position - aimDir * currentDistance;
            cueStick.transform.rotation = Quaternion.Euler(0f, 0f, CurrentAimAngle);
        }

        private void HandleShoot()
        {
            if (_cueController == null)
            {
                Debug.LogError("[InputManager] No CueController in the scene — the shot cannot be played.", this);
                return;
            }

            if (!_cueController.Shoot(CurrentAimAngle, CurrentPower)) return;

            // Reset for next turn
            CurrentPower = 0f;
            if (_uiController != null)
            {
                _uiController.SetShootButtonActive(false);
            }
        }
    }
}
