namespace EightBall.Rules
{
    /// <summary>
    /// What the rules decided about one settled shot. Each rule accumulates its findings;
    /// <c>RulesController</c> applies them to the match afterwards. Pure data so the rules
    /// stay unit-testable.
    /// </summary>
    public sealed class RuleFindings
    {
        // ── Group assignment ────────────────────────────────────────
        public bool HasGroupAssignment { get; set; }
        public int AssignedPlayerIndex { get; set; }
        public BallGroup AssignedGroup { get; set; }

        // ── Ball in hand ────────────────────────────────────────────
        /// <summary>The cue ball must be placed before the next shot.</summary>
        public bool GiveBallInHand { get; set; }

        // ── Turn ────────────────────────────────────────────────────
        /// <summary>Who plays next. Ignored when <see cref="GameOver"/> ends the match.</summary>
        public int NextPlayerIndex { get; set; }

        // ── Match end ───────────────────────────────────────────────
        public bool GameOver { get; set; }
        public int WinnerIndex { get; set; }

        // ── Table corrections ───────────────────────────────────────
        /// <summary>The 8-ball went down on the break: put it back on the foot spot.</summary>
        public bool RespotEight { get; set; }
    }
}
