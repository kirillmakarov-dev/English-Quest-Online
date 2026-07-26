using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using UnityEngine;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Persists save data to Unity Cloud Save (Player scope).
    /// Requires Unity Gaming Services to be initialised and the local player to be
    /// authenticated before any call is made.
    /// Each key is stored as a JSON string that represents a
    /// <see cref="VersionedWrapper{T}"/> so schema versions survive round-trips.
    /// </summary>
    public sealed class CloudPlayerSaveService : ISaveService
    {
        // ── Serialiser settings ───────────────────────────────────────────────────
        private static readonly JsonSerializerSettings s_settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DefaultValueHandling  = DefaultValueHandling.Populate
        };

        private readonly SaveMigrationService _migration;

        // ── Constructor ───────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises a <see cref="CloudPlayerSaveService"/> with the shared
        /// migration pipeline.
        /// </summary>
        public CloudPlayerSaveService(SaveMigrationService migration)
        {
            _migration = migration;
        }

        // ── ISaveService ─────────────────────────────────────────────────────────

        /// <summary>
        /// Wraps <paramref name="data"/> in a <see cref="VersionedWrapper{T}"/> and
        /// uploads it to Unity Cloud Save under <paramref name="key"/>.
        /// </summary>
        public async UniTask SaveAsync<T>(string key, T data)
        {
            try
            {
                int version = SaveServiceHelpers.GetCurrentVersion<T>();
                var wrapper = VersionedWrapper<T>.Create(data, version);
                string json = JsonConvert.SerializeObject(wrapper, s_settings);

                var payload = new Dictionary<string, object> { { key, json } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(payload).AsUniTask();

                AppLog.Info($"[CloudPlayerSaveService] Saved '{key}' (v{version}).");
            }
            catch (JsonException ex)
            {
                throw new SaveException(SaveErrorCode.SerializationError,
                    $"[CloudPlayerSaveService] Failed to serialise '{key}'.", ex);
            }
            catch (CloudSaveException ex) when (ex.Reason == CloudSaveExceptionReason.Unauthorized ||
                                                 ex.Reason == CloudSaveExceptionReason.AccessTokenMissing)
            {
                throw new SaveException(SaveErrorCode.AuthError,
                    $"[CloudPlayerSaveService] Auth error saving '{key}'.", ex);
            }
            catch (CloudSaveRateLimitedException ex)
            {
                throw new SaveException(SaveErrorCode.QuotaExceeded,
                    $"[CloudPlayerSaveService] Rate limited saving '{key}'.", ex);
            }
            catch (CloudSaveValidationException ex)
            {
                throw new SaveException(SaveErrorCode.NetworkError,
                    $"[CloudPlayerSaveService] Validation error saving '{key}' " +
                    $"({FormatValidationDetails(ex)}).", ex);
            }
            catch (CloudSaveException ex)
            {
                throw new SaveException(SaveErrorCode.NetworkError,
                    $"[CloudPlayerSaveService] Cloud error saving '{key}'.", ex);
            }
            catch (Exception ex) when (ex is not SaveException)
            {
                throw new SaveException(SaveErrorCode.NetworkError,
                    $"[CloudPlayerSaveService] Unexpected error saving '{key}'.", ex);
            }
        }

        /// <summary>
        /// Downloads the value stored under <paramref name="key"/>, runs the migration
        /// pipeline if the stored version is below <paramref name="latestVersion"/>,
        /// and immediately writes the migrated data back to the cloud so it is always
        /// up to date.
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key, int latestVersion)
        {
            try
            {
                var keys   = new HashSet<string> { key };
                var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys).AsUniTask();

                if (!result.TryGetValue(key, out var item))
                    return default;

                string json = item.Value.GetAs<string>();

                if (string.IsNullOrEmpty(json))
                    return default;

                // First pass: inspect the version without committing to T yet.
                var rawWrapper = JsonConvert.DeserializeObject<VersionedWrapper<JObject>>(json, s_settings);

                if (rawWrapper == null)
                    return default;

                int savedVersion = rawWrapper.Version;

                // Version guards
                if (savedVersion < SaveVersionConfig.MinSupportedVersion)
                {
                    AppLog.Warning(
                        $"[CloudPlayerSaveService] Key '{key}' is version {savedVersion}, " +
                        $"below minimum supported version {SaveVersionConfig.MinSupportedVersion}. " +
                        $"Discarding.");
                    return default;
                }

                if (savedVersion > latestVersion)
                    throw new ForwardCompatibilityException(savedVersion, latestVersion);

                // Migration
                if (savedVersion < latestVersion && _migration != null)
                {
                    JObject migrated = _migration.MigrateToLatest(rawWrapper.Data, savedVersion, latestVersion, typeof(T));
                    T migratedData   = migrated.ToObject<T>(JsonSerializer.CreateDefault(s_settings));

                    // Write the migrated data back immediately so the cloud stays current.
                    await SaveAsync(key, migratedData);
                    return migratedData;
                }

                // No migration needed; deserialise directly.
                var wrapper = JsonConvert.DeserializeObject<VersionedWrapper<T>>(json, s_settings);
                return wrapper != null ? wrapper.Data : default;
            }
            catch (SaveException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                throw new SaveException(SaveErrorCode.SerializationError,
                    $"[CloudPlayerSaveService] Failed to parse '{key}'.", ex);
            }
            catch (CloudSaveException ex) when (ex.Reason == CloudSaveExceptionReason.Unauthorized ||
                                                 ex.Reason == CloudSaveExceptionReason.AccessTokenMissing)
            {
                throw new SaveException(SaveErrorCode.AuthError,
                    $"[CloudPlayerSaveService] Auth error loading '{key}'.", ex);
            }
            catch (CloudSaveRateLimitedException ex)
            {
                throw new SaveException(SaveErrorCode.QuotaExceeded,
                    $"[CloudPlayerSaveService] Rate limited loading '{key}'.", ex);
            }
            catch (CloudSaveValidationException ex)
            {
                // Unity Cloud Save returns HTTP 400 when a requested key is not stored
                // server-side yet (common for new save domains on first boot).
                AppLog.Info(
                    $"[CloudPlayerSaveService] No cloud data for '{key}' " +
                    $"({FormatValidationDetails(ex)}).");
                return default;
            }
            catch (CloudSaveException ex)
            {
                throw new SaveException(SaveErrorCode.NetworkError,
                    $"[CloudPlayerSaveService] Cloud error loading '{key}'.", ex);
            }
            catch (Exception ex) when (ex is not SaveException and not ForwardCompatibilityException)
            {
                throw new SaveException(SaveErrorCode.NetworkError,
                    $"[CloudPlayerSaveService] Unexpected error loading '{key}'.", ex);
            }
        }

        /// <summary>
        /// Deletes the value stored under <paramref name="key"/> from Unity Cloud Save.
        /// </summary>
        public async UniTask DeleteAsync(string key)
        {
            try
            {
                await CloudSaveService.Instance.Data.Player.DeleteAsync(key).AsUniTask();
                AppLog.Info($"[CloudPlayerSaveService] Deleted '{key}'.");
            }
            catch (CloudSaveException ex) when (ex.Reason == CloudSaveExceptionReason.Unauthorized ||
                                                 ex.Reason == CloudSaveExceptionReason.AccessTokenMissing)
            {
                throw new SaveException(SaveErrorCode.AuthError,
                    $"[CloudPlayerSaveService] Auth error deleting '{key}'.", ex);
            }
            catch (CloudSaveException ex)
            {
                throw new SaveException(SaveErrorCode.NetworkError,
                    $"[CloudPlayerSaveService] Cloud error deleting '{key}'.", ex);
            }
            catch (Exception ex) when (ex is not SaveException)
            {
                throw new SaveException(SaveErrorCode.NetworkError,
                    $"[CloudPlayerSaveService] Unexpected error deleting '{key}'.", ex);
            }
        }

        private static string FormatValidationDetails(CloudSaveValidationException ex)
        {
            if (ex?.Details == null || ex.Details.Count == 0)
                return "no validation details";

            var builder = new StringBuilder();
            for (int i = 0; i < ex.Details.Count; i++)
            {
                CloudSaveValidationErrorDetail detail = ex.Details[i];
                if (i > 0)
                    builder.Append("; ");

                builder.Append(detail.Field);
                if (!string.IsNullOrEmpty(detail.Key))
                    builder.Append(" key='").Append(detail.Key).Append('\'');

                if (detail.Messages != null && detail.Messages.Count > 0)
                    builder.Append(": ").Append(detail.Messages[0]);
            }

            return builder.ToString();
        }
    }
}
