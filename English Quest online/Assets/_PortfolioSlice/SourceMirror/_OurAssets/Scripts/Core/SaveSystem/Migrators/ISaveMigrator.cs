using System;
using Newtonsoft.Json.Linq;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Implements a single version step in the migration chain for a save domain.
    /// Register implementations on <see cref="SaveMigrationService"/> at boot.
    /// </summary>
    public interface ISaveMigrator
    {
        /// <summary>
        /// The CLR type of the domain data class this migrator processes.
        /// Used as part of the compound lookup key so migrators for different
        /// domains can share the same <see cref="FromVersion"/> value without
        /// overwriting each other.
        /// </summary>
        Type DomainType { get; }

        /// <summary>
        /// The version this migrator upgrades <em>from</em>.
        /// It produces data compatible with version <c>FromVersion + 1</c>.
        /// </summary>
        int FromVersion { get; }

        /// <summary>
        /// Transforms the raw JSON object from schema <see cref="FromVersion"/>
        /// to schema <c>FromVersion + 1</c>.
        /// </summary>
        /// <param name="data">Raw JSON object at version <see cref="FromVersion"/>.</param>
        /// <returns>Migrated JSON object at version <c>FromVersion + 1</c>.</returns>
        JObject Migrate(JObject data);
    }
}
