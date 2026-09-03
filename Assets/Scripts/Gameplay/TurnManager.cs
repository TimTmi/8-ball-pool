using System;
using EightBall.Audio;
using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Pass-and-play turn flow. The rules layer decides who plays next and applies the
    /// decision through <see cref="BeginTurn"/>; this class only tracks the current player
    /// and announces the change. Rules components live alongside on the Table object
    /// (see <c>EightBall.Rules.RulesController</c>).
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public const int PlayerCount = 2;

        /// <summary>Zero-based index of the player on turn (0 = Player 1).</summary>
        public int CurrentPlayerIndex { get; private set; }

        public string CurrentPlayerName => $"Player {CurrentPlayerIndex + 1}";

        /// <summary>Raised when a turn has ended and the next player's turn starts.</summary>
        public event Action<int> OnTurnStarted;

        /// <summary>
        /// Hands the turn to <paramref name="playerIndex"/> and announces it. Called by
        /// <c>RulesController</c> after the rules have evaluated the settled shot.
        /// </summary>
        public void BeginTurn(int playerIndex)
        {
            CurrentPlayerIndex = playerIndex;
            OnTurnStarted?.Invoke(CurrentPlayerIndex);
            SfxManager.Play("YourTurn");
        }
    }
}
