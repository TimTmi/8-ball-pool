using System.Collections;
using UnityEngine;
using EightBall.Core;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Reframes the main camera at the start of every turn (and once on game start) so
    /// the whole table and the area needed for a full-power drag are just in frame —
    /// see <see cref="CameraFraming"/>. The frame depends on the cue ball's position,
    /// so it is recomputed each turn (ball in hand starts from the head spot).
    /// </summary>
    public class CameraFramer : MonoBehaviour
    {
        private Camera _camera;
        private TableSetup _tableSetup;
        private InputManager _inputManager;
        private TurnManager _turnManager;

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
            if (_tableSetup.CueBall != null) Frame();
            else StartCoroutine(FrameOnceSpawned());

            if (_turnManager != null) _turnManager.OnTurnStarted += HandleTurnStarted;
        }

        private void OnDestroy()
        {
            if (_turnManager != null) _turnManager.OnTurnStarted -= HandleTurnStarted;
        }

        private void HandleTurnStarted(int playerIndex) => Frame();

        /// <summary>Falls back to the first frame when this Start runs before TableSetup
        /// has spawned the cue ball (script execution order is undefined between them).</summary>
        private IEnumerator FrameOnceSpawned()
        {
            yield return null;
            if (_tableSetup.CueBall != null) Frame();
        }

        private void Frame()
        {
            if (_tableSetup.CueBall == null) return;

            (Vector2 center, float size) = CameraFraming.Compute(
                _tableSetup.CueBall.transform.position,
                _inputManager.FullPowerReach,
                _camera.aspect);

            _camera.orthographicSize = size;
            _camera.transform.position = new Vector3(center.x, center.y, _camera.transform.position.z);
        }
    }
}
