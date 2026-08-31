using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Authoritative constants for table geometry.
    /// All values in Unity units. Ball radius = 0.25u.
    /// </summary>
    public static class TableLayout
    {
        // Inner playfield (felt surface)
        public const float FeltWidth  = 9.0f;
        public const float FeltHeight = 4.5f;

        // Rail thickness
        public const float RailThickness = 0.4f;

        // Thickness of the green cushion pad on the felt-facing side of each rail
        public const float CushionPadThickness = 0.15f;

        // Ball
        public const float BallRadius   = 0.25f;
        public const float BallDiameter = BallRadius * 2f;

        // Pocket radius (trigger)
        public const float PocketRadius = 0.4f;

        // Outer table (felt + rails)
        public const float TableWidth  = FeltWidth  + RailThickness * 2f;
        public const float TableHeight = FeltHeight + RailThickness * 2f;

        // Half extents for convenience
        public const float HalfFeltWidth  = FeltWidth  / 2f;
        public const float HalfFeltHeight = FeltHeight / 2f;

        // Foot spot (where rack apex goes) — 3/4 along the length from head
        public static readonly Vector2 FootSpot = new Vector2(FeltWidth * 0.25f, 0f);

        // Head spot (cue ball placed here)
        public static readonly Vector2 HeadSpot = new Vector2(-FeltWidth * 0.25f, 0f);

        // Six pocket positions (world space, table centered at origin)
        public static readonly Vector2[] PocketPositions = {
            // Corners
            new Vector2(-HalfFeltWidth, -HalfFeltHeight), // Bottom-left
            new Vector2(-HalfFeltWidth,  HalfFeltHeight), // Top-left
            new Vector2( HalfFeltWidth, -HalfFeltHeight), // Bottom-right
            new Vector2( HalfFeltWidth,  HalfFeltHeight), // Top-right
            // Sides (mid pockets)
            new Vector2(0f, -HalfFeltHeight),             // Bottom-mid
            new Vector2(0f,  HalfFeltHeight),             // Top-mid
        };

        // Half-width of the opening left in the rails at each pocket (~1.8 ball widths across).
        // Wide enough for a ball to roll in, narrow enough that the rails still play like cushions.
        public const float PocketMouthHalfWidth = 0.45f;

        /// <summary>
        /// One straight run of rail. <see cref="Center"/>/<see cref="Size"/> is the cushion-only
        /// span that the collider and cushion pad use; <see cref="VisualCenter"/>/<see cref="VisualSize"/>
        /// is the wooden run, which extends past the pockets (to the felt edges at the corners).
        /// </summary>
        public readonly struct RailSegment
        {
            public readonly string Name;
            public readonly Vector2 Center;
            public readonly Vector2 Size;
            public readonly Vector2 VisualCenter;
            public readonly Vector2 VisualSize;

            public RailSegment(string name, Vector2 center, Vector2 size, Vector2 visualCenter, Vector2 visualSize)
            {
                Name = name;
                Center = center;
                Size = size;
                VisualCenter = visualCenter;
                VisualSize = visualSize;
            }
        }

        /// <summary>
        /// The six rail runs. The collider keeps a gap at every pocket so a ball can enter one,
        /// but the wood itself runs on past the pockets so the frame closes around the holes.
        /// </summary>
        public static RailSegment[] GetRailSegments()
        {
            float longRailY = (FeltHeight + RailThickness) / 2f;
            float sideRailX = (FeltWidth + RailThickness) / 2f;

            // The playable cushion spans from just past a corner pocket to just short of the mid pocket.
            float longStart = -HalfFeltWidth + PocketMouthHalfWidth;
            float longEnd = -PocketMouthHalfWidth;
            float longLength = longEnd - longStart;
            float longOffsetX = (longStart + longEnd) / 2f;

            float sideLength = FeltHeight - PocketMouthHalfWidth * 2f;

            // Left-hand long runs stretch from the felt's outer edge to the mid-pocket centre;
            // right-hand runs mirror them. Side runs stretch corner to corner. The runs stop at
            // the felt edges — the corner caps (TableSetup.SetupRailCorners) close and round
            // the corners, so the wood must not extend underneath them.
            float longVisualLength = HalfFeltWidth;

            var longSize = new Vector2(longLength, RailThickness);
            var sideSize = new Vector2(RailThickness, sideLength);
            var longVisualSize = new Vector2(longVisualLength, RailThickness);
            var sideVisualSize = new Vector2(RailThickness, FeltHeight);

            return new[]
            {
                new RailSegment("Rail_Bottom_Left",  new Vector2( longOffsetX, -longRailY), longSize,
                                                     new Vector2(-longVisualLength / 2f, -longRailY), longVisualSize),
                new RailSegment("Rail_Bottom_Right", new Vector2(-longOffsetX, -longRailY), longSize,
                                                     new Vector2( longVisualLength / 2f, -longRailY), longVisualSize),
                new RailSegment("Rail_Top_Left",     new Vector2( longOffsetX,  longRailY), longSize,
                                                     new Vector2(-longVisualLength / 2f,  longRailY), longVisualSize),
                new RailSegment("Rail_Top_Right",    new Vector2(-longOffsetX,  longRailY), longSize,
                                                     new Vector2( longVisualLength / 2f,  longRailY), longVisualSize),
                new RailSegment("Rail_Left",         new Vector2(-sideRailX, 0f), sideSize,
                                                     new Vector2(-sideRailX, 0f), sideVisualSize),
                new RailSegment("Rail_Right",        new Vector2( sideRailX, 0f), sideSize,
                                                     new Vector2( sideRailX, 0f), sideVisualSize),
            };
        }

        // Standard 8-ball rack order (5-row triangle, apex at FootSpot)
        // Row offsets from apex (using equilateral triangle packing)
        public static Vector2[] GetRackPositions()
        {
            float d  = BallDiameter;
            float dy = d * Mathf.Sin(60f * Mathf.Deg2Rad); // row spacing

            Vector2 apex = FootSpot;

            // 5-row rack: 1, 2, 3, 4, 5 balls
            var positions = new Vector2[15];
            int idx = 0;
            for (int row = 0; row < 5; row++)
            {
                float rowX = apex.x + row * dy;
                float startY = apex.y - row * d * 0.5f;
                for (int col = 0; col <= row; col++)
                {
                    positions[idx++] = new Vector2(rowX, startY + col * d);
                }
            }
            return positions;
        }

        // Ball order in rack: WPA standard placement rules.
        // Index matches rack position (0 = apex).
        // 1 = apex, 8 = center, 15/9 = back corners, rest random.
        // Returns ball numbers (1-15) for each rack position.
        public static int[] GetRackOrder()
        {
            return new int[]
            {
                1,          // row 0, pos 0  — apex
                2, 3,       // row 1
                4, 8, 5,    // row 2 — 8-ball in center
                6, 7, 9, 10, // row 3
                11, 12, 13, 14, 15 // row 4
            };
        }
    }
}
