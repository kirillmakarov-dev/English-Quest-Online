namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Project-wide versioning constants shared by all save domains.
    /// </summary>
    public static class SaveVersionConfig
    {
        /// <summary>
        /// The oldest save-data version that this build can still migrate.
        /// Data saved before this version is considered obsolete and will be
        /// discarded (with a warning) rather than migrated.
        /// </summary>
        public const int MinSupportedVersion = 1;
    }
}
