using System.Collections.Generic;

namespace EightBall.Rules
{
    /// <summary>Solids (1–7), stripes (9–15), or no group yet.</summary>
    public enum BallGroup
    {
        None,
        Solids,
        Stripes
    }

    /// <summary>
    /// Shared facts about the match, accumulated across shots by <c>RulesController</c>.
    /// Rules read this when evaluating a <see cref="ShotReport"/>; the controller writes
    /// applied findings back to it. Pure data so the rules stay unit-testable.
    /// </summary>
    public sealed class GameState
    {
        /// <summary>Group per player index (0/1). Both None means the table is open.</summary>
        public BallGroup[] PlayerGroups { get; } = { BallGroup.None, BallGroup.None };

        /// <summary>Object balls pocketed so far, excluding the shot currently being evaluated.</summary>
        public HashSet<int> PocketedBalls { get; } = new HashSet<int>();

        /// <summary>True until the opening break shot has been evaluated — break pots leave the table open.</summary>
        public bool IsFirstShot { get; set; } = true;

        public bool IsOpenTable => PlayerGroups[0] == BallGroup.None;

        public bool IsGameOver { get; set; }

        public int WinnerIndex { get; set; }
    }
}
