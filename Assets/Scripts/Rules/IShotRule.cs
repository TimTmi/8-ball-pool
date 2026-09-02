namespace EightBall.Rules
{
    /// <summary>
    /// One rule as a Unity component. <c>RulesController</c> discovers every
    /// <c>IShotRule</c> on the Table object via <see cref="UnityEngine.GameObject.GetComponents{T}"/>,
    /// so adding or removing a rule is adding or removing a component in the Inspector;
    /// the component's <c>enabled</c> checkbox turns an individual rule on or off.
    /// </summary>
    public interface IShotRule
    {
        /// <summary>
        /// Evaluates one settled shot. Read-only over <paramref name="shot"/> and
        /// <paramref name="state"/>; accumulate decisions on <paramref name="findings"/>.
        /// The controller applies findings, so rules never mutate match state directly.
        /// </summary>
        void Evaluate(ShotReport shot, GameState state, RuleFindings findings);
    }
}
