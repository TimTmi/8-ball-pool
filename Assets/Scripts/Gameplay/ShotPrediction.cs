using System.Collections.Generic;
using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Walks a shot forward through the same <see cref="SpinModel"/> the live shot uses and hands
    /// back the cue ball path in two pieces: up to the first ball it strikes, and the short run
    /// afterwards that shows what top or back spin does to it.
    ///
    /// Stepped rather than a single sweep because side spin curves the path — a straight cast
    /// could not draw it.
    /// </summary>
    public static class ShotPrediction
    {
        /// <summary>Hard stop on the walk, whichever limit bites first.</summary>
        private const int MaxSteps = 400;

        /// <summary>No straight run on this table is longer than the diagonal, near enough.</summary>
        private const float MaxPathDistance = 15f;

        /// <summary>The post-contact run is cut short on purpose: a tendency, not a solution.</summary>
        private const float MaxPostContactDistance = TableLayout.BallDiameter * 5f;

        /// <summary>The walk steps finely; the drawn line only needs a vertex now and then.</summary>
        private const float VertexSpacing = TableLayout.BallDiameter * 0.4f;

        /// <summary>
        /// Sweep a hair under ball size so a ball already resting against the cue ball does not
        /// register as an instant hit. Kept tiny on purpose: the shortfall biases the predicted cut
        /// angle, and thin cuts are sensitive — at 0.98 the error reaches 5 degrees and the sweep
        /// misses the thinnest legal cuts outright, where at 0.999 it stays under a quarter degree.
        /// </summary>
        private const float CastRadiusScale = 0.999f;

        private const float MinimumStepDistance = 0.0005f;

        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[8];

        /// <summary>
        /// Pockets must not stop the cast itself: a ball grazing the mouth rolls past. The walk
        /// instead ends where <see cref="Pocket"/> would actually capture, checked after each step.
        /// </summary>
        private static readonly ContactFilter2D CastFilter = new ContactFilter2D { useTriggers = false };

        private static readonly ContactFilter2D PocketFilter = new ContactFilter2D { useTriggers = true };

        private static readonly Collider2D[] PocketOverlaps = new Collider2D[4];

        /// <summary>Everything the prediction needs about the shot being lined up.</summary>
        public readonly struct Request
        {
            public readonly Vector2 Origin;
            public readonly Vector2 Direction;
            public readonly float Speed;
            public readonly Vector2 Spin;

            /// <summary>Rigidbody2D linear damping of the cue ball, so the preview slows as it will.</summary>
            public readonly float Damping;

            /// <summary>Excluded from the casts, or the cue ball would hit itself on step one.</summary>
            public readonly Collider2D CueBallCollider;

            public Request(Vector2 origin, Vector2 direction, float speed, Vector2 spin, float damping, Collider2D cueBallCollider)
            {
                Origin = origin;
                Direction = direction;
                Speed = speed;
                Spin = spin;
                Damping = damping;
                CueBallCollider = cueBallCollider;
            }
        }

        /// <summary>What the cue ball meets first.</summary>
        public readonly struct Result
        {
            /// <summary>True if the cue ball reaches a ball or a rail; false if it just runs out.</summary>
            public readonly bool HasContact;

            /// <summary>Where the cue ball centre sits at that contact.</summary>
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
        /// Fills <paramref name="approachPath"/> with the cue ball path up to first contact and
        /// <paramref name="cueAfterPath"/> with its short run afterwards.
        /// </summary>
        public static Result Predict(in Request request, List<Vector3> approachPath, List<Vector3> cueAfterPath)
        {
            approachPath.Clear();
            cueAfterPath.Clear();

            Vector2 position = request.Origin;
            Vector2 velocity = request.Direction.normalized * request.Speed;
            Vector2 spin = request.Spin;

            // Step at the physics rate, not a rate of our own: integrating damping and spin decay
            // on a coarser clock walks the line up to a couple of ball radii past where the ball
            // really stops.
            float stepTime = Time.fixedDeltaTime;

            // The ball we strike has to be ignored once we are past it: the walk resumes from a
            // point touching it, so the next cast would hit it again at zero distance and cut the
            // post-contact run off before it drew anything.
            Collider2D struckCollider = null;

            var result = new Result(false, position, null, Vector2.zero);
            bool contacted = false;
            float postContactDistance = 0f;
            float pathDistance = 0f;

            approachPath.Add(position);

            for (int step = 0; step < MaxSteps; step++)
            {
                float speed = velocity.magnitude;
                float distance = speed * stepTime;
                if (distance < MinimumStepDistance) break;

                RaycastHit2D hit = CastAhead(position, velocity / speed, distance, request.CueBallCollider, struckCollider);
                if (hit.collider != null)
                {
                    Ball struckBall = contacted ? null : hit.collider.GetComponent<Ball>();
                    position = hit.centroid;

                    // A rail, or a second ball: the preview has said enough.
                    if (struckBall == null)
                    {
                        AddVertex(contacted ? cueAfterPath : approachPath, position, true);

                        // Reaching a rail on the way in is still a contact worth marking, so the
                        // ghost shows where the cue ball comes to rest against the cushion. A rail
                        // after the first ball is only where the preview runs out, so the ghost
                        // stays on the ball contact it already found.
                        if (!contacted) result = new Result(true, position, null, Vector2.zero);

                        break;
                    }

                    // First contact: hand the walk over to the post-contact run.
                    AddVertex(approachPath, position, true);
                    cueAfterPath.Add(position);
                    contacted = true;
                    struckCollider = hit.collider;

                    Vector2 lineOfCentres = (Vector2)struckBall.transform.position - position;
                    result = new Result(true, position, struckBall, lineOfCentres.normalized);

                    velocity = SpinModel.ContactVelocity(velocity, lineOfCentres, spin.y);
                    spin = new Vector2(spin.x, 0f); // top/back spin is spent on the first ball
                    continue;
                }

                position += velocity * stepTime;

                // Aiming into a pocket: the mouth has no collider, so without this the walk ran
                // the line on through the hole and out the wooden rail. Stop where the ball sinks.
                if (IsInPocketCapture(position))
                {
                    AddVertex(contacted ? cueAfterPath : approachPath, position, true);

                    // Same shape as a rail contact: ghost ball at the mouth, no departure line.
                    if (!contacted) result = new Result(true, position, null, Vector2.zero);

                    break;
                }

                AddVertex(contacted ? cueAfterPath : approachPath, position, false);

                pathDistance += distance;
                if (pathDistance >= MaxPathDistance) break;

                if (contacted)
                {
                    postContactDistance += distance;
                    if (postContactDistance >= MaxPostContactDistance) break;
                }

                velocity += SpinModel.Curve(velocity, spin.x, stepTime);
                velocity /= 1f + request.Damping * stepTime; // the form Rigidbody2D damping uses
                spin = SpinModel.Decay(spin, stepTime);
            }

            // However the walk ended, the line should reach where the ball actually got to.
            AddVertex(contacted ? cueAfterPath : approachPath, position, true);

            return result;
        }

        private static RaycastHit2D CastAhead(Vector2 origin, Vector2 direction, float distance, Collider2D ignored, Collider2D alsoIgnored)
        {
            int hitCount = Physics2D.CircleCast(origin, TableLayout.BallRadius * CastRadiusScale, direction, CastFilter, Hits, distance);

            RaycastHit2D nearest = default;
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D collider = Hits[i].collider;
                if (collider == null || collider == ignored || collider == alsoIgnored) continue;
                if (nearest.collider == null || Hits[i].distance < nearest.distance) nearest = Hits[i];
            }
            return nearest;
        }

        /// <summary>
        /// True once the cue ball centre is close enough to a pocket for <see cref="Pocket"/> to
        /// drop it — the same centre-within-PocketRadius test the live capture uses.
        /// </summary>
        private static bool IsInPocketCapture(Vector2 position)
        {
            int count = Physics2D.OverlapCircle(position, TableLayout.BallRadius * 0.5f, PocketFilter, PocketOverlaps);

            for (int i = 0; i < count; i++)
            {
                if (PocketOverlaps[i].GetComponent<Pocket>() == null) continue;

                Vector2 offset = position - (Vector2)PocketOverlaps[i].transform.position;
                if (offset.sqrMagnitude <= TableLayout.PocketRadius * TableLayout.PocketRadius) return true;
            }
            return false;
        }

        private static void AddVertex(List<Vector3> path, Vector2 point, bool force)
        {
            if (path.Count > 0)
            {
                float spacingSqr = ((Vector2)path[path.Count - 1] - point).sqrMagnitude;
                float threshold = force ? MinimumStepDistance : VertexSpacing;

                if (spacingSqr < threshold * threshold) return;
            }

            path.Add(point);
        }
    }
}
