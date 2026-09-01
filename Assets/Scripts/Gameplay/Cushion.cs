using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Rail cushion. Physics 2D combines bounciness by taking the *higher* of the two materials,
    /// so the springy ball material (needed for realistic ball-to-ball contact) would otherwise
    /// make rails rebound just as hard. This drains the surplus energy from the ball immediately
    /// after the engine has resolved the bounce.
    /// Added by <see cref="TableSetup"/> to every rail.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Cushion : MonoBehaviour
    {
        [Tooltip("Fraction of the rebound speed (perpendicular to the rail) kept after a hit.")]
        [SerializeField, Range(0f, 1f)] private float _reboundRetention = 0.8f;

        [Tooltip("Fraction of the sliding speed (along the rail) kept after a hit.")]
        [SerializeField, Range(0f, 1f)] private float _slideRetention = 0.95f;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Rigidbody2D body = collision.rigidbody;
            if (body == null || collision.contactCount == 0) return;

            // Contact callbacks run after the physics step, so this is the post-bounce velocity.
            Vector2 velocity = body.linearVelocity;
            Vector2 outward = collision.GetContact(0).normal;

            // Which collider reported the contact decides the normal's sign — align it with the
            // direction the ball is actually leaving in.
            float reboundSpeed = Vector2.Dot(velocity, outward);
            if (reboundSpeed < 0f)
            {
                outward = -outward;
                reboundSpeed = -reboundSpeed;
            }

            Vector2 slide = velocity - outward * reboundSpeed;
            Vector2 rebound = slide * _slideRetention + outward * (reboundSpeed * _reboundRetention);

            // Side spin swings the angle off the rail — the main thing side spin is for. Handled
            // here rather than on the ball so one component owns the whole rail response and the
            // two callbacks cannot race each other for the same velocity.
            var cueBallSpin = collision.collider.GetComponent<CueBallSpin>();
            if (cueBallSpin != null)
            {
                rebound = SpinModel.CushionRebound(rebound, cueBallSpin.SideSpin);
                cueBallSpin.SpendOnCushion();
            }

            body.linearVelocity = rebound;
        }
    }
}
