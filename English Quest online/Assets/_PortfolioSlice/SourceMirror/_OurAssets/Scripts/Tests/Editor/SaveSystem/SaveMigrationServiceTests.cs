using System;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using EnglishKingdom.SaveSystem;
using EnglishKingdom.SaveSystem.Data;

namespace EnglishKingdom.Tests.SaveSystem
{
    [TestFixture]
    public class SaveMigrationServiceTests
    {
        private SaveMigrationService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new SaveMigrationService();
        }

        // ── Test double ───────────────────────────────────────────────────────────

        private sealed class SpyMigrator : ISaveMigrator
        {
            private readonly string _fieldNameToAdd;

            public SpyMigrator(Type domainType, int fromVersion, string fieldNameToAdd = "migrated")
            {
                DomainType       = domainType;
                FromVersion      = fromVersion;
                _fieldNameToAdd  = fieldNameToAdd;
            }

            public Type DomainType  { get; }
            public int  FromVersion { get; }
            public bool WasCalled   { get; private set; }

            public JObject Migrate(JObject data)
            {
                WasCalled = true;
                data[_fieldNameToAdd] = true;
                return data;
            }
        }

        // ── Register ──────────────────────────────────────────────────────────────

        [Test]
        public void Register_NullMigrator_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.Register(null));
        }

        [Test]
        public void Register_DuplicateKey_OverwritesPrevious()
        {
            var first  = new SpyMigrator(typeof(CoreSaveData), fromVersion: 1, "first");
            var second = new SpyMigrator(typeof(CoreSaveData), fromVersion: 1, "second");
            _service.Register(first);
            _service.Register(second);

            var raw = new JObject();
            _service.MigrateToLatest(raw, fromVersion: 1, toVersion: 2, domainType: typeof(CoreSaveData));

            Assert.IsTrue(second.WasCalled,  "Second (overwriting) migrator must be called.");
            Assert.IsFalse(first.WasCalled,  "First (overwritten) migrator must NOT be called.");
        }

        // ── MigrateToLatest ───────────────────────────────────────────────────────

        [Test]
        public void MigrateToLatest_SameVersion_ReturnsOriginalObjectUnchanged()
        {
            var raw    = new JObject { ["field"] = "value" };
            var result = _service.MigrateToLatest(raw, fromVersion: 2, toVersion: 2, domainType: typeof(CoreSaveData));

            Assert.AreSame(raw, result);
        }

        [Test]
        public void MigrateToLatest_MissingMigrator_ThrowsSaveException()
        {
            var raw = new JObject();
            var ex  = Assert.Throws<SaveException>(() =>
                _service.MigrateToLatest(raw, fromVersion: 1, toVersion: 2, domainType: typeof(CoreSaveData)));

            Assert.AreEqual(SaveErrorCode.MigrationFailed, ex.ErrorCode);
        }

        [Test]
        public void MigrateToLatest_SingleStep_CallsMigratorOnce()
        {
            var migrator = new SpyMigrator(typeof(CoreSaveData), fromVersion: 1);
            _service.Register(migrator);

            _service.MigrateToLatest(new JObject(), fromVersion: 1, toVersion: 2, domainType: typeof(CoreSaveData));

            Assert.IsTrue(migrator.WasCalled);
        }

        [Test]
        public void MigrateToLatest_MultiStep_CallsAllMigratorsInOrder()
        {
            var v1ToV2 = new SpyMigrator(typeof(CoreSaveData), fromVersion: 1, "v1v2");
            var v2ToV3 = new SpyMigrator(typeof(CoreSaveData), fromVersion: 2, "v2v3");
            _service.Register(v1ToV2);
            _service.Register(v2ToV3);

            var result = _service.MigrateToLatest(new JObject(), fromVersion: 1, toVersion: 3, domainType: typeof(CoreSaveData));

            Assert.IsTrue(v1ToV2.WasCalled);
            Assert.IsTrue(v2ToV3.WasCalled);
            Assert.IsTrue(result["v1v2"].Value<bool>());
            Assert.IsTrue(result["v2v3"].Value<bool>());
        }

        [Test]
        public void MigrateToLatest_GapInChain_ThrowsSaveException()
        {
            _service.Register(new SpyMigrator(typeof(CoreSaveData), fromVersion: 1));
            // No v2 → v3 migrator registered

            var ex = Assert.Throws<SaveException>(() =>
                _service.MigrateToLatest(new JObject(), fromVersion: 1, toVersion: 3, domainType: typeof(CoreSaveData)));

            Assert.AreEqual(SaveErrorCode.MigrationFailed, ex.ErrorCode);
        }

        [Test]
        public void MigrateToLatest_DifferentDomainTypes_RouteToCorrectMigrator()
        {
            var coreMigrator     = new SpyMigrator(typeof(CoreSaveData),     fromVersion: 1);
            var progressMigrator = new SpyMigrator(typeof(ProgressSaveData), fromVersion: 1);
            _service.Register(coreMigrator);
            _service.Register(progressMigrator);

            _service.MigrateToLatest(new JObject(), fromVersion: 1, toVersion: 2, domainType: typeof(ProgressSaveData));

            Assert.IsTrue(progressMigrator.WasCalled);
            Assert.IsFalse(coreMigrator.WasCalled);
        }
    }
}
