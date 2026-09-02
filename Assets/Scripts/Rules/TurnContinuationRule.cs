using UnityEngine;

namespace EightBall.Rules
{
    /// <summary>
    /// Decides who plays next. A scratch hands the opponent ball in hand. Pocketing at least
    /// one ball of the shooter's own group (any object ball while the table is open) keeps the
    /// turn; anything else passes it. Balls of the opponent's group stay down but do not
    /// continue the turn.
    /// </summary>
    [DisallowMultipleComponent]
    public class TurnContinuationRule : MonoBehaviour, IShotRule
    {
        public void Evaluate(ShotReport shot, GameState state, RuleFindings findings)
        {
            int opponentIndex = OtherPlayer(shot.PlayerIndex);

            if (shot.CueBallScratched)
            {
                findings.GiveBallInHand = true;
                findings.NextPlayerIndex = opponentIndex;
                return;
            }

            if (KeepsTurn(shot, state))
            {
                findings.NextPlayerIndex = shot.PlayerIndex;
                return;
            }

            findings.NextPlayerIndex = opponentIndex;
        }

        /// <summary>A pot keeps the turn while the table is open, or when it includes a ball of the shooter's group.</summary>
        private static bool KeepsTurn(ShotReport shot, GameState state)
        {
            if (state.IsOpenTable) return PottedAnyObjectBall(shot);

            BallGroup group = state.PlayerGroups[shot.PlayerIndex];
            foreach (int ballNumber in shot.PocketedBallNumbers)
            {
                if (BallGroups.IsInGroup(ballNumber, group)) return true;
            }
            return false;
        }

        private static bool PottedAnyObjectBall(ShotReport shot)
        {
            foreach (int ballNumber in shot.PocketedBallNumbers)
            {
                if (ballNumber != BallGroups.CueBall && ballNumber != BallGroups.EightBall) return true;
            }
            return false;
        }

        private static int OtherPlayer(int playerIndex) => (playerIndex + 1) % 2;
    }
}
