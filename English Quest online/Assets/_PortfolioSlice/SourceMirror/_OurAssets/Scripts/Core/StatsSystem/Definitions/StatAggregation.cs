namespace EnglishKingdom.StatsSystem
{
    /// <summary>
    /// Determines how a new value combines with the currently stored value
    /// when <see cref="IStatsService.Apply"/> is called.
    /// </summary>
    public enum StatAggregation
    {
        /// <summary>Add the incoming value to the current total. Default for counters (defeats, pickups).</summary>
        Sum,

        /// <summary>Keep the higher of the current and incoming values. Useful for high-water marks (best score, max streak).</summary>
        Max,

        /// <summary>Overwrite the current value with the incoming value.</summary>
        Set
    }
}
