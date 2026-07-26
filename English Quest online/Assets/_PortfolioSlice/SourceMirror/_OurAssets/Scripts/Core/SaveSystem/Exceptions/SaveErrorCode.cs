namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Categorises the reason a <see cref="SaveException"/> was thrown.
    /// </summary>
    public enum SaveErrorCode
    {
        /// <summary>A transient network or I/O failure occurred.</summary>
        NetworkError,

        /// <summary>A local disk read/write/access operation failed.</summary>
        StorageError,

        /// <summary>Save payload JSON could not be serialised or parsed.</summary>
        SerializationError,

        /// <summary>The player is not authenticated with Unity Gaming Services.</summary>
        AuthError,

        /// <summary>The cloud storage quota for this player has been exceeded.</summary>
        QuotaExceeded,

        /// <summary>A required migrator was missing or threw an error during migration.</summary>
        MigrationFailed,

        /// <summary>The save system was requested before it was registered/initialised.</summary>
        ServiceUnavailable
    }
}
