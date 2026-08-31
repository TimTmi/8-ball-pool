using System;
using System.Collections.Generic;
using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Strikes the cue ball and tracks when the table has come back to rest.
    /// Lives on the Table GameObject alongside <see cref="TableSetup"/>.
    /// </summary>
    [RequireComponent(typeof(TableSetup))]
    public class CueController : MonoBehaviour
    {
        [Header("Shot Speed (units/sec)")]
        [Tooltip("Cue ball speed at power 0 — a soft tap.")]
        [SerializeField] private float _minShotSpeed = 3f;

        [Tooltip("Cue ball speed at power 1 — a full-force break.")]
        [SerializeField] private float _maxShotSpeed = 26f;

        /// <summary>True while every ball is at rest, i.e. the player may aim and shoot.</summary>
        public bool IsTableSettled { get; private set; } = true;

        /// <summary>Raised on the step where the last moving ball comes to rest after a shot.</summary>
        public event Action OnTableSettled;

        private TableSetup _tableSetup;
        private readonly List<Ball> _balls = new List<Ball>(16);

        private void Awake()
        {
            _tableSetup = GetComponent<TableSetup>();
        }

        /// <summary>
        /// Strikes the cue ball along <paramref name="aimAngleDegrees"/> with a normalised
        /// <paramref name="power"/> (0–1). Returns false if the shot was rejected.
        /// </summary>
        public bool Shoot(float aimAngleDegrees, float power)
        {
            if (!IsTableSettled) return false;

            Ball cueBall = GetCueBall();
            if (cueBall == null)
            {
                Debug.LogError("[CueController] No cue ball with a Ball component on the table.", this);
                return false;
            }

            float angleRadians = aimAngleDegrees * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
            float speed = Mathf.Lerp(_minShotSpeed, _maxShotSpeed, Mathf.Clamp01(power));

            RefreshBalls();
            cueBall.Launch(direction, speed);
            IsTableSettled = false;
            return true;
        }

        private void FixedUpdate()
        {
            if (IsTableSettled || AnyBallMoving()) return;

            IsTableSettled = true;
            OnTableSettled?.Invoke();
        }

        private bool AnyBallMoving()
        {
            foreach (Ball ball in _balls)
            {
                if (ball != null && ball.IsMoving) return true;
            }
            return false;
        }

        private Ball GetCueBall()
        {
            GameObject cueBall = _tableSetup != null ? _tableSetup.CueBall : null;
            return cueBall != null ? cueBall.GetComponent<Ball>() : null;
        }

        /// <summary>Re-reads the spawned balls, so a re-rack is picked up without extra wiring.</summary>
        private void RefreshBalls()
        {
            _balls.Clear();

            GameObject[] ballObjects = _tableSetup != null ? _tableSetup.Balls : null;
            if (ballObjects == null) return;

            foreach (GameObject ballObject in ballObjects)
            {
                if (ballObject == null) continue;

                var ball = ballObject.GetComponent<Ball>();
                if (ball != null) _balls.Add(ball);
            }
        }
    }
}
