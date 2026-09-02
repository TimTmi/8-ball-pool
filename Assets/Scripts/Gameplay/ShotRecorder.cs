using System.Collections.Generic;
using UnityEngine;
using EightBall.Rules;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Observes one shot and freezes its observable outcome into a
    /// <c>EightBall.Rules.ShotReport</c> when the table settles. Subscribes to the balls'
    /// pocket events for the duration of a shot and re-emits one coherent report, so rules
    /// consumers get a single event instead of racing Unity's callback order.
    /// Lives on the Table object alongside <see cref="CueController"/>.
    /// </summary>
    [RequireComponent(typeof(CueController))]
    public class ShotRecorder : MonoBehaviour
    {
        /// <summary>The shot is over: every pocketing the rules need to see, in drop order.</summary>
        public event System.Action<ShotReport> OnShotRecorded;

        private readonly List<int> _pocketedBallNumbers = new List<int>(5);
        private readonly Dictionary<Ball, int> _ballNumbers = new Dictionary<Ball, int>(16);
        private readonly List<Ball> _subscribedBalls = new List<Ball>(16);

        private CueController _cueController;
        private TableSetup _tableSetup;
        private TurnManager _turnManager;
        private bool _isRecording;
        private bool _cueBallScratched;

        private void Start()
        {
            _tableSetup = GetComponent<TableSetup>();
            _cueController = GetComponent<CueController>();
            _turnManager = FindAnyObjectByType<TurnManager>();

            _cueController.OnShotStarted += HandleShotStarted;
            _cueController.OnTableSettled += HandleTableSettled;
        }

        private void OnDestroy()
        {
            if (_cueController != null)
            {
                _cueController.OnShotStarted -= HandleShotStarted;
                _cueController.OnTableSettled -= HandleTableSettled;
            }

            UnsubscribeFromBalls();
        }

        private void HandleShotStarted()
        {
            RefreshBalls();
            _pocketedBallNumbers.Clear();
            _cueBallScratched = false;
            _isRecording = true;
        }

        private void HandleTableSettled()
        {
            if (!_isRecording) return;
            _isRecording = false;

            int playerIndex = _turnManager != null ? _turnManager.CurrentPlayerIndex : 0;
            OnShotRecorded?.Invoke(new ShotReport(playerIndex, _pocketedBallNumbers, _cueBallScratched));
        }

        private void HandleBallPocketed(Ball ball)
        {
            if (!_isRecording) return;

            if (_ballNumbers.TryGetValue(ball, out int number))
            {
                if (number == BallGroups.CueBall) _cueBallScratched = true;
                else _pocketedBallNumbers.Add(number);
            }
        }

        /// <summary>Re-reads the spawned balls and subscribes to their pocket events.</summary>
        private void RefreshBalls()
        {
            UnsubscribeFromBalls();
            _ballNumbers.Clear();

            GameObject[] ballObjects = _tableSetup != null ? _tableSetup.Balls : null;
            if (ballObjects == null) return;

            for (int number = 0; number < ballObjects.Length; number++)
            {
                GameObject ballObject = ballObjects[number];
                if (ballObject == null) continue;

                var ball = ballObject.GetComponent<Ball>();
                if (ball == null) continue;

                _ballNumbers[ball] = number;
                ball.OnPocketed += HandleBallPocketed;
                _subscribedBalls.Add(ball);
            }
        }

        private void UnsubscribeFromBalls()
        {
            foreach (Ball ball in _subscribedBalls)
            {
                if (ball != null) ball.OnPocketed -= HandleBallPocketed;
            }
            _subscribedBalls.Clear();
        }
    }
}
