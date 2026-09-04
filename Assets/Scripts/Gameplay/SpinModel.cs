using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// How spin moves the cue ball. The live shot (<see cref="CueBallSpin"/>, <see cref="Cushion"/>)
    /// and the aim preview (<see cref="ShotPrediction"/>) both run through these formulas, which is
    /// what keeps the drawn curve honest — the prediction/simulation parity design.md asks for.
    ///
    /// Spin is the hit point on the cue ball face, matching the spin widget:
    /// x = side (+1 right), y = top (+1) through centre (0) to back (-1), inside the unit circle.
    ///
    /// Behaviour follows the 8 Ball Pool spin guide: top spin carries the cue ball on, back spin
    /// pulls it back, and side spin both curves the shot and swings the angle off a cushion —
    /// left spin moving the ball left from the cue ball's point of view.
    /// </summary>
    public static class SpinModel
    {
        /// <summary>How hard top/back spin drives the cue ball along the line of centres after contact.</summary>
        public const float ContactStrength = 0.45f;

        /// <summary>Sideways drift per second at full side spin, as a fraction of forward speed.</summary>
        public const float CurveRate = 0.2f;

        /// <summary>Sideways kick off a rail at full side spin, as a fraction of rebound speed.</summary>
        public const float CushionKick = 0.35f;

        /// <summary>Fraction of the side spin left after a rail has taken its bite.</summary>
        public const float CushionSpinRetained = 0.4f;

        /// <summary>Spin bleeds into the cloth at this rate per second.</summary>
        public const float DecayRate = 1.4f;

        private const float MinimumSpeedSqr = 0.0001f;

        /// <summary>
        /// The velocity the cue ball leaves a ball-to-ball contact with.
        /// <paramref name="lineOfCentres"/> runs from the cue ball to the ball it struck.
        /// </summary>
        public static Vector2 ContactVelocity(Vector2 incomingVelocity, Vector2 lineOfCentres, float topSpin)
        {
            if (lineOfCentres.sqrMagnitude < MinimumSpeedSqr) return incomingVelocity;

            Vector2 centres = lineOfCentres.normalized;

            // Equal masses with the object ball at rest: the component along the line of centres is
            // handed over and the cue ball keeps the tangent. That alone is the natural stun path.
            float transferred = Vector2.Dot(incomingVelocity, centres);
            Vector2 tangent = incomingVelocity - centres * transferred;

            // Top spin drives the cue ball on down that same line; back spin pulls it up it.
            return tangent + centres * (topSpin * ContactStrength * Mathf.Abs(transferred));
        }

        /// <summary>
        /// The sideways velocity change side spin adds over <paramref name="deltaTime"/>.
        /// Scaled by speed, because the curve is an angle off the aim line: a fixed acceleration
        /// would swing a soft shot sideways and barely bend a hard one.
        /// </summary>
        public static Vector2 Curve(Vector2 velocity, float sideSpin, float deltaTime)
        {
            if (sideSpin == 0f || velocity.sqrMagnitude < MinimumSpeedSqr) return Vector2.zero;

            return RightOf(velocity) * (sideSpin * CurveRate * velocity.magnitude * deltaTime);
        }

        /// <summary>
        /// Mirrors an incoming velocity off a rail and damps it in one step: the component along
        /// <paramref name="normal"/> is flipped and scaled by <paramref name="normalRetention"/>,
        /// the component along the rail kept and scaled by <paramref name="slideRetention"/>.
        ///
        /// For <see cref="ShotPrediction"/>, which has no solver to bounce the velocity for it the
        /// way <see cref="Cushion"/> does for the live shot — so the caller folds the collider
        /// restitution into <paramref name="normalRetention"/> before handing it over. Which way
        /// <paramref name="normal"/> faces makes no difference: flipping it flips the dot product
        /// with it too, and the reflected component comes out the same either way.
        /// </summary>
        public static Vector2 CushionReflect(Vector2 incomingVelocity, Vector2 normal, float normalRetention, float slideRetention)
        {
            Vector2 intoRail = normal * Vector2.Dot(incomingVelocity, normal);
            Vector2 alongRail = incomingVelocity - intoRail;

            return alongRail * slideRetention - intoRail * normalRetention;
        }

        /// <summary>
        /// Swings a rail rebound toward the side that was struck, which is what side spin is mostly
        /// for. Speed is preserved: a cushion never hands energy back, and the rail's own damping
        /// has already been applied by then.
        /// </summary>
        public static Vector2 CushionRebound(Vector2 reboundVelocity, float sideSpin)
        {
            if (sideSpin == 0f || reboundVelocity.sqrMagnitude < MinimumSpeedSqr) return reboundVelocity;

            float speed = reboundVelocity.magnitude;
            Vector2 kicked = reboundVelocity + RightOf(reboundVelocity) * (sideSpin * CushionKick * speed);

            return kicked.normalized * speed;
        }

        /// <summary>What is left of the spin after <paramref name="deltaTime"/> of cloth friction.</summary>
        public static Vector2 Decay(Vector2 spin, float deltaTime) => spin * Mathf.Exp(-DecayRate * deltaTime);

        /// <summary>Unit vector to the right of travel, looking along it from behind the ball.</summary>
        private static Vector2 RightOf(Vector2 velocity)
        {
            Vector2 direction = velocity.normalized;
            return new Vector2(direction.y, -direction.x);
        }
    }
}
