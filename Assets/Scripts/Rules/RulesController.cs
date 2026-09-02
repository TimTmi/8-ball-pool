using System;
using System.Collections.Generic;
using UnityEngine;
using EightBall.Gameplay;

namespace EightBall.Rules
{
    /// <summary>
    /// Applies the rules to each settled shot. Discovers every active <see cref="IShotRule"/>
    /// component on the Table object — adding or removing a rule component in the Inspector
    /// adds or removes it from the evaluation — then feeds the frozen <see cref="ShotReport"/>
    /// to each rule and applies the accumulated findings: turn hand-over via
    /// <see cref="TurnManager"/>, ball-in-hand, and match end. Also owns ball-in-hand
    /// placement, since restoring and positioning the cue ball is a rules decision.
    /// </summary>
    public class RulesController : MonoBehaviour
    {
        // Centre-to-centre clearances for a placed cue ball (ball radius is 0.1u)
        private const float BallClearance = TableLayout.BallDiameter * 1.05f;
        private const float PocketClearance = TableLayout.PocketRadius + TableLayout.BallRadius;

        private static float BallClearanceSq => BallClearance * BallClearance;
        private static float PocketClearanceSq => PocketClearance * PocketClearance;

        /// <summary>The match is over. Input and turn flow stop; the UI shows the result.</summary>
        public event Action<int> OnGameOver;

        /// <summary>True once a rule has ended the match; input and further rules evaluation stop.</summary>
        public bool IsGameOver { get; private set; }

        /// <summary>True while the player on turn must place the cue ball before aiming.</summary>
        public bool IsBallInHandPending { get; private set; }

        /// <summary>The accumulated match state rules write to; a read-only view for UI relays.</summary>
        public GameState State => _state;

        private readonly GameState _state = new GameState();
        private readonly List<IShotRule> _rules = new List<IShotRule>();

        private TableSetup _tableSetup;
        private ShotRecorder _shotRecorder;
        private TurnManager _turnManager;

        private void Start()
        {
            _tableSetup = GetComponent<TableSetup>();
            _shotRecorder = GetComponent<ShotRecorder>();
            _turnManager = GetComponent<TurnManager>();

            if (_shotRecorder == null || _turnManager == null)
            {
                Debug.LogError("[RulesController] Table needs a ShotRecorder and a TurnManager.", this);
                enabled = false;
                return;
            }

            CollectRules();
            _shotRecorder.OnShotRecorded += HandleShotRecorded;
        }

        private void OnDestroy()
        {
            if (_shotRecorder != null) _shotRecorder.OnShotRecorded -= HandleShotRecorded;
        }

        /// <summary>Rules are active components on this object, so the set is editable in the Inspector.</summary>
        private void CollectRules()
        {
            _rules.Clear();
            foreach (IShotRule rule in GetComponents<IShotRule>())
            {
                _rules.Add(rule);
            }
        }

        private void HandleShotRecorded(ShotReport shot)
        {
            if (_state.IsGameOver) return;

            CollectRules();

            var findings = new RuleFindings();
            foreach (IShotRule rule in _rules)
            {
                rule.Evaluate(shot, _state, findings);
            }

            ApplyFindings(shot, findings);
        }

        private void ApplyFindings(ShotReport shot, RuleFindings findings)
        {
            if (findings.HasGroupAssignment)
            {
                _state.PlayerGroups[findings.AssignedPlayerIndex] = findings.AssignedGroup;
                int otherPlayer = (findings.AssignedPlayerIndex + 1) % 2;
                _state.PlayerGroups[otherPlayer] = BallGroups.Opposite(findings.AssignedGroup);
            }

            if (findings.RespotEight) RespotEightBall();

            foreach (int ballNumber in shot.PocketedBallNumbers)
            {
                _state.PocketedBalls.Add(ballNumber);
            }
            if (findings.RespotEight) _state.PocketedBalls.Remove(BallGroups.EightBall);

            _state.IsFirstShot = false;

            if (findings.GameOver)
            {
                _state.IsGameOver = true;
                _state.WinnerIndex = findings.WinnerIndex;
                IsGameOver = true;
                OnGameOver?.Invoke(findings.WinnerIndex);
                return;
            }

            if (findings.GiveBallInHand)
            {
                BeginBallInHand(findings.NextPlayerIndex);
                return;
            }

            // Safety net: with the scratch rule removed there is nobody to bring a pocketed
            // cue ball back, so the game would stall with no ball on the table.
            RestoreCueBallIfPocketed();

            _turnManager.BeginTurn(findings.NextPlayerIndex);
        }

        /// <summary>Gives the player ball in hand: the cue ball comes back on the head spot and must be placed.</summary>
        private void BeginBallInHand(int playerIndex)
        {
            Ball cueBall = GetCueBall();
            if (cueBall != null)
            {
                cueBall.Restore();
                cueBall.transform.localPosition = TableLayout.HeadSpot;
            }

            IsBallInHandPending = true;
            _turnManager.BeginTurn(playerIndex);
        }

        /// <summary>Commits the placed cue ball position and returns to normal aiming.</summary>
        public void CompleteBallInHand()
        {
            IsBallInHandPending = false;
            SetCueBallTint(Color.white);
        }

        /// <summary>
        /// True when <paramref name="worldPosition"/> is a legal cue ball spot: on the felt,
        /// clear of every ball, and clear of the pocket mouths.
        /// </summary>
        public bool IsCueBallPlacementLegal(Vector3 worldPosition)
        {
            Vector2 local = transform.InverseTransformPoint(worldPosition);

            if (Mathf.Abs(local.x) > TableLayout.HalfFeltWidth - TableLayout.BallRadius) return false;
            if (Mathf.Abs(local.y) > TableLayout.HalfFeltHeight - TableLayout.BallRadius) return false;

            foreach (Vector2 pocket in TableLayout.PocketPositions)
            {
                if ((local - pocket).sqrMagnitude < PocketClearanceSq) return false;
            }

            GameObject[] ballObjects = _tableSetup != null ? _tableSetup.Balls : null;
            if (ballObjects == null) return true;

            foreach (GameObject ballObject in ballObjects)
            {
                if (ballObject == null || ballObject == _tableSetup.CueBall) continue;

                var ball = ballObject.GetComponent<Ball>();
                if (ball == null || ball.IsPocketed) continue;

                Vector2 ballLocal = transform.InverseTransformPoint(ballObject.transform.position);
                if ((local - ballLocal).sqrMagnitude < BallClearanceSq) return false;
            }
            return true;
        }

        /// <summary>Clamps <paramref name="worldPosition"/> onto the felt and moves the cue ball there.</summary>
        public void PlaceCueBallAt(Vector3 worldPosition)
        {
            if (_tableSetup == null || _tableSetup.CueBall == null) return;

            Vector2 local = transform.InverseTransformPoint(worldPosition);
            float x = Mathf.Clamp(local.x, -TableLayout.HalfFeltWidth + TableLayout.BallRadius, TableLayout.HalfFeltWidth - TableLayout.BallRadius);
            float y = Mathf.Clamp(local.y, -TableLayout.HalfFeltHeight + TableLayout.BallRadius, TableLayout.HalfFeltHeight - TableLayout.BallRadius);

            _tableSetup.CueBall.transform.localPosition = new Vector3(x, y, 0f);
        }

        /// <summary>Puts a pocketed 8-ball back on the foot spot.</summary>
        private void RespotEightBall()
        {
            GameObject[] ballObjects = _tableSetup != null ? _tableSetup.Balls : null;
            if (ballObjects == null || ballObjects.Length <= BallGroups.EightBall) return;

            GameObject eight = ballObjects[BallGroups.EightBall];
            if (eight == null) return;

            var ball = eight.GetComponent<Ball>();
            if (ball == null || !ball.IsPocketed) return;

            ball.Restore();
            eight.transform.localPosition = TableLayout.FootSpot;
        }

        private void RestoreCueBallIfPocketed()
        {
            Ball cueBall = GetCueBall();
            if (cueBall == null || !cueBall.IsPocketed) return;

            cueBall.Restore();
            cueBall.transform.localPosition = TableLayout.HeadSpot;
            SetCueBallTint(Color.white);
        }

        private Ball GetCueBall()
        {
            return _tableSetup != null && _tableSetup.CueBall != null
                ? _tableSetup.CueBall.GetComponent<Ball>()
                : null;
        }

        /// <summary>Red while the carried position is illegal, white when legal.</summary>
        public void SetCueBallLegalMarker(bool isLegal)
        {
            SetCueBallTint(isLegal ? Color.white : Color.red);
        }

        private void SetCueBallTint(Color color)
        {
            SpriteRenderer renderer = _tableSetup != null && _tableSetup.CueBall != null
                ? _tableSetup.CueBall.GetComponent<SpriteRenderer>()
                : null;
            if (renderer != null) renderer.color = color;
        }
    }
}
