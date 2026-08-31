using System.Collections;
using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Motion state for a single pool ball. Owns the rigidbody, launches the ball on a shot,
    /// reports whether it is still rolling so the shot lifecycle knows when the table settles,
    /// and drops out of play when it is pocketed.
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

        /// <summary>Angular speed (radians/sec) below which a ball's spin counts as stopped.</summary>
        public const float StopAngularSpeedThreshold = 0.5f;

        private const float SinkDuration = 0.18f;
        private const float SinkEndScale = 0.25f;

        public bool IsMoving { get; private set; }

        /// <summary>True once the ball has dropped into a pocket and left play.</summary>
        public bool IsPocketed { get; private set; }

        private Rigidbody2D _body;
        private Collider2D _collider;
        private SpriteRenderer _renderer;
        private Vector3 _fullScale = Vector3.one;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _fullScale = transform.localScale;
        }

        /// <summary>
        /// Sends the ball off along <paramref name="direction"/> at <paramref name="speed"/> units/sec.
        /// A ball is always at rest when it is struck, so the impulse mass x speed produces exactly
        /// this velocity; assigning it directly keeps the shot deterministic and frame-order independent.
        /// </summary>
        public void Launch(Vector2 direction, float speed)
        {
            if (_body == null || IsPocketed) return;

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

        /// <summary>
        /// Kills any remaining sub-threshold movement and rotation. Called once the table has
        /// settled, so the next turn starts with every ball perfectly at rest instead of
        /// drifting or spinning imperceptibly.
        /// </summary>
        public void Stabilize()
        {
            if (!IsMoving) StopImmediately();
        }

        /// <summary>
        /// Takes the ball out of play: physics off first — so it stops shoving the balls still on
        /// the table — then a short sink before it disappears down the hole.
        /// </summary>
        public void Drop()
        {
            if (IsPocketed) return;

            IsPocketed = true;
            StopImmediately();

            if (_collider != null) _collider.enabled = false;
            if (_body != null) _body.simulated = false;

            _fullScale = transform.localScale;
            StartCoroutine(SinkIntoPocket());
        }

        /// <summary>Puts a pocketed ball back on the table (a scratched cue ball, or a re-rack).</summary>
        public void Restore()
        {
            StopAllCoroutines();

            IsPocketed = false;
            transform.localScale = _fullScale;
            SetSpriteAlpha(1f);

            gameObject.SetActive(true);
            if (_collider != null) _collider.enabled = true;
            if (_body != null) _body.simulated = true;

            StopImmediately();
        }

        private void FixedUpdate()
        {
            if (_body == null || IsPocketed) return;

            // Safety net: a ball that squeezed past a pocket mouth must not roll away forever,
            // which would also leave the table permanently unsettled.
            if (HasLeftTheTable())
            {
                Drop();
                return;
            }

            if (_body.linearVelocity.sqrMagnitude > StopSpeedThreshold * StopSpeedThreshold
                || Mathf.Abs(_body.angularVelocity) > StopAngularSpeedThreshold)
            {
                IsMoving = true;
                return;
            }

            if (IsMoving) StopImmediately();
        }

        private bool HasLeftTheTable()
        {
            Vector3 position = transform.localPosition;
            return Mathf.Abs(position.x) > TableLayout.TableWidth * 0.5f + TableLayout.BallDiameter
                || Mathf.Abs(position.y) > TableLayout.TableHeight * 0.5f + TableLayout.BallDiameter;
        }

        private IEnumerator SinkIntoPocket()
        {
            Vector3 sunkScale = _fullScale * SinkEndScale;

            for (float elapsed = 0f; elapsed < SinkDuration; elapsed += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / SinkDuration);
                transform.localScale = Vector3.Lerp(_fullScale, sunkScale, progress);
                SetSpriteAlpha(1f - progress);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        private void SetSpriteAlpha(float alpha)
        {
            if (_renderer == null) return;

            Color color = _renderer.color;
            color.a = alpha;
            _renderer.color = color;
        }
    }
}
