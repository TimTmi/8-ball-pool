using System.Collections.Generic;

namespace EightBall.Rules
{
    /// <summary>
    /// The observable outcome of one shot, frozen by <see cref="Gameplay.ShotRecorder"/> when the
    /// table settles. Rules evaluate this plus <see cref="GameState"/> — never the live scene.
    /// </summary>
    public sealed class ShotReport
    {
        /// <summary>Player who took the shot.</summary>
        public int PlayerIndex { get; }

        /// <summary>Object ball numbers pocketed, in drop order. The cue ball is not listed.</summary>
        public IReadOnlyList<int> PocketedBallNumbers { get; }

        /// <summary>True when the cue ball went down during the shot.</summary>
        public bool CueBallScratched { get; }

        public ShotReport(int playerIndex, IReadOnlyList<int> pocketedBallNumbers, bool cueBallScratched)
        {
            PlayerIndex = playerIndex;
            PocketedBallNumbers = pocketedBallNumbers ?? new List<int>();
            CueBallScratched = cueBallScratched;
        }
    }
}
