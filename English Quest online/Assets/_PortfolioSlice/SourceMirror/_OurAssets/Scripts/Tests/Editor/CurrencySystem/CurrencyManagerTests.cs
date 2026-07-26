using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EnglishKingdom.CurrencySystem;
using EnglishKingdom.SaveSystem;
using EnglishKingdom.SaveSystem.Data;
using UnityServiceLocator;

namespace EnglishKingdom.Tests.CurrencySystem
{
    /// <summary>
    /// Integration tests for <see cref="CurrencyManager"/> — covers the full
    /// <see cref="ICurrencyService"/> contract: initialization, Add, TrySpend,
    /// event firing, and save persistence.
    ///
    /// A single <see cref="StubSaveService"/> is registered once on
    /// <see cref="ServiceLocator.Global"/> for the lifetime of the fixture.
    /// This is intentional: <see cref="SaveManager"/> caches the first resolved
    /// <see cref="ISaveService"/> and we need the cached reference to stay valid
    /// across all tests. Each test resets the stub's state via <see cref="StubSaveService.Reset"/>.
    /// </summary>
    [TestFixture]
    public class CurrencyManagerTests
    {
        // ── In-memory stub ────────────────────────────────────────────────────────

        private sealed class StubSaveService : ISaveService
        {
            private readonly Dictionary<string, object> _store = new Dictionary<string, object>();

            public int SaveCallCount;

            public void Reset()
            {
                _store.Clear();
                SaveCallCount = 0;
            }

            public void Seed(string key, object data) => _store[key] = data;

            public UniTask SaveAsync<T>(string key, T data)
            {
                _store[key] = data;
                SaveCallCount++;
                return UniTask.CompletedTask;
            }

            public UniTask<T> LoadAsync<T>(string key, int latestVersion)
            {
                if (_store.TryGetValue(key, out var raw) && raw is T value)
                    return UniTask.FromResult(value);
                return UniTask.FromResult(default(T));
            }

            public UniTask DeleteAsync(string key)
            {
                _store.Remove(key);
                return UniTask.CompletedTask;
            }
        }

        // ── Fixture state ─────────────────────────────────────────────────────────

        // Single instance — SaveManager caches whichever ISaveService it first resolves.
        private readonly StubSaveService _stub = new StubSaveService();

        private GameObject _go;
        private CurrencyManager _manager;
        private GameObject _serviceLocatorGo;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // SaveManager caches ISaveService in a static field. If the editor previously
            // entered Play Mode, that field holds a real HybridSaveService and bypasses
            // our stub entirely. Clear it up-front so the next access re-resolves from
            // the ServiceLocator we are about to inject.
            typeof(SaveManager)
                .GetField("_service", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);

            // ServiceLocator.Global auto-creates a DontDestroyOnLoad object, which is
            // forbidden in Edit Mode tests. We bypass this by creating a plain ServiceLocator
            // component and injecting it into the private 'global' static field via
            // reflection — DontDestroyOnLoad is never called.
            _serviceLocatorGo = new GameObject("ServiceLocator [Global] (Test)");
            var locator = _serviceLocatorGo.AddComponent<UnityServiceLocator.ServiceLocator>();

            typeof(UnityServiceLocator.ServiceLocator)
                .GetField("global", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, locator);

            locator.Register<ISaveService>(_stub);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Clear SaveManager's cached service reference so it doesn't leak to other fixtures.
            typeof(SaveManager)
                .GetField("_service", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);

            // Clear the global ServiceLocator reference.
            typeof(UnityServiceLocator.ServiceLocator)
                .GetField("global", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);

            if (_serviceLocatorGo != null)
                Object.DestroyImmediate(_serviceLocatorGo);
            _serviceLocatorGo = null;
        }

        [SetUp]
        public void SetUp()
        {
            _stub.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go); // triggers OnDestroy → Deregister<ICurrencyService>
            _go = null;
            _manager = null;
        }

        // ── Helper ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates the <see cref="CurrencyManager"/> component and explicitly awaits
        /// <see cref="CurrencyManager.InitializeAsync"/>.
        /// In Edit Mode tests, Unity does NOT invoke Awake when AddComponent is called
        /// (Awake only fires on entering Play Mode), so initialisation must be triggered
        /// manually here.
        /// </summary>
        private async UniTask CreateManagerAsync()
        {
            _go = new GameObject("CurrencyManager_Test");
            _manager = _go.AddComponent<CurrencyManager>();
            await _manager.InitializeAsync();
        }

        // ── InitializeAsync ───────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator InitializeAsync_WithNoSaveData_StartsAtZero() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();

                Assert.AreEqual(0, _manager.Balance);
            });

        [UnityTest]
        public IEnumerator InitializeAsync_WithSavedBalance_RestoresBalance() =>
            UniTask.ToCoroutine(async () =>
            {
                _stub.Seed(CurrencySaveData.Key, new CurrencySaveData { Balance = 250 });

                await CreateManagerAsync();

                Assert.AreEqual(250, _manager.Balance);
            });

        // ── Add ───────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Add_PositiveAmount_IncreasesBalance() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();

                _manager.Add(100);

                Assert.AreEqual(100, _manager.Balance);
            });

        [UnityTest]
        public IEnumerator Add_ZeroAmount_IsIgnored_BalanceUnchanged() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                _manager.Add(50);
                int balanceBefore = _manager.Balance;

                _manager.Add(0);

                Assert.AreEqual(balanceBefore, _manager.Balance);
            });

        [UnityTest]
        public IEnumerator Add_NegativeAmount_IsIgnored_BalanceUnchanged() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                _manager.Add(50);
                int balanceBefore = _manager.Balance;

                _manager.Add(-10);

                Assert.AreEqual(balanceBefore, _manager.Balance);
            });

        [UnityTest]
        public IEnumerator Add_FiresOnBalanceChanged_WithNewBalance() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();

                int receivedBalance = -1;
                _manager.OnBalanceChanged += b => receivedBalance = b;

                _manager.Add(75);

                Assert.AreEqual(75, receivedBalance);
            });

        [UnityTest]
        public IEnumerator Add_PersistsCurrencyViaSaveManager() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                _stub.Reset(); // clear save count from InitializeAsync

                _manager.Add(50);

                Assert.AreEqual(1, _stub.SaveCallCount, "Add must persist the balance.");
            });

        // ── TrySpend ──────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator TrySpend_SufficientFunds_ReturnsTrue() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                _manager.Add(100);

                bool result = _manager.TrySpend(60);

                Assert.IsTrue(result);
            });

        [UnityTest]
        public IEnumerator TrySpend_SufficientFunds_DeductsBalance() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                _manager.Add(100);

                _manager.TrySpend(60);

                Assert.AreEqual(40, _manager.Balance);
            });

        [UnityTest]
        public IEnumerator TrySpend_InsufficientFunds_ReturnsFalse() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                // Balance is 0 — can't spend anything.

                bool result = _manager.TrySpend(1);

                Assert.IsFalse(result);
            });

        [UnityTest]
        public IEnumerator TrySpend_InsufficientFunds_BalanceUnchanged() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                _manager.Add(30);

                _manager.TrySpend(100);

                Assert.AreEqual(30, _manager.Balance);
            });

        [UnityTest]
        public IEnumerator TrySpend_SuccessfulSpend_FiresOnBalanceChanged() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                _manager.Add(100);

                int receivedBalance = -1;
                _manager.OnBalanceChanged += b => receivedBalance = b;

                _manager.TrySpend(40);

                Assert.AreEqual(60, receivedBalance);
            });

        [UnityTest]
        public IEnumerator TrySpend_InsufficientFunds_DoesNotFireOnBalanceChanged() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                bool eventFired = false;
                _manager.OnBalanceChanged += _ => eventFired = true;

                _manager.TrySpend(999); // definitely more than the 0 balance

                Assert.IsFalse(eventFired, "OnBalanceChanged must not fire on a failed spend.");
            });

        [UnityTest]
        public IEnumerator TrySpend_SuccessfulSpend_PersistsCurrency() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                _manager.Add(100);
                _stub.Reset(); // clear save count from Add

                _manager.TrySpend(50);

                Assert.AreEqual(1, _stub.SaveCallCount, "TrySpend must persist the balance.");
            });

        [UnityTest]
        public IEnumerator TrySpend_FailedSpend_DoesNotPersist() =>
            UniTask.ToCoroutine(async () =>
            {
                await CreateManagerAsync();
                _stub.Reset(); // baseline — 0 saves so far

                _manager.TrySpend(999); // will fail — balance is 0

                Assert.AreEqual(0, _stub.SaveCallCount, "A failed TrySpend must not trigger a save.");
            });
    }
}
