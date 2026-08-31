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

        /// <summary>One straight run of rail between two pocket mouths.</summary>
        public readonly struct RailSegment
        {
            public readonly string Name;
            public readonly Vector2 Center;
            public readonly Vector2 Size;

            public RailSegment(string name, Vector2 center, Vector2 size)
            {
                Name = name;
                Center = center;
                Size = size;
            }
        }

        /// <summary>
        /// The six rail runs, with a gap left at every pocket so a ball can actually enter one.
        /// Long rails run corner-to-mid pocket; side rails run corner-to-corner.
        /// </summary>
        public static RailSegment[] GetRailSegments()
        {
            float longRailY = (FeltHeight + RailThickness) / 2f;
            float sideRailX = (FeltWidth + RailThickness) / 2f;

            // A long rail spans from just past a corner pocket to just short of the mid pocket.
            float longStart = -HalfFeltWidth + PocketMouthHalfWidth;
            float longEnd = -PocketMouthHalfWidth;
            float longLength = longEnd - longStart;
            float longOffsetX = (longStart + longEnd) / 2f;

            float sideLength = FeltHeight - PocketMouthHalfWidth * 2f;

            var longSize = new Vector2(longLength, RailThickness);
            var sideSize = new Vector2(RailThickness, sideLength);

            return new[]
            {
                new RailSegment("Rail_Bottom_Left",  new Vector2( longOffsetX, -longRailY), longSize),
                new RailSegment("Rail_Bottom_Right", new Vector2(-longOffsetX, -longRailY), longSize),
                new RailSegment("Rail_Top_Left",     new Vector2( longOffsetX,  longRailY), longSize),
                new RailSegment("Rail_Top_Right",    new Vector2(-longOffsetX,  longRailY), longSize),
                new RailSegment("Rail_Left",         new Vector2(-sideRailX, 0f), sideSize),
                new RailSegment("Rail_Right",        new Vector2( sideRailX, 0f), sideSize),
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
