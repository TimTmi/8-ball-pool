using UnityEngine;

namespace EightBall.Rules
{
    /// <summary>
    /// An 8-ball down on the break is respotted instead of ending the match — the break
    /// never decides the game. <see cref="EightBallWinRule"/> calls the same pot a loss;
    /// the controller lets this rule's respot veto that, so the two stay order-independent.
    /// Toggleable from the main menu; without it the break pot is a plain early-8 loss.
    /// </summary>
    [DisallowMultipleComponent]
    public class EightBallBreakRule : MonoBehaviour, IShotRule
    {
        public void Evaluate(ShotReport shot, GameState state, RuleFindings findings)
        {
            if (!state.IsFirstShot) return;
            if (!BallGroups.WasPotted(shot, BallGroups.EightBall)) return;

            findings.RespotEight = true;
        }
    }
}
