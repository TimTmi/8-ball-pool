using UnityEngine;

namespace EightBall.Rules
{
    /// <summary>
    /// Decides the match from the 8-ball's fate. Potting the 8 after the shooter's group was
    /// already cleared (before the shot) wins; potting it early — on the break included — or
    /// alongside a scratch, loses. When <see cref="EightBallBreakRule"/> is also active it
    /// flags the break pot for a respot and <c>RulesController</c> lets that respot veto this
    /// rule's loss, so the two rules stay order-independent.
    /// </summary>
    [DisallowMultipleComponent]
    public class EightBallWinRule : MonoBehaviour, IShotRule
    {
        public void Evaluate(ShotReport shot, GameState state, RuleFindings findings)
        {
            if (!BallGroups.WasPotted(shot, BallGroups.EightBall)) return;

            // The 8 is only a legal target once the group was cleared before this shot;
            // potting it in the same stroke as the last group ball loses, as does a scratch.
            bool groupCleared = GroupCleared(state, shot.PlayerIndex);
            bool isWin = !shot.CueBallScratched && GroupAssigned(state, shot.PlayerIndex) && groupCleared;

            findings.GameOver = true;
            findings.WinnerIndex = isWin ? shot.PlayerIndex : OtherPlayer(shot.PlayerIndex);
        }

        /// <summary>True when every one of the player's group balls was already down before the shot.</summary>
        private static bool GroupCleared(GameState state, int playerIndex)
        {
            BallGroup group = state.PlayerGroups[playerIndex];
            if (group == BallGroup.None) return false;

            for (int i = 1; i <= 7; i++)
            {
                int ballNumber = group == BallGroup.Solids ? i : i + 8;
                if (!state.PocketedBalls.Contains(ballNumber)) return false;
            }
            return true;
        }

        private static bool GroupAssigned(GameState state, int playerIndex)
        {
            return state.PlayerGroups[playerIndex] != BallGroup.None;
        }

        private static int OtherPlayer(int playerIndex) => (playerIndex + 1) % 2;
    }
}
