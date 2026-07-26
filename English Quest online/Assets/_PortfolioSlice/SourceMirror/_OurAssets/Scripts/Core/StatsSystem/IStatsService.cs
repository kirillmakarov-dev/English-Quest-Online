using System;

namespace EnglishKingdom.StatsSystem
{
    /// <summary>
    /// Generic runtime store for gameplay statistics defined by <see cref="StatDefinitionSO"/> assets.
    /// Resolved via <c>ServiceLocator.Global.Get&lt;IStatsService&gt;()</c>.
    ///
    /// Notifications are scoped per stat: subscribing to one <see cref="StatDefinitionSO"/> only
    /// receives callbacks for that stat, never for unrelated ones. This keeps consumers (such as
    /// the future achievement system) from having to filter through every mutation in the game.
    /// </summary>
    public interface IStatsService
    {
        /// <summary>True once persisted stat values have finished loading.</summary>
        bool IsInitialized { get; }

        /// <summary>Current value of <paramref name="stat"/>, or 0 if never recorded.</summary>
        int Get(StatDefinitionSO stat);

        /// <summary>Current value for a raw stat id, or 0 if never recorded.</summary>
        int Get(string statId);

        /// <summary>
        /// Combines <paramref name="deltaOrValue"/> into the stat's stored value according to
        /// its <see cref="StatDefinitionSO.Aggregation"/> (Sum adds, Max keeps the highest,
        /// Set overwrites), then persists the result and notifies subscribers of that stat only.
        /// </summary>
        void Apply(StatDefinitionSO stat, int deltaOrValue);

        /// <summary>
        /// Registers <paramref name="onChanged"/> to be invoked (with old and new values) only
        /// when <paramref name="stat"/> changes. Other stats never trigger this callback.
        /// </summary>
        void Subscribe(StatDefinitionSO stat, Action<int, int> onChanged);

        /// <summary>Removes a callback previously registered via <see cref="Subscribe"/>.</summary>
        void Unsubscribe(StatDefinitionSO stat, Action<int, int> onChanged);
    }
}
