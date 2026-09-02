using UnityEngine;

namespace EightBall.Rules
{
    /// <summary>
    /// Assigns the groups on the first object ball pocketed after the break. While the table
    /// is open, the group of the first ball down in a shot becomes the shooter's group and the
    /// opponent takes the other. Break pots leave the table open. Pots of both groups in one
    /// shot assign from the first ball down — shooter's choice is out of scope for pass-and-play.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroupAssignmentRule : MonoBehaviour, IShotRule
    {
        public void Evaluate(ShotReport shot, GameState state, RuleFindings findings)
        {
            if (!state.IsOpenTable || state.IsFirstShot) return;

            foreach (int ballNumber in shot.PocketedBallNumbers)
            {
                BallGroup group = BallGroups.GroupOf(ballNumber);
                if (group == BallGroup.None) continue;

                findings.HasGroupAssignment = true;
                findings.AssignedPlayerIndex = shot.PlayerIndex;
                findings.AssignedGroup = group;
                return;
            }
        }
    }
}
