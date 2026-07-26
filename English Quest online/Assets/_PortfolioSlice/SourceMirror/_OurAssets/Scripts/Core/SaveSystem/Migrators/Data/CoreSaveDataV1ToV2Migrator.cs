using System;
using EnglishKingdom.SaveSystem.Data;
using Newtonsoft.Json.Linq;

namespace EnglishKingdom.SaveSystem.Migrators
{
    /// <summary>
    /// Migrates <see cref="Data.CoreSaveData"/> from schema version 1 to version 2.
    /// Fill in the <see cref="Migrate"/> body when schema changes are made.
    /// </summary>
    public sealed class CoreSaveDataV1ToV2Migrator : ISaveMigrator
    {
        /// <inheritdoc/>
        public Type DomainType => typeof(CoreSaveData);

        /// <inheritdoc/>
        public int FromVersion => 1;

        /// <summary>
        /// Transforms a v1 <see cref="Data.CoreSaveData"/> JSON object to the v2 shape.
        /// Add field renames, default injections, or structural changes here.
        /// </summary>
        public JObject Migrate(JObject data)
        {
            // TODO: implement v1 → v2 migration logic.
            // Example:
            //   data["newField"] = "defaultValue";
            //   data.Remove("removedField");
            return data;
        }
    }
}
