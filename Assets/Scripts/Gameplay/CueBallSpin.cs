using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Carries the spin the player dialled in and applies it to the live shot through
    /// <see cref="SpinModel"/>: side spin curves the ball as it travels and swings its angle off a
    /// rail, top/back spin fires on the first ball it strikes.
    /// Added by <see cref="TableSetup"/> to the cue ball only.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class CueBallSpin : MonoBehaviour
    {
        /// <summary>Spin still live on the ball. Decays as it rolls.</summary>
        public Vector2 Spin { get; private set; }

        /// <summary>Side component alone, which is what a rail rebound cares about.</summary>
        public float SideSpin => Spin.x;

        private Rigidbody2D _body;

        /// <summary>
        /// Velocity at the start of the physics step. Collision callbacks arrive after the solver
        /// has already resolved the bounce, so this is the only record of how hard the cue ball
        /// went into the object ball.
        /// </summary>
        private Vector2 _velocityEnteringStep;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        /// <summary>Arms the spin for the shot about to be taken.</summary>
        public void SetSpin(Vector2 spin) => Spin = spin;

        public void ClearSpin() => Spin = Vector2.zero;

        /// <summary>A rail takes most of the side spin with it; whatever is left keeps curving.</summary>
        public void SpendOnCushion() => Spin = new Vector2(Spin.x * SpinModel.CushionSpinRetained, Spin.y);

        private void FixedUpdate()
        {
            if (_body == null) return;

            _velocityEnteringStep = _body.linearVelocity;
            if (Spin == Vector2.zero) return;

            _body.linearVelocity += SpinModel.Curve(_velocityEnteringStep, Spin.x, Time.fixedDeltaTime);
            Spin = SpinModel.Decay(Spin, Time.fixedDeltaTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_body == null || Spin.y == 0f) return;

            var struckBall = collision.collider.GetComponent<Ball>();
            if (struckBall == null) return;

            Vector2 lineOfCentres = (Vector2)struckBall.transform.position - (Vector2)transform.position;
            if (lineOfCentres.sqrMagnitude < 0.0001f) return;

            // The engine has already produced the stun path, so only the top/back term it cannot
            // know about is added here. Same formula the aim preview draws with.
            Vector2 centres = lineOfCentres.normalized;
            float transferred = Vector2.Dot(_velocityEnteringStep, centres);
            _body.linearVelocity += centres * (Spin.y * SpinModel.ContactStrength * Mathf.Abs(transferred));

            // Top and back spin are spent on the first ball; side spin lives on.
            Spin = new Vector2(Spin.x, 0f);
        }
    }
}
