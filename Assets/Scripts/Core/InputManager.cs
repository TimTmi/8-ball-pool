using System.Collections;
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
        private TurnManager _turnManager;

        [Header("Power")]
        [Tooltip("Max cue pull-back distance (world units). Pointer distance from the cue ball maps 1:1 to the cue pull and is clamped here; full power is reached at this distance.")]
        [SerializeField] private float _maxPullDistance = 2.5f;

        [Header("Shot")]
        [Tooltip("How long the cue takes to drive forward into the ball. Hard shots stroke faster than this, soft ones slower.")]
        [SerializeField] private float _strikeDuration = 0.07f;

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

        /// <summary>True while the cue is being driven into the ball, which owns the cue transform.</summary>
        private bool _isStriking;

        // Aim/power state captured when the current drag started, restored if the player
        // drags into the cancel zone around the cue ball
        private float _dragStartAimAngle;
        private float _dragStartPower;
        private bool _dragStartHasAim;

        /// <summary>Aiming is only allowed once every ball has come to rest.</summary>
        private bool CanAim => !_isStriking && (_cueController == null || _cueController.IsTableSettled);

        /// <summary>Cue length in world units; the sprite is authored at this size.</summary>
        private const float CueLength = 8f;

        /// <summary>Distance from the cue ball centre to the cue centre with the tip resting at the ball.</summary>
        private static float CueRestDistance => (CueLength * 0.5f) + TableLayout.BallRadius + 0.1f;

        private void Start()
        {
            _camera = Camera.main;
            if (_uiController != null)
            {
                _uiController.OnShootEvent += HandleShoot;
            }
            _tableSetup = FindAnyObjectByType<TableSetup>();
            _cueController = FindAnyObjectByType<CueController>();
            _turnManager = FindAnyObjectByType<TurnManager>();
            if (_turnManager != null)
            {
                _turnManager.OnTurnStarted += HandleTurnStarted;
            }
        }

        private void OnDestroy()
        {
            if (_uiController != null)
            {
                _uiController.OnShootEvent -= HandleShoot;
            }

            if (_turnManager != null)
            {
                _turnManager.OnTurnStarted -= HandleTurnStarted;
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

            // The strike coroutine drives the cue itself while it is running
            if (_isStriking) return;

            GameObject cueStick = _tableSetup.CueStick;

            // The cue is only on the table once the player has aimed during this turn
            bool showCue = CanAim && _hasAim;
            if (cueStick.activeSelf != showCue) cueStick.SetActive(showCue);
            if (!showCue) return;

            Transform cueBall = _tableSetup.CueBall.transform;

            // Calculate the direction the player is aiming
            Vector3 aimDir = Quaternion.Euler(0f, 0f, CurrentAimAngle) * Vector3.right;

            // Cue centre sits half its length back from the tip
            float minDistance = CueRestDistance;
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

            // Guard re-entry: the button is hidden during the stroke, but a queued click must not
            // start a second one.
            if (_isStriking || !_cueController.IsTableSettled) return;

            StartCoroutine(StrikeAndShoot(CurrentAimAngle, CurrentPower));
        }

        /// <summary>
        /// Drives the cue forward into the ball and plays the shot at the moment of contact.
        /// Without this the cue simply blinks out on the frame the shot is taken, because
        /// <see cref="CanAim"/> goes false as soon as the table stops being settled.
        /// </summary>
        private IEnumerator StrikeAndShoot(float aimAngle, float power)
        {
            _isStriking = true;
            yield return StrokeCue(aimAngle, power);

            bool shotPlayed = _cueController.Shoot(aimAngle, power);
            _isStriking = false;

            if (!shotPlayed) yield break;

            // Reset for next turn: no power and no cue until the player aims again
            CurrentPower = 0f;
            _hasAim = false;
            if (_uiController != null)
            {
                _uiController.SetShootButtonActive(false);
                _uiController.UnlockAimAndPower();
                _uiController.SetInputHudVisible(false);
            }
        }

        /// <summary>Slides the cue from its pulled-back position up to the ball.</summary>
        private IEnumerator StrokeCue(float aimAngle, float power)
        {
            GameObject cueStick = _tableSetup != null ? _tableSetup.CueStick : null;
            GameObject cueBall = _tableSetup != null ? _tableSetup.CueBall : null;
            if (cueStick == null || cueBall == null) yield break;

            Vector3 aimDir = Quaternion.Euler(0f, 0f, aimAngle) * Vector3.right;
            Vector3 contact = cueBall.transform.position - aimDir * CueRestDistance;
            Vector3 pulledBack = contact - aimDir * (_maxPullDistance * Mathf.Clamp01(power));

            cueStick.SetActive(true);
            cueStick.transform.rotation = Quaternion.Euler(0f, 0f, aimAngle);

            // A hard shot strokes faster than a soft one, so power reads in the stroke too
            float duration = _strikeDuration * Mathf.Lerp(1.7f, 0.6f, Mathf.Clamp01(power));

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                // Squared easing: the cue accelerates into the ball rather than gliding in
                float progress = Mathf.Clamp01(elapsed / duration);
                cueStick.transform.position = Vector3.Lerp(pulledBack, contact, progress * progress);
                yield return null;
            }

            cueStick.transform.position = contact;
        }

        private void HandleTurnStarted(int playerIndex)
        {
            if (_uiController != null && _turnManager != null)
            {
                _uiController.SetInputHudVisible(true);
                _uiController.SetTurnLabelText($"{_turnManager.CurrentPlayerName}'s Turn");
            }
        }
    }
}
