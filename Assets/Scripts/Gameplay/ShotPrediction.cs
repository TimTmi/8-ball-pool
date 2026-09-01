using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Works out what the cue ball will run into if it is shot along a given aim direction.
    /// One sweep of the cue ball's own shape — exact, and cheap enough to run every frame while
    /// the player is aiming. Spin is not modelled yet, so the path is a straight line.
    /// </summary>
    public static class ShotPrediction
    {
        /// <summary>
        /// Sweep a hair under ball size so a ball already resting against the cue ball does not
        /// register as an instant hit. Kept tiny on purpose: the shortfall biases the predicted cut
        /// angle, and thin cuts are sensitive — at 0.98 the error reaches 5 degrees and the sweep
        /// misses the thinnest legal cuts outright, where at 0.999 it stays under a quarter degree.
        /// </summary>
        private const float CastRadiusScale = 0.999f;

        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[8];

        /// <summary>What the cue ball meets first.</summary>
        public readonly struct Result
        {
            /// <summary>False when nothing lies within range, in which case the path just runs out.</summary>
            public readonly bool HasContact;

            /// <summary>Where the cue ball's centre sits at the moment of contact.</summary>
            public readonly Vector2 ContactPoint;

            /// <summary>The ball that gets struck, or null when a rail comes first.</summary>
            public readonly Ball StruckBall;

            /// <summary>Direction the struck ball is sent, along the line joining the two centres.</summary>
            public readonly Vector2 StruckBallDirection;

            public Result(bool hasContact, Vector2 contactPoint, Ball struckBall, Vector2 struckBallDirection)
            {
                HasContact = hasContact;
                ContactPoint = contactPoint;
                StruckBall = struckBall;
                StruckBallDirection = struckBallDirection;
            }
        }

        /// <summary>
        /// Sweeps the cue ball from <paramref name="origin"/> along <paramref name="direction"/>.
        /// <paramref name="cueBallCollider"/> is excluded, or the cue ball would hit itself at once.
        /// </summary>
        public static Result Predict(Vector2 origin, Vector2 direction, Collider2D cueBallCollider, float maxDistance)
        {
            Vector2 heading = direction.normalized;
            var filter = new ContactFilter2D { useTriggers = false }; // pockets must not stop the line

            int hitCount = Physics2D.CircleCast(origin, TableLayout.BallRadius * CastRadiusScale, heading, filter, Hits, maxDistance);

            RaycastHit2D nearest = default;
            for (int i = 0; i < hitCount; i++)
            {
                if (Hits[i].collider == null || Hits[i].collider == cueBallCollider) continue;
                if (nearest.collider == null || Hits[i].distance < nearest.distance) nearest = Hits[i];
            }

            if (nearest.collider == null)
            {
                return new Result(false, origin + heading * maxDistance, null, Vector2.zero);
            }

            Vector2 contactPoint = nearest.centroid; // centre of the swept circle where it stopped
            var struckBall = nearest.collider.GetComponent<Ball>();

            // Equal masses: the struck ball leaves along the line joining the two centres.
            Vector2 struckBallDirection = struckBall != null
                ? ((Vector2)struckBall.transform.position - contactPoint).normalized
                : Vector2.zero;

            return new Result(true, contactPoint, struckBall, struckBallDirection);
        }
    }
}
