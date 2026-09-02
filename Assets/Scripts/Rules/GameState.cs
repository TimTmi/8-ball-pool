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

        /// <summary>
        /// Ball numbers the player on <paramref name="group"/> still has to pocket: the
        /// group's balls minus pocketed ones, ending with the 8 once the whole group is
        /// down. Empty while the table is open — there is no group to show.
        /// </summary>
        public List<int> RemainingBallsFor(BallGroup group)
        {
            var remaining = new List<int>();
            if (group == BallGroup.None) return remaining;

            int first = group == BallGroup.Solids ? 1 : 9;
            for (int number = first; number < first + 7; number++)
            {
                if (!PocketedBalls.Contains(number)) remaining.Add(number);
            }

            if (remaining.Count == 0) remaining.Add(BallGroups.EightBall);
            return remaining;
        }

        public bool IsGameOver { get; set; }

        public int WinnerIndex { get; set; }
    }
}
