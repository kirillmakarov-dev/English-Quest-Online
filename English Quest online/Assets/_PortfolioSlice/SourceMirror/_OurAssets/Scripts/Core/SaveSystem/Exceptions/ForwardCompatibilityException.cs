using System;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Thrown when the version stored in the save file is <em>newer</em> than the
    /// version the running code understands.  The caller should present a
    /// "please update your game" message to the player.
    /// </summary>
    public sealed class ForwardCompatibilityException : Exception
    {
        /// <summary>Version number found in the persisted data.</summary>
        public int SavedVersion { get; }

        /// <summary>Highest version this build can handle.</summary>
        public int LatestVersion { get; }

        /// <summary>
        /// Initialises a new <see cref="ForwardCompatibilityException"/>.
        /// </summary>
        /// <param name="savedVersion">Version number read from the save file.</param>
        /// <param name="latestVersion">Highest version the running code supports.</param>
        public ForwardCompatibilityException(int savedVersion, int latestVersion)
            : base(
                $"Save data is version {savedVersion}, but this build only supports up to version " +
                $"{latestVersion}. Please update the game.")
        {
            SavedVersion  = savedVersion;
            LatestVersion = latestVersion;
        }
    }
}
