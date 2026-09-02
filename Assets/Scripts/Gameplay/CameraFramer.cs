using System.Collections;
using UnityEngine;
using EightBall.Core;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Reframes the main camera at the start of every turn (and once on game start) so
    /// the whole table and the area needed for a full-power drag are just in frame —
    /// see <see cref="CameraFraming"/>. The frame depends on the cue ball's position,
    /// so it is recomputed each turn (ball in hand starts from the head spot). Turn
    /// changes tween from the previous frame to the new one; the initial frame snaps.
    /// </summary>
    public class CameraFramer : MonoBehaviour
    {
        [Header("Tween")]
        [Tooltip("Seconds the camera takes to move from the previous turn's frame to the new one.")]
        [SerializeField] private float _tweenDuration = 0.4f;

        private Camera _camera;
        private TableSetup _tableSetup;
        private InputManager _inputManager;
        private TurnManager _turnManager;
        private Coroutine _tween;

        private void Start()
        {
            _camera = Camera.main;
            _tableSetup = FindAnyObjectByType<TableSetup>();
            _inputManager = FindAnyObjectByType<InputManager>();
            _turnManager = FindAnyObjectByType<TurnManager>();

            if (_camera == null || _tableSetup == null || _inputManager == null)
            {
                Debug.LogError("[CameraFramer] Needs a main camera, a TableSetup and an InputManager.", this);
                enabled = false;
                return;
            }

            // The first turn does not go through OnTurnStarted (player 1 starts by default)
            if (_tableSetup.CueBall != null) Frame(animate: false);
            else StartCoroutine(FrameOnceSpawned());

            if (_turnManager != null) _turnManager.OnTurnStarted += HandleTurnStarted;
        }

        private void OnDestroy()
        {
            if (_turnManager != null) _turnManager.OnTurnStarted -= HandleTurnStarted;
        }

        private void HandleTurnStarted(int playerIndex) => Frame(animate: true);

        /// <summary>Falls back to the first frame when this Start runs before TableSetup
        /// has spawned the cue ball (script execution order is undefined between them).</summary>
        private IEnumerator FrameOnceSpawned()
        {
            yield return null;
            if (_tableSetup.CueBall != null) Frame(animate: false);
        }

        private void Frame(bool animate)
        {
            if (_tableSetup.CueBall == null) return;

            (Vector2 center, float size) = CameraFraming.Compute(
                _tableSetup.CueBall.transform.position,
                _inputManager.FullPowerReach,
                _camera.aspect);

            var targetPosition = new Vector3(center.x, center.y, _camera.transform.position.z);

            if (!animate || _tweenDuration <= 0f)
            {
                // Snap path only runs before the first turn, so no tween to cancel
                _camera.orthographicSize = size;
                _camera.transform.position = targetPosition;
                return;
            }

            if (_tween != null) StopCoroutine(_tween);
            _tween = StartCoroutine(TweenTo(targetPosition, size));
        }

        private IEnumerator TweenTo(Vector3 targetPosition, float targetSize)
        {
            Vector3 startPosition = _camera.transform.position;
            float startSize = _camera.orthographicSize;

            for (float elapsed = 0f; elapsed < _tweenDuration; elapsed += Time.deltaTime)
            {
                // SmoothStep eases in and out; position and zoom move together
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _tweenDuration);
                _camera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                _camera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                yield return null;
            }

            _camera.transform.position = targetPosition;
            _camera.orthographicSize = targetSize;
            _tween = null;
        }
    }
}
