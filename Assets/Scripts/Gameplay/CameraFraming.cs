using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Pure camera-framing math: the orthographic view that shows the whole table plus
    /// the area around the cue ball needed to reach full power (a disc of
    /// <c>cancelRadius + maxPull</c> — the pointer must be able to reach that distance
    /// from the ball in any aim direction), fitted tightly to the viewport aspect.
    /// </summary>
    public static class CameraFraming
    {
        /// <summary>View centre and orthographic size (half-height) that frame the table
        /// and the cue ball's full-power disc just inside the viewport.</summary>
        public static (Vector2 Center, float Size) Compute(Vector2 cueBall, float fullPowerReach, float aspect)
        {
            float tableHalfWidth = TableLayout.TableWidth / 2f;
            float tableHalfHeight = TableLayout.TableHeight / 2f;

            // Union of the table rect and the full-power disc's bounding box
            float minX = Mathf.Min(-tableHalfWidth, cueBall.x - fullPowerReach);
            float maxX = Mathf.Max(tableHalfWidth, cueBall.x + fullPowerReach);
            float minY = Mathf.Min(-tableHalfHeight, cueBall.y - fullPowerReach);
            float maxY = Mathf.Max(tableHalfHeight, cueBall.y + fullPowerReach);

            var center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);

            // Orthographic size is half the viewport height; widen for narrow aspects
            float halfWidth = (maxX - minX) / 2f;
            float halfHeight = (maxY - minY) / 2f;
            float size = Mathf.Max(halfHeight, halfWidth / aspect);

            return (center, size);
        }
    }
}
