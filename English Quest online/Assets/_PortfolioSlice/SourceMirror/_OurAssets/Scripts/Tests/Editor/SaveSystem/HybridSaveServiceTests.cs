using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using EnglishKingdom.SaveSystem;
using EnglishKingdom.SaveSystem.Data;

namespace EnglishKingdom.Tests.SaveSystem
{
    [TestFixture]
    public class HybridSaveServiceTests
    {
        // ── In-memory stub ────────────────────────────────────────────────────────

        private sealed class StubSaveService : ISaveService
        {
            private readonly Dictionary<string, object> _store = new Dictionary<string, object>();

            public int  SaveCallCount;
            public bool ShouldThrowOnSave;
            public bool ShouldThrowOnLoad;
            public Exception                     ExceptionToThrow = new InvalidOperationException("stub failure");
            public ForwardCompatibilityException ForwardException;

            public UniTask SaveAsync<T>(string key, T data)
            {
                if (ShouldThrowOnSave) throw ExceptionToThrow;
                SaveCallCount++;
                _store[key] = data;
                return UniTask.CompletedTask;
            }

            public UniTask<T> LoadAsync<T>(string key, int latestVersion)
            {
                if (ForwardException != null) throw ForwardException;
                if (ShouldThrowOnLoad) throw ExceptionToThrow;

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

        // ── Constructor guards ────────────────────────────────────────────────────

        [Test]
        public void Constructor_NullLocal_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HybridSaveService(null, new StubSaveService()));
        }

        [Test]
        public void Constructor_NullCloud_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HybridSaveService(new StubSaveService(), null));
        }

        // ── SaveAsync ─────────────────────────────────────────────────────────────

        [Test]
        public void SaveAsync_WritesToBothBackends()
        {
            var local  = new StubSaveService();
            var cloud  = new StubSaveService();
            var hybrid = new HybridSaveService(local, cloud);

            hybrid.SaveAsync("key", new CoreSaveData()).GetAwaiter().GetResult();

            Assert.AreEqual(1, local.SaveCallCount, "Local must be written.");
            Assert.AreEqual(1, cloud.SaveCallCount, "Cloud must be written.");
        }

        [Test]
        public void SaveAsync_CloudFails_LocalStillSaved_ExceptionNotPropagated()
        {
            var local  = new StubSaveService();
            var cloud  = new StubSaveService { ShouldThrowOnSave = true };
            var hybrid = new HybridSaveService(local, cloud);

            Assert.DoesNotThrow(() =>
                hybrid.SaveAsync("key", new CoreSaveData()).GetAwaiter().GetResult());

            Assert.AreEqual(1, local.SaveCallCount, "Local must be written despite cloud failure.");
        }

        // ── LoadAsync ─────────────────────────────────────────────────────────────

        [Test]
        public void LoadAsync_CloudHasData_ReturnsCloudData()
        {
            var cloudData = new CoreSaveData { Level = 10 };
            var local     = new StubSaveService();
            var cloud     = new StubSaveService();
            cloud.SaveAsync("key", cloudData).GetAwaiter().GetResult();

            var hybrid = new HybridSaveService(local, cloud);
            var result = hybrid.LoadAsync<CoreSaveData>("key", CoreSaveData.CurrentVersion)
                .GetAwaiter().GetResult();

            Assert.AreEqual(10, result.Level);
        }

        [Test]
        public void LoadAsync_CloudThrows_FallsBackToLocal()
        {
            var localData = new CoreSaveData { Level = 5 };
            var local     = new StubSaveService();
            local.SaveAsync("key", localData).GetAwaiter().GetResult();

            var cloud  = new StubSaveService { ShouldThrowOnLoad = true };
            var hybrid = new HybridSaveService(local, cloud);
            var result = hybrid.LoadAsync<CoreSaveData>("key", CoreSaveData.CurrentVersion)
                .GetAwaiter().GetResult();

            Assert.AreEqual(5, result.Level);
        }

        [Test]
        public void LoadAsync_CloudReturnsNull_FallsBackToLocal()
        {
            var localData = new CoreSaveData { Level = 3 };
            var local     = new StubSaveService();
            local.SaveAsync("key", localData).GetAwaiter().GetResult();

            var cloud  = new StubSaveService(); // nothing stored → returns null
            var hybrid = new HybridSaveService(local, cloud);
            var result = hybrid.LoadAsync<CoreSaveData>("key", CoreSaveData.CurrentVersion)
                .GetAwaiter().GetResult();

            Assert.AreEqual(3, result.Level);
        }

        [Test]
        public void LoadAsync_CloudThrowsForwardCompatibility_RethrowsImmediately()
        {
            var local = new StubSaveService();
            var cloud = new StubSaveService
            {
                ForwardException = new ForwardCompatibilityException(savedVersion: 99, latestVersion: 1)
            };
            var hybrid = new HybridSaveService(local, cloud);

            Assert.Throws<ForwardCompatibilityException>(() =>
                hybrid.LoadAsync<CoreSaveData>("key", latestVersion: 1).GetAwaiter().GetResult());
        }

        // ── DeleteAsync ───────────────────────────────────────────────────────────

        [Test]
        public void DeleteAsync_DeletesFromBothBackends()
        {
            var local = new StubSaveService();
            var cloud = new StubSaveService();
            local.SaveAsync("key", new CoreSaveData()).GetAwaiter().GetResult();
            cloud.SaveAsync("key", new CoreSaveData()).GetAwaiter().GetResult();

            var hybrid = new HybridSaveService(local, cloud);
            hybrid.DeleteAsync("key").GetAwaiter().GetResult();

            Assert.IsNull(local.LoadAsync<CoreSaveData>("key", 1).GetAwaiter().GetResult());
            Assert.IsNull(cloud.LoadAsync<CoreSaveData>("key", 1).GetAwaiter().GetResult());
        }
    }
}
