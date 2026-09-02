using NUnit.Framework;
using UnityEngine;
using EightBall.Gameplay;

namespace EightBall.Tests
{
    /// <summary>
    /// EditMode tests for the turn-start camera framing math (pure, no play session).
    /// </summary>
    public class CameraFramingTests
    {
        private const float TableHalfWidth = TableLayout.TableWidth / 2f;
        private const float TableHalfHeight = TableLayout.TableHeight / 2f;

        [Test]
        public void BallAtHeadSpot_Landscape_FramesTablePlusPullDisc()
        {
            // Head spot cue ball, full-power reach 3.1u (0.6 cancel + 2.5 pull):
            // the disc pushes the frame left of the table and above/below it
            (Vector2 center, float size) = CameraFraming.Compute(TableLayout.HeadSpot, 3.1f, 16f / 9f);

            // The disc must fit: every point reach away from the ball inside the view
            Assert.LessOrEqual(center.x - size * (16f / 9f), TableLayout.HeadSpot.x - 3.1f);
            Assert.GreaterOrEqual(center.x + size * (16f / 9f), TableLayout.HeadSpot.x + 3.1f);
            Assert.LessOrEqual(center.y - size, TableLayout.HeadSpot.y - 3.1f);
            Assert.GreaterOrEqual(center.y + size, TableLayout.HeadSpot.y + 3.1f);

            // The table must fit too
            Assert.LessOrEqual(center.x - size * (16f / 9f), -TableHalfWidth);
            Assert.GreaterOrEqual(center.x + size * (16f / 9f), TableHalfWidth);
            Assert.LessOrEqual(center.y - size, -TableHalfHeight);
            Assert.GreaterOrEqual(center.y + size, TableHalfHeight);
        }

        [Test]
        public void BallAtHeadSpot_Landscape_DiscDominatesSoTableIsJustInFrame()
        {
            // At 16:9 the disc's height (2 x 3.1) is the binding constraint, so the
            // frame is exactly tall enough for the disc and the table is fully inside
            (Vector2 center, float size) = CameraFraming.Compute(TableLayout.HeadSpot, 3.1f, 16f / 9f);

            Assert.AreEqual(3.1f, size);
            Assert.AreEqual(0f, center.y); // vertically centred: disc top and bottom tie
        }

        [Test]
        public void BallAtTableCentre_ResultIsTableAlone()
        {
            // A reach small enough to stay inside the table bounds changes nothing;
            // at 16:9 the table's width (not height) drives the zoom
            float reach = 1f;
            (Vector2 center, float size) = CameraFraming.Compute(Vector2.zero, reach, 16f / 9f);

            Assert.AreEqual(0f, center.x);
            Assert.AreEqual(0f, center.y);
            Assert.AreEqual(Mathf.Max(TableHalfHeight, TableHalfWidth / (16f / 9f)), size);
        }

        [Test]
        public void NarrowAspect_WidthDominatesZoom()
        {
            // On a portrait viewport the frame must widen to fit the table's length,
            // which inflates the orthographic size beyond the height requirement
            (Vector2 center, float size) = CameraFraming.Compute(Vector2.zero, 0f, 0.5f);

            Assert.Greater(size, TableHalfHeight);
            Assert.GreaterOrEqual(size * 0.5f, TableHalfWidth);
        }
    }
}
