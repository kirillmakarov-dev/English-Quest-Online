using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Persists save data to <see cref="Application.persistentDataPath"/> as UTF-8
    /// JSON files (one file per key).  Key slashes are replaced with underscores to
    /// produce a flat, filesystem-safe filename.
    /// </summary>
    public sealed class LocalSaveService : ISaveService
    {
        // ── Serialiser settings ───────────────────────────────────────────────────
        private static readonly JsonSerializerSettings s_settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DefaultValueHandling  = DefaultValueHandling.Populate,
            Formatting            = Formatting.None
        };

        private readonly SaveMigrationService _migration;

        /// <summary>
        /// Initialises a <see cref="LocalSaveService"/> with an optional migration
        /// pipeline.  Pass <c>null</c> to disable automatic migration on load.
        /// </summary>
        public LocalSaveService(SaveMigrationService migration)
        {
            _migration = migration;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string GetFilePath(string key)
        {
            string safeKey = key.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
            return Path.Combine(Application.persistentDataPath, safeKey);
        }

        // ── ISaveService ─────────────────────────────────────────────────────────

        /// <summary>
        /// Serialises <paramref name="data"/> into a <see cref="VersionedWrapper{T}"/>
        /// and writes it to disk.  The version is read from a static
        /// <c>CurrentVersion</c> constant on <typeparamref name="T"/> (default 1).
        /// </summary>
        public async UniTask SaveAsync<T>(string key, T data)
        {
            try
            {
                int version = SaveServiceHelpers.GetCurrentVersion<T>();
                var wrapper = VersionedWrapper<T>.Create(data, version);
                string json = JsonConvert.SerializeObject(wrapper, s_settings);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                string path = GetFilePath(key);
                await WriteAllBytesAsync(path, bytes);

                AppLog.Info($"[LocalSaveService] Saved '{key}' (v{version}) → {path}");
            }
            catch (JsonException ex)
            {
                throw new SaveException(SaveErrorCode.SerializationError,
                    $"[LocalSaveService] Failed to serialise '{key}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new SaveException(SaveErrorCode.StorageError,
                    $"[LocalSaveService] Access denied while saving '{key}'.", ex);
            }
            catch (IOException ex)
            {
                throw new SaveException(SaveErrorCode.StorageError,
                    $"[LocalSaveService] I/O failure while saving '{key}'.", ex);
            }
            catch (Exception ex) when (ex is not SaveException)
            {
                throw new SaveException(SaveErrorCode.StorageError,
                    $"[LocalSaveService] Failed to save '{key}'.", ex);
            }
        }

        /// <summary>
        /// Reads the file for <paramref name="key"/>, runs migration if the stored
        /// version is below <paramref name="latestVersion"/>, and writes the migrated
        /// data back to disk for future loads.
        /// Returns <c>default(T)</c> when the file is absent or the stored version
        /// is below <see cref="SaveVersionConfig.MinSupportedVersion"/>.
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key, int latestVersion)
        {
            string path = GetFilePath(key);

            if (!File.Exists(path))
                return default;

            try
            {
                byte[] bytes = await ReadAllBytesAsync(path);
                string json  = Encoding.UTF8.GetString(bytes);

                // First pass: deserialise wrapper with JObject data to inspect version.
                var rawWrapper = JsonConvert.DeserializeObject<VersionedWrapper<JObject>>(json, s_settings);

                if (rawWrapper == null)
                    return default;

                int savedVersion = rawWrapper.Version;

                // Version guards
                if (savedVersion < SaveVersionConfig.MinSupportedVersion)
                {
                    AppLog.Warning(
                        $"[LocalSaveService] Save data for '{key}' is version {savedVersion}, " +
                        $"which is below the minimum supported version " +
                        $"{SaveVersionConfig.MinSupportedVersion}. Discarding.");
                    return default;
                }

                if (savedVersion > latestVersion)
                    throw new ForwardCompatibilityException(savedVersion, latestVersion);

                // Migration
                if (savedVersion < latestVersion && _migration != null)
                {
                    JObject migrated = _migration.MigrateToLatest(rawWrapper.Data, savedVersion, latestVersion, typeof(T));
                    T result = migrated.ToObject<T>(JsonSerializer.CreateDefault(s_settings));

                    // Write migrated data back so disk stays up to date.
                    await SaveAsync(key, result);
                    return result;
                }

                // No migration needed; deserialise directly.
                var wrapper = JsonConvert.DeserializeObject<VersionedWrapper<T>>(json, s_settings);
                return wrapper != null ? wrapper.Data : default;
            }
            catch (JsonException ex)
            {
                throw new SaveException(SaveErrorCode.SerializationError,
                    $"[LocalSaveService] Failed to parse '{key}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new SaveException(SaveErrorCode.StorageError,
                    $"[LocalSaveService] Access denied while loading '{key}'.", ex);
            }
            catch (IOException ex)
            {
                throw new SaveException(SaveErrorCode.StorageError,
                    $"[LocalSaveService] I/O failure while loading '{key}'.", ex);
            }
            catch (Exception ex) when (ex is not SaveException and not ForwardCompatibilityException)
            {
                throw new SaveException(SaveErrorCode.StorageError,
                    $"[LocalSaveService] Failed to load '{key}'.", ex);
            }
        }

        /// <summary>Deletes the local file for <paramref name="key"/>.</summary>
        public UniTask DeleteAsync(string key)
        {
            try
            {
                string path = GetFilePath(key);
                if (File.Exists(path))
                    File.Delete(path);

                return UniTask.CompletedTask;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new SaveException(SaveErrorCode.StorageError,
                    $"[LocalSaveService] Access denied while deleting '{key}'.", ex);
            }
            catch (IOException ex)
            {
                throw new SaveException(SaveErrorCode.StorageError,
                    $"[LocalSaveService] I/O failure while deleting '{key}'.", ex);
            }
            catch (Exception ex)
            {
                throw new SaveException(SaveErrorCode.StorageError,
                    $"[LocalSaveService] Failed to delete '{key}'.", ex);
            }
        }

        // ── Async file helpers ────────────────────────────────────────────────────

        // Use the BCL helpers which guarantee all bytes are written/read in a
        // single atomic operation, avoiding the partial-read/write pitfall of
        // calling Stream.ReadAsync / WriteAsync with a manually-managed buffer.
        // useCurrentSynchronizationContext: true ensures continuations resume on the
        // Unity main thread, keeping Application.persistentDataPath and other
        // main-thread-only Unity APIs accessible after each await.
        private static UniTask WriteAllBytesAsync(string path, byte[] bytes) =>
            File.WriteAllBytesAsync(path, bytes).AsUniTask(useCurrentSynchronizationContext: true);

        private static UniTask<byte[]> ReadAllBytesAsync(string path) =>
            File.ReadAllBytesAsync(path).AsUniTask(useCurrentSynchronizationContext: true);
    }
}
