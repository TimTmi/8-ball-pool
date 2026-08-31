using System;
using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Pass-and-play turn flow: once the table has settled after a shot, the current
    /// player's turn ends and the other player's begins. No rules yet — every settled
    /// shot hands over unconditionally; fouls, groups, and win/loss belong to the
    /// later rules phase and will replace the unconditional hand-over.
    /// </summary>
    [RequireComponent(typeof(CueController))]
    public class TurnManager : MonoBehaviour
    {
        public const int PlayerCount = 2;

        /// <summary>Zero-based index of the player on turn (0 = Player 1).</summary>
        public int CurrentPlayerIndex { get; private set; }

        public string CurrentPlayerName => $"Player {CurrentPlayerIndex + 1}";

        /// <summary>Raised when a turn has ended and the next player's turn starts.</summary>
        public event Action<int> OnTurnStarted;

        private CueController _cueController;

        private void Awake()
        {
            _cueController = GetComponent<CueController>();
        }

        private void OnEnable()
        {
            _cueController.OnTableSettled += HandleTableSettled;
        }

        private void OnDisable()
        {
            _cueController.OnTableSettled -= HandleTableSettled;
        }

        private void HandleTableSettled()
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % PlayerCount;
            OnTurnStarted?.Invoke(CurrentPlayerIndex);
        }
    }
}
