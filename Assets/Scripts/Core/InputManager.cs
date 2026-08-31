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

        [Header("Power")]
        [Tooltip("Max cue pull-back distance (world units). Pointer distance from the cue ball maps 1:1 to the cue pull and is clamped here; full power is reached at this distance.")]
        [SerializeField] private float _maxPullDistance = 2.5f;

        [Header("Cancel")]
        [Tooltip("Radius around the cue ball (world units) that cancels the current drag: while the pointer is inside, aim and power revert to their pre-drag values, and releasing here cancels the shot setup.")]
        [SerializeField] private float _cancelRadius = 0.6f;

        public float CurrentAimAngle { get; private set; }
        public float CurrentPower { get; private set; } // Normalized 0 to 1

        private Camera _camera;
        private bool _isDragging;
        /// <summary>True once the player has aimed during the current turn; the cue only shows then.</summary>
        private bool _hasAim;
        private PowerBar _powerBar;

        // Aim/power state captured when the current drag started, restored if the player
        // drags into the cancel zone around the cue ball
        private float _dragStartAimAngle;
        private float _dragStartPower;
        private bool _dragStartHasAim;

        /// <summary>Aiming is only allowed once every ball has come to rest.</summary>
        private bool CanAim => _cueController == null || _cueController.IsTableSettled;

        private void Start()
        {
            _camera = Camera.main;
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
            UpdatePowerBar();
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

            // The HUD reports presses it owns (on HUD elements, or used to close the spin panel)
            bool pressOnUI = _uiController != null && _uiController.IsPointerPressOnUI;

            // Simple touch/mouse input via new Input System
            if (pointer.press.wasPressedThisFrame)
            {
                // Don't start an aim/power drag if the press landed on the HUD
                if (!pressOnUI)
                {
                    _isDragging = true;
                    _dragStartAimAngle = CurrentAimAngle;
                    _dragStartPower = CurrentPower;
                    _dragStartHasAim = _hasAim;
                }
            }
            else if (pointer.press.isPressed && _isDragging)
            {
                // A press the HUD claims (including one that landed on it a frame late)
                // never becomes an aim/power drag
                if (pressOnUI)
                {
                    _isDragging = false;
                }
                else
                {
                    UpdateAimAndPower(pointer.position.ReadValue());
                }
            }
            else if (pointer.press.wasReleasedThisFrame)
            {
                if (_isDragging)
                {
                    _isDragging = false;

                    if (IsPointerInCancelZone(pointer.position.ReadValue()))
                    {
                        // Released in the cancel zone: discard the whole drag
                        RestorePreDragState();
                    }
                    else
                    {
                        // Show shoot button if we actually aimed/powered up
                        if (_uiController != null)
                        {
                            _uiController.SetShootButtonActive(true);
                        }
                    }
                }
            }
        }

        private bool IsPointerInCancelZone(Vector2 pointerPosition)
        {
            if (_tableSetup == null || _tableSetup.CueBall == null || _camera == null) return false;

            Vector3 screenPosition = new Vector3(pointerPosition.x, pointerPosition.y, -_camera.transform.position.z);
            Vector2 pointerWorld = _camera.ScreenToWorldPoint(screenPosition);
            return (pointerWorld - (Vector2)_tableSetup.CueBall.transform.position).sqrMagnitude
                <= _cancelRadius * _cancelRadius;
        }

        /// <summary>Reverts aim and power to the state captured when the current drag started.</summary>
        private void RestorePreDragState()
        {
            CurrentAimAngle = _dragStartAimAngle;
            CurrentPower = _dragStartPower;
            _hasAim = _dragStartHasAim;
        }

        /// <summary>
        /// Aims from the pointer through the cue ball (the finger drags on the cue's
        /// side, like pulling the stick back) and sets power from the pointer's
        /// distance to the cue ball, both in world space.
        /// </summary>
        private void UpdateAimAndPower(Vector2 pointerPosition)
        {
            if (_uiController == null) return;
            if (_tableSetup == null || _tableSetup.CueBall == null) return;
            if (_camera == null) return;

            Vector3 screenPosition = new Vector3(pointerPosition.x, pointerPosition.y, -_camera.transform.position.z);
            Vector2 pointerWorld = _camera.ScreenToWorldPoint(screenPosition);
            Vector2 toBall = (Vector2)_tableSetup.CueBall.transform.position - pointerWorld;

            // Pointer is in the cancel zone around the cue ball: revert to the pre-drag
            // aim/power until it leaves the zone (which resumes aiming from scratch)
            if (toBall.magnitude <= _cancelRadius)
            {
                RestorePreDragState();
                return;
            }

            _hasAim = true;

            if (!_uiController.IsAimLocked)
            {
                CurrentAimAngle = Mathf.Atan2(toBall.y, toBall.x) * Mathf.Rad2Deg;
            }

            if (!_uiController.IsPowerLocked)
            {
                // Pointer distance pulls the cue back 1:1, capped at max pull
                float pullDistance = Mathf.Min(toBall.magnitude, _maxPullDistance);
                CurrentPower = pullDistance / _maxPullDistance;
            }
        }

        private void UpdateCueVisuals()
        {
            if (_tableSetup == null || _tableSetup.CueStick == null || _tableSetup.CueBall == null) return;

            GameObject cueStick = _tableSetup.CueStick;

            // The cue is only on the table once the player has aimed during this turn
            bool showCue = CanAim && _hasAim;
            if (cueStick.activeSelf != showCue) cueStick.SetActive(showCue);
            if (!showCue) return;

            Transform cueBall = _tableSetup.CueBall.transform;

            // Calculate the direction the player is aiming
            Vector3 aimDir = Quaternion.Euler(0f, 0f, CurrentAimAngle) * Vector3.right;

            // Cue stick is 8 units long, so center is 4 units from tip
            float cueLength = 8f;
            float minDistance = (cueLength * 0.5f) + TableLayout.BallRadius + 0.1f;
            // Full power = cue pulled back by _maxPullDistance, matching the 1:1 pointer mapping
            float maxDistance = minDistance + _maxPullDistance;
            float currentDistance = Mathf.Lerp(minDistance, maxDistance, CurrentPower);

            // Position cue stick behind the cue ball, pointing towards the aim direction
            cueStick.transform.position = cueBall.position - aimDir * currentDistance;
            cueStick.transform.rotation = Quaternion.Euler(0f, 0f, CurrentAimAngle);
        }

        private void UpdatePowerBar()
        {
            // The bar is a live drag readout: only while dragging, and only when
            // the player can still change the power it represents
            bool showPowerBar = CanAim && _isDragging && _uiController != null && !_uiController.IsPowerLocked;
            if (!showPowerBar)
            {
                if (_powerBar != null) _powerBar.Hide();
                return;
            }

            if (_powerBar == null && _tableSetup != null && _tableSetup.PowerBar != null)
                _powerBar = _tableSetup.PowerBar.GetComponent<PowerBar>();

            if (_powerBar != null && _tableSetup.CueBall != null)
                _powerBar.Show(_tableSetup.CueBall.transform.position, CurrentPower);
        }

        private void HandleShoot()
        {
            if (_cueController == null)
            {
                Debug.LogError("[InputManager] No CueController in the scene — the shot cannot be played.", this);
                return;
            }

            if (!_cueController.Shoot(CurrentAimAngle, CurrentPower)) return;

            // Reset for next turn: no power and no cue until the player aims again
            CurrentPower = 0f;
            _hasAim = false;
            if (_uiController != null)
            {
                _uiController.SetShootButtonActive(false);
                _uiController.UnlockAimAndPower();
            }
        }
    }
}
