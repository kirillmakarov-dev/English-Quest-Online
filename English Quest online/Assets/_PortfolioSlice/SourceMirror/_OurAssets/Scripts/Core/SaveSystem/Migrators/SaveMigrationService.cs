using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Chains <see cref="ISaveMigrator"/> steps to bring save data from an old
    /// schema version up to the current one.  Register one migrator per version
    /// transition (N → N+1) at boot via <see cref="Register"/>.
    /// </summary>
    public sealed class SaveMigrationService
    {
        private readonly Dictionary<(Type domain, int fromVersion), ISaveMigrator> _migrators =
            new Dictionary<(Type, int), ISaveMigrator>();

        /// <summary>
        /// Registers a migrator for the version transition it handles.
        /// Only one migrator per <see cref="ISaveMigrator.FromVersion"/> is allowed;
        /// a duplicate registration overwrites the previous one (logged as a warning).
        /// </summary>
        /// <param name="migrator">The migrator to register.</param>
        public void Register(ISaveMigrator migrator)
        {
            if (migrator == null) throw new ArgumentNullException(nameof(migrator));

            var key = (migrator.DomainType, migrator.FromVersion);
            if (_migrators.ContainsKey(key))
            {
                AppLog.Warning(
                    $"[SaveMigrationService] Overwriting migrator for {migrator.DomainType.Name} " +
                    $"version {migrator.FromVersion} → {migrator.FromVersion + 1}.");
            }
            _migrators[key] = migrator;
        }

        /// <summary>
        /// Applies every registered migration step in order from
        /// <paramref name="fromVersion"/> up to (but not including) <paramref name="toVersion"/>.
        /// </summary>
        /// <param name="raw">JSON object at <paramref name="fromVersion"/>.</param>
        /// <param name="fromVersion">Schema version of <paramref name="raw"/>.</param>
        /// <param name="toVersion">Target schema version.</param>
        /// <returns>Migrated JSON object at <paramref name="toVersion"/>.</returns>
        /// <exception cref="SaveException">
        /// Thrown with <see cref="SaveErrorCode.MigrationFailed"/> when a required
        /// step N → N+1 has no registered migrator.
        /// </exception>
        public JObject MigrateToLatest(JObject raw, int fromVersion, int toVersion, Type domainType)
        {
            if (fromVersion == toVersion)
                return raw;

            JObject current = raw;

            for (int v = fromVersion; v < toVersion; v++)
            {
                if (!_migrators.TryGetValue((domainType, v), out ISaveMigrator migrator))
                {
                    throw new SaveException(
                        SaveErrorCode.MigrationFailed,
                        $"No migrator registered for version {v} → {v + 1}. " +
                        $"Cannot migrate save data from version {fromVersion} to {toVersion}.");
                }

                current = migrator.Migrate(current);
            }

            return current;
        }
    }
}
