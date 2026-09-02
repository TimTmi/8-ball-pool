using System;
using System.Collections.Generic;

namespace EightBall.Rules
{
    /// <summary>
    /// The rules the main menu's rules screen may toggle. Only rules with a safe "off"
    /// game belong here: <see cref="GroupAssignmentRule"/> (the table would stay open
    /// forever, no win possible) and <see cref="EightBallWinRule"/> (the match could
    /// never end) are always active and are deliberately not listed. Turning off
    /// <see cref="TurnContinuationRule"/> falls back to alternating turns — the controller
    /// defaults the next player to the opponent when no rule decides otherwise.
    /// </summary>
    public static class RuleCatalog
    {
        /// <summary>One toggleable rule: how the menu labels it and which component it maps to.</summary>
        public readonly struct Entry
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly Type ComponentType;

            public Entry(string id, string displayName, string description, Type componentType)
            {
                Id = id;
                DisplayName = displayName;
                Description = description;
                ComponentType = componentType;
            }
        }

        /// <summary>Every rule the rules screen shows a toggle for, in display order.</summary>
        public static readonly IReadOnlyList<Entry> Toggleable = new[]
        {
            new Entry(
                "turn-continuation",
                "Keep turn on pot",
                "Potting a ball of your own group (any ball while the table is open) lets you shoot again, and a scratch gives the opponent ball in hand. Without it, players strictly alternate turns and a scratched cue ball just returns to the head spot.",
                typeof(TurnContinuationRule)
            ),
            new Entry(
                "respot-eight-on-break",
                "Respot 8 on break",
                "An 8-ball potted on the break is returned to the foot spot. Without it, potting the 8 on the break loses the match.",
                typeof(EightBallBreakRule)
            ),
        };
    }
}
