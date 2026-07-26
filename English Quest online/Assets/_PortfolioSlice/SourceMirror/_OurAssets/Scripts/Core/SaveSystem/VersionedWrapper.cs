using System;
using Newtonsoft.Json;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Envelope stored on disk or in the cloud for every saved value.
    /// Carries the schema <see cref="Version"/> and a UTC timestamp so
    /// the migration pipeline knows exactly where to start.
    /// </summary>
    public sealed class VersionedWrapper<T>
    {
        /// <summary>Schema version of <see cref="Data"/> at the time it was written.</summary>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>Unix timestamp (UTC seconds) recorded when the data was written.</summary>
        [JsonProperty("savedAt")]
        public long SavedAt { get; set; }

        /// <summary>The actual domain payload.</summary>
        [JsonProperty("data")]
        public T Data { get; set; }

        /// <summary>
        /// Creates a new wrapper stamped with the current UTC time.
        /// </summary>
        /// <param name="data">Domain payload to wrap.</param>
        /// <param name="version">Schema version of the payload.</param>
        public static VersionedWrapper<T> Create(T data, int version) =>
            new VersionedWrapper<T>
            {
                Version = version,
                SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Data    = data
            };
    }
}
