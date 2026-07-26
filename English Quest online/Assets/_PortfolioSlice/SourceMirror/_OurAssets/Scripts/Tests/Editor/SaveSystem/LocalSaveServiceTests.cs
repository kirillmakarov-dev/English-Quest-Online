using System;
using System.Collections;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EnglishKingdom.SaveSystem;
using EnglishKingdom.SaveSystem.Data;
using Newtonsoft.Json.Linq;

namespace EnglishKingdom.Tests.SaveSystem
{
    /// <summary>
    /// Editor integration tests for <see cref="LocalSaveService"/>.
    /// Each test uses a uniquely-named key so files never collide across parallel
    /// runs. The file written per test is deleted in TearDown.
    /// </summary>
    [TestFixture]
    public class LocalSaveServiceTests
    {
        private LocalSaveService _service;
        private string _testKey;

        [SetUp]
        public void SetUp()
        {
            _service = new LocalSaveService(migration: null);
            // Unique key per test – avoids using colons which are invalid in Windows filenames.
            _testKey = "test_local_" + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            _service.DeleteAsync(_testKey).GetAwaiter().GetResult();
        }

        // ── Round-trip ────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SaveAsync_ThenLoadAsync_ReturnsEquivalentData() =>
            UniTask.ToCoroutine(async () =>
            {
                var data = new SettingsSaveData
                {
                    MasterVolume = 0.42f,
                    LanguageCode = "he",
                    QualityLevel = 1
                };

                await _service.SaveAsync(_testKey, data);

                var loaded = await _service.LoadAsync<SettingsSaveData>(_testKey, SettingsSaveData.CurrentVersion);

                Assert.IsNotNull(loaded);
                Assert.AreEqual(0.42f, loaded.MasterVolume, delta: 1e-5f);
                Assert.AreEqual("he", loaded.LanguageCode);
                Assert.AreEqual(1, loaded.QualityLevel);
            });

        [UnityTest]
        public IEnumerator SaveAsync_OverwritesPreviousValue() =>
            UniTask.ToCoroutine(async () =>
            {
                await _service.SaveAsync(_testKey, new CoreSaveData { Level = 1 });
                await _service.SaveAsync(_testKey, new CoreSaveData { Level = 42 });

                var loaded = await _service.LoadAsync<CoreSaveData>(_testKey, CoreSaveData.CurrentVersion);

                Assert.AreEqual(42, loaded.Level);
            });

        // ── Missing file ──────────────────────────────────────────────────────────

        [Test]
        public void LoadAsync_MissingFile_ReturnsNull()
        {
            var result = _service.LoadAsync<SettingsSaveData>(
                    "nonexistent_save_key_" + Guid.NewGuid().ToString("N"), latestVersion: 1)
                .GetAwaiter().GetResult();

            Assert.IsNull(result);
        }

        // ── Delete ────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator DeleteAsync_RemovesSavedFile() =>
            UniTask.ToCoroutine(async () =>
            {
                await _service.SaveAsync(_testKey, new CoreSaveData());
                await _service.DeleteAsync(_testKey);

                var result = await _service.LoadAsync<CoreSaveData>(_testKey, CoreSaveData.CurrentVersion);

                Assert.IsNull(result);
            });

        [Test]
        public void DeleteAsync_NonExistentKey_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                _service.DeleteAsync("nonexistent_delete_key_" + Guid.NewGuid().ToString("N"))
                    .GetAwaiter().GetResult());
        }

        // ── Version guards ────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator LoadAsync_SavedVersionExceedsLatest_ThrowsForwardCompatibilityException() =>
            UniTask.ToCoroutine(async () =>
            {
                WriteRawJson(_testKey, version: 99, dataJson: "{}");

                try
                {
                    await _service.LoadAsync<SettingsSaveData>(_testKey, latestVersion: 1);
                    Assert.Fail("Expected ForwardCompatibilityException was not thrown.");
                }
                catch (ForwardCompatibilityException)
                {
                    // Expected — test passes.
                }
            });

        [UnityTest]
        public IEnumerator LoadAsync_SavedVersionBelowMinSupported_ReturnsNull() =>
            UniTask.ToCoroutine(async () =>
            {
                // SaveVersionConfig.MinSupportedVersion == 1; write version 0.
                WriteRawJson(_testKey, version: 0, dataJson: "{}");

                var result = await _service.LoadAsync<SettingsSaveData>(_testKey, latestVersion: 1);

                Assert.IsNull(result);
            });

        // ── Migration ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator LoadAsync_OlderVersion_WithMigrator_MigratesDataAndReturnsResult() =>
            UniTask.ToCoroutine(async () =>
            {
                WriteRawJson(_testKey, version: 1, dataJson: "{\"masterVolume\":0.3}");

                var migration = new SaveMigrationService();
                migration.Register(new SettingsV1ToV2Migrator());
                var serviceWithMigration = new LocalSaveService(migration);

                // Load with latestVersion = 2 forces migration.
                var result = await serviceWithMigration
                    .LoadAsync<SettingsSaveDataV2>(_testKey, latestVersion: 2);

                Assert.IsNotNull(result);
                Assert.IsTrue(result.WasMigrated);
            });

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes a <see cref="VersionedWrapper{T}"/>-shaped JSON file directly to
        /// disk, bypassing <c>LocalSaveService.SaveAsync</c> so any version can be
        /// injected without relying on the data class's own <c>CurrentVersion</c>.
        /// </summary>
        private static void WriteRawJson(string key, int version, string dataJson)
        {
            string safeKey  = key.Replace('/', '_').Replace('\\', '_');
            string path     = Path.Combine(Application.persistentDataPath, safeKey);
            string envelope = $"{{\"version\":{version},\"savedAt\":0,\"data\":{dataJson}}}";
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes(envelope));
        }

        // ── Stub types used only in migration tests ───────────────────────────────

        private sealed class SettingsSaveDataV2
        {
            [JsonProperty("masterVolume")] public float MasterVolume { get; set; }
            [JsonProperty("wasMigrated")]  public bool  WasMigrated  { get; set; }
        }

        private sealed class SettingsV1ToV2Migrator : ISaveMigrator
        {
            public Type    DomainType  => typeof(SettingsSaveDataV2);
            public int     FromVersion => 1;

            public JObject Migrate(JObject data)
            {
                data["wasMigrated"] = true;
                return data;
            }
        }
    }
}
