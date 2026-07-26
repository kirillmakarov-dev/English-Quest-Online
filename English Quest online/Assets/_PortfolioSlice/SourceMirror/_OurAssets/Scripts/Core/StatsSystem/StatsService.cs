using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EnglishKingdom.SaveSystem;
using EnglishKingdom.SaveSystem.Data;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.StatsSystem
{
    /// <summary>
    /// Tracks generic gameplay statistics defined by <see cref="StatDefinitionSO"/> assets.
    /// Self-registers as <see cref="IStatsService"/> on the global ServiceLocator and persists
    /// every mutation through <see cref="SaveManager"/>, mirroring <c>CurrencyManager</c>.
    ///
    /// Change notifications are scoped per stat (see <see cref="Subscribe"/>) so a consumer such
    /// as the future achievement system only hears about the stats it registered for, not every
    /// stat in the game.
    /// </summary>
    public class StatsService : MonoBehaviour, IStatsService
    {
        [SerializeField] private StatCatalogSO _catalog;

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Debug-only: fires for every stat change regardless of subscriptions. Intended for a
        /// dev overlay. Deliberately absent from <see cref="IStatsService"/> — gameplay and
        /// achievement code must use <see cref="Subscribe"/> instead.
        /// </summary>
        public event Action<StatDefinitionSO, int, int> OnAnyStatChanged;

        private readonly Dictionary<string, int> _values = new();
        private readonly Dictionary<string, List<Action<int, int>>> _subscribers = new();

        private void Awake()
        {
            ServiceLocator.Global.Register<IStatsService>(this);
            ValidateCatalog();
        }

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private void OnDestroy()
        {
            ServiceLocator.DeregisterGlobal<IStatsService>();
        }

        // ── IStatsService ─────────────────────────────────────────────────────────

        public int Get(StatDefinitionSO stat) => stat == null ? 0 : Get(stat.StatId);

        public int Get(string statId)
        {
            if (string.IsNullOrEmpty(statId))
                return 0;

            return _values.TryGetValue(statId, out int value) ? value : 0;
        }

        public void Apply(StatDefinitionSO stat, int deltaOrValue)
        {
            if (stat == null || string.IsNullOrEmpty(stat.StatId))
            {
                AppLog.Warning("[StatsService] Apply called with a null stat or missing StatId. Ignored.");
                return;
            }

            if (!IsInitialized)
                AppLog.Warning($"[StatsService] Apply('{stat.StatId}') called before initialization finished. The value may be lost once the load completes.");

            int oldValue = Get(stat.StatId);
            int newValue = Aggregate(stat.Aggregation, oldValue, deltaOrValue);
            if (newValue == oldValue)
                return;

            _values[stat.StatId] = newValue;
            PersistAsync().Forget();

            NotifySubscribers(stat.StatId, oldValue, newValue);
            OnAnyStatChanged?.Invoke(stat, oldValue, newValue);
        }

        public void Subscribe(StatDefinitionSO stat, Action<int, int> onChanged)
        {
            if (stat == null || onChanged == null || string.IsNullOrEmpty(stat.StatId))
                return;

            if (!_subscribers.TryGetValue(stat.StatId, out List<Action<int, int>> callbacks))
            {
                callbacks = new List<Action<int, int>>();
                _subscribers[stat.StatId] = callbacks;
            }

            callbacks.Add(onChanged);
        }

        public void Unsubscribe(StatDefinitionSO stat, Action<int, int> onChanged)
        {
            if (stat == null || onChanged == null)
                return;

            if (_subscribers.TryGetValue(stat.StatId, out List<Action<int, int>> callbacks))
                callbacks.Remove(onChanged);
        }

        // ── Aggregation ───────────────────────────────────────────────────────────

        private static int Aggregate(StatAggregation aggregation, int currentValue, int deltaOrValue)
        {
            return aggregation switch
            {
                StatAggregation.Sum => currentValue + deltaOrValue,
                StatAggregation.Max => Mathf.Max(currentValue, deltaOrValue),
                StatAggregation.Set => deltaOrValue,
                _ => currentValue
            };
        }

        // ── Notification ──────────────────────────────────────────────────────────

        private void NotifySubscribers(string statId, int oldValue, int newValue)
        {
            if (!_subscribers.TryGetValue(statId, out List<Action<int, int>> callbacks) || callbacks.Count == 0)
                return;

            // Snapshot in case a handler subscribes/unsubscribes during notification.
            foreach (Action<int, int> callback in callbacks.ToArray())
                callback?.Invoke(oldValue, newValue);
        }

        // ── Initialization ────────────────────────────────────────────────────────

        private async UniTaskVoid InitializeAsync()
        {
            await UniTask.WaitUntil(
                () => ServiceLocator.Global != null && ServiceLocator.Global.TryGet<ISaveService>(out _),
                cancellationToken: destroyCancellationToken);

            StatsSaveData data = await SaveManager.LoadStatsAsync();
            foreach (KeyValuePair<string, int> pair in data.Values)
                _values[pair.Key] = pair.Value;

            IsInitialized = true;
            AppLog.Info($"[StatsService] Initialized with {_values.Count} tracked stat(s).");
        }

        private void ValidateCatalog()
        {
            if (_catalog == null)
            {
                AppLog.Warning("[StatsService] No StatCatalogSO assigned. Stats will still work, but editor-time validation is skipped.");
                return;
            }

            var seenIds = new HashSet<string>();
            foreach (StatDefinitionSO stat in _catalog.Stats)
            {
                if (stat == null || string.IsNullOrEmpty(stat.StatId))
                {
                    AppLog.Warning("[StatsService] Catalog contains a null entry or a stat with an empty StatId.");
                    continue;
                }

                if (!seenIds.Add(stat.StatId))
                    AppLog.Warning($"[StatsService] Duplicate stat id '{stat.StatId}' in catalog.");
            }
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private async UniTaskVoid PersistAsync()
        {
            await SaveManager.SaveStatsAsync(new StatsSaveData { Values = new Dictionary<string, int>(_values) });
        }
    }
}
