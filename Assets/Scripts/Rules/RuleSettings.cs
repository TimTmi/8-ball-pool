using System.Collections.Generic;
using UnityEngine;

namespace EightBall.Rules
{
    /// <summary>
    /// Persisted on/off state for the toggleable rules in <see cref="RuleCatalog"/>.
    /// Backed by PlayerPrefs; a rule missing from the store defaults to enabled.
    /// <c>RulesController</c> reads this when it collects its rules at match start.
    /// </summary>
    public static class RuleSettings
    {
        private const string StoreKey = "rules.disabled";

        private static HashSet<string> _disabledIds;

        public static bool IsEnabled(string ruleId) => !DisabledIds().Contains(ruleId);

        public static void SetEnabled(string ruleId, bool isEnabled)
        {
            HashSet<string> disabledIds = DisabledIds();
            if (isEnabled) disabledIds.Remove(ruleId);
            else disabledIds.Add(ruleId);

            PlayerPrefs.SetString(StoreKey, string.Join(",", disabledIds));
            PlayerPrefs.Save();
        }

        private static HashSet<string> DisabledIds()
        {
            if (_disabledIds != null) return _disabledIds;

            string raw = PlayerPrefs.GetString(StoreKey, "");
            _disabledIds = new HashSet<string>();
            if (!string.IsNullOrEmpty(raw))
            {
                foreach (string id in raw.Split(',')) _disabledIds.Add(id);
            }
            return _disabledIds;
        }
    }
}
