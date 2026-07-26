using Cysharp.Threading.Tasks;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Contract for all save/load backends (local, cloud, hybrid).
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// Serialises <paramref name="data"/> and persists it under <paramref name="key"/>.
        /// The wrapper version is derived automatically from a <c>CurrentVersion</c> constant
        /// declared on <typeparamref name="T"/> (defaults to 1 if absent).
        /// </summary>
        UniTask SaveAsync<T>(string key, T data);

        /// <summary>
        /// Loads and deserialises the value stored under <paramref name="key"/>.
        /// Runs the migration pipeline when the stored version is older than
        /// <paramref name="latestVersion"/>.
        /// Returns <c>default(T)</c> and logs a warning when the stored version
        /// is below <see cref="SaveVersionConfig.MinSupportedVersion"/>.
        /// Throws <see cref="ForwardCompatibilityException"/> when the stored
        /// version exceeds <paramref name="latestVersion"/>.
        /// </summary>
        UniTask<T> LoadAsync<T>(string key, int latestVersion);

        /// <summary>
        /// Permanently removes the data stored under <paramref name="key"/>.
        /// </summary>
        UniTask DeleteAsync(string key);
    }
}
