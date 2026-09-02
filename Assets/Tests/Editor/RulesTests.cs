using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using EightBall.Rules;

namespace EightBall.Tests
{
    /// <summary>
    /// EditMode tests for the shot rules. Rules are pure evaluators over a
    /// <see cref="ShotReport"/> and <see cref="GameState"/>, so every scenario is a
    /// fabricated report — no play session needed.
    /// </summary>
    public class RulesTests
    {
        private const int P1 = 0;
        private const int P2 = 1;

        private GameState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new GameState();
            _state.IsFirstShot = false; // most tests are mid-game shots
        }

        // ── Group assignment ─────────────────────────────────────────

        [Test]
        public void FirstPotAfterBreak_AssignsGroup()
        {
            ShotReport shot = Shot(P1, 3);

            RuleFindings findings = Evaluate<GroupAssignmentRule>(shot);

            Assert.IsTrue(findings.HasGroupAssignment);
            Assert.AreEqual(P1, findings.AssignedPlayerIndex);
            Assert.AreEqual(BallGroup.Solids, findings.AssignedGroup);
        }

        [Test]
        public void BreakPot_LeavesTableOpen()
        {
            _state.IsFirstShot = true;
            ShotReport shot = Shot(P1, 3);

            RuleFindings findings = Evaluate<GroupAssignmentRule>(shot);

            Assert.IsFalse(findings.HasGroupAssignment);
        }

        [Test]
        public void BothGroupsPotted_AssignsFromFirstBallDown()
        {
            ShotReport shot = Shot(P1, 11, 4); // stripe first, then a solid

            RuleFindings findings = Evaluate<GroupAssignmentRule>(shot);

            Assert.AreEqual(BallGroup.Stripes, findings.AssignedGroup);
        }

        [Test]
        public void EightBallPot_DoesNotAssignGroup()
        {
            ShotReport shot = Shot(P1, BallGroups.EightBall);

            RuleFindings findings = Evaluate<GroupAssignmentRule>(shot);

            Assert.IsFalse(findings.HasGroupAssignment);
        }

        // ── Turn continuation ────────────────────────────────────────

        [Test]
        public void Scratch_GivesOpponentBallInHand()
        {
            ShotReport shot = ScratchedShot(P1, 3);

            RuleFindings findings = Evaluate<TurnContinuationRule>(shot);

            Assert.IsTrue(findings.GiveBallInHand);
            Assert.AreEqual(P2, findings.NextPlayerIndex);
        }

        [Test]
        public void OwnGroupPot_KeepsTurn()
        {
            AssignGroups(P1, BallGroup.Solids);

            RuleFindings findings = Evaluate<TurnContinuationRule>(Shot(P1, 3));

            Assert.AreEqual(P1, findings.NextPlayerIndex);
            Assert.IsFalse(findings.GiveBallInHand);
        }

        [Test]
        public void OnlyOpponentGroupPot_PassesTurn()
        {
            AssignGroups(P1, BallGroup.Solids);

            RuleFindings findings = Evaluate<TurnContinuationRule>(Shot(P1, 11));

            Assert.AreEqual(P2, findings.NextPlayerIndex);
        }

        [Test]
        public void NothingPotted_PassesTurn()
        {
            RuleFindings findings = Evaluate<TurnContinuationRule>(Shot(P1));

            Assert.AreEqual(P2, findings.NextPlayerIndex);
            Assert.IsFalse(findings.GiveBallInHand);
        }

        [Test]
        public void OpenTablePot_KeepsTurn()
        {
            Assert.IsTrue(_state.IsOpenTable);

            RuleFindings findings = Evaluate<TurnContinuationRule>(Shot(P1, 11));

            Assert.AreEqual(P1, findings.NextPlayerIndex);
        }

        [Test]
        public void BreakPot_KeepsTurnWithOpenTable()
        {
            Assert.IsTrue(_state.IsFirstShot, "break state expected");

            RuleFindings findings = Evaluate<TurnContinuationRule>(Shot(P1, 11));

            Assert.AreEqual(P1, findings.NextPlayerIndex);
        }

        // ── 8-ball outcome ───────────────────────────────────────────

        [Test]
        public void EightOnBreak_IsRespottedNotLost()
        {
            _state.IsFirstShot = true;

            RuleFindings findings = Evaluate<EightBallWinRule>(Shot(P1, BallGroups.EightBall));

            Assert.IsTrue(findings.RespotEight);
            Assert.IsFalse(findings.GameOver);
        }

        [Test]
        public void EarlyEight_LosesMatch()
        {
            AssignGroups(P1, BallGroup.Solids);

            RuleFindings findings = Evaluate<EightBallWinRule>(Shot(P1, BallGroups.EightBall));

            Assert.IsTrue(findings.GameOver);
            Assert.AreEqual(P2, findings.WinnerIndex);
        }

        [Test]
        public void EightWithScratch_Loses()
        {
            AssignGroups(P1, BallGroup.Solids);
            PocketGroup(_state, P1);

            ShotReport shot = ScratchedShot(P1, BallGroups.EightBall);
            RuleFindings findings = Evaluate<EightBallWinRule>(shot);

            Assert.IsTrue(findings.GameOver);
            Assert.AreEqual(P2, findings.WinnerIndex);
        }

        [Test]
        public void LastGroupBallAndEightSameShot_Loses()
        {
            AssignGroups(P1, BallGroup.Solids);
            PocketGroup(_state, P1);
            _state.PocketedBalls.Remove(7); // one solid still on the table

            ShotReport shot = Shot(P1, 7, BallGroups.EightBall);

            RuleFindings findings = Evaluate<EightBallWinRule>(shot);

            Assert.IsTrue(findings.GameOver);
            Assert.AreEqual(P2, findings.WinnerIndex);
        }

        // ── Ball number classification ───────────────────────────────

        [Test]
        public void BallGroups_ClassifiesNumbers()
        {
            Assert.AreEqual(BallGroup.Solids, BallGroups.GroupOf(1));
            Assert.AreEqual(BallGroup.Solids, BallGroups.GroupOf(7));
            Assert.AreEqual(BallGroup.Stripes, BallGroups.GroupOf(9));
            Assert.AreEqual(BallGroup.Stripes, BallGroups.GroupOf(15));
            Assert.AreEqual(BallGroup.None, BallGroups.GroupOf(BallGroups.CueBall));
            Assert.AreEqual(BallGroup.None, BallGroups.GroupOf(BallGroups.EightBall));
        }

        // ── Remaining balls for the player panels ────────────────────

        [Test]
        public void RemainingBalls_OpenTable_IsEmpty()
        {
            Assert.IsEmpty(_state.RemainingBallsFor(BallGroup.None));
        }

        [Test]
        public void RemainingBalls_FreshGroup_ShowsWholeGroup()
        {
            var remaining = _state.RemainingBallsFor(BallGroup.Solids);

            CollectionAssert.AreEqual(new List<int> { 1, 2, 3, 4, 5, 6, 7 }, remaining);
        }

        [Test]
        public void RemainingBalls_PartiallyPocketed_ShowsOnlyRemaining()
        {
            AssignGroups(P1, BallGroup.Stripes);
            _state.PocketedBalls.Add(9);
            _state.PocketedBalls.Add(15);

            var remaining = _state.RemainingBallsFor(BallGroup.Stripes);

            CollectionAssert.AreEqual(new List<int> { 10, 11, 12, 13, 14 }, remaining);
        }

        [Test]
        public void RemainingBalls_GroupCleared_ShowsOnlyTheEight()
        {
            AssignGroups(P1, BallGroup.Solids);
            PocketGroup(_state, P1);

            var remaining = _state.RemainingBallsFor(BallGroup.Solids);

            CollectionAssert.AreEqual(new List<int> { BallGroups.EightBall }, remaining);
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static ShotReport Shot(int playerIndex, params int[] pocketed)
        {
            return new ShotReport(playerIndex, new List<int>(pocketed), false);
        }

        private static ShotReport ScratchedShot(int playerIndex, params int[] pocketed)
        {
            return new ShotReport(playerIndex, new List<int>(pocketed), true);
        }

        /// <summary>Evaluates the rule of type <typeparamref name="T"/> on a fresh GameObject.</summary>
        private RuleFindings Evaluate<T>(ShotReport shot) where T : MonoBehaviour, IShotRule
        {
            var rule = new GameObject("Rule").AddComponent<T>();
            var findings = new RuleFindings();
            rule.Evaluate(shot, _state, findings);
            return findings;
        }

        private void AssignGroups(int playerIndex, BallGroup group)
        {
            _state.PlayerGroups[playerIndex] = group;
            _state.PlayerGroups[(playerIndex + 1) % 2] = BallGroups.Opposite(group);
        }

        private void PocketGroup(GameState state, int playerIndex)
        {
            BallGroup group = state.PlayerGroups[playerIndex];
            for (int i = 1; i <= 7; i++)
            {
                int ballNumber = group == BallGroup.Solids ? i : i + 8;
                state.PocketedBalls.Add(ballNumber);
            }
        }
    }
}
