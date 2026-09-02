using UnityEngine;

namespace EightBall.Rules
{
    /// <summary>
    /// An 8-ball down on the break is respotted instead of ending the match — the break
    /// never decides the game. <see cref="EightBallWinRule"/> ignores break shots for the
    /// same reason, so the two rules stay order-independent.
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
