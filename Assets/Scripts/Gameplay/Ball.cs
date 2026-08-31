using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Motion state for a single pool ball. Owns the rigidbody, launches the ball on a shot,
    /// and reports whether it is still rolling so the shot lifecycle knows when the table settles.
    /// Added by <see cref="TableSetup"/> to every spawned ball.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Ball : MonoBehaviour
    {
        /// <summary>
        /// Speed (units/sec) below which a ball is snapped to a full stop. Linear damping decays
        /// velocity exponentially, so a ball never actually reaches zero on its own.
        /// </summary>
        public const float StopSpeedThreshold = 0.06f;

        public bool IsMoving { get; private set; }

        private Rigidbody2D _body;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// Sends the ball off along <paramref name="direction"/> at <paramref name="speed"/> units/sec.
        /// A ball is always at rest when it is struck, so the impulse mass x speed produces exactly
        /// this velocity; assigning it directly keeps the shot deterministic and frame-order independent.
        /// </summary>
        public void Launch(Vector2 direction, float speed)
        {
            if (_body == null) return;

            _body.WakeUp();
            _body.linearVelocity = direction.normalized * speed;
            IsMoving = true;
        }

        /// <summary>Cancels all motion immediately.</summary>
        public void StopImmediately()
        {
            if (_body == null) return;

            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            IsMoving = false;
        }

        private void FixedUpdate()
        {
            if (_body == null) return;

            if (_body.linearVelocity.sqrMagnitude > StopSpeedThreshold * StopSpeedThreshold)
            {
                IsMoving = true;
                return;
            }

            if (IsMoving) StopImmediately();
        }
    }
}
