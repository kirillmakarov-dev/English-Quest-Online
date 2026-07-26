using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// The primary <see cref="ISaveService"/> implementation.
    /// Writes to <em>both</em> a local and a cloud backend; reads from the cloud
    /// with an automatic local fallback, giving offline-safe persistence with
    /// automatic cloud sync.
    /// </summary>
    public sealed class HybridSaveService : ISaveService
    {
        private readonly ISaveService _local;
        private readonly ISaveService _cloud;

        /// <summary>
        /// Initialises a <see cref="HybridSaveService"/>.
        /// </summary>
        /// <param name="local">Offline-capable local backend.</param>
        /// <param name="cloud">Cloud backend (Unity Cloud Save).</param>
        public HybridSaveService(ISaveService local, ISaveService cloud)
        {
            _local = local ?? throw new ArgumentNullException(nameof(local));
            _cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));
        }

        // ── ISaveService ─────────────────────────────────────────────────────────

        /// <summary>
        /// Writes <paramref name="data"/> to local storage first (offline-safe),
        /// then propagates the same data to the cloud backend.
        /// Cloud failures are logged but not re-thrown so gameplay can continue
        /// offline with local data as the source of truth.
        /// </summary>
        public async UniTask SaveAsync<T>(string key, T data)
        {
            // Local is always written first so the player never loses data on
            // a network failure during the cloud write.
            await _local.SaveAsync(key, data);

            try
            {
                await _cloud.SaveAsync(key, data);
            }
            catch (Exception ex)
            {
                // The local write succeeded; log the cloud failure but do not
                // surface it to the caller — offline use-case is intentional.
                AppLog.Warning(
                    $"[HybridSaveService] Cloud save for '{key}' failed (local is up to date). " +
                    $"It will sync on the next online session.\n{ex}");
            }
        }

        /// <summary>
        /// Tries the cloud backend first.  Falls back to local storage on any
        /// exception so the player can still load in offline scenarios.
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key, int latestVersion)
        {
            try
            {
                T cloudResult = await _cloud.LoadAsync<T>(key, latestVersion);
                if (cloudResult != null)
                    return cloudResult;

                // Cloud has no record for this key yet (e.g. first boot after
                // local-only sessions). Fall through to the local backend.
                AppLog.Info($"[HybridSaveService] Cloud has no data for '{key}'; trying local.");
            }
            catch (ForwardCompatibilityException)
            {
                // Re-throw forward-compatibility errors — these must be surfaced
                // to the player regardless of the active backend.
                throw;
            }
            catch (Exception cloudEx)
            {
                AppLog.Warning(
                    $"[HybridSaveService] Cloud load for '{key}' failed; falling back to local.\n{cloudEx}");
            }

            return await _local.LoadAsync<T>(key, latestVersion);
        }

        /// <summary>
        /// Deletes the value under <paramref name="key"/> from both backends
        /// concurrently.  If either deletion fails the exception is propagated
        /// after both tasks have settled.
        /// </summary>
        public async UniTask DeleteAsync(string key)
        {
            await UniTask.WhenAll(
                _local.DeleteAsync(key),
                _cloud.DeleteAsync(key));
        }
    }
}
