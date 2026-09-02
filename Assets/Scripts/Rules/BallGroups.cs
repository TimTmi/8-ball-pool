namespace EightBall.Rules
{
    /// <summary>Classification of ball numbers into rule groups.</summary>
    public static class BallGroups
    {
        public const int EightBall = 8;
        public const int CueBall = 0;

        /// <summary>Group a ball number belongs to. The cue (0) and the 8 have no group.</summary>
        public static BallGroup GroupOf(int ballNumber)
        {
            if (ballNumber >= 1 && ballNumber <= 7) return BallGroup.Solids;
            if (ballNumber >= 9 && ballNumber <= 15) return BallGroup.Stripes;
            return BallGroup.None;
        }

        /// <summary>The group the other player receives when one player takes <paramref name="group"/>.</summary>
        public static BallGroup Opposite(BallGroup group)
        {
            return group switch
            {
                BallGroup.Solids => BallGroup.Stripes,
                BallGroup.Stripes => BallGroup.Solids,
                _ => BallGroup.None
            };
        }

        /// <summary>True when the ball number belongs to the group (the cue and 8 belong to none).</summary>
        public static bool IsInGroup(int ballNumber, BallGroup group)
        {
            return GroupOf(ballNumber) == group;
        }
    }
}
